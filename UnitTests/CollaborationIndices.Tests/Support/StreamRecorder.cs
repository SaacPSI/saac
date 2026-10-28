// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.Psi;

namespace CollaborationIndices.Tests
{
    /// <summary>
    /// Records the messages of several streams as text, so that two runs can be compared and
    /// a reference run can be kept in a file.
    /// </summary>
    internal sealed class StreamRecorder
    {
        private readonly object sync = new object();
        private readonly SortedDictionary<string, List<Sample>> streams = new SortedDictionary<string, List<Sample>>(StringComparer.Ordinal);

        /// <summary>Gets the names of the recorded streams.</summary>
        public IEnumerable<string> Keys => this.streams.Keys;

        /// <summary>Reads a recording written by <see cref="Save"/>.</summary>
        /// <param name="path">Path of the file.</param>
        /// <returns>The recording.</returns>
        public static StreamRecorder Load(string path)
        {
            var recorder = new StreamRecorder();
            foreach (string line in File.ReadAllLines(path))
            {
                if (line.Length == 0)
                {
                    continue;
                }

                string[] parts = line.Split(new[] { '|' }, 3);
                recorder.Add(parts[0], new DateTime(long.Parse(parts[1], CultureInfo.InvariantCulture), DateTimeKind.Utc), parts[2]);
            }

            return recorder;
        }

        /// <summary>Formats a number so that it can be read back without loss.</summary>
        /// <param name="value">The value.</param>
        /// <returns>The text.</returns>
        public static string Format(double value) => value.ToString("R", CultureInfo.InvariantCulture);

        /// <summary>Formats a dictionary of numbers, in key order.</summary>
        /// <typeparam name="TKey">Type of the keys.</typeparam>
        /// <param name="values">The values.</param>
        /// <returns>The text.</returns>
        public static string Format<TKey>(IEnumerable<KeyValuePair<TKey, double>> values)
            => string.Join(";", values.Select(entry => new KeyValuePair<string, double>(entry.Key.ToString(), entry.Value))
                                      .OrderBy(entry => entry.Key, StringComparer.Ordinal)
                                      .Select(entry => $"{entry.Key}={Format(entry.Value)}"));

        /// <summary>Records a stream of numbers.</summary>
        /// <param name="key">Name of the recording.</param>
        /// <param name="source">The stream.</param>
        public void Record(string key, IProducer<double> source)
            => this.RecordWith(key, source, Format);

        /// <summary>Records a stream of booleans.</summary>
        /// <param name="key">Name of the recording.</param>
        /// <param name="source">The stream.</param>
        public void Record(string key, IProducer<bool> source)
            => this.RecordWith(key, source, value => value ? "true" : "false");

        /// <summary>Records a stream of keyed numbers.</summary>
        /// <typeparam name="TKey">Type of the keys.</typeparam>
        /// <param name="key">Name of the recording.</param>
        /// <param name="source">The stream.</param>
        public void Record<TKey>(string key, IProducer<Dictionary<TKey, double>> source)
            => this.RecordWith(key, source, values => Format(values));

        /// <summary>Records a stream with a custom text form.</summary>
        /// <typeparam name="T">Type of the messages.</typeparam>
        /// <param name="key">Name of the recording.</param>
        /// <param name="source">The stream.</param>
        /// <param name="format">Text form of a message.</param>
        public void RecordWith<T>(string key, IProducer<T> source, Func<T, string> format)
        {
            if (source == null)
            {
                return;
            }

            lock (this.sync)
            {
                if (!this.streams.ContainsKey(key))
                {
                    this.streams[key] = new List<Sample>();
                }
            }

            // The text is built inside the handler: \psi recycles the payload once it returns.
            source.Do((value, envelope) => this.Add(key, envelope.OriginatingTime, format(value)));
        }

        /// <summary>Adds one sample.</summary>
        /// <param name="key">Name of the recording.</param>
        /// <param name="time">Originating time.</param>
        /// <param name="payload">Text form of the message.</param>
        public void Add(string key, DateTime time, string payload)
        {
            lock (this.sync)
            {
                if (!this.streams.TryGetValue(key, out var samples))
                {
                    samples = new List<Sample>();
                    this.streams[key] = samples;
                }

                samples.Add(new Sample(time, payload));
            }
        }

        /// <summary>Samples of one recording, in order of arrival.</summary>
        /// <param name="key">Name of the recording.</param>
        /// <returns>The samples, empty when nothing was recorded.</returns>
        public IReadOnlyList<Sample> Get(string key)
        {
            lock (this.sync)
            {
                return this.streams.TryGetValue(key, out var samples) ? samples.ToList() : new List<Sample>();
            }
        }

        /// <summary>Writes the recording as text, one line per message.</summary>
        /// <param name="path">Path of the file.</param>
        public void Save(string path)
        {
            lock (this.sync)
            {
                using (var writer = new StreamWriter(path))
                {
                    foreach (var stream in this.streams)
                    {
                        foreach (Sample sample in stream.Value)
                        {
                            writer.WriteLine($"{stream.Key}|{sample.Time.Ticks.ToString(CultureInfo.InvariantCulture)}|{sample.Payload}");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Differences with another recording, restricted to the given streams. Numbers are
        /// compared with a tolerance, everything else as text.
        /// </summary>
        /// <param name="expected">The reference recording.</param>
        /// <param name="keys">Streams to compare.</param>
        /// <param name="tolerance">Absolute tolerance on the numbers.</param>
        /// <returns>One line per difference, empty when the recordings agree.</returns>
        public List<string> DifferencesWith(StreamRecorder expected, IEnumerable<string> keys, double tolerance = 1e-9)
        {
            var differences = new List<string>();
            foreach (string key in keys)
            {
                IReadOnlyList<Sample> mine = this.Get(key);
                IReadOnlyList<Sample> theirs = expected.Get(key);

                if (mine.Count != theirs.Count)
                {
                    differences.Add($"{key}: {mine.Count} messages, expected {theirs.Count}");
                    continue;
                }

                for (int i = 0; i < mine.Count; i++)
                {
                    if (mine[i].Time != theirs[i].Time)
                    {
                        differences.Add($"{key}[{i}]: time {mine[i].Time:HH:mm:ss.fffffff}, expected {theirs[i].Time:HH:mm:ss.fffffff}");
                        break;
                    }

                    if (!PayloadsMatch(mine[i].Payload, theirs[i].Payload, tolerance))
                    {
                        differences.Add($"{key}[{i}] at {mine[i].Time:HH:mm:ss.fff}: '{mine[i].Payload}', expected '{theirs[i].Payload}'");
                        break;
                    }
                }
            }

            return differences;
        }

        /// <summary>Reads a payload written by <see cref="Format{TKey}"/>.</summary>
        /// <param name="payload">The text.</param>
        /// <returns>The values by key.</returns>
        public static Dictionary<string, double> ParseDictionary(string payload)
        {
            var values = new Dictionary<string, double>();
            foreach (string entry in payload.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                int separator = entry.LastIndexOf('=');
                values[entry.Substring(0, separator)] = double.Parse(entry.Substring(separator + 1), CultureInfo.InvariantCulture);
            }

            return values;
        }

        private static bool PayloadsMatch(string actual, string expected, double tolerance)
        {
            if (actual == expected)
            {
                return true;
            }

            string[] actualParts = actual.Split(';');
            string[] expectedParts = expected.Split(';');
            if (actualParts.Length != expectedParts.Length)
            {
                return false;
            }

            for (int i = 0; i < actualParts.Length; i++)
            {
                int actualSeparator = actualParts[i].LastIndexOf('=');
                int expectedSeparator = expectedParts[i].LastIndexOf('=');
                if (actualSeparator != expectedSeparator || actualParts[i].Substring(0, actualSeparator + 1) != expectedParts[i].Substring(0, expectedSeparator + 1))
                {
                    return false;
                }

                if (!double.TryParse(actualParts[i].Substring(actualSeparator + 1), NumberStyles.Float, CultureInfo.InvariantCulture, out double actualValue) ||
                    !double.TryParse(expectedParts[i].Substring(expectedSeparator + 1), NumberStyles.Float, CultureInfo.InvariantCulture, out double expectedValue))
                {
                    return false;
                }

                if (double.IsNaN(actualValue) != double.IsNaN(expectedValue) || Math.Abs(actualValue - expectedValue) > tolerance)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>One recorded message.</summary>
        internal readonly struct Sample
        {
            /// <summary>Initializes a new instance of the <see cref="Sample"/> struct.</summary>
            /// <param name="time">Originating time.</param>
            /// <param name="payload">Text form of the message.</param>
            public Sample(DateTime time, string payload)
            {
                this.Time = time;
                this.Payload = payload;
            }

            /// <summary>Gets the originating time.</summary>
            public DateTime Time { get; }

            /// <summary>Gets the text form of the message.</summary>
            public string Payload { get; }
        }
    }
}

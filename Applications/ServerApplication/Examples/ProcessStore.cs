// <copyright file="ProcessStore.cs" company="SAAC">
// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.
// </copyright>

using Microsoft.Psi;
using Microsoft.Psi.Data;
using SAAC.PipelineServices;

namespace ServerApplication.Examples
{
    /// <summary>
    /// Where a run of the collaboration process stores its streams: partitions of the
    /// session of the process in the dataset, one per kind of data ("Positions",
    /// "SpeechEvents", "Indices_20s"...).
    ///
    /// The first run of a live session uses these names. A process started again after
    /// "Stop Process" gets partitions of its own, with the number of the run in their names
    /// ("Indices_20s_run2"), and so does each post processing of a recorded dataset
    /// ("Indices_20s_post1"): what was stored before is kept, and a name is never used twice
    /// in a dataset.
    /// </summary>
    public class ProcessStore
    {
        private readonly DatasetPipeline server;
        private readonly Pipeline pipeline;
        private readonly List<string> partitions = new List<string>();

        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessStore"/> class.
        /// </summary>
        /// <param name="server">The server owning the dataset.</param>
        /// <param name="session">The session of the process; null when the server has no dataset: the streams are then declared and not stored.</param>
        /// <param name="pipeline">The pipeline of the process.</param>
        /// <param name="suffix">What ends the names of the partitions of this run: empty, "_run2", "_post1"...</param>
        /// <param name="label">How the log calls this run: "run 2", "post processing 1"...</param>
        public ProcessStore(DatasetPipeline server, Session? session, Pipeline pipeline, string suffix, string label)
        {
            this.server = server;
            this.Session = session;
            this.pipeline = pipeline;
            this.Suffix = suffix ?? string.Empty;
            this.Label = label ?? string.Empty;
        }

        /// <summary>Gets the session of the process; null when nothing is stored.</summary>
        public Session? Session { get; }

        /// <summary>Gets what ends the names of the partitions of this run.</summary>
        public string Suffix { get; }

        /// <summary>Gets how the log calls this run.</summary>
        public string Label { get; }

        /// <summary>Gets a value indicating whether a stream of this run is stored.</summary>
        public bool StoresStreams => this.Session != null && this.partitions.Any(partition => this.StreamCount(partition) > 0);

        /// <summary>The name of the partition of a kind of data for this run.</summary>
        /// <param name="kind">Kind of data: "Positions", "Indices_20s"...</param>
        /// <returns>The kind followed by the suffix of this run.</returns>
        public string Partition(string kind) => kind + this.Suffix;

        /// <summary>
        /// Says that a partition belongs to this run, for a component that writes its streams by itself.
        /// </summary>
        /// <param name="kind">Kind of data.</param>
        /// <returns>The name of the partition.</returns>
        public string Declare(string kind)
        {
            string partition = this.Partition(kind);
            if (!this.partitions.Contains(partition))
            {
                this.partitions.Add(partition);
            }

            return partition;
        }

        /// <summary>Stores a stream in the partition of a kind of data.</summary>
        /// <typeparam name="T">Type of the messages.</typeparam>
        /// <param name="kind">Kind of data.</param>
        /// <param name="streamName">Name of the stream in the partition.</param>
        /// <param name="stream">The stream.</param>
        /// <returns>False when the partition already has a stream of that name: it is not stored twice.</returns>
        public bool Write<T>(string kind, string streamName, IProducer<T> stream)
        {
            string partition = this.Declare(kind);
            if (this.server.Connectors.TryGetValue(partition, out var streams) && streams.ContainsKey(streamName))
            {
                return false;
            }

            this.server.CreateConnectorAndStore(streamName, partition, this.Session, this.pipeline, typeof(T), stream);
            return true;
        }

        /// <summary>The partitions of this run and their number of streams, for the log.</summary>
        /// <returns>"Positions (9 streams), SpeechEvents (8 streams)...".</returns>
        public string Describe()
            => string.Join(", ", this.partitions.Where(partition => this.StreamCount(partition) > 0).Select(partition => $"{partition} ({this.StreamCount(partition)} streams)"));

        /// <summary>
        /// Removes from the server what this run declared, once its pipeline is disposed: the
        /// stores are closed, and a later run or another consumer must not find these streams.
        /// </summary>
        public void Forget()
        {
            foreach (string partition in this.partitions)
            {
                this.server.Connectors.Remove(partition);
                if (this.Session != null && this.server.Stores.TryGetValue(this.Session.Name, out var stores))
                {
                    stores.Remove(partition);
                }
            }

            this.partitions.Clear();
        }

        private int StreamCount(string partition)
            => this.server.Connectors.TryGetValue(partition, out var streams) ? streams.Count : 0;
    }
}

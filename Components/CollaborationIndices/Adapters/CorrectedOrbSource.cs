// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.

namespace SAAC.CollaborationIndices
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Numerics;

    /// <summary>
    /// Supplies corrected orb positions during replay, from the unified corrected CSV.
    ///
    /// Row selection: the file holds every session, condition and participant, so rows
    /// are filtered on session_id / condition / participant_num at load time.
    ///
    /// Positions only. The corrected columns are head_ghost_pos_*, left_hand_ghost_pos_*
    /// and right_hand_ghost_pos_*; the file carries no corrected rotations, so callers
    /// forward the recorded orientation unchanged.
    ///
    /// Frame: the ghost positions live in the same external (right-handed, Z-up) frame
    /// as the raw export, i.e. they went through PositionOrientationPreProcessing.SwapYZ.
    /// That operation is an involution, so applying it again returns Unity's Y-up frame.
    ///
    /// Time: utc_timestamp_s is read on magnitude rather than on its name, because some
    /// exports store milliseconds in that column.
    /// </summary>
    public class CorrectedOrbSource
    {
        /// <summary>Which tracked part a lookup refers to.</summary>
        public enum Part
        {
            /// <summary>Head.</summary>
            Head,

            /// <summary>Left hand.</summary>
            LeftHand,

            /// <summary>Right hand.</summary>
            RightHand,
        }

        private static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        private readonly double[] times;      // seconds since epoch, ascending
        private readonly Vector3[] head;
        private readonly Vector3[] leftHand;
        private readonly Vector3[] rightHand;

        private Action<string> log;
        private long hits;
        private long misses;

        private CorrectedOrbSource(double[] times, Vector3[] head, Vector3[] leftHand, Vector3[] rightHand)
        {
            this.times = times;
            this.head = head;
            this.leftHand = leftHand;
            this.rightHand = rightHand;
        }

        /// <summary>
        /// Gets or sets whether every lookup is logged. Off by default: a session emits
        /// tens of thousands of messages per stream, so this is a debugging aid rather
        /// than something to leave on. Prefer SummaryLogInterval for routine checks.
        /// </summary>
        public bool VerboseLookup { get; set; }

        /// <summary>
        /// Gets or sets how many lookups pass between summary lines. Zero disables them.
        /// </summary>
        public int SummaryLogInterval { get; set; } = 0;

        /// <summary>Gets the number of rows retained for this participant.</summary>
        public int Count => this.times.Length;

        /// <summary>Gets the number of successful lookups.</summary>
        public long Hits => this.hits;

        /// <summary>Gets the number of lookups that fell outside the corrected range.</summary>
        public long Misses => this.misses;

        /// <summary>Gets the first corrected timestamp, UTC.</summary>
        public DateTime FirstTime => UnixEpoch.AddSeconds(this.times[0]);

        /// <summary>Gets the last corrected timestamp, UTC.</summary>
        public DateTime LastTime => UnixEpoch.AddSeconds(this.times[this.times.Length - 1]);

        /// <summary>
        /// Loads the corrected rows for one participant. Returns null when the path is
        /// empty, the file is missing, or no row matches — so callers can treat "no
        /// correction available" as a normal case and forward recorded poses unchanged.
        /// </summary>
        /// <param name="path">Path to the unified corrected CSV.</param>
        /// <param name="sessionId">Value to match in session_id.</param>
        /// <param name="condition">Value to match in condition.</param>
        /// <param name="participantNum">Value to match in participant_num (one-based).</param>
        /// <param name="log">Optional logging callback.</param>
        /// <returns>A loaded source, or null.</returns>
        public static CorrectedOrbSource Load(
            string path, int sessionId, string condition, int participantNum, Action<string> log = null)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return null;
            }

            if (!File.Exists(path))
            {
                log?.Invoke($"[CorrectedOrbSource] file not found: {path}");
                return null;
            }

            var lines = File.ReadAllLines(path);
            if (lines.Length < 2)
            {
                log?.Invoke($"[CorrectedOrbSource] no data rows in {path}");
                return null;
            }

            char sep = DetectSeparator(lines[0]);
            var header = lines[0].Split(sep);
            var col = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < header.Length; i++)
            {
                col[header[i].Trim()] = i;
            }

            string[] required =
            {
                "session_id", "condition", "participant_num", "utc_timestamp_s",
                "head_ghost_pos_x", "head_ghost_pos_y", "head_ghost_pos_z",
                "left_hand_ghost_pos_x", "left_hand_ghost_pos_y", "left_hand_ghost_pos_z",
                "right_hand_ghost_pos_x", "right_hand_ghost_pos_y", "right_hand_ghost_pos_z",
            };

            foreach (var name in required)
            {
                if (!col.ContainsKey(name))
                {
                    log?.Invoke($"[CorrectedOrbSource] missing column '{name}' in {path}");
                    return null;
                }
            }

            var inv = CultureInfo.InvariantCulture;
            string sessionText = sessionId.ToString(inv);
            string participantText = participantNum.ToString(inv);

            var tList = new List<double>();
            var hList = new List<Vector3>();
            var lList = new List<Vector3>();
            var rList = new List<Vector3>();

            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i]))
                {
                    continue;
                }

                var p = lines[i].Split(sep);
                if (p.Length < header.Length)
                {
                    continue;   // truncated trailing line
                }

                if (p[col["session_id"]].Trim() != sessionText ||
                    p[col["participant_num"]].Trim() != participantText ||
                    !string.Equals(p[col["condition"]].Trim(), condition, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                tList.Add(F(p, col, "utc_timestamp_s", inv));
                hList.Add(SwapYZ(V(p, col, "head_ghost_pos", inv)));
                lList.Add(SwapYZ(V(p, col, "left_hand_ghost_pos", inv)));
                rList.Add(SwapYZ(V(p, col, "right_hand_ghost_pos", inv)));
            }

            if (tList.Count == 0)
            {
                log?.Invoke($"[CorrectedOrbSource] no row matched session={sessionId} " +
                            $"condition={condition} participant={participantNum}");
                return null;
            }

            // The column is named utc_timestamp_s but some exports hold milliseconds.
            // Decide on magnitude rather than on the name: a current epoch timestamp is
            // ~1.8e9 in seconds and ~1.8e12 in milliseconds. The 1e11 threshold sits at
            // year 5138 in seconds and 1973 in milliseconds, so it cannot misfire on
            // real data. Getting this wrong is silent — every lookup would fall outside
            // the range and quietly forward the uncorrected pose.
            if (tList[0] > 1e11)
            {
                for (int i = 0; i < tList.Count; i++)
                {
                    tList[i] /= 1000.0;
                }

                log?.Invoke("[CorrectedOrbSource] timestamps read as milliseconds despite the column name.");
            }

            var times = tList.ToArray();
            var head = hList.ToArray();
            var left = lList.ToArray();
            var right = rList.ToArray();

            // BinarySearch requires ascending order; the export should already be sorted,
            // but an unsorted file would silently return wrong rows rather than fail.
            if (!IsAscending(times))
            {
                var order = new int[times.Length];
                for (int i = 0; i < order.Length; i++)
                {
                    order[i] = i;
                }

                Array.Sort((double[])times.Clone(), order);
                times = Reorder(times, order);
                head = Reorder(head, order);
                left = Reorder(left, order);
                right = Reorder(right, order);
                log?.Invoke("[CorrectedOrbSource] rows were not time-ordered; sorted on load.");
            }

            var source = new CorrectedOrbSource(times, head, left, right) { log = log };
            log?.Invoke($"[CorrectedOrbSource] {times.Length} rows for session={sessionId} " +
                        $"condition={condition} participant={participantNum} " +
                        $"({source.FirstTime:HH:mm:ss} → {source.LastTime:HH:mm:ss} UTC)");
            return source;
        }

        /// <summary>
        /// Returns the corrected position for a part at the given originating time, in
        /// Unity frame, interpolating between the two surrounding rows. False means the
        /// time falls outside the corrected range and the caller should forward the
        /// recorded position unchanged.
        /// </summary>
        /// <param name="originatingTime">The message originating time.</param>
        /// <param name="part">Which tracked part.</param>
        /// <param name="unityPosition">The corrected position, Unity frame.</param>
        /// <returns>True when a corrected position was produced.</returns>
        public bool TryGetUnityPosition(DateTime originatingTime, Part part, out Vector3 unityPosition)
        {
            unityPosition = default;

            double tq = (originatingTime.ToUniversalTime() - UnixEpoch).TotalSeconds;

            // Outside the corrected range: forwarding the recorded position is better
            // than clamping, which would freeze the orb at an endpoint for the whole gap.
            if (tq < this.times[0] || tq > this.times[this.times.Length - 1])
            {
                this.misses++;
                if (this.VerboseLookup)
                {
                    double gap = tq < this.times[0]
                        ? this.times[0] - tq
                        : tq - this.times[this.times.Length - 1];
                    this.log?.Invoke($"[CorrectedOrbSource] {part} @{originatingTime:HH:mm:ss.fff} " +
                                     $"MISS — hors plage de {gap:F2} s");
                }

                this.AfterLookup();
                return false;
            }

            var source = this.Source(part);

            int idx = Array.BinarySearch(this.times, tq);
            if (idx >= 0)
            {
                unityPosition = source[idx];
                this.hits++;
                if (this.VerboseLookup)
                {
                    this.log?.Invoke($"[CorrectedOrbSource] {part} @{originatingTime:HH:mm:ss.fff} " +
                                     $"HIT exact [{idx}] → {unityPosition:F3}");
                }

                this.AfterLookup();
                return true;
            }

            idx = ~idx;
            int hi = Math.Min(idx, this.times.Length - 1);
            int lo = Math.Max(0, hi - 1);

            double span = this.times[hi] - this.times[lo];
            float alpha = span > 1e-9 ? (float)((tq - this.times[lo]) / span) : 0f;

            unityPosition = Vector3.Lerp(source[lo], source[hi], alpha);
            this.hits++;
            if (this.VerboseLookup)
            {
                this.log?.Invoke($"[CorrectedOrbSource] {part} @{originatingTime:HH:mm:ss.fff} " +
                                 $"HIT interp [{lo}→{hi}] α={alpha:F3} Δt={span * 1000.0:F1} ms " +
                                 $"→ {unityPosition:F3}");
            }

            this.AfterLookup();
            return true;
        }

        /// <summary>Writes a one-line summary of lookups so far.</summary>
        public void LogSummary()
        {
            long total = this.hits + this.misses;
            double rate = total > 0 ? 100.0 * this.hits / total : 0.0;
            this.log?.Invoke($"[CorrectedOrbSource] {this.hits} corrigées / {total} messages ({rate:F1} %), " +
                             $"{this.misses} hors plage");
        }

        private void AfterLookup()
        {
            if (this.SummaryLogInterval <= 0)
            {
                return;
            }

            long total = this.hits + this.misses;
            if (total % this.SummaryLogInterval == 0)
            {
                this.LogSummary();
            }
        }

        private static char DetectSeparator(string headerLine)
        {
            if (headerLine.Contains("\t"))
            {
                return '\t';
            }

            if (headerLine.Contains(";"))
            {
                return ';';
            }

            return ',';
        }

        private static double F(string[] p, Dictionary<string, int> col, string name, IFormatProvider inv)
        {
            string s = p[col[name]].Trim();
            if (string.IsNullOrEmpty(s))
            {
                return 0.0;
            }

            return double.Parse(s, NumberStyles.Float, inv);
        }

        private static Vector3 V(string[] p, Dictionary<string, int> col, string prefix, IFormatProvider inv)
            => new Vector3(
                (float)F(p, col, prefix + "_x", inv),
                (float)F(p, col, prefix + "_y", inv),
                (float)F(p, col, prefix + "_z", inv));

        /// <summary>Swaps Y and Z. Its own inverse, so it converts both ways.</summary>
        private static Vector3 SwapYZ(Vector3 v) => new Vector3(v.X, v.Z, v.Y);

        private static bool IsAscending(double[] a)
        {
            for (int i = 1; i < a.Length; i++)
            {
                if (a[i] < a[i - 1])
                {
                    return false;
                }
            }

            return true;
        }

        private static T[] Reorder<T>(T[] a, int[] order)
        {
            var result = new T[order.Length];
            for (int i = 0; i < order.Length; i++)
            {
                result[i] = a[order[i]];
            }

            return result;
        }

        private Vector3[] Source(Part part)
        {
            switch (part)
            {
                case Part.Head: return this.head;
                case Part.LeftHand: return this.leftHand;
                case Part.RightHand: return this.rightHand;
                default: throw new ArgumentOutOfRangeException(nameof(part));
            }
        }
    }
}

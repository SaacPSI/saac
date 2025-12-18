// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.PsiStudio
{
    /// <summary>
    /// Message class message from <see cref="NetworkStreamsManager"/>.
    /// </summary>
    public class PsiStudioNetworkInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PsiStudioNetworkInfo"/> class.
        /// </summary>
        /// <param name="evt">State of PsiStudio.</param>
        /// <param name="interval">Current Interval of playback.</param>
        /// <param name="playSpeed">Current speed of playback.</param>
        /// <param name="sessionName">Name of the surrent session.</param>
        public PsiStudioNetworkInfo(PsiStudioNetworkEvent evt, TimeInterval interval = null, double playSpeed = 1.0, string sessionName = "")
        {
            this.Event = evt;
            this.SessionName = sessionName;
            this.PlaySpeed = playSpeed;
            this.Interval = interval ?? TimeInterval.Infinite;
        }

        /// <summary>
        /// Enum providing the state of PsiStudio.
        /// </summary>
        public enum PsiStudioNetworkEvent
        {
            /// <summary>
            /// PsiStudio is playing in playback so the stream will start shortly.
            /// </summary>
            Playing,

            /// <summary>
            /// PsiStudio is stoping in playback no more data incoming.
            /// </summary>
            Stopping,,

            /// <summary>
            /// PlaySpeed iof the playback.
            /// </summary>
            PlaySpeed,
        }

        /// <summary>
        /// Gets the PsiTudioEvent instance.
        /// </summary>
        public PsiStudioNetworkEvent Event { get; private set; }

        /// <summary>
        /// Gets the current session name.
        /// </summary>
        public string SessionName { get; private set; }

        /// <summary>
        /// Gets the current timeInterval for the playback.
        /// </summary>
        public TimeInterval Interval { get; private set; }

        /// <summary>
        /// Gets the current playspeed of the playback.
        /// </summary>
        public double PlaySpeed { get; private set; } = 1.0;
    }
}

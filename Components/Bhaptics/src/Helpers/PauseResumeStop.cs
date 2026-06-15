// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.Bhaptic.Helpers
{
    /// <summary>
    /// Represents a playback state change for a running haptic pattern.
    /// Used to pause, resume, or stop an event identified by its <see cref="EventId"/>.
    /// </summary>
    public struct PauseResumeStop
    {
        /// <summary>Identifier of the haptic pattern event whose playback state is being changed.</summary>
        public string EventId;

        /// <summary>
        /// Enumerates the possible playback state transitions for a haptic event.
        /// </summary>
        public enum EPauseResumeStop
        {
            /// <summary>Temporarily suspends playback of the event.</summary>
            Pause,

            /// <summary>Resumes a previously paused event.</summary>
            Resume,

            /// <summary>Permanently stops playback and clears the event.</summary>
            Stop
        }

        /// <summary>The playback state transition to apply to the event.</summary>
        public EPauseResumeStop State;

        /// <summary>
        /// Initializes a new instance of the <see cref="PauseResumeStop"/> struct.
        /// </summary>
        /// <param name="eventId">Identifier of the haptic pattern event to control.</param>
        /// <param name="state">The playback state transition to apply.</param>
        public PauseResumeStop(string eventId, EPauseResumeStop state)
        {
            this.EventId = eventId;
            this.State = state;
        }
    }
}

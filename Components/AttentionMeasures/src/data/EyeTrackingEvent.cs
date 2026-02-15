// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.AttentionMeasures
{
    /// <summary>
    /// Class to send events to PSI.
    /// </summary>
    public class EyeTrackingEvent
    {
        /// <summary>
        /// Enumeration of event types.
        /// </summary>
        public enum EventType
        {
            /// <summary>Beginning of experiment.</summary>
            BeginningExperiment,

            /// <summary>Ending of experiment.</summary>
            EndingExperiment
        }

        /// <summary>
        /// Gets or sets the event type.
        /// </summary>
        public EventType EventTypeValue { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="EyeTrackingEvent"/> class.
        /// </summary>
        /// <param name="e">The event type.</param>
        public EyeTrackingEvent(EventType e)
        {
            this.EventTypeValue = e;
        }
    }
}

// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.AttentionMeasures
{
    // Class to send events to PSI
    public class EyeTrackingEvent
    {
        public enum EventType
        {
            BeginningExperiment,
            EndingExperiment
        }

        public EventType eventType;

        // Constructors
        public EyeTrackingEvent(EventType e)
        {
            this.eventType = e;
        }
    }
}

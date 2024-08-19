
namespace SAAC.AttentionMeasures
{
    //Class to send events to PSI
    public class EyeTrackingEvent
    {
        public enum EventType
        {
            BeginningExperiment,
            EndingExperiment
        }

        public EventType eventType;

        //Constructors
        public EyeTrackingEvent(EventType e) { this.eventType = e; }
    }
}


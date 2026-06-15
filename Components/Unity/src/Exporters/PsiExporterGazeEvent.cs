using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.UI;

public class PsiExporterGazeEvent : PsiExporter<SAAC.GlobalHelpers.GazeEvent>
{
    protected SAAC.GlobalHelpers.GazeEvent _lastGazedEvent;
    protected const long TicksToSubtract = 100;

    public void OnGazeHoverEntered(HoverEnterEventArgs args)
    {
        GenerateGazeEvent(SAAC.GlobalHelpers.EEventType.Hover, args.interactableObject.transform.name, args.interactableObject.transform.position, true);
    }

    public void OnGazeHoverExited(HoverExitEventArgs args)
    {
        GenerateGazeEvent(SAAC.GlobalHelpers.EEventType.Hover, args.interactableObject.transform.name, args.interactableObject.transform.position, false);
    }

    public void OnGazeSelectEntered(SelectEnterEventArgs args)
    {
        GenerateGazeEvent(SAAC.GlobalHelpers.EEventType.Select, args.interactableObject.transform.name, args.interactableObject.transform.position, true);
    }

    public void OnGazeSelectExited(SelectExitEventArgs args)
    {
        GenerateGazeEvent(SAAC.GlobalHelpers.EEventType.Select, args.interactableObject.transform.name, args.interactableObject.transform.position, false);
    }

    public void OnGazeUIHoverEntered(UIHoverEventArgs args)
    {
        GenerateGazeEvent(SAAC.GlobalHelpers.EEventType.UI, args.uiObject.name, Vector3.zero, true);
    }

    public void OnGazeUIHoverExited(UIHoverEventArgs args)
    {
        GenerateGazeEvent(SAAC.GlobalHelpers.EEventType.UI, args.uiObject.name, Vector3.zero, false);
    }

    public void Post(SAAC.GlobalHelpers.GazeEvent message, bool subDelta = false)
    {
        if (CanSend())
        {
            if (subDelta)
                Out.Post(message, Timestamp.AddTicks(-1 * TicksToSubtract));
            else
                Out.Post(message, Timestamp);
        }
    }

    protected void GenerateGazeEvent(SAAC.GlobalHelpers.EEventType type, string objectID, Vector3 position, bool isGazed)
    {
        CheckAndPostGazeEvent(new SAAC.GlobalHelpers.GazeEvent(type, PsiManager.UsedProcessName, objectID, new System.Numerics.Vector3(position.x, position.y, position.z), isGazed));
    }

    protected void CheckAndPostGazeEvent(SAAC.GlobalHelpers.GazeEvent newEvent)
    {
        if (_lastGazedEvent != null)
        {
            // No gaze out event was triggered.
            if (_lastGazedEvent.IsGazed && _lastGazedEvent.ObjectID != newEvent.ObjectID)
            {
                Post(new SAAC.GlobalHelpers.GazeEvent(_lastGazedEvent.Type, _lastGazedEvent.ObjectID, _lastGazedEvent.UserID, _lastGazedEvent.Position, false), true);
            }
            // Same gaze event as previous.
            else if (_lastGazedEvent.IsGazed == newEvent.IsGazed && _lastGazedEvent.ObjectID == newEvent.ObjectID)
            {
                return;
            }
        }
        Post(newEvent);
        _lastGazedEvent = newEvent;
    }

#if PSI_TCP_STREAMS
    protected override Microsoft.Psi.Interop.Serialization.IFormatSerializer<SAAC.GlobalHelpers.GazeEvent> GetSerializer()
    {
        return SAAC.PsiFormats.PsiFormatGazeEvent.GetFormat();
    }
#endif
}

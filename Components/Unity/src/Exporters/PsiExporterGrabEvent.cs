using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class PsiExporterGrabEvent : PsiExporter<SAAC.GlobalHelpers.GrabEvent>
{
    protected SAAC.GlobalHelpers.GrabEvent _lastGrabbedEvent;
    protected const long TicksToSubtract = 100;

    public void OnGrabHoverEntered(HoverEnterEventArgs args)
    {
        GenerateGrabEvent(SAAC.GlobalHelpers.EEventType.Hover, args.interactableObject.transform.name, true);
    }

    public void OnGrabHoverExited(HoverExitEventArgs args)
    {
        GenerateGrabEvent(SAAC.GlobalHelpers.EEventType.Hover, args.interactableObject.transform.name, false);
    }

    public void OnGrabSelectEntered(SelectEnterEventArgs args)
    {
        GenerateGrabEvent(SAAC.GlobalHelpers.EEventType.Select, args.interactableObject.transform.name, true);
    }

    public void OnGrabSelectExited(SelectExitEventArgs args)
    {
        GenerateGrabEvent(SAAC.GlobalHelpers.EEventType.Select, args.interactableObject.transform.name, false);
    }

    public void OnGrabFocusHoverEntered(FocusEnterEventArgs args)
    {
        GenerateGrabEvent(SAAC.GlobalHelpers.EEventType.Focus, args.interactableObject.transform.name, true);
    }

    public void OnGrabFocusHoverExited(FocusExitEventArgs args)
    {
        GenerateGrabEvent(SAAC.GlobalHelpers.EEventType.Focus, args.interactableObject.transform.name, false);
    }

    public void OnGrabActivateEntered(ActivateEventArgs args)
    {
        GenerateGrabEvent(SAAC.GlobalHelpers.EEventType.Activate, args.interactableObject.transform.name, true);
    }

    public void OnGrabActivateExited(ActivateEventArgs args)
    {
        GenerateGrabEvent(SAAC.GlobalHelpers.EEventType.Activate, args.interactableObject.transform.name, false);
    }

    public void Post(SAAC.GlobalHelpers.GrabEvent message, bool subDelta = false)
    {
        if (CanSend())
        {
            if (subDelta)
                Out.Post(message, Timestamp.AddTicks(-1 * TicksToSubtract));
            else
                Out.Post(message, Timestamp);
        }
    }

    protected void GenerateGrabEvent(SAAC.GlobalHelpers.EEventType type, string objectID, bool isGrabd)
    {
        CheckAndPostGrabEvent(new SAAC.GlobalHelpers.GrabEvent(0, PsiManager.UsedProcessName, objectID, isGrabd));
    }

    protected void CheckAndPostGrabEvent(SAAC.GlobalHelpers.GrabEvent newEvent)
    {
        if (_lastGrabbedEvent != null)
        {
            // No grab out event was triggered.
            if (_lastGrabbedEvent.IsGrabbed && _lastGrabbedEvent.ObjectID != newEvent.ObjectID)
            {
                Post(new SAAC.GlobalHelpers.GrabEvent(_lastGrabbedEvent.Type, _lastGrabbedEvent.ObjectID, _lastGrabbedEvent.UserID, false), true);
            }
            // Same grab event as previous.
            else if (_lastGrabbedEvent.IsGrabbed == newEvent.IsGrabbed && _lastGrabbedEvent.ObjectID == newEvent.ObjectID)
            {
                return;
            }
        }
        Post(newEvent);
        _lastGrabbedEvent = newEvent;
    }

#if PSI_TCP_STREAMS
    protected override Microsoft.Psi.Interop.Serialization.IFormatSerializer<SAAC.GlobalHelpers.GrabEvent> GetSerializer()
    {
        return SAAC.PsiFormats.PsiFormatGrabEvent.GetFormat();
    }
#endif
}

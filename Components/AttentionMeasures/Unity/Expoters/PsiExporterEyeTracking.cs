using System;
using System.Collections.Generic;
using System.IO;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Assertions.Must;


//Exporter to send eye-tracking data to psi
public class PsiExporterEyeTracking : PsiExporter<Dictionary<ETData, IEyeTracking>>
{
    //Gameobjects to recover infos on the player
    [Space, SerializeField]
    private GameObject PlayerHead;
    [SerializeField]
    private GameObject LeftEye;
    [SerializeField]
    private GameObject RightEye;
    //Bool to activate the debug visualization
    [SerializeField]
    private bool ActivateDebug = false;
    [SerializeField]
    //Prefab allowing to see the eye tracking
    private GameObject EyeTrackingVisualization;
    //Layermask filtering what to collide with eye tracking rays
    [SerializeField]
    private LayerMask LayersToHit;

    //Dictionary holding the previous eyetracking data
    private Dictionary<ETData, IEyeTracking> PreviousEyeTracking = new Dictionary<ETData, IEyeTracking>();

    override public void Start()
    {
        base.Start();

        if (PlayerHead == null) { PlayerHead = gameObject; }

        //Setting the first PreviousEyeTracking
        EyeTrackingTemplate template = new EyeTrackingTemplate();
        foreach (var kvp in template.content)
        {
            if (kvp.Value.GetType() == typeof(EyeTrackingVector3)) { PreviousEyeTracking.Add(kvp.Key, new EyeTrackingUnityVector3()); }
            else { PreviousEyeTracking.Add(kvp.Key, kvp.Value); }
        }

        //Activating the debug
        if (EyeTrackingVisualization != null) { EyeTrackingVisualization.SetActive(ActivateDebug); }

    }

    void Update()
    {
        //Dictionary holding data to send
        Dictionary<ETData, IEyeTracking> eyeTracking = new Dictionary<ETData, IEyeTracking>();

        //------------ Collecting data in variables
        //Data of the left eye
        var leftEyePosition = LeftEye.transform.position;
        var leftEyeRotation = LeftEye.transform.eulerAngles;
        var leftGaze = LeftEye.transform.rotation * Vector3.forward;
        //Data of the right eye
        var rightEyePosition = RightEye.transform.position;
        var rightEyeRotation = RightEye.transform.eulerAngles;
        var rightGaze = RightEye.transform.rotation * Vector3.forward;
        //Head data
        var headDirection = PlayerHead.transform.forward;
        var centerEyePosition = (leftEyePosition + rightEyePosition) / 2;
        //Average gaze : central gaze vector formed by the left and right gaze
        var averageGaze = (leftGaze + rightGaze).normalized;
        //Casting a ray to recover gazed objects
        RaycastHit hit;
        bool isGazingAtSomething = Physics.Raycast(centerEyePosition, averageGaze, out hit, 100f, LayersToHit);
        Vector3 firstIntersectionPoint = Vector3.zero;
        int gazedObjectID = 0;
        string gazedObjectName = "null";
        bool hasEyeTrackingTags = false;
        List<string> eyeTrackingTagsList = new List<string>();

        //If the ray hits, recover the data on hitobject
        if (isGazingAtSomething)
        {
            firstIntersectionPoint = hit.point;
            if (ActivateDebug) { Debug.DrawRay(centerEyePosition, averageGaze * 10, Color.magenta); DebugDisplay(firstIntersectionPoint); }
            gazedObjectID = hit.transform.gameObject.GetInstanceID();
            gazedObjectName = hit.transform.name;
            hasEyeTrackingTags = hit.transform.TryGetComponent<EyeTrackingTags>(out EyeTrackingTags t);
            if (hasEyeTrackingTags) { eyeTrackingTagsList = t.AllTagsListStrings(); }

        }

        if (ActivateDebug) { Debug.DrawRay(centerEyePosition, averageGaze * 10, Color.magenta); }

        //Adding data to the dictionary
        eyeTracking.Add(ETData.LeftEyePosition, new EyeTrackingVector3(leftEyePosition.x, leftEyePosition.y, leftEyePosition.z));
        eyeTracking.Add(ETData.LeftEyeRotation, new EyeTrackingVector3(leftEyeRotation.x, leftEyeRotation.y, leftEyeRotation.z));
        eyeTracking.Add(ETData.LeftGaze, new EyeTrackingVector3(leftGaze.x, leftGaze.y, leftGaze.z));
        eyeTracking.Add(ETData.RightEyePosition, new EyeTrackingVector3(rightEyePosition.x, rightEyePosition.y, rightEyePosition.z));
        eyeTracking.Add(ETData.RightEyeRotation, new EyeTrackingVector3(rightEyeRotation.x, rightEyeRotation.y, rightEyeRotation.z));
        eyeTracking.Add(ETData.RightGaze, new EyeTrackingVector3(rightGaze.x, rightGaze.y, rightGaze.z));
        eyeTracking.Add(ETData.HeadDirection, new EyeTrackingVector3(headDirection.x, headDirection.y, headDirection.z));
        eyeTracking.Add(ETData.CenterEyePosition, new EyeTrackingVector3(centerEyePosition.x, centerEyePosition.y, centerEyePosition.z));
        eyeTracking.Add(ETData.AverageGaze, new EyeTrackingVector3(averageGaze.x, averageGaze.y, averageGaze.z));
        eyeTracking.Add(ETData.IsGazingAtSomething, new EyeTrackingBool(isGazingAtSomething));
        eyeTracking.Add(ETData.FirstIntersectionPoint, new EyeTrackingVector3(firstIntersectionPoint.x, firstIntersectionPoint.y, firstIntersectionPoint.z));
        eyeTracking.Add(ETData.GazedObjectID, new EyeTrackingInt(gazedObjectID));
        eyeTracking.Add(ETData.GazedObjectName, new EyeTrackingString(gazedObjectName));
        eyeTracking.Add(ETData.HasEyeTrackingTags, new EyeTrackingBool(hasEyeTrackingTags));
        eyeTracking.Add(ETData.EyeTrackingTagsList, new EyeTrackingStringList(eyeTrackingTagsList));

        //Sending data if different from the lastinput
        if (CanSend() && !IsSameData(eyeTracking))
        {
            Out.Post(eyeTracking, GetCurrentTime());
            PsiManager.AddLog("EyeTracking log sent at " + Time.time);
            UpdatePreviousData(eyeTracking);
        }

    }

    //Display of where the eye tracking is hitting
    private void DebugDisplay(Vector3 position)
    {
        if (EyeTrackingVisualization != null) { EyeTrackingVisualization.transform.position = position; }
    }

    //Checking if the data is the same as the previous data
    private bool IsSameData(Dictionary<ETData, IEyeTracking> eyeTracking)
    {
        bool isSameData = true;
        foreach (var kvp in PreviousEyeTracking)
        {
            if (kvp.Value.GetType() == typeof(EyeTrackingInt) && eyeTracking[kvp.Key].GetType() == typeof(EyeTrackingInt))
            {
                isSameData = ((EyeTrackingInt)kvp.Value).Compare((EyeTrackingInt)eyeTracking[kvp.Key]);
            }
            else if (kvp.Value.GetType() == typeof(EyeTrackingBool) && eyeTracking[kvp.Key].GetType() == typeof(EyeTrackingBool))
            {
                isSameData = ((EyeTrackingBool)kvp.Value).Compare((EyeTrackingBool)eyeTracking[kvp.Key]);
            }
            else if (kvp.Value.GetType() == typeof(EyeTrackingString) && eyeTracking[kvp.Key].GetType() == typeof(EyeTrackingString))
            {
                isSameData = ((EyeTrackingString)kvp.Value).Compare((EyeTrackingString)eyeTracking[kvp.Key]);
            }
            else if (kvp.Value.GetType() == typeof(EyeTrackingStringList) && eyeTracking[kvp.Key].GetType() == typeof(EyeTrackingStringList))
            {
                isSameData = ((EyeTrackingStringList)kvp.Value).Compare((EyeTrackingStringList)eyeTracking[kvp.Key]);
            }
            else if (kvp.Value.GetType() == typeof(EyeTrackingUnityVector3) && eyeTracking[kvp.Key].GetType() == typeof(EyeTrackingVector3))
            {
                isSameData = (((EyeTrackingUnityVector3)kvp.Value).ToEyeTrackingVector3()).Compare((EyeTrackingVector3)eyeTracking[kvp.Key]);
            }
            if (!isSameData) { break; }
        }
        return isSameData;
    }

    //Update the previous data
    private void UpdatePreviousData(Dictionary<ETData, IEyeTracking> eyeTracking)
    {
        foreach (var key in eyeTracking.Keys)
        {
            if (PreviousEyeTracking[key].GetType() == typeof(EyeTrackingUnityVector3))
            {
                PreviousEyeTracking[key] = new EyeTrackingUnityVector3((EyeTrackingVector3)eyeTracking[key]);
            }
            else
            {
                PreviousEyeTracking[key] = eyeTracking[key];
            }
        }
    }


#if PSI_TCP_SOURCE
    protected override Microsoft.Psi.Interop.Serialization.IFormatSerializer<Dictionary<ETData, IEyeTracking>> GetSerializer()
    {
        return PsiFormatEyeTracking.GetFormat();
    }
#endif
}

//Class of IEyeTracking used to compare with Unity Vector3 instead of System.Numerics.Vector3
public class EyeTrackingUnityVector3 : IEyeTracking
{

    public UnityEngine.Vector3 content;

    public EyeTrackingUnityVector3() { content = Vector3.down; }
    public EyeTrackingUnityVector3(UnityEngine.Vector3 v) { content = v; }
    public EyeTrackingUnityVector3(EyeTrackingVector3 other) { content.x = other.content.X; content.y = other.content.Y; content.z = other.content.Z; }

    public new Type GetType() { return typeof(EyeTrackingUnityVector3); }
    public void Write(BinaryWriter writer) { writer.Write(content.x); writer.Write(content.y); writer.Write(content.z); }
    public IEyeTracking Read(BinaryReader reader) { return new EyeTrackingUnityVector3(Vector3.down); }
    public EyeTrackingVector3 ToEyeTrackingVector3() { return new EyeTrackingVector3(content.x, content.y, content.z); }
}


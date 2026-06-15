
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using SAAC.PsiFormats;
using System;

public class PsiExporterEyeTracking : PsiExporter<Tuple<System.Numerics.Vector3, System.Numerics.Vector3>>
{
    [Header("Input Actions (from XRI Input Action Asset)")]
    [SerializeField] private InputActionReference _eyeGazePositionAction;
    [SerializeField] private InputActionReference _eyeGazeRotationAction;
    [SerializeField] private InputActionReference _eyeGazeTrackingStateAction;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        base.Start();
    }

    // Update is called once per frame
    void Update()
    {
        if (!CanSend() || _eyeGazePositionAction == null || _eyeGazeRotationAction == null || _eyeGazeTrackingStateAction == null
           || (_eyeGazeTrackingStateAction.action.ReadValue<int>() & 1) == 0)
            return;

        Vector3 eyePosition = LocalVRPlayerInfos.Instance.XROriginPos + LocalVRPlayerInfos.Instance.XROriginRot * _eyeGazePositionAction.action.ReadValue<Vector3>();
        Quaternion eyeRotation = _eyeGazeRotationAction.action.ReadValue<Quaternion>() * LocalVRPlayerInfos.Instance.XROriginRot;
        Vector3 euler = eyeRotation.eulerAngles;

        Out.Post(new (new System.Numerics.Vector3(eyePosition.x, eyePosition.y, eyePosition.z), new System.Numerics.Vector3(euler.x, euler.y, euler.z)), Timestamp);
    }
   
#if PSI_TCP_STREAMS
    protected override Microsoft.Psi.Interop.Serialization.IFormatSerializer<Tuple<System.Numerics.Vector3, System.Numerics.Vector3>> GetSerializer()
    {
        return PsiFormatTupleOfVector.GetFormat();
    }
#endif
}

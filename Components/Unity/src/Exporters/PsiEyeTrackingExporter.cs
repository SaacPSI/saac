
using UnityEngine;
using UnityEngine.XR.OpenXR.Features.Interactions;
using UnityEngine.XR;
using System.Collections.Generic;
using SAAC.PsiFormats;

public class EyeTrackingExporter : PsiExporter<System.Numerics.Matrix4x4>
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    private InputDevice? _inputDevice = null;

    void Start()
    {
        base.Start();
    }

    // Update is called once per frame
    void Update()
    {
        if (_inputDevice is null)
        {
            return;
        }
        var inputDevice = (InputDevice)_inputDevice; 
        if (CanSend() && inputDevice.isValid) 
        {
            //if (inputDevice.TryGetFeatureValue(CommonUsages.eyesData, out Eyes eyes))
            //{
            //    eyes.TryGetLeftEyePosition(out Vector3 leftEyePosition);
            //    Debug.Log($"Left Eye Position : {leftEyePosition}");
            //    eyes.TryGetLeftEyeRotation(out Quaternion leftEyeRotation);
            //    Debug.Log($"Left Eye Rotation : {leftEyeRotation}");
            //    eyes.TryGetLeftEyeOpenAmount(out float leftEyeOpenAmount);
            //    Debug.Log($"Left Eye Open Amount : {leftEyeOpenAmount}");
            //    eyes.TryGetRightEyePosition(out Vector3 rightEyePosition);
            //    Debug.Log($"Right Eye Position : {rightEyePosition}");
            //    eyes.TryGetRightEyeRotation(out Quaternion rightEyeRotation);
            //    Debug.Log($"Right Eye Rotation : {rightEyeRotation}");
            //    eyes.TryGetRightEyeOpenAmount(out float rightEyeOpenAmount);
            //    Debug.Log($"Right Eye Open Amount : {rightEyeOpenAmount}");
            //}
            if (inputDevice.TryGetFeatureValue(EyeTrackingUsages.gazePosition, out Vector3 position) && inputDevice.TryGetFeatureValue(EyeTrackingUsages.gazeRotation, out Quaternion quaternion))
            {
                Out.Post(Matrix4x4(position, quaternion), Timestamp);
            }
        }
    }
    protected void Awake()
    {
        // Check if we have eye tracking support
        List<InputDevice> inputDeviceList = new List<InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.EyeTracking, inputDeviceList);
        if (inputDeviceList.Count > 0)
        {
            Debug.Log("Eye tracking device found!", this);
            return;
        }

        foreach (var device in inputDeviceList)
        {
            if (device.isValid)
            {
                Debug.Log("Eye gaze device found!", this);
                _inputDevice = device;
                return;
            }
        }

        Debug.LogWarning($"Could not find a device that supports eye tracking on Awake. {this} has subscribed to device connected events and will activate the GameObject when an eye tracking device is connected.", this);

        InputDevices.deviceConnected += OnDeviceConnected;
    }

    /// <summary>
    /// See <see cref="MonoBehaviour"/>.
    /// </summary>
    protected void OnDestroy()
    {
        InputDevices.deviceConnected -= OnDeviceConnected;
    }

    void OnDeviceConnected(InputDevice inputDevice)
    {
        if (_inputDevice != null || !inputDevice.characteristics.HasFlag(InputDeviceCharacteristics.EyeTracking))
        {
            return;
        }

        Debug.Log("Eye tracking device found!", this);
        _inputDevice = inputDevice;
    }

    public static System.Numerics.Matrix4x4 Matrix4x4(Vector3 position, Quaternion q)
    {
        // Extract quaternion components
        double w = q.w;
        double x = q.x;
        double y = q.y;
        double z = q.z;

        // Compute rotation matrix elements
        double xx = x * x;
        double yy = y * y;
        double zz = z * z;
        double xy = x * y;
        double xz = x * z;
        double yz = y * z;
        double wx = w * x;
        double wy = w * y;
        double wz = w * z;

        System.Numerics.Matrix4x4 matrix = new System.Numerics.Matrix4x4();

        matrix.M11 = (float)(1 - 2 * (yy + zz));
        matrix.M12 = (float)(2 * (xy - wz));
        matrix.M13 = (float)(2 * (xz + wy));
        matrix.M14 = (float)(position.x);

        matrix.M21 = (float)(2 * (xy + wz));
        matrix.M22 = (float)(1 - 2 * (xx + zz));
        matrix.M23 = (float)(2 * (yz - wx));
        matrix.M24 = (float)(position.y);

        matrix.M31 = (float)(2 * (xz - wy));
        matrix.M32 = (float)(2 * (yz + wx));
        matrix.M33 = (float)(1 - 2 * (xx + yy));
        matrix.M34 = (float)(position.z);

        matrix.M41 = 0;
        matrix.M42 = 0;
        matrix.M43 = 0;
        matrix.M44 = 1;

        return matrix;
    }

#if PSI_TCP_STREAMS
    protected override Microsoft.Psi.Interop.Serialization.IFormatSerializer<System.Numerics.Matrix4x4> GetSerializer()
    {
        return PsiFormatMatrix4x4.GetFormat();
    }
#endif
}

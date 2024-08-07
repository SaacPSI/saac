﻿using nuitrack;
using nuitrack.device;
using Microsoft.Psi.DeviceManagement;

namespace SAAC.Nuitrack
{
    /// <summary>
    /// Internal Nuitrack component class.
    /// </summary>
    internal sealed class NuitrackCore : IDisposable
    {
        static private NuitrackCore? instance = null;

        private static List<CameraDeviceInfo>? allDevices = null;

        private bool nuitrackInit = false;
        private bool nuitrackRelease = false;
        private Thread? CaptureThread = null;

        private Dictionary<string, ColorSensor> colorSensors = new Dictionary<string, ColorSensor>();
        private Dictionary<string, DepthSensor> DepthSensors = new Dictionary<string, DepthSensor>();
        private Dictionary<string, SkeletonTracker> SkeletonTrackers = new Dictionary<string, SkeletonTracker>();
        private Dictionary<string, HandTracker> handTrackers = new Dictionary<string, HandTracker>();
        private Dictionary<string, UserTracker> userTrackers = new Dictionary<string, UserTracker>();
        private Dictionary<string, GestureRecognizer> gestureRecognizers = new Dictionary<string, GestureRecognizer>();
        private List<NuitrackDevice> devices = new List<NuitrackDevice>();
        private List<Tuple<NuitrackSensorConfiguration, NuitrackSensor>> configurations = new List<Tuple<NuitrackSensorConfiguration, NuitrackSensor>>();
        private List<Module> waitingModule = new List<Module>();


        private bool isStarted = false;
        private bool shutdown = false;

        /// <summary>
        /// The underlying Nuitrack device.
        /// </summary>
        /// 
        private readonly object cameraOpenLock = new object();

        static public ref NuitrackCore GetNuitrackCore()
        {
            if(instance == null)
                instance = new NuitrackCore();
            return ref instance;
        }

        private NuitrackCore()
        {     
        }

        public void RegisterSensor(NuitrackSensorConfiguration configuration, NuitrackSensor sensor)
        {
            configurations.Add(new Tuple<NuitrackSensorConfiguration, NuitrackSensor>(configuration, sensor));
        }

        /// <summary>
        /// Returns the number of Kinect for Azure devices available on the system.
        /// </summary>
        /// <returns>Number of available devices.</returns>
        public int GetInstalledCount()
        {
            return devices.Count;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (devices.Count > 0)
            {
                Release();
                devices.Clear();
            }
        }

        public Vector3 toProj(Vector3 point, string sensor)
        {
            if (DepthSensors.ContainsKey(sensor))
                return DepthSensors[sensor].ConvertRealToProjCoords(point);
            return point;
        }

        /// <inheritdoc/>
        public bool Start(Action<DateTime> notifyCompletionTime)
        {
            // notify that this is an infinite source component
            notifyCompletionTime(DateTime.MaxValue);
            if (isStarted)
                return false;
            isStarted = true;

            // Prevent device open race condition.
            lock (cameraOpenLock)
            {
                Initialize();
                List<NuitrackDevice> devices = nuitrack.Nuitrack.GetDeviceList();
                foreach(var pair in configurations)
                {
                    NuitrackDevice? found = null;
                    foreach (NuitrackDevice device in devices)
                    {
                        if (pair.Item1.DeviceSerialNumber != device.GetInfo(DeviceInfoType.SERIAL_NUMBER))
                            continue;
                        found = device;
                        break;
                    }
                    if(found == null)
                        throw new ArgumentException("Failed to retrieve device: "+ pair.Item1.DeviceSerialNumber + "!");
                    nuitrack.Nuitrack.SetDevice(found);
                    this.devices.Add(found);
                    try
                    {
                        Module? WaitingObject = null;
                        // activate selected device
                        bool isActivated = Convert.ToBoolean(found.GetActivationStatus());
                        if (!isActivated)
                        {
                            found.Activate(pair.Item1.ActivationKey);
                            if (!Convert.ToBoolean(found.GetActivationStatus()))
                                throw new ArgumentException("Invalid activation key: " + pair.Item1.DeviceSerialNumber);
                        }

                        if (pair.Item1.OutputColor)
                        {
                            var colorSensor = ColorSensor.Create();
                            colorSensor.OnUpdateEvent += pair.Item2.onColorSensorUpdate;
                            colorSensors.Add(pair.Item1.DeviceSerialNumber, colorSensor);
                            WaitingObject = colorSensor;
                        }

                        if (pair.Item1.OutputDepth)
                        {
                            var depthSensor = DepthSensor.Create();
                            depthSensor.OnUpdateEvent += pair.Item2.onDepthSensorUpdate;
                            DepthSensors.Add(pair.Item1.DeviceSerialNumber, depthSensor);
                            WaitingObject = depthSensor;
                        }

                        if (pair.Item1.OutputSkeletonTracking)
                        {
                            var skeletonTracker = SkeletonTracker.Create();
                            skeletonTracker.SetAutoTracking(true);
                            skeletonTracker.OnSkeletonUpdateEvent += pair.Item2.onSkeletonUpdate;
                            SkeletonTrackers.Add(pair.Item1.DeviceSerialNumber, skeletonTracker);
                            WaitingObject = skeletonTracker;
                        }

                        if (pair.Item1.OutputHandTracking)
                        {
                            var handTracker = HandTracker.Create();
                            handTracker.OnUpdateEvent += pair.Item2.onHandUpdate;
                            handTrackers.Add(pair.Item1.DeviceSerialNumber, handTracker);
                        }

                        if (pair.Item1.OutputUserTracking)
                        {
                            var userTracker = UserTracker.Create();
                            userTracker.OnUpdateEvent += pair.Item2.onUserUpdate;
                            userTrackers.Add(pair.Item1.DeviceSerialNumber, userTracker);
                        }

                        if (pair.Item1.OutputGestureRecognizer)
                        {
                            var gestureRecognizer = GestureRecognizer.Create();
                            gestureRecognizer.OnUpdateEvent += pair.Item2.onGestureUpdate;
                            gestureRecognizers.Add(pair.Item1.DeviceSerialNumber, gestureRecognizer);
                        }
                        if (WaitingObject != null)
                            waitingModule.Add(WaitingObject);
                    }
                    catch (nuitrack.Exception exception)
                    {
                        throw new ArgumentException("Invalid operation: " + exception.ToString());
                    }
                }
                nuitrack.Nuitrack.Run();
                CaptureThread = new Thread(new ThreadStart(CaptureThreadProc));
                CaptureThread.Start();
            }
            return true;
        }

        /// <inheritdoc/>
        public bool Stop(DateTime finalOriginatingTime, Action notifyCompleted)
        {
            if (shutdown)
                return false;
            shutdown = true;
            Release();
            TimeSpan waitTime = TimeSpan.FromSeconds(1);
            if (CaptureThread != null && CaptureThread.Join(waitTime) != true)
                CaptureThread.Abort();
            notifyCompleted();
            return true;
        }

        private void CaptureThreadProc()
        {
            if(waitingModule.Count == 0)
                throw new ArgumentException("No tracker available");
            while (!shutdown)
            {
                foreach(var waitingObject in waitingModule)
                    nuitrack.Nuitrack.WaitUpdate(waitingObject);
            }
        }

        private void Initialize()
        {
            if (nuitrackInit)
                return;
            nuitrackInit = true;
            nuitrack.Nuitrack.Init();
        }

        private void Release()
        {
            if (nuitrackRelease)
                return;
            nuitrackRelease = true;
            nuitrack.Nuitrack.Release();
        }

        private static List<CameraDeviceInfo.Sensor.ModeInfo> getVideoModes(List<nuitrack.device.VideoMode> videoModes)
        {
            List<CameraDeviceInfo.Sensor.ModeInfo> modes = new List<CameraDeviceInfo.Sensor.ModeInfo>();
            foreach (var videoMode in videoModes)
            {
                modes.Add(new CameraDeviceInfo.Sensor.ModeInfo
                {
                    Format = Microsoft.Psi.Imaging.PixelFormat.BGRA_32bpp,
                    FrameRateNumerator = (uint)videoMode.fps,
                    FrameRateDenominator = 1,
                    ResolutionWidth = (uint)videoMode.width,
                    ResolutionHeight = (uint)videoMode.height,
                });
            }
            return modes;
        }

        /// <summary>
        /// Gets a list of all available capture devices.
        /// </summary>
        public static IEnumerable<CameraDeviceInfo> AllDevices
        {
            get
            {
                if (allDevices == null)
                {
                    nuitrack.Nuitrack.Init("");
                    allDevices = new List<CameraDeviceInfo>();
                    List<NuitrackDevice> listing = nuitrack.Nuitrack.GetDeviceList();
                    int numDevices = 0;
                    foreach (NuitrackDevice nuiDi in listing)
                    {
                        CameraDeviceInfo di = new CameraDeviceInfo();
                        di.SerialNumber = nuiDi.GetInfo(nuitrack.device.DeviceInfoType.SERIAL_NUMBER);
                        di.FriendlyName = nuiDi.GetInfo(nuitrack.device.DeviceInfoType.DEVICE_NAME) + " - " + di.SerialNumber;
                        di.Sensors = new List<CameraDeviceInfo.Sensor>();
                        di.DeviceId = numDevices++;
                        CameraDeviceInfo.Sensor sensor = new CameraDeviceInfo.Sensor();
                        sensor.Modes = new List<CameraDeviceInfo.Sensor.ModeInfo>();

                        for (int k = 0; k < (int)nuitrack.device.StreamType.Count; k++)
                        {
                            var videoModes = getVideoModes(nuiDi.GetAvailableVideoModes((nuitrack.device.StreamType)k));
                            foreach (var videoMode in videoModes)
                                sensor.Modes.Add(videoMode);
                        }
                        di.Sensors.Add(sensor);
                        allDevices.Add(di);
                    }
                    nuitrack.Nuitrack.Release();
                }
                return allDevices;
            }
        }
    }
}

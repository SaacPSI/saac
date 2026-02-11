// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.Nuitrack
{
    using Microsoft.Psi.DeviceManagement;
    using nuitrack;
    using nuitrack.device;

    /// <summary>
    /// Internal Nuitrack component class.
    /// </summary>
    internal sealed class NuitrackCore : IDisposable
    {
        private static NuitrackCore? instance = null;
        private static List<CameraDeviceInfo>? allDevices = null;

        private readonly object cameraOpenLock = new object();
        private bool nuitrackInit = false;
        private bool nuitrackRelease = false;
        private Thread? captureThread = null;

        private Dictionary<string, ColorSensor> colorSensors = new Dictionary<string, ColorSensor>();
        private Dictionary<string, DepthSensor> depthSensors = new Dictionary<string, DepthSensor>();
        private Dictionary<string, SkeletonTracker> skeletonTrackers = new Dictionary<string, SkeletonTracker>();
        private Dictionary<string, HandTracker> handTrackers = new Dictionary<string, HandTracker>();
        private Dictionary<string, UserTracker> userTrackers = new Dictionary<string, UserTracker>();
        private Dictionary<string, GestureRecognizer> gestureRecognizers = new Dictionary<string, GestureRecognizer>();
        private List<NuitrackDevice> devices = new List<NuitrackDevice>();
        private List<Tuple<NuitrackSensorConfiguration, NuitrackSensor>> configurations = new List<Tuple<NuitrackSensorConfiguration, NuitrackSensor>>();
        private List<Module> waitingModule = new List<Module>();

        private bool isStarted = false;
        private bool shutdown = false;

        private NuitrackCore()
        {
        }

        /// <summary>
        /// Gets the singleton instance of NuitrackCore.
        /// </summary>
        /// <returns>Reference to the NuitrackCore instance.</returns>
        public static ref NuitrackCore GetNuitrackCore()
        {
            if (instance == null)
            {
                instance = new NuitrackCore();
            }

            return ref instance;
        }

        /// <summary>
        /// Registers a sensor configuration with this core instance.
        /// </summary>
        /// <param name="configuration">The sensor configuration.</param>
        /// <param name="sensor">The sensor instance.</param>
        public void RegisterSensor(NuitrackSensorConfiguration configuration, NuitrackSensor sensor)
        {
            this.configurations.Add(new Tuple<NuitrackSensorConfiguration, NuitrackSensor>(configuration, sensor));
        }

        /// <summary>
        /// Returns the number of Kinect for Azure devices available on the system.
        /// </summary>
        /// <returns>Number of available devices.</returns>
        public int GetInstalledCount()
        {
            return this.devices.Count;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (this.devices.Count > 0)
            {
                this.Release();
                this.devices.Clear();
            }
        }

        /// <summary>
        /// Converts real world coordinates to projection coordinates for the specified sensor.
        /// </summary>
        /// <param name="point">The 3D point in real world coordinates.</param>
        /// <param name="sensor">The sensor identifier.</param>
        /// <returns>The converted projection coordinates.</returns>
        public Vector3 ToProj(Vector3 point, string sensor)
        {
            if (this.depthSensors.ContainsKey(sensor))
            {
                return this.depthSensors[sensor].ConvertRealToProjCoords(point);
            }

            return point;
        }

        /// <summary>
        /// Starts the Nuitrack sensor capture.
        /// </summary>
        /// <param name="notifyCompletionTime">Delegate to notify completion time.</param>
        /// <returns>True if started successfully; otherwise false.</returns>
        public bool Start(Action<DateTime> notifyCompletionTime)
        {
            // notify that this is an infinite source component
            notifyCompletionTime(DateTime.MaxValue);
            if (this.isStarted)
            {
                return false;
            }

            this.isStarted = true;

            // Prevent device open race condition.
            lock (this.cameraOpenLock)
            {
                this.Initialize();
                List<NuitrackDevice> devices = nuitrack.Nuitrack.GetDeviceList();
                foreach (var pair in this.configurations)
                {
                    NuitrackDevice? found = null;
                    foreach (NuitrackDevice device in devices)
                    {
                        if (pair.Item1.DeviceSerialNumber != device.GetInfo(DeviceInfoType.SERIAL_NUMBER))
                        {
                            continue;
                        }

                        found = device;
                        break;
                    }

                    if (found == null)
                    {
                        throw new ArgumentException("Failed to retrieve device: " + pair.Item1.DeviceSerialNumber + "!");
                    }

                    nuitrack.Nuitrack.SetDevice(found);
                    this.devices.Add(found);
                    try
                    {
                        Module? waitingObject = null;

                        // activate selected device
                        bool isActivated = Convert.ToBoolean(found.GetActivationStatus());
                        if (!isActivated)
                        {
                            found.Activate(pair.Item1.ActivationKey);
                            if (!Convert.ToBoolean(found.GetActivationStatus()))
                            {
                                throw new ArgumentException("Invalid activation key: " + pair.Item1.DeviceSerialNumber);
                            }
                        }

                        if (pair.Item1.OutputColor)
                        {
                            var colorSensor = ColorSensor.Create();
                            colorSensor.OnUpdateEvent += pair.Item2.OnColorSensorUpdate;
                            this.colorSensors.Add(pair.Item1.DeviceSerialNumber, colorSensor);
                            waitingObject = colorSensor;
                        }

                        if (pair.Item1.OutputDepth)
                        {
                            var depthSensor = DepthSensor.Create();
                            depthSensor.OnUpdateEvent += pair.Item2.OnDepthSensorUpdate;
                            this.depthSensors.Add(pair.Item1.DeviceSerialNumber, depthSensor);
                            waitingObject = depthSensor;
                        }

                        if (pair.Item1.OutputSkeletonTracking)
                        {
                            var skeletonTracker = SkeletonTracker.Create();
                            skeletonTracker.SetAutoTracking(true);
                            skeletonTracker.OnSkeletonUpdateEvent += pair.Item2.OnSkeletonUpdate;
                            this.skeletonTrackers.Add(pair.Item1.DeviceSerialNumber, skeletonTracker);
                            waitingObject = skeletonTracker;
                        }

                        if (pair.Item1.OutputHandTracking)
                        {
                            var handTracker = HandTracker.Create();
                            handTracker.OnUpdateEvent += pair.Item2.OnHandUpdate;
                            this.handTrackers.Add(pair.Item1.DeviceSerialNumber, handTracker);
                        }

                        if (pair.Item1.OutputUserTracking)
                        {
                            var userTracker = UserTracker.Create();
                            userTracker.OnUpdateEvent += pair.Item2.OnUserUpdate;
                            this.userTrackers.Add(pair.Item1.DeviceSerialNumber, userTracker);
                        }

                        if (pair.Item1.OutputGestureRecognizer)
                        {
                            var gestureRecognizer = GestureRecognizer.Create();
                            gestureRecognizer.OnUpdateEvent += pair.Item2.OnGestureUpdate;
                            this.gestureRecognizers.Add(pair.Item1.DeviceSerialNumber, gestureRecognizer);
                        }

                        if (waitingObject != null)
                        {
                            this.waitingModule.Add(waitingObject);
                        }
                    }
                    catch (nuitrack.Exception exception)
                    {
                        throw new ArgumentException("Invalid operation: " + exception.ToString());
                    }
                }

                nuitrack.Nuitrack.Run();
                this.captureThread = new Thread(new ThreadStart(this.CaptureThreadProc));
                this.captureThread.Start();
            }

            return true;
        }

        /// <summary>
        /// Stops the Nuitrack capture and releases resources.
        /// </summary>
        /// <param name="finalOriginatingTime">The final originating time.</param>
        /// <param name="notifyCompleted">Delegate to notify completion.</param>
        /// <returns>True if stopped successfully; otherwise false.</returns>
        public bool Stop(DateTime finalOriginatingTime, Action notifyCompleted)
        {
            if (this.shutdown)
            {
                return false;
            }

            this.shutdown = true;
            this.Release();
            TimeSpan waitTime = TimeSpan.FromSeconds(1);
            if (this.captureThread != null && this.captureThread.Join(waitTime) != true)
            {
                this.captureThread.Abort();
            }

            notifyCompleted();
            return true;
        }

        /// <summary>
        /// Thread procedure for capturing Nuitrack data updates.
        /// </summary>
        private void CaptureThreadProc()
        {
            if (this.waitingModule.Count == 0)
            {
                throw new ArgumentException("No tracker available");
            }

            while (!this.shutdown)
            {
                foreach (var waitingObject in this.waitingModule)
                {
                    nuitrack.Nuitrack.WaitUpdate(waitingObject);
                }
            }
        }

        /// <summary>
        /// Initializes the Nuitrack library.
        /// </summary>
        private void Initialize()
        {
            if (this.nuitrackInit)
            {
                return;
            }

            this.nuitrackInit = true;
            nuitrack.Nuitrack.Init();
        }

        /// <summary>
        /// Releases the Nuitrack library resources.
        /// </summary>
        private void Release()
        {
            if (this.nuitrackRelease)
            {
                return;
            }

            this.nuitrackRelease = true;
            nuitrack.Nuitrack.Release();
        }

        /// <summary>
        /// Converts Nuitrack video modes to camera device mode information.
        /// </summary>
        /// <param name="videoModes">The list of Nuitrack video modes.</param>
        /// <returns>The list of converted mode information.</returns>
        private static List<CameraDeviceInfo.Sensor.ModeInfo> GetVideoModes(List<nuitrack.device.VideoMode> videoModes)
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
                    nuitrack.Nuitrack.Init(string.Empty);
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
                            var videoModes = GetVideoModes(nuiDi.GetAvailableVideoModes((nuitrack.device.StreamType)k));
                            foreach (var videoMode in videoModes)
                            {
                                sensor.Modes.Add(videoMode);
                            }
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

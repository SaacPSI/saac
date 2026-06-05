using System.Collections.Generic;
using UnityEngine;
using Microsoft.Psi.Data;
using UnityEngine.LightTransport;
using UnityEngine.UIElements;



#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using System.Linq;
#endif

namespace Bhaptics.SDK2
{
    /// <summary>
    /// Contains the functions for using haptic devices from bHaptics.
    /// </summary>
    public class BhapticsLibrary
    {
        public delegate void PsiHapticPlay(string eventId, int requestId, int startMillis, float intensity, float duration, float angleX, float offsetY, int count = 0);
        public static PsiHapticPlay OnHapticPlay;

        public delegate void PsiMotorPlay(int position, int requestId, int[] motors, int durationMillis);
        public static PsiMotorPlay OnMotorPlay;
       
        public delegate void PsiPauseResumeStop(string eventId, int state);
        public static PsiPauseResumeStop OnPauseResumeStop;

        private static readonly object Lock = new object();
        private static readonly List<HapticDevice> EmptyDevices = new List<HapticDevice>();

        private static AndroidHaptic android = null;
        private static bool _initialized = false;
        private static bool isAvailable = false;
        private static bool isAvailableChecked = false;

        
        private static bool enableUniversalConf = true;
        private static RuntimePlatform[] excludeUniversalPlatforms =
        {
            RuntimePlatform.Android,
            RuntimePlatform.WindowsPlayer,
        };

        public static bool enableUniversal = UniversalEnabled();

        private static Universal.BhapticsTcpClient _client = new Universal.BhapticsTcpClient();


        private static bool UniversalEnabled()
        {
            if (!enableUniversalConf)
            {
                return false;
            }
            
            foreach (var excludeUniversalPlatform in excludeUniversalPlatforms)
            {
                if (Application.platform == excludeUniversalPlatform)
                {
                    return false;
                }
            }

            return true;
        }


        public static bool IsBhapticsAvailable(bool isAutoRunPlayer)
        {
            if (isAvailableChecked)
            {
                return isAvailable;
            }

            return IsBhapticsAvailableForce(isAutoRunPlayer);
        }

        public static bool IsBhapticsAvailableForce(bool isAutoRunPlayer)
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                if (android == null)
                {
                    BhapticsLogManager.LogErrorFormat("IsBhapticsAvailable() android object not initialized.");
                    isAvailable = false;
                    return isAvailable;
                }

                android.RefreshPairing();
                isAvailable = android.CheckBhapticsAvailable();
                isAvailableChecked = true;
                return isAvailable;
            }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            if (!bhaptics_library.isPlayerInstalled())
            {
                isAvailable = false;
                isAvailableChecked = true;
                return isAvailable;
            }

            if (!bhaptics_library.isPlayerRunning() && isAutoRunPlayer)
            {
                BhapticsLogManager.LogFormat("bHaptics Player(PC) is not running, so try launch it.");
                bhaptics_library.launchPlayer(true);
            }

#endif
            isAvailable = true;
            isAvailableChecked = true;

            return isAvailable;
        }



        public static bool Initialize(string appId, string apiKey, string json, bool autoRequestBluetoothPermission = true)
        {
            lock (Lock)
            {
                if (_initialized)
                {
                    return false;
                }
                _initialized = true;
            }

            if (enableUniversal)
            {
                _client.Initialize(appId, apiKey, json);
            }

            if (Application.platform == RuntimePlatform.Android)
            {
                if (android == null)
                {
                    BhapticsLogManager.Log("BhapticsLibrary - Initialize ");
                    android = new AndroidHaptic();
                    android.InitializeWithPermission(appId, apiKey, json, autoRequestBluetoothPermission);
                    _initialized = true;
                    return true;
                }

                return false;
            }
            
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            
            if (bhaptics_library.wsIsConnected())
            {
                BhapticsLogManager.Log("BhapticsLibrary - connection already opened");
                //return false;       // NOTE-230117      Temporary comment out for IL2CPP
            }

            BhapticsLogManager.LogFormat("BhapticsLibrary - Initialize() {0} {1}", apiKey, appId);
            return bhaptics_library.registryAndInit(apiKey, appId, json);
#else
            return false;
#endif
        }

        public static void Destroy()
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                if (android != null)
                {
                    android.Dispose();
                    android = null;
                }
            }
            
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            BhapticsLogManager.LogFormat("Destroy()");
            bhaptics_library.wsClose();
#endif

            if (enableUniversal)
            {
                _client.Destroy();
            }

            _initialized = false;
        }

        /// <summary>
        /// checks if the same device as the type in the parameter is connected.
        /// </summary>
        /// <param name="type">type of device</param>
        /// <returns>true if there are more than zero connected devices.</returns>
        public static bool IsConnect(PositionType type)
        {
            if (!isAvailable)
            {
                return false;
            }

            return GetConnectedDevices(type).Count > 0;
        }

        /// <summary>
        /// Play haptic event, with adjusting the strength, duration, and direction of the haptic.
        /// <para>eventId refers to the name of an event as defined on <a href="https://developer.bhaptics.com">developer.bhaptics.com</a></para>
        /// </summary>
        /// <param name="eventId">Name of haptic event which you want to play.</param>
        /// <param name="intensity">The haptic intensity is multiplied by this value.</param>
        /// <param name="duration">The haptic duration is multiplied by this value.</param>
        /// <param name="angleX">Rotate haptic counterclockwise around the global Vector3.up. Valid range is: [0.0f - 360.0f]</param>
        /// <param name="offsetY">Move haptic up or down. Valid range is: [-0.5f - 0.5f]</param>
        /// <returns>requestId. You can use the requestId to stop the haptic. It returns -1 if the return fails.</returns>
        public static int PlayParam(string eventId, float intensity = 1.0f, float duration = 1.0f, float angleX = 0.0f, float offsetY = 0.0f)
        {
            return Play(eventId, 0, intensity, duration, angleX, offsetY);
        }

        /// <summary>
        /// Play haptic event, with adjusting the direction of the haptic.
        /// <para>eventId refers to the name of an event as defined on <a href="https://developer.bhaptics.com">developer.bhaptics.com</a></para>
        /// </summary>
        /// <param name="eventId">Name of haptic event which you want to play.</param>
        /// <param name="angleX">Rotate haptic counterclockwise around the global Vector3.up. Valid range is: [0.0f - 360.0f]</param>
        /// <param name="offsetY">Move haptic up or down. Valid range is: [-0.5f - 0.5f]</param>
        /// <returns>requestId. You can use the requestId to stop the haptic. It returns -1 if the return fails.</returns>
        public static int PlayAngle(string eventId, float angleX, float offsetY)
        {
            return PlayParam(eventId, 1.0f, 1.0f, angleX, offsetY);
        }


        /// <summary>
        /// Play haptic event, with adjusting the strength, duration, and direction of the haptic.
        /// <para>eventId refers to the name of an event as defined on <a href="https://developer.bhaptics.com">developer.bhaptics.com</a></para>
        /// </summary>
        /// <param name="eventId">Name of haptic event which you want to play.</param>
        /// <param name="startMillis">[windows/android Only]The delay in milliseconds before the haptic event starts playing. The haptic will begin after this time has elapsed.</param>
        /// <param name="intensity">The haptic intensity is multiplied by this value.</param>
        /// <param name="duration">The haptic duration is multiplied by this value.</param>
        /// <param name="angleX">Rotate haptic counterclockwise around the global Vector3.up. Valid range is: [0.0f - 360.0f]</param>
        /// <param name="offsetY">Move haptic up or down. Valid range is: [-0.5f - 0.5f]</param>
        /// <returns>requestId. You can use the requestId to stop the haptic. It returns -1 if the return fails.</returns>
        public static int Play(string eventId, int startMillis = 0, float intensity = 1f, float duration = 1.0f, float angleX = 0.0f, float offsetY = 0.0f)
        {
            if (!isAvailable)
            {
                return -1;
            }

            if (eventId == null || eventId.Equals(string.Empty))
            {
                return -1;
            }

            if (startMillis < 0)
            {
                return -1;
            }
            int requestId = UnityEngine.Random.Range(1, int.MaxValue);

            if (enableUniversal)
            {
               _client.Play(eventId, requestId, startMillis, intensity, duration, angleX, offsetY);
            }

            if (Application.platform == RuntimePlatform.Android)
            {
                if (android != null)
                {
                    OnHapticPlay?.Invoke(eventId, requestId, startMillis, intensity, duration, angleX, offsetY);
                    return android.Play(eventId, requestId, startMillis, intensity, duration, angleX, offsetY);
                }

                return -1;
            }
            OnHapticPlay?.Invoke(eventId, requestId, startMillis, intensity, duration, angleX, offsetY);
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            return bhaptics_library.playWithStartTime(eventId, requestId, startMillis, intensity, duration, angleX, offsetY);
#else
            if (enableUniversal)
            {
                return requestId;
            }

            return -1;
#endif
        }

        /// <summary>
        /// Play the haptic repeatedly. Additionally, like the function PlayHapticWithOption, you can adjust the strength, duration, and direction of the haptic.
        /// <para>eventId refers to the name of an event as defined on <a href="https://developer.bhaptics.com">developer.bhaptics.com</a></para>
        /// </summary>
        /// <param name="eventId">Name of haptic event which you want to play.</param>
        /// <param name="intensity">The haptic intensity is multiplied by this value.</param>
        /// <param name="duration">The haptic duration is multiplied by this value.</param>
        /// <param name="angleX">Rotate haptic counterclockwise around the global Vector3.up. Valid range is: [0.0f - 360.0f]</param>
        /// <param name="offsetY">Move haptic up or down. Valid range is: [-0.5f - 0.5f]</param>
        /// <param name="interval">The time interval between loops, measured in milliseconds.</param>
        /// <param name="maxCount">The number of loops.</param>
        /// <returns>requestId. You can use the requestId to stop the haptic. It returns -1 if the return fails.</returns>
        public static int PlayLoop(string eventId, float intensity = 1.0f, float duration = 1.0f, float angleX = 0.0f, float offsetY = 0.0f, int interval = 200, int maxCount = 999999)
        {
            if (!isAvailable)
            {
                return -1;
            }

            if (eventId == null || eventId.Equals(string.Empty))
            {
                return -1;
            }
            
            int requestId = UnityEngine.Random.Range(1, int.MaxValue);
            
            if (enableUniversal)
            {
                _client.PlayLoop(eventId, requestId, intensity, duration, angleX, offsetY, interval, maxCount);
            }

            if (Application.platform == RuntimePlatform.Android)
            {
                if (android != null)
                {
                    OnHapticPlay?.Invoke(eventId, requestId, 0, intensity, duration, angleX, offsetY, maxCount);
                    return android.PlayLoop(eventId, requestId, intensity, duration, angleX, offsetY, interval, maxCount);
                }

                return -1;
            }
            OnHapticPlay?.Invoke(eventId, requestId, 0, intensity, duration, angleX, offsetY, maxCount);
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            return bhaptics_library.playLoop(eventId, requestId, intensity, duration, angleX, offsetY, interval, maxCount);
#else
            if (enableUniversal)
            {
                return requestId;
            }

            return -1;
#endif
        }

        /// <summary>
        /// Play the haptic repeatedly.
        /// <para>eventId refers to the name of an event as defined on <a href="https://developer.bhaptics.com">developer.bhaptics.com</a></para>
        /// </summary>
        /// <param name="eventId">Name of haptic event which you want to play.</param>
        /// <param name="interval">The time interval between loops, measured in milliseconds.</param>
        /// <param name="maxCount">The number of loops.</param>
        /// <returns>requestId. You can use the requestId to stop the haptic. It returns -1 if the return fails.</returns>
        public static int PlayLoopOnly(string eventId, int interval = 200, int maxCount = 999999)
        {
            return PlayLoop(eventId, 1.0f, 1.0f, 0.0f, 0.0f, interval, maxCount);
        }

        /// <summary>
        /// Play haptic feedback on the specific haptic actuator. You can use this function without creating an event.
        /// </summary>
        /// <param name="position">
        /// <para>type of device(int)</para>
        /// <para> 0 = <see cref="PositionType.Vest"/></para>
        /// <para> 1 = <see cref="PositionType.ForearmL"/> || 2 = <see cref="PositionType.ForearmR"/> || 3 = <see cref="PositionType.Head"/> || 4 = <see cref="PositionType.HandL"/> || 5 = <see cref="PositionType.HandR"/> </para>
        /// <para> 6 = <see cref="PositionType.FootL"/> || 7 = <see cref="PositionType.ForearmR"/> || 8 = <see cref="PositionType.GloveL"/> || 9 = <see cref="PositionType.GloveR"/> </para>
        /// </param>
        /// <param name="motors">Assign the length of the array by the number of motors for device. Values in the array means motors' intensity. Valid range for each value in the array is: [1 - 100]</param>
        /// <param name="durationMillis">The duration of haptic, measured in millisecond. Greater than or equal to 100 is recommended.</param>
        /// <returns>requestId. You can use the requestId to stop the haptic. It returns -1 if the return fails.</returns>
        public static int PlayMotors(int position, int[] motors, int durationMillis)
        {
            if (!isAvailable)
            {
                return -1;
            }
            
            int requestId = UnityEngine.Random.Range(1, int.MaxValue);
            
            if (enableUniversal)
            {
                _client.PlayMotors(position, requestId, motors, durationMillis);
            }

            if (Application.platform == RuntimePlatform.Android)
            {
                if (android != null)
                {
                    OnMotorPlay?.Invoke(position, requestId, motors, durationMillis);
                    return android.PlayMotors(position, motors, durationMillis);
                }

                return -1;
            }
            OnMotorPlay?.Invoke(position, requestId, motors, durationMillis);
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            return bhaptics_library.playDot(requestId, position, durationMillis, motors, motors.Length);
#else
            if (enableUniversal)
            {
                return requestId;
            }

            return -1;
#endif
        }

        /// <summary>
        /// Initiates a single motor vibration based on the specified parameters.
        /// <para>Note: This function incurs significant overhead and is recommended for simple tests only.
        /// For production or repeated calls, consider caching arrays and using the PlayMotor function to reduce overhead and enhance performance.</para>
        /// </summary>
        /// <param name="position">
        /// <para>type of device(int)</para>
        /// <para> 0 = <see cref="PositionType.Vest"/></para>
        /// <para> 1 = <see cref="PositionType.ForearmL"/> || 2 = <see cref="PositionType.ForearmR"/> || 3 = <see cref="PositionType.Head"/> || 4 = <see cref="PositionType.HandL"/> || 5 = <see cref="PositionType.HandR"/> </para>
        /// <para> 6 = <see cref="PositionType.FootL"/> || 7 = <see cref="PositionType.ForearmR"/> || 8 = <see cref="PositionType.GloveL"/> || 9 = <see cref="PositionType.GloveR"/> </para>
        /// </param>
        /// <param name="motorNumber">The number of the motor to vibrate</param>
        /// <param name="intensity">vibration intensity</param>
        /// <param name="durationMillis">The duration of haptic, measured in millisecond. Greater than or equal to 100 is recommended.</param>
        /// <returns>requestId. You can use the requestId to stop the haptic. It returns -1 if the return fails.</returns>
        public static int PlaySingleMotor(PositionType position, int motorNumber, int intensity, int durationMillis)
        {
            int[] devices;
            switch (position)
            {
                case PositionType.Vest: devices = new int[32]; break;
                case PositionType.ForearmL: devices = new int[3]; break;
                case PositionType.ForearmR: devices = new int[3]; break;
                case PositionType.Head: devices = new int[4]; break;
                case PositionType.HandL: devices = new int[3]; break;
                case PositionType.HandR: devices = new int[3]; break;
                case PositionType.FootL: devices = new int[3]; break;
                case PositionType.FootR: devices = new int[3]; break;
                case PositionType.GloveL: devices = new int[6]; break;
                case PositionType.GloveR: devices = new int[6]; break;

                default: Debug.LogError($"Invalid position type."); return -1;
            }
            devices[motorNumber] = intensity;


            return PlayMotors((int)position, devices, durationMillis);
        }

        /// <summary>
        /// TactGlove Only. Play haptics in TactGlove. Unlike using PlayMotors, you can finely adjust haptic duration and vibration intensity changes. This allows for even finer expression of haptic feedback.
        /// <para>Each array must have six elements, and at least one element is required to work.</para>
        /// </summary>
        /// <param name="positionType">
        /// <para>type of device</para>
        /// <para> 8 = <see cref="PositionType.GloveL"/> || 9 = <see cref="PositionType.GloveR"/> </para>
        /// </param>
        /// <param name="motorValues">
        /// <para> An array consisting of six elements, each representing the intensity of a motor.The array must have a length of six, as there are six motors in one TactGlove. Valid range for each value in the array is: [1 - 100] </para>
        /// <para> Index 0: Tip of the thumb </para>
        /// <para> Index 1: Tip of the index finger </para>
        /// <para> Index 2: Tip of the middle finger </para>
        /// <para> Index 3: Tip of the ring finger </para>
        /// <para> Index 4: Tip of the little finger </para>
        /// <para> Index 5: On the wrist </para>
        /// </param>
        /// <param name="playTimeValues">
        /// <para> An array consisting of six elements, each representing a time interval for actuation, with time defined using Bhaptics.SDK2.GlovePlayTime enums.</para>
        /// <para> (5ms : 1  /  10ms : 2  /  20ms : 4  /  30ms : 6  /  40ms : 8)</para>
        /// </param>
        /// <param name="shapeValues">
        /// <para>  An array consisting of six elements, each representing the forms of haptic intensity changes over time, specified by the Bhaptics.SDK2.GloveShapeValue enums. </para>
        /// <para> Constant : Constant intensity for the duration </para>
        /// <para> Decreasing : Starts at the specified intensity and decreases by half </para>
        /// <para> Increasing : Starts at half of the specified intensity and increases to the specified one. </para>
        /// </param>
        /// <returns>requestId. You can use the requestId to stop the haptic. It returns -1 if the return fails.</returns>
        public static int PlayWaveform(PositionType positionType, int[] motorValues, GlovePlayTime[] playTimeValues, GloveShapeValue[] shapeValues)
        {
            if (!isAvailable)
            {
                return -1;
            }

            if (motorValues.Length != 6 || playTimeValues.Length != 6 || shapeValues.Length != 6)
            {
                BhapticsLogManager.LogError("[bHaptics] BhapticsLibrary - PlayWaveform() 'motorValues, playTimeValues, shapeValues' necessarily require 6 values each.");
                return -1;
            }


            var playTimes = new int[playTimeValues.Length];
            var shapeVals = new int[shapeValues.Length];

            for (int i = 0; i < playTimes.Length; i++)
            {
                playTimes[i] = (int)playTimeValues[i];
            }
            for (int i = 0; i < shapeVals.Length; i++)
            {
                shapeVals[i] = (int)shapeValues[i];
            }
            
            int requestId = UnityEngine.Random.Range(1, int.MaxValue);

            if (enableUniversal)
            {
                _client.PlayWaveform((int)positionType, requestId, motorValues, playTimes, shapeVals);
            }

            if (Application.platform == RuntimePlatform.Android)
            {
                if (android != null)
                {
                    return android.PlayGlove((int)positionType, motorValues, playTimes, shapeVals);
                }
                return -1;
            }
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            return bhaptics_library.playWaveform(requestId, (int)positionType, motorValues, playTimes, shapeVals, 1, 6);
#else
            if (enableUniversal)
            {
                return requestId;
            }

            return -1;
#endif
        }

        /// <summary>
        /// <para>Play haptic around specific coordinates. Unlike PlayMotors function, which specifies the haptic intensity for each haptic actuator individually, this method specify the haptic intensity for particular coordinates.</para>
        /// <para>When specifying haptic position, PlayMotors offers discrete control, while PlayPath is more continuous. PlayMotors assigns intensity to individual actuators, whereas PlayPath allows intensity specification for specific coordinates (between 0 and 1 for both X and Y axis), causing nearby actuators to vibrate accordingly.</para>
        /// <para>You can put multiple coordinates with multiple intensities. Note that all actuators around these coordinates in the array will activate simultaneously (at the same time), not sequentially. Moreover, the size of all arrays must be the same.</para>
        /// <para>By continuously calling this function while gradually changing the values, you can achieve the effect of a moving haptic point.</para>
        /// </summary>
        /// <param name="position">
        /// <para>type of device(int)</para>
        /// <para> 0 = <see cref="PositionType.Vest"/></para>
        /// <para> 1 = <see cref="PositionType.ForearmL"/> || 2 = <see cref="PositionType.ForearmR"/> || 3 = <see cref="PositionType.Head"/> || 4 = <see cref="PositionType.HandL"/> || 5 = <see cref="PositionType.HandR"/> </para>
        /// <para> 6 = <see cref="PositionType.FootL"/> || 7 = <see cref="PositionType.ForearmR"/> || 8 = <see cref="PositionType.GloveL"/> || 9 = <see cref="PositionType.GloveR"/> </para>
        /// </param>
        /// <param name="xValues">Assign X coordinate. Valid range for each value in the array is: [0.0f - 1.0f]</param>
        /// <param name="yValues">Assign Y coordinate. Valid range for each value in the array is: [0.0f - 1.0f]</param>
        /// <param name="intensityValues">Assign the length of the array by the number of coordinate. Values in the array means coordinates' intensity. Valid range for each value in the array is: [1 - 100]</param>
        /// <param name="duration">The duration of haptic, measured in millisecond. Greater than or equal to 100 is recommended.</param>
        /// <returns>requestId. You can use the requestId to stop the haptic. It returns -1 if the return fails.</returns>
        public static int PlayPath(int position, float[] xValues, float[] yValues, int[] intensityValues, int duration)
        {
            if (!isAvailable)
            {
                return -1;
            }
            
            int requestId = UnityEngine.Random.Range(1, int.MaxValue);

            if (enableUniversal)
            {
                _client.PlayPath(position, requestId, xValues, yValues, intensityValues, duration);
            }

            if (Application.platform == RuntimePlatform.Android)
            {
                if (android != null)
                {
                    return android.PlayPath(position, xValues, yValues, intensityValues, duration);
                }

                return -1;
            }
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            return bhaptics_library.playPath(requestId, position, duration, xValues, yValues, intensityValues, xValues.Count());
#else
            if (enableUniversal)
            {
                return requestId;
            }

            return -1;
#endif
        }
        
        /// <summary>
        /// Pauses an active haptic event. Use PauseByEventId(string eventId) to pause a specific haptic event. The paused event can later be resumed using ResumeByEventId(string eventId).
        /// <para>eventId refers to the name of an event as defined on <a href="https://developer.bhaptics.com">developer.bhaptics.com</a></para>
        /// </summary>
        /// <param name="eventId">Name of haptic event which you want to pause.</param>
        public static void PauseByEventId(string eventId)
        {
            if (!isAvailable)
            {
                return;
            }

            if (eventId == null || eventId.Equals(string.Empty))
            {
                return;
            }

            if (enableUniversal)
            {
                _client.PausePlay(eventId);
            }

            if (Application.platform == RuntimePlatform.Android)
            {
                if (android != null)
                {
                    OnPauseResumeStop?.Invoke(eventId, 0);
                    android.Pause(eventId);
                }

                return;
            }
            OnPauseResumeStop?.Invoke(eventId, 0);
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            bhaptics_library.pause(eventId);
#endif
        }

        /// <summary>
        /// Resumes a paused haptic event. Use ResumeByEventId(string eventId) to resume playback from the position it was paused at.
        /// <para>eventId refers to the name of an event as defined on <a href="https://developer.bhaptics.com">developer.bhaptics.com</a></para>
        /// </summary>
        /// <param name="eventId">Name of haptic event which you want to resume.</param>
        public static void ResumeByEventId(string eventId)
        {
            if (!isAvailable)
            {
                return;
            }

            if (eventId == null || eventId.Equals(string.Empty))
            {
                return;
            }

            if (enableUniversal)
            {
                _client.ResumePlay(eventId);
            }

            if (Application.platform == RuntimePlatform.Android)
            {
                if (android != null)
                {
                    OnPauseResumeStop?.Invoke(eventId, 1);
                    android.Resume(eventId);
                }

                return;
            }
            OnPauseResumeStop?.Invoke(eventId, 1);
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            bhaptics_library.resume(eventId);
#endif
        }

        /// <summary>
        /// Stop the haptic event by Event ID.
        /// </summary>
        /// <param name="eventId">Name of haptic event which you want to stop.</param>
        /// <returns>Returns whether the stop was successful.</returns>
        public static bool StopByEventId(string eventId)
        {
            if (!isAvailable)
            {
                return false;
            }

            if (enableUniversal)
            {
                _client.StopByEventId(eventId);
            }

            if (Application.platform == RuntimePlatform.Android)
            {
                if (android != null)
                {
                    OnPauseResumeStop?.Invoke(eventId, 2);
                    return android.StopByEventId(eventId);
                }

                return false;
            }
            OnPauseResumeStop?.Invoke(eventId, 2);
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            return bhaptics_library.stopByEventId(eventId);
#else
            return false;
#endif
        }

        /// <summary>
        /// Stop the Haptic Event by using the Request ID from the return of the function that executes the haptic.
        /// </summary>
        /// <param name="requestId">Request ID of playing haptic event which you want to stop.</param>
        /// <returns>Returns whether the stop was successful.</returns>
        public static bool StopInt(int requestId)
        {
            if (!isAvailable)
            {
                return false;
            }

            if (enableUniversal)
            {
                _client.StopByRequestId(requestId);
            }

            if (Application.platform == RuntimePlatform.Android)
            {
                if (android != null)
                {
                    return android.StopByRequestId(requestId);
                }

                return false;
            }
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            return bhaptics_library.stop(requestId);
#else
            return false;
#endif
        }

        /// <summary>
        /// Stops all haptic currently playing. 
        /// </summary>
        /// <returns>Returns whether the stop was successful.</returns>
        public static bool StopAll()
        {
            if (!isAvailable)
            {
                return false;
            }

            if (enableUniversal)
            {
                _client.StopAll();
            }

            if (Application.platform == RuntimePlatform.Android)
            {
                if (android != null)
                {
                    return android.Stop();
                }

                return false;
            }
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            return bhaptics_library.stopAll();
#else
            return false;
#endif
        }

        /// <summary>
        /// Check if any haptic is currently playing.
        /// </summary>
        /// <returns>Whether the event is playing.</returns>
        public static bool IsPlaying()
        {
            if (!isAvailable)
            {
                return false;
            }


            if (enableUniversal)
            {
                // TODO ;
            }

            if (Application.platform == RuntimePlatform.Android)
            {
                if (android != null)
                {
                    return android.IsPlaying();
                }

                return false;
            }
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            return bhaptics_library.isPlaying();
#else
            return false;
#endif
        }

        /// <summary>
        /// Check if the haptic event for this Event ID is currently playing.
        /// </summary>
        /// <param name="eventId">Name of haptic event to check if it is currently playing.</param>
        /// <returns>Whether the event is playing.</returns>
        public static bool IsPlayingByEventId(string eventId)
        {
            if (!isAvailable)
            {
                return false;
            }


            if (enableUniversal)
            {
                // TODO ;
            }

            if (Application.platform == RuntimePlatform.Android)
            {
                if (android != null)
                {
                    return android.IsPlayingByEventId(eventId);
                }

                return false;
            }
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            return bhaptics_library.isPlayingByEventId(eventId);
#else
            return false;
#endif
        }

        /// <summary>
        /// Check if the haptic event for this Request ID is currently playing.
        /// </summary>
        /// <param name="requestId">Request ID of haptic event to check if it is currently playing.</param>
        /// <returns>Whether the event is playing.</returns>
        public static bool IsPlayingByRequestId(int requestId)
        {
            if (!isAvailable)
            {
                return false;
            }

            if (Application.platform == RuntimePlatform.Android)
            {
                if (android != null)
                {
                    return android.IsPlayingByRequestId(requestId);
                }

                return false;
            }
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            return bhaptics_library.isPlayingByRequestId(requestId);
#else
            return false;
#endif
        }

        public static List<HapticDevice> GetDevices()
        {
            if (!isAvailable)
            {
                return EmptyDevices;
            }
            

            if (Application.platform == RuntimePlatform.Android)
            {
                if (android != null)
                {
                    return android.GetDevices();
                }

                return EmptyDevices;
            }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            return bhaptics_library.GetDevices();
#else
            if (enableUniversal)
            {
                return _client.GetDevices();
            }

            return EmptyDevices;
#endif
            
            
        }

        public static List<HapticDevice> GetConnectedDevices(PositionType pos)
        {
            if (!isAvailable)
            {
                return EmptyDevices;
            }

            var pairedDeviceList = new List<HapticDevice>();
            var devices = GetDevices();
            foreach (var device in devices)
            {
                if (device.IsPaired && device.Position == pos && device.IsConnected)
                {
                    pairedDeviceList.Add(device);
                }
            }

            return pairedDeviceList;
        }

        public static List<HapticDevice> GetPairedDevices(PositionType pos)
        {
            if (!isAvailable)
            {
                return EmptyDevices;
            }

            var res = new List<HapticDevice>();
            var devices = GetDevices();
            foreach (var device in devices)
            {
                if (device.IsPaired && device.Position == pos)
                {
                    res.Add(device);
                }
            }

            return res;
        }

        public static void Ping(PositionType pos)
        {
            if (!isAvailable)
            {
                return;
            }

            var currentDevices = GetConnectedDevices(pos);

            foreach (var device in currentDevices)
            {
                Ping(device);
            }
        }

        public static void Ping(HapticDevice targetDevice)
        {
            if (!isAvailable)
            {
                return;
            }

            if (Application.platform == RuntimePlatform.Android)
            {
                if (android != null)
                {
                    android.Ping(targetDevice.Address);
                }

                return;
            }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            bhaptics_library.ping(targetDevice.Address);
#endif

            if (enableUniversal)
            {
                _client.Ping(targetDevice.Address);
            }
        }

        public static void PingAll()
        {
            if (!isAvailable)
            {
                return;
            }

            if (Application.platform == RuntimePlatform.Android)
            {
                if (android != null)
                {
                    android.PingAll();
                }

                return;
            }
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            bhaptics_library.pingAll();
#endif
            
            if (enableUniversal)
            {
                _client.PingAll();
            }
        }

        public static void TogglePosition(HapticDevice targetDevice)
        {
            if (!isAvailable)
            {
                return;
            }

            if (Application.platform == RuntimePlatform.Android)
            {
                if (android != null)
                {
                    android.TogglePosition(targetDevice.Address);
                }

                return;
            }
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            bhaptics_library.swapPosition(targetDevice.Address);
#endif
            if (enableUniversal)
            {
                _client.TogglePosition(targetDevice.Address);
            }
        }

        public static void OnApplicationFocus()
        {
            IsBhapticsAvailableForce(false);
        }

        public static void OnApplicationPause()
        {
            StopAll();
        }

        public static void OnApplicationQuit()
        {
            Destroy();
        }
    }
}

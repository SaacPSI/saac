namespace Nuitrack
{
    public class NuitrackSensorConfiguration
    {
        /// <summary>
        /// Gets or sets the serialNumber of the device to open.
        /// </summary>
        public string  DeviceSerialNumber { get; set; } = "";

        /// <summary>
        /// Gets or sets the nuitrack licence key of the device to open.
        /// </summary>
        public string ActivationKey { get; set; } = "";

        /// <summary>
        /// Gets or sets a value indicating whether the color stream is emitted.
        /// </summary>
        public bool OutputColor { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether the depth stream is emitted.
        /// </summary>
        public bool OutputDepth { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether the infrared stream is emitted.
        /// </summary>
        public bool OutputSkeletonTracking { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether the hand informations is emitted.
        /// </summary>
        public bool OutputHandTracking { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether the user tracking informations is emitted.
        /// </summary>
        public bool OutputUserTracking { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether the gesture streams is emitted.
        /// </summary>
        public bool OutputGestureRecognizer { get; set; } = false;
    }
}

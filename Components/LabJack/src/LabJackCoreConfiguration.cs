// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.LabJackComponent
{
    using LabJack.LabJackUD;

    /// <summary>
    /// Configuration settings for the LabJack device.
    /// </summary>
    public class LabJackCoreConfiguration
    {
        /// <summary>
        /// Defines the type of LabJack device.
        /// </summary>
        public enum LabJackType
        {
            /// <summary>
            /// LabJack U3 device.
            /// </summary>
            U3,

            /// <summary>
            /// LabJack U6 device.
            /// </summary>
            U6,

            /// <summary>
            /// LabJack UE9 device.
            /// </summary>
            UE9
        }

        /// <summary>
        /// Gets or sets the type of LabJack device to use.
        /// </summary>
        public LabJackType DeviceType { get; set; } = LabJackType.U6;

        /// <summary>
        /// Gets or sets the connection type (USB, Ethernet, etc.).
        /// </summary>
        public LJUD.CONNECTION ConnnectionType { get; set; } = LJUD.CONNECTION.USB;

        /// <summary>
        /// Gets or sets the device address (serial number or IP address).
        /// </summary>
        public string DeviceAdress { get; set; } = "0";

        /// <summary>
        /// Gets or sets a value indicating whether to connect to the first device found.
        /// </summary>
        public bool FirstDeviceFound { get; set; } = true;

        /// <summary>
        /// Gets or sets the commands to execute on the device.
        /// </summary>
        public Commands Commands { get; set; }
    }
}

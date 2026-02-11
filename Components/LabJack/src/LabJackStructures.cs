// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.LabJackComponent
{
    using LabJack.LabJackUD;

    /// <summary>
    /// Represents a PUT command to send to the LabJack device.
    /// </summary>
    public struct PutCommand
    {
        /// <summary>
        /// The I/O type for the command.
        /// </summary>
        public LJUD.IO IoType;

        /// <summary>
        /// The channel to use for the command.
        /// </summary>
        public LJUD.CHANNEL Channel;

        /// <summary>
        /// The value to set.
        /// </summary>
        public double Val;

        /// <summary>
        /// Additional parameter data.
        /// </summary>
        public byte[] X1;
    }

    /// <summary>
    /// Represents a REQUEST command to query the LabJack device.
    /// </summary>
    public struct RequestCommand
    {
        /// <summary>
        /// The I/O type for the request.
        /// </summary>
        public LJUD.IO IoType;

        /// <summary>
        /// The channel to query.
        /// </summary>
        public LJUD.CHANNEL Channel;

        /// <summary>
        /// The value parameter for the request.
        /// </summary>
        public double Val;

        /// <summary>
        /// Additional integer parameter.
        /// </summary>
        public int X1;

        /// <summary>
        /// User-defined data associated with the request.
        /// </summary>
        public double UserData;
    }

    /// <summary>
    /// Represents a RESPONSE command configuration.
    /// </summary>
    public struct ResponseCommand
    {
        /// <summary>
        /// Defines the getter type for retrieving responses.
        /// </summary>
        public enum EGetterType
        {
            /// <summary>
            /// Use GetFirstResult/GetNextResult pattern.
            /// </summary>
            First_Next,

            /// <summary>
            /// Use eGet method.
            /// </summary>
            E_Get
        }

        /// <summary>
        /// The getter type to use for retrieving responses.
        /// </summary>
        public EGetterType GetterType;
    }

    /// <summary>
    /// Encapsulates all commands to be sent to the LabJack device.
    /// </summary>
    public struct Commands
    {
        /// <summary>
        /// List of PUT commands to execute.
        /// </summary>
        public List<PutCommand> PutCommands;

        /// <summary>
        /// List of REQUEST commands to execute.
        /// </summary>
        public List<RequestCommand> RequestCommands;

        /// <summary>
        /// Configuration for retrieving responses.
        /// </summary>
        public ResponseCommand ResponseCommand;
    }
}

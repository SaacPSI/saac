// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.AudioRecording
{
    /// <summary>
    /// Represents a user with associated microphone and channel information for audio recording.
    /// </summary>
    public class User
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="User"/> class.
        /// </summary>
        /// <param name="id">The user identifier.</param>
        /// <param name="microphone">The microphone device name.</param>
        /// <param name="channel">The audio channel number.</param>
        public User(string id, string microphone, int channel)
        {
            this.Id = id;
            this.Microphone = microphone;
            this.Channel = channel;
        }

        /// <summary>
        /// Gets the user identifier.
        /// </summary>
        public string Id { get; private set; }

        /// <summary>
        /// Gets the microphone device name.
        /// </summary>
        public string Microphone { get; private set; }

        /// <summary>
        /// Gets the audio channel number.
        /// </summary>
        public int Channel { get; private set; }
    }
}

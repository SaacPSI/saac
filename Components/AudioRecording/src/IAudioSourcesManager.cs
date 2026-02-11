// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.AudioRecording
{
    using Microsoft.Psi;
    using Microsoft.Psi.Audio;

    /// <summary>
    /// Interface for managing audio sources.
    /// </summary>
    public interface IAudioSourcesManager
    {
        /// <summary>
        /// Gets a dictionary mapping user IDs to their audio streams.
        /// </summary>
        /// <returns>Dictionary of user IDs to audio buffer producers.</returns>
        Dictionary<string, IProducer<AudioBuffer>> GetDictonaryIdAudioStream();

        /// <summary>
        /// Stops the audio sources manager.
        /// </summary>
        void Stop();
    }
}

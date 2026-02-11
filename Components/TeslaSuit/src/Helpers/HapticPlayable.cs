// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.TeslaSuit
{
    /// <summary>
    /// Represents a playable haptic effect with an identifier and parameters.
    /// </summary>
    public struct HapticPlayable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HapticPlayable"/> struct.
        /// </summary>
        /// <param name="id">The unique identifier for the haptic effect.</param>
        /// <param name="hapticParams">The haptic parameters.</param>
        public HapticPlayable(ulong id, HapticParams hapticParams)
        {
            this.Id = id;
            this.HapticParams = hapticParams;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HapticPlayable"/> struct.
        /// </summary>
        /// <param name="id">The unique identifier for the haptic effect.</param>
        /// <param name="frequency">The frequency of the haptic feedback.</param>
        /// <param name="amplitude">The amplitude of the haptic feedback.</param>
        /// <param name="pulseWidth">The pulse width of the haptic feedback.</param>
        /// <param name="duration">The duration of the haptic feedback in milliseconds.</param>
        public HapticPlayable(ulong id, int frequency, int amplitude, int pulseWidth, long duration)
        {
            this.Id = id;
            this.HapticParams = new HapticParams(frequency, amplitude, pulseWidth, duration);
        }

        /// <summary>
        /// Gets the unique identifier for the haptic effect.
        /// </summary>
        public ulong Id { get; private set; }

        /// <summary>
        /// Gets the haptic parameters for this playable effect.
        /// </summary>
        public HapticParams HapticParams { get; private set; }
    }
}

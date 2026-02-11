// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.TeslaSuit
{
    /// <summary>
    /// Represents haptic feedback parameters for TeslaSuit devices.
    /// </summary>
    public struct HapticParams
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HapticParams"/> struct.
        /// </summary>
        /// <param name="frequency">The frequency of the haptic feedback.</param>
        /// <param name="amplitude">The amplitude of the haptic feedback.</param>
        /// <param name="pulseWidth">The pulse width of the haptic feedback.</param>
        /// <param name="duration">The duration of the haptic feedback in milliseconds.</param>
        public HapticParams(int frequency, int amplitude, int pulseWidth, long duration)
        {
            this.Frequency = frequency;
            this.Amplitude = amplitude;
            this.PulseWidth = pulseWidth;
            this.Duration = duration;
        }

        /// <summary>
        /// Gets the frequency of the haptic feedback.
        /// </summary>
        public int Frequency { get; private set; }

        /// <summary>
        /// Gets the amplitude of the haptic feedback.
        /// </summary>
        public int Amplitude { get; private set; }

        /// <summary>
        /// Gets the pulse width of the haptic feedback.
        /// </summary>
        public int PulseWidth { get; private set; }

        /// <summary>
        /// Gets the duration of the haptic feedback in milliseconds.
        /// </summary>
        public long Duration { get; private set; }
    }
}

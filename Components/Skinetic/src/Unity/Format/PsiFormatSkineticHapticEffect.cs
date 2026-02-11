// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.PsiFormats
{
    using System.IO;
    using Microsoft.Psi.Interop.Serialization;
    using SAAC.Skinectic;

    /// <summary>
    /// Provides Psi format serialization for Skinetic haptic effect data.
    /// </summary>
    public class PsiFormatSkineticHapticEffect
    {
        /// <summary>
        /// Gets the format configuration for Skinetic haptic effect data.
        /// </summary>
        /// <returns>The format configuration.</returns>
        public static Format<SkineticHapticEffect> GetFormat()
        {
            return new Format<SkineticHapticEffect>(WriteSkineticHapticEffect, ReadSkineticHapticEffect);
        }

        /// <summary>
        /// Writes Skinetic haptic effect data to a binary writer.
        /// </summary>
        /// <param name="effect">The haptic effect to write.</param>
        /// <param name="writer">The binary writer.</param>
        public static void WriteSkineticHapticEffect(SkineticHapticEffect effect, BinaryWriter writer)
        {
            writer.Write(effect.Name);
            writer.Write(effect.IsActive);
        }

        /// <summary>
        /// Reads Skinetic haptic effect data from a binary reader.
        /// </summary>
        /// <param name="reader">The binary reader.</param>
        /// <returns>The Skinetic haptic effect.</returns>
        public static SkineticHapticEffect ReadSkineticHapticEffect(BinaryReader reader)
        {
            SkineticHapticEffect effect = new SkineticHapticEffect();
            effect.Name = reader.ReadString();
            effect.IsActive = reader.ReadBoolean();
            return effect;
        }
    }
}

using SAAC.Skinectic;
using Microsoft.Psi.Interop.Serialization;
using System.IO;

namespace SAAC.PsiFormats
{
    public class PsiFormatSkineticHapticEffect
    {
        public static Format<SkineticHapticEffect> GetFormat()
        {
            return new Format<SkineticHapticEffect>(WriteSkineticHapticEffect, ReadSkineticHapticEffect);
        }

        public static void WriteSkineticHapticEffect(SkineticHapticEffect effect, BinaryWriter writer)
        {
            writer.Write(effect.Name);
            writer.Write(effect.IsActive);
        }

        public static SkineticHapticEffect ReadSkineticHapticEffect(BinaryReader reader)
        {
            SkineticHapticEffect effect = new SkineticHapticEffect();
            effect.Name = reader.ReadString();
            effect.IsActive = reader.ReadBoolean();
            return effect;
        }
    }
}

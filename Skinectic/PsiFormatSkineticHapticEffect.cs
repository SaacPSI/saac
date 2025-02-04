using SAAC.PipelineServices;
using Microsoft.Psi.Interop.Serialization;
using System.IO;

namespace SAAC.Skinetic
{
    public class SkineticHapticEffect
    {
        public string Name;
        public bool IsActive;
    }

    public class PsiFormatSkineticHapticEffect : IPsiFormat
    {
        public dynamic GetFormat()
        {
            return new Format<SkineticHapticEffect>(WriteSkineticHapticEffect, ReadSkineticHapticEffect);
        }

        public void WriteSkineticHapticEffect(SkineticHapticEffect effect, BinaryWriter writer)
        {
            writer.Write(effect.Name);
            writer.Write(effect.IsActive);
        }

        public SkineticHapticEffect ReadSkineticHapticEffect(BinaryReader reader)
        {
            SkineticHapticEffect effect = new SkineticHapticEffect();
            effect.Name = reader.ReadString();
            effect.IsActive = reader.ReadBoolean();
            return effect;
        }
    }
}

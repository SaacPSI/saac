using SAAC.PipelineServices;

namespace SAAC.TeslaSuit
{
    public class PsiFormatTsPPG : IPsiFormat
    {
        public dynamic GetFormat()
        {
            return PsiFormats.PsiFormatTsPPG.GetFormat();
        }
    }
}
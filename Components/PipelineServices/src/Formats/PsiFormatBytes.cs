using System.IO;

namespace SAAC.PipelineServices 
{
    public class PsiFormatBytes : IPsiFormat
    {
        public dynamic GetFormat()
        {
            return PsiFormats.PsiFormatBytes.GetFormat();
        }
    }
}

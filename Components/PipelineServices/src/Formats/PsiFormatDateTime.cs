namespace SAAC.PipelineServices
{
    public class PsiFormatDateTime : IPsiFormat
    {
        public dynamic GetFormat()
        {
            return PsiFormats.PsiFormatDateTime.GetFormat();
        }
    }
}
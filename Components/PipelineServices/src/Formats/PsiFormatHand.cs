namespace SAAC.PipelineServices
{
    public class PsiFormatHand : IPsiFormat
    {
        public dynamic GetFormat()
        {
            return SAAC.PsiFormats.PsiFormatHand.GetFormat();
        }
    }
}

namespace SAAC.PipelineServices
{
    public class PsiFormatGrabEvent : IPsiFormat
    {
        public dynamic GetFormat()
        {
            return SAAC.PsiFormats.PsiFormatGrabEvent.GetFormat();
        }
    }
}

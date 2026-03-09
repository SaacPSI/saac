namespace SAAC.PipelineServices
{
    public class PsiFormatBatteryEvent : IPsiFormat
    {
        public dynamic GetFormat()
        {
            return PsiFormats.PsiFormatBatteryEvent.GetFormat();
        }
    }
}


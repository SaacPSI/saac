namespace SAAC.PipelineServices
{
    public class PsiFormatBatteryModuleEvent : IPsiFormat
    {
        public dynamic GetFormat()
        {
            return PsiFormats.PsiFormatBatteryModuleEvent.GetFormat();
        }
    }
}


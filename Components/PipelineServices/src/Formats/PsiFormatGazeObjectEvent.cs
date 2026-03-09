namespace SAAC.PipelineServices
{
    public class PsiFormatGazeObjectEvent : IPsiFormat
    {
        public dynamic GetFormat()
        {
            return PsiFormats.PsiFormatGazeObjectEvent.GetFormat();
        }
    }
}


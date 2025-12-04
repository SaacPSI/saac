namespace SAAC.PipelineServices
{
    public class PsiFormatGazeEvent : IPsiFormat
    {
        public dynamic GetFormat()
        {
            return SAAC.PsiFormats.PsiFormatGazeEvent.GetFormat();
        }
    }
}

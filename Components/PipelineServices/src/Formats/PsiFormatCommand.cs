namespace SAAC
{
    namespace PipelineServices
    {
        public class PsiFormatCommand : IPsiFormat
        {
            public dynamic GetFormat()
            {
                return PsiFormats.PsiFormatCommand.GetFormat();
            }
        }
    }
}
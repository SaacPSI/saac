using Microsoft.Psi.Data;

namespace Microsoft.Psi.PsiStudio
{
    internal interface IPsiStudioPipeline
    {
        public Dataset GetDataset();
        public void RunPipeline(TimeInterval timeInterval);
        public void StopPipeline();
    }
}

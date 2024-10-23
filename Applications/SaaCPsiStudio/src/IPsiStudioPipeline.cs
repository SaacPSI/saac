using Microsoft.Psi.Data;

namespace Microsoft.Psi.PsiStudio
{
    internal interface IPsiStudioPipeline
    {
        public Dataset GetDataset();
        public void RunPipeline();
        public void StopPipeline();
    }
}

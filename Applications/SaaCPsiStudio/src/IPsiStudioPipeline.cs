namespace Microsoft.Psi.PsiStudio
{
    internal interface IPsiStudioPipeline
    {
        public string GetDataset();
        public void RunPipeline();
        public void StopPipeline();
    }
}

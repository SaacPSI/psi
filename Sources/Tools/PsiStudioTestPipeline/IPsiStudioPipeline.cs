namespace Microsoft.Psi.PsiStudio
{
    public interface IPsiStudioPipeline
    {
        public string GetDataset();
        public void RunPipeline();
        public void StopPipeline();
    }
}

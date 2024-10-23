using Microsoft.Psi.Data;

namespace Microsoft.Psi.PsiStudio
{
    public interface IPsiStudioPipeline
    {
        public Dataset GetDataset();
        public void RunPipeline();
        public void StopPipeline();
    }
}

using Microsoft.Psi;

namespace SAAC.PipelineServices
{
    public class ReplayPipelineConfiguration : DatasetPipelineConfiguration
    {
        public ReplayPipeline.ReplayType ReplayType = ReplayPipeline.ReplayType.RealTime;
        public TimeInterval ReplayInterval = TimeInterval.Infinite;
        public bool DatasetBackup = true;
    }
}

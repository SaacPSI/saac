﻿using Microsoft.Psi;

namespace RendezVousPipelineServices
{
    public class ReplayPipelineConfiguration : DatasetPipelineConfiguration
    {
        public ReplayPipeline.ReplayType ReplayType = ReplayPipeline.ReplayType.RealTime;
        public TimeInterval ReplayInterval = TimeInterval.Infinite;
    }
}
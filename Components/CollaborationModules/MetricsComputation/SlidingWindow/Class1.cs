using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Psi;
using SAAC.PipelineServices;

namespace SAAC.Components.CollaborationModules
{
    public class SlidingAverageWindowConfiguration
    {
        public double threshold;
        public int participantNumber;
        public TextWriter IndexesWriter;
    }

    public class SlidingAverageWindow
    {
        public SlidingAverageWindowConfiguration slidingWindowConfiguration;

        public SlidingAverageWindow(Pipeline pipeline, DatasetPipeline server, SlidingAverageWindowConfiguration? configuration = null)
        {
            this.slidingWindowConfiguration = configuration ?? new SlidingAverageWindowConfiguration();
        }
    }
}

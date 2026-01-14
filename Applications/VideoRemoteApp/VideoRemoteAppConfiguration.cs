using Microsoft.Psi;
using SAAC.PipelineServices;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;

namespace VideoRemoteApp
{
    public class VideoRemoteAppConfiguration
    {
        public TimeSpan Interval { get; set; } = TimeSpan.FromMilliseconds(100.0);
        public int EncodingVideoLevel { get; set; } = 75;
        public Dictionary<string, Rectangle> CroppingAreas { get; set; } = new Dictionary<string, Rectangle>();
    }
}

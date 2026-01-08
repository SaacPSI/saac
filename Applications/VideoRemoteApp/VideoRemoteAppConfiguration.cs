using Microsoft.Psi;
using SAAC.PipelineServices;
using System;
using System.Diagnostics;
using System.Drawing;

namespace VideoRemoteApp
{
    public class VideoRemoteAppConfiguration
    {
        public TimeSpan Interval { get; set; } = TimeSpan.FromMilliseconds(100.0);
        public int EncodingVideoLevel { get; set; } = 75;
        public Rectangle CroppingArea { get; set; }
    }
}

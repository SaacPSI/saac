﻿using Microsoft.Psi.Remoting;

namespace SAAC.RemoteConnectors
{
    public class KinectAzureRemoteStreamsConfiguration
    {
        public int KinectDeviceIndex { get; set; } = 0;

        // Stream available
        public bool StreamAudio { get; set; } = true;
        public bool StreamSkeleton { get; set; } = true;
        public bool StreamVideo { get; set; } = true;
        public bool StreamDepth { get; set; } = false;
        public bool StreamDepthCalibration { get; set; } = false;
        public bool StreamIMU { get; set; } = false;

        // Configuration for video stream
        public int EncodingVideoLevel { get; set; } = 90;
        public Microsoft.Azure.Kinect.Sensor.ColorResolution VideoResolution { get; set; } = Microsoft.Azure.Kinect.Sensor.ColorResolution.R1080p;
        public Microsoft.Azure.Kinect.Sensor.FPS FPS { get; set; } = Microsoft.Azure.Kinect.Sensor.FPS.FPS30;

        // Network
        public TransportKind ConnectionType { get; set; } = TransportKind.Tcp;
        public string IpToUse { get; set; } = "localhost";
        public int StartingPort { get; set; } = 11411;
        public string RendezVousApplicationName { get; set; } = "RemoteKinectAzureServer";
    }
}

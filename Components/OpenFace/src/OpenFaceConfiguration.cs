using System.Numerics;

namespace OpenFace
{
    public class OpenFaceConfiguration
    {
        public string ModelDirectory { get; set; }

        public bool Pose { get; set; } = true;
        public bool Eyes { get; set; } = true;
        public bool Face { get; set; } = true;

        public OpenFaceConfiguration(string modelDirectory)
        {
            ModelDirectory = modelDirectory;
        }
    }
}

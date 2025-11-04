using static LSL.liblsl;

namespace LabStreamLayer
{
    public interface ILabStreamLayerComponent
    {
        public StreamInfo GetStreamInfo();

        public Type? GetStreamChannelType();

        void Dispose();
    }
}

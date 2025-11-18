using System.Linq.Expressions;
using static LSL.liblsl;

namespace SAAC.LabStreamLayer
{
    public interface ILabStreamLayerComponent
    {
        public StreamInfo GetStreamInfo();

        public Type? GetStreamChannelType();

        void Dispose();
    }
}

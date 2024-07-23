using TsSDK;

namespace SAAC.TeslaSuit
{
    public class RawPpgData : TsSDK.IRawPpgData
    {
        public IEnumerable<RawPpgNodeData> NodesData { get; private set; }

        public RawPpgData(List<RawPpgNodeData> nodes)
        {
            NodesData = nodes;
        }
    }
}

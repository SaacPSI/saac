using TsSDK;

namespace SAAC.TeslaSuit
{
    public class PpgData : TsSDK.IProcessedPpgData
    {
        public IEnumerable<ProcessedPpgNodeData> NodesData { get; private set; }

        public PpgData(List<ProcessedPpgNodeData> nodes)
        {
            NodesData = nodes;
        }
    }
}

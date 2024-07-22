using Microsoft.Psi.Interop.Serialization;
using System.IO;
using SAAC.RendezVousPipelineServices;
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

    public class PsiFormatTSPPG : IPsiFormat
    {
        public dynamic GetFormat()
        {
            return new Format<TsSDK.IProcessedPpgData>(WriteRawPpgData, ReadRawPpgData);
        }

        public void WriteRawPpgData(TsSDK.IProcessedPpgData data, BinaryWriter writer)
        {
            writer.Write(data.NodesData.Count());
            foreach (var node in data.NodesData)
            {
                writer.Write(node.nodeIndex);
                writer.Write(node.timestamp);
                writer.Write(node.isHeartrateValid);
                writer.Write(node.heartRate);
            }
        }

        public TsSDK.IProcessedPpgData ReadRawPpgData(BinaryReader reader)
        {
            int count = reader.ReadInt32();
            List<ProcessedPpgNodeData> listData = new List<ProcessedPpgNodeData>(count);
            for (int i = 0; i < count; i++)
            {
                ProcessedPpgNodeData data = new ProcessedPpgNodeData();
                data.nodeIndex = reader.ReadInt32();
                data.timestamp = reader.ReadUInt64();
                data.isHeartrateValid = reader.ReadBoolean();
                data.heartRate = reader.ReadInt32();
                listData.Add(data);
            }

            //missing check count
            return new PpgData(listData);
        }
    }
}
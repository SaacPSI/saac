using Microsoft.Psi.Interop.Serialization;
using System.IO;
using SAAC.RendezVousPipelineServices;
using TsSDK;

namespace SAAC.TeslaSuit
{
    public class PsiFormatTsPPG : IPsiFormat
    {
        public dynamic GetFormat()
        {
            return new Format<List<ProcessedPpgNodeData>>(WritePpgData, ReadPpgData);
        }

        public void WritePpgData(List<ProcessedPpgNodeData> data, BinaryWriter writer)
        {
            writer.Write(data.Count());
            foreach (var node in data)
            {
                writer.Write(node.nodeIndex);
                writer.Write(node.timestamp);
                writer.Write(node.isHeartrateValid);
                writer.Write(node.heartRate);
            }
        }

        public List<ProcessedPpgNodeData> ReadPpgData(BinaryReader reader)
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
            return listData;
        }
    }
}
// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.PsiFormats
{
    using System.IO;
    using Microsoft.Psi.Interop.Serialization;
    using TsSDK;

    /// <summary>
    /// Provides Psi format serialization for TeslaSuit processed PPG data.
    /// </summary>
    public class PsiFormatTsPPG
    {
        /// <summary>
        /// Gets the format configuration for TeslaSuit processed PPG data.
        /// </summary>
        /// <returns>The format configuration.</returns>
        public static Format<List<ProcessedPpgNodeData>> GetFormat()
        {
            return new Format<List<ProcessedPpgNodeData>>(WritePpgData, ReadPpgData);
        }

        /// <summary>
        /// Writes processed PPG data to a binary writer.
        /// </summary>
        /// <param name="data">The processed PPG data to write.</param>
        /// <param name="writer">The binary writer.</param>
        public static void WritePpgData(List<ProcessedPpgNodeData> data, BinaryWriter writer)
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

        /// <summary>
        /// Reads processed PPG data from a binary reader.
        /// </summary>
        /// <param name="reader">The binary reader.</param>
        /// <returns>The list of processed PPG node data.</returns>
        public static List<ProcessedPpgNodeData> ReadPpgData(BinaryReader reader)
        {
            int count = reader.ReadInt32();
            List<ProcessedPpgNodeData> listData = new List<ProcessedPpgNodeData>(count);
            for (int i = 0; i < count; i++)
            {
                ProcessedPpgNodeData data = default(ProcessedPpgNodeData);
                data.nodeIndex = reader.ReadInt32();
                data.timestamp = reader.ReadUInt64();
                data.isHeartrateValid = reader.ReadBoolean();
                data.heartRate = reader.ReadInt32();
                listData.Add(data);
            }

            return listData;
        }
    }
}

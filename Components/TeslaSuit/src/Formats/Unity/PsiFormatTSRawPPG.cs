// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.PsiFormats
{
    using System.IO;
    using Microsoft.Psi.Interop.Serialization;
    using TsSDK;

    /// <summary>
    /// Provides Psi format serialization for TeslaSuit raw PPG data.
    /// </summary>
    public class PsiFormatTsRawPPG
    {
        /// <summary>
        /// Gets the format configuration for TeslaSuit raw PPG data.
        /// </summary>
        /// <returns>The format configuration.</returns>
        public static Format<List<RawPpgNodeData>> GetFormat()
        {
            return new Format<List<RawPpgNodeData>>(WriteRawPpgData, ReadRawPpgData);
        }

        /// <summary>
        /// Writes raw PPG data to a binary writer.
        /// </summary>
        /// <param name="data">The raw PPG data to write.</param>
        /// <param name="writer">The binary writer.</param>
        public static void WriteRawPpgData(List<RawPpgNodeData> data, BinaryWriter writer)
        {
            writer.Write(data.Count());
            foreach (var node in data)
            {
                writer.Write(node.nodeIndex);
                writer.Write(node.timestamp);
                writer.Write(node.red_data.Length);
                foreach (long value in node.red_data)
                {
                    writer.Write(BitConverter.GetBytes(value));
                }

                writer.Write(node.green_data.Length);
                foreach (long value in node.green_data)
                {
                    writer.Write(BitConverter.GetBytes(value));
                }

                writer.Write(node.blue_data.Length);
                foreach (long value in node.blue_data)
                {
                    writer.Write(BitConverter.GetBytes(value));
                }

                writer.Write(node.infrared_data.Length);
                foreach (long value in node.infrared_data)
                {
                    writer.Write(BitConverter.GetBytes(value));
                }
            }
        }

        /// <summary>
        /// Reads raw PPG data from a binary reader.
        /// </summary>
        /// <param name="reader">The binary reader.</param>
        /// <returns>The list of raw PPG node data.</returns>
        public static List<RawPpgNodeData> ReadRawPpgData(BinaryReader reader)
        {
            int count = reader.ReadInt32();
            List<RawPpgNodeData> listData = new List<RawPpgNodeData>(count);
            for (int i = 0; i < count; i++)
            {
                RawPpgNodeData rawPpgNodeData = default(RawPpgNodeData);
                rawPpgNodeData.nodeIndex = reader.ReadInt32();
                rawPpgNodeData.timestamp = reader.ReadUInt64();

                int redCount = reader.ReadInt32();
                rawPpgNodeData.red_data = new long[redCount];
                for (int j = 0; j < redCount; j++)
                {
                    rawPpgNodeData.red_data[j] = reader.ReadInt64();
                }

                int greenCount = reader.ReadInt32();
                rawPpgNodeData.green_data = new long[greenCount];
                for (int j = 0; j < greenCount; j++)
                {
                    rawPpgNodeData.green_data[j] = reader.ReadInt64();
                }

                int blueCount = reader.ReadInt32();
                rawPpgNodeData.blue_data = new long[blueCount];
                for (int j = 0; j < blueCount; j++)
                {
                    rawPpgNodeData.blue_data[j] = reader.ReadInt64();
                }

                int infraredCount = reader.ReadInt32();
                rawPpgNodeData.infrared_data = new long[infraredCount];
                for (int j = 0; j < infraredCount; j++)
                {
                    rawPpgNodeData.infrared_data[j] = reader.ReadInt64();
                }

                listData.Add(rawPpgNodeData);
            }

            return listData;
        }
    }
}

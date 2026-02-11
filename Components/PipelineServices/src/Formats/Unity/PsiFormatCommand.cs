// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

using System.IO;
using Microsoft.Psi.Interop.Serialization;

namespace SAAC
{
    namespace PsiFormats
    {
        public class PsiFormatCommand
        {
            public static Format<(PipelineServices.RendezVousPipeline.Command, string)> GetFormat()
            {
                return new Format<(PipelineServices.RendezVousPipeline.Command, string)>(WriteIntString, ReadIntSring);
            }

            public static void WriteIntString((PipelineServices.RendezVousPipeline.Command, string) data, BinaryWriter writer)
            {
                writer.Write((int)data.Item1);
                writer.Write(data.Item2);
            }

            public static (PipelineServices.RendezVousPipeline.Command, string) ReadIntSring(BinaryReader reader)
            {
                return new ((PipelineServices.RendezVousPipeline.Command)reader.ReadInt32(), reader.ReadString());
            }
        }
    }
}

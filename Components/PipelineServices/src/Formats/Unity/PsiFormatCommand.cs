// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.PsiFormats
{
    using System.IO;
    using Microsoft.Psi.Interop.Serialization;
    using SAAC.PipelineServices;

    /// <summary>
    ///  Command format for the RendezVousPipeline, consisting of an integer command and a string argument.
    /// </summary>
    public class PsiFormatCommand
    {
        /// <summary>
        /// Creates a format instance for serializing and deserializing a tuple containing a RendezVousPipeline command
        /// and a string.
        /// </summary>
        /// <remarks>Use this method to obtain a format suitable for transmitting or persisting
        /// command-string pairs in the RendezVous pipeline. The returned format ensures compatibility with the expected
        /// serialization logic for these types.</remarks>
        /// <returns>A Format object that handles serialization and deserialization of a tuple consisting of a Command and a string.</returns>
        public static Format<(Command, string)> GetFormat()
        {
            return new Format<(Command, string)>(WriteIntString, ReadIntSring);
        }

        /// <summary>
        /// Writes a command and its associated string value to the specified binary writer.
        /// </summary>
        /// <param name="data">A tuple containing the command to write and the associated string value.</param>
        /// <param name="writer">The binary writer to which the command and string value are written. Cannot be null.</param>
        public static void WriteIntString((Command, string) data, BinaryWriter writer)
        {
            writer.Write((int)data.Item1);
            writer.Write(data.Item2);
        }

        /// <summary>
        /// Reads a command and its associated string value from the specified binary reader.
        /// </summary>
        /// <param name="reader">The binary reader from which the command and string value are read. Cannot be null
        /// and must be positioned at the beginning of the expected data.</param>
        /// <returns>A tuple containing the command read as a value of the Command enumeration and the string
        /// read from the stream.</returns>
        public static (Command, string) ReadIntSring(BinaryReader reader)
        {
            return new ((Command)reader.ReadInt32(), reader.ReadString());
        }
    }
}

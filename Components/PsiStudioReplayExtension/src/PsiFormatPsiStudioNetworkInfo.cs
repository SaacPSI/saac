// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.PsiStudio
{
    using System;
    using System.IO;
    using Microsoft.Psi.Interop.Serialization;

    /// <summary>
    /// Class to (de)serialize message of type <see cref="PsiStudioNetworkInfo"/>.for <see cref="NetworkStreamsManager"/>.
    /// </summary>
    internal class PsiFormatPsiStudioNetworkInfo
    {
        /// <summary>
        /// Static method to generate (de)serialize the <see cref="PsiStudioNetworkInfo"/> class.
        /// </summary>
        /// <returns>Retuns a new format of type <see cref="PsiStudioNetworkInfo"/>. </returns>
        public static Format<PsiStudioNetworkInfo> GetFormat()
        {
            return new Format<PsiStudioNetworkInfo>(WritePsiStudioNetworkInfo, ReadPsiStudioNetworkInfo);
        }

        /// <summary>
        /// Static method to serialize the <see cref="PsiStudioNetworkInfo"/> class.
        /// </summary>
        /// <param name="info">The message to serialze with availble data.</param>
        /// <param name="writer">The writer to write to.</param>
        public static void WritePsiStudioNetworkInfo(PsiStudioNetworkInfo info, BinaryWriter writer)
        {
            writer.Write((int)info.Event);
            writer.Write(info.Interval.Left.Ticks);
            writer.Write(info.Interval.Right.Ticks);
            writer.Write(info.PlaySpeed);
            writer.Write(info.SessionName);
        }

        /// <summary>
        /// Static method to deserialize the <see cref="PsiStudioNetworkInfo"/> class.
        /// </summary>
        /// <param name="reader">The reader with availble data.</param>
        /// <returns>Retuns a new instance of type <see cref="PsiStudioNetworkInfo"/>.</returns>
        public static PsiStudioNetworkInfo ReadPsiStudioNetworkInfo(BinaryReader reader)
        {
            return new PsiStudioNetworkInfo((PsiStudioNetworkInfo.PsiStudioNetworkEvent)reader.ReadInt32(), new TimeInterval(new DateTime(reader.ReadInt64()), new DateTime(reader.ReadInt64())), reader.ReadDouble(), reader.ReadString());
        }
    }
}
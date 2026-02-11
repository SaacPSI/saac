// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.PsiFormats
{
    using System.Numerics;
    using Microsoft.Psi.Interop.Serialization;

    /// <summary>
    /// Provides serialization format for SAAC.GlobalHelpers.Hand type.
    /// </summary>
    public class PsiFormatHand
    {
        /// <summary>
        /// Gets the format for serializing and deserializing Hand objects.
        /// </summary>
        /// <returns>A Format instance for Hand serialization.</returns>
        public static Format<SAAC.GlobalHelpers.Hand> GetFormat()
        {
            return new Format<SAAC.GlobalHelpers.Hand>(WriteHand, ReadHand);
        }

        /// <summary>
        /// Writes a Hand object to a binary writer.
        /// </summary>
        /// <param name="hand">The Hand object to write.</param>
        /// <param name="writer">The binary writer to write to.</param>
        public static void WriteHand(SAAC.GlobalHelpers.Hand hand, BinaryWriter writer)
        {
            writer.Write((int)hand.Type);
            writer.Write((int)hand.Origin);
            writer.Write(hand.RootPosition.X);
            writer.Write(hand.RootPosition.Y);
            writer.Write(hand.RootPosition.Z);
            writer.Write(hand.RootOrientation.X);
            writer.Write(hand.RootOrientation.Y);
            writer.Write(hand.RootOrientation.Z);
            writer.Write(hand.RootOrientation.W);
            writer.Write(hand.HandJoints.Count);
            foreach (var joint in hand.HandJoints)
            {
                writer.Write((int)joint.Key);
                writer.Write(joint.Value.X);
                writer.Write(joint.Value.Y);
                writer.Write(joint.Value.Z);
            }
        }

        /// <summary>
        /// Reads a Hand object from a binary reader.
        /// </summary>
        /// <param name="reader">The binary reader to read from.</param>
        /// <returns>The deserialized Hand object.</returns>
        public static SAAC.GlobalHelpers.Hand ReadHand(BinaryReader reader)
        {
            SAAC.GlobalHelpers.Hand hand = SAAC.GlobalHelpers.Hand.CreateHand((SAAC.GlobalHelpers.Hand.EHandType)reader.ReadInt32(), (SAAC.GlobalHelpers.Hand.EOrigin)reader.ReadInt32());
            hand.RootPosition = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            hand.RootOrientation = new Quaternion(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            int count = reader.ReadInt32();
            for (int iterator = 0; iterator < count; iterator++)
            {
                hand.HandJoints.Add((SAAC.GlobalHelpers.Hand.EHandJointID)reader.ReadInt32(), new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()));
            }

            return hand;
        }
    }
}

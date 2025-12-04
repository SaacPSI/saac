using System.Collections.Generic;
using System.IO;
using Microsoft.Psi.Interop.Serialization;
using Microsoft.Win32.SafeHandles;
using System.Numerics;

namespace SAAC.PsiFormats
{
    public class PsiFormatHand
    {
        public static Format<SAAC.GlobalHelpers.Hand> GetFormat()
        {
            return new Format<SAAC.GlobalHelpers.Hand>(WriteHand, ReadHand);
        }

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

        public static SAAC.GlobalHelpers.Hand ReadHand(BinaryReader reader)
        {
            SAAC.GlobalHelpers.Hand hand = SAAC.GlobalHelpers.Hand.CreateHand((SAAC.GlobalHelpers.Hand.EHandType)reader.ReadInt32(), (SAAC.GlobalHelpers.Hand.EOrigin)reader.ReadInt32());
            hand.RootPosition = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            hand.RootOrientation = new Quaternion(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            int count = reader.ReadInt32();
            for (int iterator = 0; iterator < count; iterator++)
                hand.HandJoints.Add((SAAC.GlobalHelpers.Hand.EHandJointID)reader.ReadInt32(), new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()));
            return hand;
        }
    }
}
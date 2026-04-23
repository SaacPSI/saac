namespace SAAC.PsiFormats
{
    using Microsoft.Psi.Interop.Serialization;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;

    public class PsiFormatObjectInteraction
    {
        public static Format<ObjectInteraction> GetFormat()
        {
            return new Format<ObjectInteraction>(WritePieceState, ReadPieceState);
        }

        public static void WritePieceState(ObjectInteraction pieceStatus, BinaryWriter writer)
        {
            writer.Write(pieceStatus.userID);
            writer.Write(pieceStatus.objectID);
            writer.Write((int)pieceStatus.state);
            writer.Write(pieceStatus.isActive);
            writer.Write(pieceStatus.currentLocation);
        }

        public static ObjectInteraction ReadPieceState(BinaryReader reader)
        {
            int userID = reader.ReadInt32();
            string objectID = reader.ReadString();
            State objectType = (State)reader.ReadInt32();
            bool isActive = reader.ReadBoolean();
            string lastZone = reader.ReadString();
            string currentLocation = reader.ReadString();

            return new ObjectInteraction(userID, objectID, objectType, isActive, currentLocation);
        }
    }
}

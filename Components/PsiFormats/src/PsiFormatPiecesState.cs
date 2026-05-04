using System.IO;
using Microsoft.Psi.Interop.Serialization;

namespace SAAC.PsiFormats
{
    public class PsiFormatPiecesState
    {
        public Format<PieceStatus> GetFormat()
        {
            return new Format<PieceStatus>(WritePieceState, ReadPieceState);
        }

        public void WritePieceState(PieceStatus pieceStatus, BinaryWriter writer)
        {
            writer.Write(pieceStatus.userID);
            writer.Write(pieceStatus.objectID);
            writer.Write((int)pieceStatus.type);
            writer.Write(pieceStatus.isActive);
            writer.Write(pieceStatus.lastZone);
            writer.Write((int)pieceStatus.currentLocation);
        }

        public PieceStatus ReadPieceState(BinaryReader reader)
        {
            int userID = reader.ReadInt32();
            string objectID = reader.ReadString();
            State objectType = (State)reader.ReadInt32();
            bool isActive = reader.ReadBoolean();
            string lastZone = reader.ReadString();
            Location currentLocation = (Location)reader.ReadInt32();

            return new PieceStatus(userID, objectID, objectType, isActive, lastZone, currentLocation);
        }
    }
}

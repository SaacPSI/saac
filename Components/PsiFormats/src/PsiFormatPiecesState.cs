namespace SAAC.PsiFormats
{
    using Microsoft.Psi.Interop.Serialization;

    public class PsiFormatPiecesState
    {
        public static Format<PieceStatus> GetFormat()
        {
            return new Format<PieceStatus>(WritePieceState, ReadPieceState);
        }

        public static void WritePieceState(PieceStatus pieceStatus, BinaryWriter writer)
        {
            writer.Write(pieceStatus.userID);
            writer.Write(pieceStatus.objectID);
            writer.Write((int)pieceStatus.state);
            writer.Write(pieceStatus.isActive);
            writer.Write(pieceStatus.lastZone);
            writer.Write((int)pieceStatus.currentLocation);
        }

        public static PieceStatus ReadPieceState(BinaryReader reader)
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

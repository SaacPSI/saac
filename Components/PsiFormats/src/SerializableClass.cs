using Microsoft.Psi.Interop.Serialization;
using System.IO;

namespace SAAC.PsiFormats
{
    public class Class1
    {
    }

    public class IDs
    {
        public int userID;
        public string objectID;
        public DateTime originatingTime;
    }

    public class ObjectGazeEvent : IDs
    {
        public string type;
        public bool status;

        public ObjectGazeEvent(string t, int id, string o, bool status)
        {
            this.type = t;
            this.userID = id;
            this.objectID = o;
            this.status = status;
        }
    }

    public class ObjectInteraction : IDs
    {
        public State state;// État de la pièce
        public bool isActive;// Indique si la pièce est active
        public string currentLocation;// Tuple<Vector3,Vector3> in string format

        // Constructeur pour initialiser les valeurs
        public ObjectInteraction(int id, string objectid, State t, bool currentState, string loc)
        {
            this.userID = id;
            this.objectID = objectid;
            this.state = t;
            this.isActive = currentState;
            this.currentLocation = loc;
        }
    }

    public class PieceStatus : IDs
    {
        public State type;
        public bool isActive;
        public string lastZone;
        public Location currentLocation;

        public PieceStatus(int id, string objectid, State t, bool currentState, string lz, Location loc)
        {
            this.userID = id;
            this.objectID = objectid;
            this.type = t;
            this.isActive = currentState;
            this.lastZone = lz;
            this.currentLocation = loc;
        }
    }

    public enum State
    {
        Spawn = 1,
        Destroy = 2,
        Grab = 3,
        Ungrab = 4,
        Placed = 5,
        Unplaced = 6,
        Colored = 7,
        Uncolored = 8
    }
    public enum Location
    {
        Sol = 1,
        Generator1 = 2,
        Generator2 = 3,
        CentraleTableZone = 4,
        IterationTable = 5,
        Hand = 6,
        Button = 7,
        Outside = 8,
        None = 9
    }
}

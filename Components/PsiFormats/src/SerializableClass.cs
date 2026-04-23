using Microsoft.Psi.Interop.Serialization;
using System.IO;

namespace SAAC.PsiFormats
{
    public class Class1
    {
    }
    [System.Serializable]
    public class IDs
    {
        public int userID;
        public string objectID;
    }

    [System.Serializable]
    public class ObjectGazeEvent : IDs
    {
        public string type;
        public bool status;

        public ObjectGazeEvent(string t, int id, string o, bool status)
        {
            type = t;
            userID = id;
            objectID = o;
            this.status = status;
        }
    }

    [System.Serializable]
    public class AvatarGazeEvent : IDs
    {
        public string type;
        public bool status;
        public AvatarGazeEvent(string t, int gazerid, string gazedid, bool status)
        {
            type = t;
            userID = gazerid;
            objectID = gazedid;
            this.status = status;
        }
    }

    // Classe pour représenter l'état d'une pièce
    [System.Serializable]
    public class ObjectInteraction : IDs
    {
        public State state;// État de la pièce
        public bool isActive;// Indique si la pièce est active
        public string currentLocation;// Tuple<Vector3,Vector3> in string format

        // Constructeur pour initialiser les valeurs
        public ObjectInteraction(int id, string objectid, State t, bool currentState, string loc)
        {
            userID = id;
            objectID = objectid;
            state = t;
            isActive = currentState;
            currentLocation = loc;
        }
    }
    // Enumération des différents états possibles
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
    // Enumération des différentes localisations possibles
    public enum Location
    {
        Sol = 1,
        Generator1 = 2,
        Generator2 = 3,
        CentraleTableZone = 4,
        IterationTable = 5,
        Hand = 6,
        None = 7
    }

    // Classe pour représenter l'état d'une pièce
    [System.Serializable]
    public class PieceStatus : IDs
    {
        public State state;// État de la pièce
        public bool isActive;// Indique si la pièce est active
        public string lastZone;// Dernière zone où la pièce était
        public Location currentLocation;// Localisation actuelle de la pièce

        // Constructeur pour initialiser les valeurs
        public PieceStatus(int id, string objectid, State t, bool currentState, string lz, Location loc)
        {
            userID = id;
            objectID = objectid;
            state = t;
            isActive = currentState;
            lastZone = lz;
            currentLocation = loc;
        }
    }
}

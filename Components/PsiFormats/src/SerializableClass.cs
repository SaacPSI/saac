using System.Data;
using System.IO;
using System.Numerics;
using Microsoft.Psi.Interop.Serialization;

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

    public abstract class OT
    {
        public DateTime originatingTime;

        public OT(DateTime originatingTime)
        {
            this.originatingTime = originatingTime;
        }
    }

    public class PositionData
    {
        public DateTime OriginatingTime;
        public float Deltatime;
        public int UserID;
        public string HeadPos;
        public string LHandPos;
        public string RHandPos;
        public Vector3 HeadPosv;
        public Vector3 LHandPosv;
        public Vector3 RHandPosv;

        public PositionData(string value)
        {
            var parts = value.Split(';');
            this.Deltatime = float.Parse(parts[0]);
            this.UserID = int.Parse(parts[1]);
            this.HeadPos = parts[2];
            this.LHandPos = parts[3];
            this.RHandPos = parts[4];
        }

        public void ToVectHeadPos()
        {
            var partsHeadPos = this.HeadPos.Split('_');
            float xHeadpos = float.Parse(partsHeadPos[0]);
            float yHeadpos = float.Parse(partsHeadPos[1]);
            float zHeadpos = float.Parse(partsHeadPos[2]);
            this.HeadPosv = new Vector3(xHeadpos, yHeadpos, zHeadpos);
        }

        public void ToVectLeftHandPos()
        {
            var partsLhandPos = this.LHandPos.Split('_');
            float xLHandpos = float.Parse(partsLhandPos[0]);
            float yLHandpos = float.Parse(partsLhandPos[1]);
            float zLHandpos = float.Parse(partsLhandPos[2]);
            this.LHandPosv = new Vector3(xLHandpos, yLHandpos, zLHandpos);
        }

        public void ToVectRightHandPos()
        {
            var partsRhandPos = this.RHandPos.Split('_');
            float xRHandpos = float.Parse(partsRhandPos[0]);
            float yRHandpos = float.Parse(partsRhandPos[1]);
            float zRHandpos = float.Parse(partsRhandPos[2]);
            this.RHandPosv = new Vector3(xRHandpos, yRHandpos, zRHandpos);
        }
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

    public class AvatarGazeEvent : IDs
    {
        public string type;
        public bool status;
        public AvatarGazeEvent(string t, int id, string o, bool status)
        {
            type = t;
            userID = id;
            objectID = o;
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

    public class BodyPartPosition
    {
        public Vector3 Position { get; set; }

        public DateTime Timestamp { get; set; }
    }

    public class NewJVAData : OT
    {
        public DateTime startTimejvainitiator;
        public DateTime endTimejvainitiator;
        public DateTime startTimejvaresponder;
        public DateTime endTimejvaresponder;
        public TimeSpan durationTime;
        public string objectID;
        public int initiator;
        public int responder;
        public bool isAlreadyAddToCurrentThreshold = false;

        public NewJVAData(DateTime stinit, DateTime etinit, DateTime stresp, DateTime etresp, TimeSpan duration, string id, int init, int resp) : base(stinit)
        {
            //originatingTime = stinit;
            startTimejvainitiator = stinit;
            endTimejvainitiator = etinit;
            startTimejvaresponder = stresp;
            endTimejvaresponder = etresp;
            durationTime = duration;
            objectID = id;
            initiator = init;
            responder = resp;
        }
    }

    public class JVAGroupData
    {
        public string objectID;
        public List<int> participants; // Identifiants des utilisateurs
        public Dictionary<int, (DateTime start, DateTime end)> individualTimes; // start/end par participant
        public DateTime groupStart; // Début du chevauchement
        public DateTime groupEnd;   // Fin du chevauchement
    }

    public class TimeData : OT
    {
        public DateTime StartOriginatingTime;
        public DateTime EndOriginatingTime;
        public double DurationTime;
        public string Text;

        public TimeData(DateTime startot, DateTime endot, double time, string txt) : base(startot)
        {
            StartOriginatingTime = startot;
            EndOriginatingTime = endot;
            DurationTime = time;
            Text = txt;
        }
    }

    public class SpeakingTimeIDData : OT
    {

        public int ID;
        public DateTime StartOriginatingTime;
        public DateTime EndOriginatingTime;
        public double DurationTime;
        public string Text;
        public bool IsAlreadyAddToCurrentThreshold;
        public DuoType DuoType;

        public SpeakingTimeIDData(int i, DateTime startot, DateTime endot, double time, string txt, bool b) : base(startot)
        {
            this.ID = i;
            this.StartOriginatingTime = startot;
            this.EndOriginatingTime = endot;
            this.DurationTime = time;
            this.Text = txt;
            this.IsAlreadyAddToCurrentThreshold = b;
        }

        public SpeakingTimeIDData DefaultValue()
        {
            var value = new SpeakingTimeIDData(0, DateTime.MinValue, DateTime.MinValue, 0, "", false);
            return value;
        }
    }

    public class TTData : OT
    {
        public int CurrentSpeaker;
        public int LastSpeaker;
        public string Type;
        public bool IsAlreadyAddToCurrentThreshold = false;
        public double Duration = 0;

        public TTData(int i, int l, string t, DateTime ot, double d) : base(ot)
        {
            this.CurrentSpeaker = i;
            this.LastSpeaker = l;
            this.Type = t;
            this.Duration = d;
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

    public enum CollaborativeProfile
    {
        None = 0,
        EverythingNothing = 1,
        IndependentSolitary = 2,
        IndependentSociable = 3,
        TeacherStudent = 4,
        LeaderFollower = 5,
        TurnTakersNonAccurate = 6,
        TurnTakersAccurate = 7,
    }

    public enum DuoType
    {
        P01 = 0,
        P02 = 1,
        P12 = 2,
        Trio = 5,
        Nan = 6,
        All = 7,
    }
}

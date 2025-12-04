
namespace SAAC.GlobalHelpers
{
    public class IDs
    {
        public string UserID { get; private set; }// Identifiant utilisateur
        public string ObjectID { get; private set; }// Identifiant d'objet

        public IDs(string userID, string objectID)
        {
            UserID = userID;
            ObjectID = objectID;
        }
    }

    public enum EEventType { Hover, Select, UI, Focus, Activate }


    public class GazeEvent : IDs
    {
        public EEventType Type { get; private set; }
        public bool IsGazed { get; private set; }// Statut de l'événement (activé/désactivé)

        // Constructeur pour initialiser les valeurs
        public GazeEvent(EEventType type, string userId, string objectID, bool isGazed)
            : base(userId, objectID)
        {
            Type = type;
            IsGazed = isGazed;
        }
    }

    public class GrabEvent : IDs
    {
        public EEventType Type { get; private set; }
        public bool IsGrabbed { get; private set; }// Statut de l'événement (activé/désactivé)

        // Constructeur pour initialiser les valeurs
        public GrabEvent(EEventType type, string userId, string objectID, bool isGrabbed)
            : base(userId, objectID)
        {
            Type = type;
            IsGrabbed = isGrabbed;
        }
    }
}
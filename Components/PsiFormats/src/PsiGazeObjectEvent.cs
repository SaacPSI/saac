namespace SAAC.PsiFormats
{
    /// <summary>
    /// Structure representing a gaze event on a specific object.
    /// Matches the Unity SendGazeEvent signature:
    /// (int userId,
    ///  int objectId,
    ///  bool isGazing,
    ///  string objectType)
    /// </summary>
    public struct PsiGazeObjectEvent
    {
        public int UserId { get; set; }
        public int ObjectId { get; set; }
        public bool IsGazing { get; set; }
        public string ObjectType { get; set; }

        public PsiGazeObjectEvent(int userId, int objectId, bool isGazing, string objectType)
        {
            UserId = userId;
            ObjectId = objectId;
            IsGazing = isGazing;
            ObjectType = objectType;
        }
    }
}


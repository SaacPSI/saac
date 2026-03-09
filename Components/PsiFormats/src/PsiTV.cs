namespace SAAC.PsiFormats
{
    /// <summary>
    /// Structure representing a TV interaction event.
    /// Matches the Unity sendData signature:
    /// (int id,
    ///  int val1,
    ///  int val2,
    ///  string message)
    /// </summary>
    public struct PsiTV
    {
        public int Id { get; set; }
        public int Val1 { get; set; }
        public int Val2 { get; set; }
        public string Message { get; set; }

        public PsiTV(int id, int val1, int val2, string message)
        {
            Id = id;
            Val1 = val1;
            Val2 = val2;
            Message = message;
        }
    }
}


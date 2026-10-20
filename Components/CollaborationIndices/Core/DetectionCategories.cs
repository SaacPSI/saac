namespace SAAC.CollaborationIndices
{
    /// <summary>
    /// Categories of the events and intervals published by the detectors that no indicator
    /// counts yet. The categories the indicators count are in <see cref="IndexCategories"/>.
    /// </summary>
    public static class DetectionCategories
    {
        /// <summary>A participant looks at an object; the object is in the label.</summary>
        public const string GazeOnObject = "GazeOnObject";

        /// <summary>A shared spatial formation of two participants begins; the kind is in the label.</summary>
        public const string FormationStart = "FormationStart";

        /// <summary>An object passes from one participant to another.</summary>
        public const string ObjectHandover = "ObjectHandover";
    }
}

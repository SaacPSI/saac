namespace SAAC.PipelineServices
{
    /// <summary>
    /// Provides a wrapper for the serialization format of the gaze events on objects.
    /// </summary>
    public class PsiFormatGazeObjectEvent : IPsiFormat
    {
        /// <summary>
        /// Gets the format for serializing and deserializing ObjectGazeEvent objects.
        /// </summary>
        /// <returns>A format instance for ObjectGazeEvent serialization.</returns>
        public dynamic GetFormat()
        {
            return PsiFormats.PsiFormatGazeObjectEvent.GetFormat();
        }
    }
}

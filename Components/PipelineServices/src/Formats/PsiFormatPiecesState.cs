namespace SAAC.PipelineServices
{
    /// <summary>
    /// Provides a wrapper for tuple of Vector3 serialization format.
    /// </summary>
    public class PsiFormatPiecesState : IPsiFormat
    {
        /// <summary>
        /// Gets the format for serializing and deserializing tuples of Vector3 objects.
        /// </summary>
        /// <returns>A format instance for tuple of Vector3 serialization.</returns>
        public dynamic GetFormat()
        {
            return PsiFormats.PsiFormatPiecesState.GetFormat();
        }
    }
}

// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.Whisper
{
    /// <summary>
    /// Segmentation restriction options for Whisper speech recognition.
    /// </summary>
    public enum SegmentationRestriction : byte
    {
        /// <summary>
        /// No segmentation restriction set.
        /// </summary>
        NotSet = 0,

        /// <summary>
        /// One segment per word.
        /// </summary>
        OnePerWord = 1,

        /// <summary>
        /// One segment per utterance.
        /// </summary>
        OnePerUtterence = 2,
    }
}

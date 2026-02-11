// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

#pragma warning disable SA1602 // Enumeration items should be documented - Language names are self-documenting

namespace SAAC.Whisper
{
    /// <summary>
    /// Supported languages for Whisper speech recognition.
    /// </summary>
    public enum Language : byte
    {
        /// <summary>
        /// Language not set (auto-detect).
        /// </summary>
        NotSet = 0,

        Afrikaans,
        Arabic,
        Armenian,
        Azerbaijani,
        Belarusian,
        Bosnian,
        Bulgarian,
        Catalan,
        Chinese,
        Croatian,
        Czech,
        Danish,
        Dutch,
        English,
        Estonian,
        Finnish,
        French,
        Galician,
        German,
        Greek,
        Hebrew,
        Hindi,
        Hungarian,
        Icelandic,
        Indonesian,
        Italian,
        Japanese,
        Kannada,
        Kazakh,
        Korean,
        Latvian,
        Lithuanian,
        Macedonian,
        Malay,
        Marathi,
        Maori,
        Nepali,
        Norwegian,
        Persian,
        Polish,
        Portuguese,
        Romanian,
        Russian,
        Serbian,
        Slovak,
        Slovenian,
        Spanish,
        Swahili,
        Swedish,
        Tagalog,
        Tamil,
        Thai,
        Turkish,
        Ukrainian,
        Urdu,
        Vietnamese,
        Welsh,
    }
}

#pragma warning restore SA1602

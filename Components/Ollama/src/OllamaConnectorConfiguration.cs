// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.Ollama
{
    /// <summary>
    /// Configuration for the Ollama connector.
    /// </summary>
    public class OllamaConectorConfiguration
    {
        /// <summary>
        /// Gets or sets the Ollama server address.
        /// </summary>
        public Uri OllamaAddress { get; set; } = new Uri("http://localhost:11434");

        /// <summary>
        /// Gets or sets the model name to use.
        /// </summary>
        public string Model { get; set; } = "llama2";
    }
}

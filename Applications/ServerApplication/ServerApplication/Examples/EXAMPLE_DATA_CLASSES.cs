/**
 * EXEMPLE DE CLASSE DE DONNÉES PERSONNALISÉE
 * 
 * Ce fichier montre comment créer une DLL compatible avec le ConfigurationLoader
 * 
 * Instructions :
 * 1. Créez un nouveau projet Class Library (.NET Framework 4.8)
 * 2. Copiez les classes ci-dessous dans votre projet
 * 3. Compilez en .dll
 * 4. Utilisez "Charger DLL" dans l'application pour la charger
 * 
 * Exemple de code C# :
 */

/*
using System;

namespace MyDataClasses
{
    /// <summary>
    /// Classe représentant des données de capteur
    /// </summary>
    public class SensorData
    {
        public float Temperature { get; set; }
        public float Humidity { get; set; }
        public DateTime Timestamp { get; set; }

        public override string ToString()
        {
            return $"Sensor(Temp={Temperature}°C, Humidity={Humidity}%, Time={Timestamp})";
        }
    }

    /// <summary>
    /// Classe représentant un frame audio
    /// </summary>
    public class AudioFrame
    {
        public byte[] Data { get; set; }
        public int SampleRate { get; set; }
        public int Channels { get; set; }

        public override string ToString()
        {
            return $"AudioFrame(Samples={Data?.Length}, SampleRate={SampleRate}Hz, Channels={Channels})";
        }
    }

    /// <summary>
    /// Classe représentant des données vidéo
    /// </summary>
    public class VideoFrame
    {
        public byte[] ImageData { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public string Format { get; set; } // "RGB", "YUV", etc.

        public override string ToString()
        {
            return $"VideoFrame({Width}x{Height}, {Format})";
        }
    }

    /// <summary>
    /// Classe représentant un message texte
    /// </summary>
    public class TextMessage
    {
        public string Content { get; set; }
        public string Source { get; set; }
        public DateTime Timestamp { get; set; }

        public override string ToString()
        {
            return $"Message from {Source}: {Content}";
        }
    }

    /// <summary>
    /// Exemple de transformer pour traiter les données du capteur
    /// </summary>
    public class SensorDataTransformer
    {
        // À implémenter selon votre interface de transformer
        public SensorData Transform(SensorData input)
        {
            // Appliquer des transformations si nécessaire
            return input;
        }
    }
}

*/

// Pour utiliser cette DLL :
// 1. Compilez-la
// 2. Ouvrez MainWindow.xaml.cs
// 3. Appelez LoadAssemblyFromFile() pour charger la DLL
// 4. Utilisez le template JSON généré pour configurer les topics
// 5. Appelez LoadConfigurationFromJsonFile() pour charger la configuration

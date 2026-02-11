// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.BiopacDataIntegration
{
    using System.IO;
    using Microsoft.Psi;
    using Microsoft.Psi.Data;

    /// <summary>
    /// Component that writes Biopac data from AcqKnowledge file to a PSI store, synchronizing with a given stream or time reference.
    /// </summary>
    internal class GTLoader
    {
        private readonly string dataPath;
        private readonly Pipeline pipeline;
        private readonly PsiExporter store;
        private readonly List<Emitter<float>> channels = new List<Emitter<float>>();
        private float timePerSample = 0.0f;

        /// <summary>
        /// Initializes a new instance of the <see cref="GTLoader"/> class.
        /// </summary>
        /// <param name="datasetPath">The path to the dataset where the store will be created and the session will be added.</param>
        public GTLoader(in string datasetPath)
        {
            this.pipeline = Pipeline.Create("BiopacDataIntegration", enableDiagnostics: true);
            this.dataPath = datasetPath.Substring(0, datasetPath.LastIndexOf('\\'));
            this.store = PsiStore.Create(this.pipeline, "Biopac", this.dataPath);
            var dataset = Dataset.Load(datasetPath);
            var session = dataset.AddEmptySession("Biopac");
            session.AddPartitionFromPsiStoreAsync("Biopac", this.dataPath);
            dataset.Save();
        }

        /// <summary>
        /// Loads the time reference from a specified stream in a PSI store.
        /// </summary>
        /// <typeparam name="T">The type of the stream data.</typeparam>
        /// <param name="refStoreName">The name of the reference store to load.</param>
        /// <param name="refStreamName">The name of the reference stream to load.</param>
        /// <param name="dateTimeReference">The loaded time reference.</param>
        /// <returns>True if the time reference was successfully loaded; otherwise false.</returns>
        public bool LoadReferenceTime<T>(in string refStoreName, in string refStreamName, out DateTime dateTimeReference)
        {
            var store = new PsiStoreStreamReader(refStoreName, this.dataPath);
            dateTimeReference = DateTime.MinValue;
            if (store == null)
            {
                return false;
            }

            Envelope envelope;
            var stream = store.OpenStreamIndex<T>(refStreamName, (d, e) => { envelope = e; });
            if (stream != null)
            {
                dateTimeReference = stream.FirstMessageOriginatingTime;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Parses a GTL (AcqKnowledge) file and writes the data to the PSI store.
        /// </summary>
        /// <param name="gtlFile">The path to the GTL file to parse.</param>
        /// <param name="dateTimeReference">The time reference for synchronization.</param>
        /// <returns>True if parsing was successful; otherwise false.</returns>
        public bool Parse(in string gtlFile, in DateTime dateTimeReference)
        {
            string[] lines = File.ReadAllLines(gtlFile);
            if (lines.Length == 0)
            {
                return false;
            }

            int channelsNumber = 0;
            bool startAsynchPipeline = false;
            long index = (2 * channelsNumber) + 5;
            for (long iterator = 0; iterator < lines.Length; iterator++)
            {
                if (iterator < 3) // Header
                {
                    this.ParseHeader(lines[iterator], iterator, out channelsNumber);
                    continue;
                }

                if (iterator < ((2 * channelsNumber) + 3)) // Metadata
                {
                    this.ParseMetadata(lines[iterator], iterator);
                    continue;
                }

                if (iterator < index) // Channels
                {
                    continue;
                }

                if (startAsynchPipeline == false)
                {
                    this.pipeline.RunAsync();
                    startAsynchPipeline = true;
                }

                this.ParseData(lines[iterator], iterator - index, dateTimeReference);
            }

            this.pipeline.Dispose();
            return false;
        }

        /// <summary>
        /// Parses the header section of the GTL file.
        /// </summary>
        /// <param name="header">The header line content.</param>
        /// <param name="lineIndex">The line index in the file.</param>
        /// <param name="channelsNumber">The number of channels parsed from the header.</param>
        private void ParseHeader(in string header, in long lineIndex, out int channelsNumber)
        {
            channelsNumber = 0;
            switch (lineIndex)
            {
                case 1:
                    float.TryParse(header.Split(' ')[0].Replace('.', ','), out this.timePerSample);
                    this.timePerSample *= 10000f;
                    break;
                case 2:
                    int.TryParse(header.Split(' ')[0], out channelsNumber);
                    break;
            }
        }

        /// <summary>
        /// Parses the metadata section of the GTL file and creates channel emitters.
        /// </summary>
        /// <param name="header">The metadata line content.</param>
        /// <param name="lineIndex">The line index in the file.</param>
        private void ParseMetadata(in string header, in long lineIndex)
        {
            if (lineIndex % 2 == 0)
            {
                return;
            }

            var splited = header.Split(',');
            Emitter<float> channel = this.pipeline.CreateEmitter<float>(this.pipeline, splited[0]);
            this.channels.Add(channel);
            this.store.Write(channel, channel.Name);
        }

        /// <summary>
        /// Parses the data section of the GTL file and posts values to channel emitters.
        /// </summary>
        /// <param name="header">The data line content.</param>
        /// <param name="sampleNumber">The sample number.</param>
        /// <param name="dateTimeReference">The time reference for timestamp calculation.</param>
        private void ParseData(in string header, in long sampleNumber, in DateTime dateTimeReference)
        {
            if (sampleNumber % 2 == 1)
            {
                return;
            }

            var splited = header.Split('\t');
            DateTime time = dateTimeReference.AddTicks((long)this.timePerSample * sampleNumber);
            for (int iterator = 1; iterator < splited.Length; iterator++)
            {
                float data;
                var val = splited[iterator].Replace('.', ',');
                if (float.TryParse(val, out data))
                {
                    this.channels[iterator - 1].Post(data, time);
                }
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Psi;
using Microsoft.Psi.Data;

namespace BiopacDataIntegration
{
    class GTLoader
    {
        private float timePerSample = 0.0f;
        private string dataPath;

        private Pipeline pipeline;
        private PsiExporter store;
        private List<Emitter<float>> channels = new List<Emitter<float>>();

        public GTLoader(in string datasetPath) 
        {
            pipeline = Pipeline.Create("BiopacDataIntegration", enableDiagnostics: true);
            dataPath = datasetPath.Substring(0, datasetPath.LastIndexOf('\\'));
            store = PsiStore.Create(pipeline, "Biopac", dataPath);
            var dataset = Dataset.Load(datasetPath);
            var session = dataset.CreateSession("Biopac");
            session.AddPsiStorePartition("Biopac", dataPath);
            dataset.Save();
        }

        public bool LoadReferenceTime(out DateTime dateTimeReference)
        {
            var store = new PsiStoreStreamReader("VoiceDetecion", dataPath);
            dateTimeReference = DateTime.MinValue;
            if (store == null)
                return false;
            Envelope envelope;
            var stream = store.OpenStreamIndex<bool>("VoiceDetecion", (d, e) => { envelope = e; });
            if (stream != null)
            {
                dateTimeReference = stream.FirstMessageOriginatingTime;
                return true;
            }
            return false;
        }

        public bool Parse(in string gtlFile, in DateTime dateTimeReference)
        {
            string[] lines = File.ReadAllLines(gtlFile);
            if(lines.Length == 0)
                return false;
                
            int channelsNumber = 0;
            bool startAsynchPipeline = false;
            long index = (2 * channelsNumber) + 5;
            for (long iterator  = 0; iterator < lines.Length; iterator++)
            {
                if(iterator < 3) //Header
                {
                    ParseHeader(lines[iterator], iterator, out channelsNumber);
                    continue;
                }
                if(iterator < ((2 * channelsNumber) + 3)) //Metadata
                {
                    ParseMetadata(lines[iterator], iterator); 
                    continue;
                }
                if (iterator < index) //Channels
                {
                    continue;
                }
                if (startAsynchPipeline == false)
                {
                    pipeline.RunAsync();
                    startAsynchPipeline = true;
                }
                ParseData(lines[iterator], iterator-index, dateTimeReference);
            }
            pipeline.Dispose();
            return false;
        }

        private void ParseHeader(in string header, in long lineIndex, out int channelsNumber) 
        {
            channelsNumber = 0;
            switch(lineIndex)
            {
                case 1:
                    float.TryParse(header.Split(' ')[0].Replace('.',','), out timePerSample);
                    timePerSample *= 10000f;
                    break;
                case 2:
                    int.TryParse(header.Split(' ')[0], out channelsNumber);
                    break;
            }
        }

        private void ParseMetadata(in string header, in long lineIndex)
        {
            if (lineIndex % 2 == 0)
                return;
            var splited = header.Split(',');
            Emitter<float> channel = pipeline.CreateEmitter<float>(pipeline, splited[0]);
            channels.Add(channel);
            store.Write(channel, channel.Name);
        }

        private void ParseData(in string header, in long sampleNumber, in DateTime dateTimeReference)
        {
            if (sampleNumber % 2 == 1)
                return;
            var splited = header.Split('\t');
            DateTime time = dateTimeReference.AddTicks((long)timePerSample * sampleNumber);
            for (int iterator = 1; iterator < splited.Length; iterator++)
            {
                float data;
                var val = splited[iterator].Replace('.', ',');
                if (float.TryParse(val, out data))
                    channels[iterator-1].Post(data, time);
            }
        }
    }
}

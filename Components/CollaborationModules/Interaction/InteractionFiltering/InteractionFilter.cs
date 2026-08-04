using System;
using System.IO;
using Microsoft.Psi;
using Microsoft.Psi.Data;
using SAAC.PipelineServices;
using SAAC.PsiFormats;

namespace InteractionFiltering
{
    public class InteractionFilterConfiguration
    {
        public int sessionNum = 0;
        public string csvAdress = string.Empty;
        public int userID;
        public string condition = string.Empty;
    }

    public class InteractionFilter
    {
        public StreamWriter interactionWriter;
        private InteractionFilterConfiguration interactionConfiguration;
        private string interactionEventHeadupEU = "utc_timestamp_ms,participant_id,interaction_type,interaction_state,object_id,area".Replace(',', ';');
        private Session sessionName;
        private bool isHeadup = false;

        public InteractionFilter(Pipeline pipeline, DatasetPipeline server, InteractionFilterConfiguration? configuration = null)
        {
            this.interactionConfiguration = configuration ?? new InteractionFilterConfiguration();

            // Receiver
            this.InteractionIn = pipeline.CreateReceiver<PieceStatus>(this, Process, nameof(this.InteractionIn));

            // Emitter
            this.InteractionOut = pipeline.CreateEmitter<PieceStatus>(this, nameof(this.InteractionOut));
            this.InteractionStringOut = pipeline.CreateEmitter<string>(this, nameof(this.InteractionStringOut));

            this.sessionName = server.GetSession("RawDataPipelineProcess.000");

            server.CreateConnectorAndStore($"{this.interactionConfiguration.userID + 1}_Interactions", "LiveVisualization", this.sessionName, pipeline, this.InteractionOut.Type, this.InteractionOut, true);
            server.CreateConnectorAndStore($"{this.interactionConfiguration.userID + 1}_Interactions_string", "LiveVisualization", this.sessionName, pipeline, this.InteractionStringOut.Type, this.InteractionStringOut, true);

            this.interactionWriter = new StreamWriter($@"{configuration.csvAdress}\{configuration.sessionNum}_{configuration.condition}-{configuration.userID + 1}_interaction_event.csv");

            if (!this.isHeadup)
            {
                this.interactionWriter.WriteLine(this.interactionEventHeadupEU);
                this.isHeadup = true;
            }
        }

        private void Process(PieceStatus status, Envelope envelope)
        {
            status.originatingTime = envelope.OriginatingTime;
            string messageEventFormat = $"{envelope.OriginatingTime.ToUniversalTime().Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds.ToString().Replace(',', '.')};{status.userID};Piece;{status.type};{status.objectID};{status.currentLocation}".Replace(',', '.');
            this.interactionWriter.WriteLine(messageEventFormat);
            this.InteractionOut.Post(status, envelope.OriginatingTime);
            this.InteractionStringOut.Post(messageEventFormat, envelope.OriginatingTime);
        }

        #region Receivers
        public Receiver<PieceStatus> InteractionIn { get; private set; }
        #endregion

        #region Emitters
        public Emitter<PieceStatus> InteractionOut { get; private set; }

        public Emitter<string> InteractionStringOut { get; private set; }
        #endregion
    }
}

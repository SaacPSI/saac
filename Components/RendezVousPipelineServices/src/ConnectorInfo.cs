using Microsoft.Psi;

namespace SAAC.RendezVousPipelineServices
{
    public class ConnectorInfo
    {
        public string SourceName { get; private set; }
        public string SessionName { get; private set; }
        public Type DataType { get; private set;}
        private dynamic source;

        public ConnectorInfo(string sourceName, string sessionName, Type dataType, dynamic source)
        {
            SourceName = sourceName;
            SessionName = sessionName;
            DataType = dataType;
            this.source = source;
        }

        public dynamic CreateBridge<T>(Pipeline pipeline)
        {
            return Microsoft.Psi.Operators.BridgeTo(source, pipeline, $"{SourceName}->{pipeline.Name}");
        }
    }
}

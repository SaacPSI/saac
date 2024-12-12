using Microsoft.Psi;

namespace SAAC.PipelineServices
{
    public class ConnectorInfo
    {
        public string SourceName { get; private set; }
        public string SessionName { get; private set; }
        public string StoreName { get; private set; }
        public Type DataType { get; private set;}
        private dynamic source;

        public ConnectorInfo(string sourceName, string sessionName, string storeName, Type dataType, dynamic source)
        {
            SourceName = sourceName;
            SessionName = sessionName;
            StoreName = storeName;
            DataType = dataType;
            this.source = source;
        }

        public dynamic CreateBridge<T>(Pipeline pipeline)
        {
            return Microsoft.Psi.Operators.BridgeTo(source, pipeline, $"{SourceName}->{pipeline.Name}");
        }
    }
}

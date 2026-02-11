// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.PipelineServices
{
    using Microsoft.Psi;

    /// <summary>
    /// Contains information about a pipeline connector including source, session, store, and data type.
    /// </summary>
    public class ConnectorInfo
    {
        /// <summary>
        /// Gets the name of the source stream.
        /// </summary>
        public string SourceName { get; private set; }

        /// <summary>
        /// Gets the name of the session.
        /// </summary>
        public string SessionName { get; private set; }

        /// <summary>
        /// Gets the name of the store.
        /// </summary>
        public string StoreName { get; private set; }

        /// <summary>
        /// Gets the data type of the connector.
        /// </summary>
        public Type DataType { get; private set; }

        private dynamic source;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectorInfo"/> class.
        /// </summary>
        /// <param name="sourceName">The source stream name.</param>
        /// <param name="sessionName">The session name.</param>
        /// <param name="storeName">The store name.</param>
        /// <param name="dataType">The data type.</param>
        /// <param name="source">The source object.</param>
        public ConnectorInfo(string sourceName, string sessionName, string storeName, Type dataType, dynamic source)
        {
            this.SourceName = sourceName;
            this.SessionName = sessionName;
            this.StoreName = storeName;
            this.DataType = dataType;
            this.source = source;
        }

        /// <summary>
        /// Creates a bridge connector for the specified pipeline.
        /// </summary>
        /// <typeparam name="T">The type of data to bridge.</typeparam>
        /// <param name="pipeline">The pipeline to create the bridge in.</param>
        /// <returns>A dynamic bridge connector.</returns>
        public dynamic CreateBridge<T>(Pipeline pipeline)
        {
            return Microsoft.Psi.Operators.BridgeTo(this.source, pipeline, $"{this.SourceName}->{pipeline.Name}");
        }
    }
}

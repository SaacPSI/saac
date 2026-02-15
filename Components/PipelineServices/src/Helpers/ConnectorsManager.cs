// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.PipelineServices
{
    using Microsoft.Psi;
    using Microsoft.Psi.Data;

    /// <summary>
    /// Abstract base class for managing pipeline connectors.
    /// </summary>
    public abstract class ConnectorsManager : IDisposable
    {
        /// <summary>
        /// Gets the dictionary of connectors organized by store and stream name.
        /// </summary>
        public Dictionary<string, Dictionary<string, ConnectorInfo>> Connectors { get; internal set; }

        /// <summary>
        /// Event handler for new connector entries.
        /// </summary>
        internal EventHandler<(string, Dictionary<string, Dictionary<string, ConnectorInfo>>)>? NewEntry;

        /// <summary>
        /// Event handler for removed connector entries.
        /// </summary>
        internal EventHandler<string>? RemovedEntry;

        /// <summary>
        /// The name of this manager.
        /// </summary>
        protected string name;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectorsManager"/> class.
        /// </summary>
        /// <param name="connectors">Optional dictionary of existing connectors.</param>
        /// <param name="name">The name of the manager.</param>
        public ConnectorsManager(Dictionary<string, Dictionary<string, ConnectorInfo>>? connectors = null, string name = nameof(ConnectorsManager))
        {
            this.name = name;
            this.Connectors = connectors ?? new Dictionary<string, Dictionary<string, ConnectorInfo>>();
        }

        /// <summary>
        /// Disposes resources used by this manager.
        /// </summary>
        public virtual void Dispose()
        {
            this.NewEntry = null;
            this.RemovedEntry = null;
            this.Connectors.Clear();
        }

        /// <summary>
        /// Creates a connector for the specified stream.
        /// </summary>
        /// <typeparam name="T">The type of data in the stream.</typeparam>
        /// <param name="streamName">The stream name.</param>
        /// <param name="storeName">The store name.</param>
        /// <param name="session">The session to use.</param>
        /// <param name="type">The data type.</param>
        /// <param name="stream">The producer stream.</param>
        public void CreateConnector<T>(string streamName, string storeName, Session? session, Type type, IProducer<T> stream)
        {
            if (!this.Connectors.ContainsKey(storeName))
            {
                this.Connectors.Add(storeName, new Dictionary<string, ConnectorInfo>());
            }

            this.Connectors[storeName].Add(streamName, new ConnectorInfo(streamName, storeName, session == null ? string.Empty : session.Name, type, stream));
        }

        /// <summary>
        /// Triggers the new process event.
        /// </summary>
        /// <param name="name">The process name.</param>
        public void TriggerNewProcessEvent(string name)
        {
            this.NewEntry?.Invoke(this, (name, this.Connectors));
        }

        /// <summary>
        /// Triggers the remove process event.
        /// </summary>
        /// <param name="name">The process name.</param>
        public void TriggerRemoveProcessEvent(string name)
        {
            this.RemovedEntry?.Invoke(this, name);
        }

        /// <summary>
        /// Adds an event handler for new process events.
        /// </summary>
        /// <param name="handler">The event handler to add.</param>
        public virtual void AddNewProcessEvent(EventHandler<(string, Dictionary<string, Dictionary<string, ConnectorInfo>>)> handler)
        {
            this.NewEntry += handler;
        }

        /// <summary>
        /// Adds an event handler for remove process events.
        /// </summary>
        /// <param name="handler">The event handler to add.</param>
        public virtual void AddRemoveProcessEvent(EventHandler<string> handler)
        {
            this.RemovedEntry += handler;
        }

        /// <inheritdoc/>
        public override string ToString() => this.name;
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.PsiStudio
{
    using System;
    using Microsoft.Psi.Interop.Transport;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Provides a base class for stream  objects that show the stream value at cursor.
    /// </summary>
    /// <typeparam name="TData">The type of stream values to visualize.</typeparam>
    public class NetworkedStreamValueVisualisationObject<TData> : IDisposable
    {
        private TcpSimpleWriter<TData> tcpSimpleWriter;

        /// <summary>
        /// Initializes a new instance of the <see cref="NetworkedStreamValueVisualisationObject{TData}"/> class.
        /// </summary>
        /// <param name="writer">Writer to send data over Tcp.</param>
        /// <param name="streamSource">StreamSource to bound with.</param>
        public NetworkedStreamValueVisualisationObject(TcpSimpleWriter<TData> writer, StreamSource streamSource)
        {
            this.tcpSimpleWriter = writer;
            this.StreamSource = streamSource;

            // TODO check RelativeTimeInterval & TimeInterval.
            this.SubscriberId = DataManager.Instance.RegisterStreamValueSubscriber<TData>(
              this.StreamSource,
              RelativeTimeInterval.Infinite,
              this.OnValueReceived,
              TimeInterval.Empty);
        }

        /// <summary>
        /// Gets the source for the stream data, or null if the visualization object is
        /// not currently bound to a source.
        /// </summary>
        public StreamSource StreamSource { get; private set; } = null;

        /// <summary>
        /// Gets or sets the visualization object's subscriber id.  This value is Guid.Empty
        /// if the visualization object is not currently subscribed to a data provider.
        /// </summary>
        protected Guid SubscriberId { get; set; } = Guid.Empty;

        /// <inheritdoc />
        public void Dispose()
        {
            // Unregister the stream value visualization object from the data manager
            if (this.SubscriberId != Guid.Empty)
            {
                DataManager.Instance.UnregisterStreamValueSubscriber<TData>(this.SubscriberId);
                this.SubscriberId = Guid.Empty;
                this.tcpSimpleWriter.Dispose();
            }
        }

        /// <summary>
        /// Called when the current value has changed.  This method is called on a worker thread.
        /// </summary>
        /// <param name="dataAvailable">Indicates whether data is available.</param>
        /// <param name="value">The new value.</param>
        /// <param name="originatingTime">The originating time for the new value.</param>
        /// <param name="creationTime">The creation time for the new value.</param>
        private void OnValueReceived(bool dataAvailable, TData value, DateTime originatingTime, DateTime creationTime)
        {
            if (dataAvailable)
            {
                // TODO check sourceid & sequenceid.
                this.tcpSimpleWriter.Receive(value, new Envelope(originatingTime, creationTime, 0, 0));
            }
        }
    }
}

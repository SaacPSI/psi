// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Interop.Transport
{
    using Microsoft.Psi;
    using Microsoft.Psi.Interop.Serialization;

    /// <summary>
    /// Component that serializes and writes messages to a remote host over UDP.
    /// </summary>
    /// <typeparam name="T">The type of the messages.</typeparam>
    public class UdpWriter<T> : UdpSimpleWriter<T>, IConsumer<T>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UdpWriter{T}"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="port">The destination port.</param>
        /// <param name="serializer">The serializer to use to serialize messages.</param>
        /// <param name="isBroadcasting">Flag indicating whether the writer should broadcast messages.</param>
        /// <param name="name">An optional name for the component.</param>
        public UdpWriter(
            Pipeline pipeline,
            int port,
            IFormatSerializer serializer,
            bool isBroadcasting = false,
            string name = nameof(UdpWriter<T>))
            : base(port, serializer, isBroadcasting, name)
        {
            this.In = pipeline.CreateReceiver<T>(this, this.Receive, nameof(this.In));
        }

        /// <inheritdoc/>
        public Receiver<T> In { get; }
    }
}

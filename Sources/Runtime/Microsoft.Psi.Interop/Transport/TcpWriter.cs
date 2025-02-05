// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Interop.Transport
{
    using System;
    using System.Diagnostics;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;
    using Microsoft.Psi;
    using Microsoft.Psi.Interop.Serialization;

    /// <summary>
    /// Component that serializes and writes messages to a remote server over TCP.
    /// </summary>
    /// <typeparam name="T">The type of the messages.</typeparam>
    public class TcpWriter<T> : TcpSimpleWriter<T>, IConsumer<T>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TcpWriter{T}"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="port">The connection port.</param>
        /// <param name="serializer">The serializer to use to serialize messages.</param>
        /// <param name="name">An optional name for the component.</param>
        public TcpWriter(Pipeline pipeline, int port, IFormatSerializer serializer, string name = nameof(TcpWriter<T>))
            : base (port, serializer, name)
        {
            this.In = pipeline.CreateReceiver<T>(this, this.Receive, nameof(this.In));
        }

        /// <inheritdoc/>
        public Receiver<T> In { get; }
    }
}

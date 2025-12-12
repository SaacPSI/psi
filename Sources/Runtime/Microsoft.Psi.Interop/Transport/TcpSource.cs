// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Interop.Transport
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Net.Sockets;
    using System.Threading;
    using Microsoft.Psi;
    using Microsoft.Psi.Components;
    using Microsoft.Psi.Interop.Serialization;

    /// <summary>
    /// Component that reads and deserializes messages from a remote server over TCP.
    /// </summary>
    /// <typeparam name="T">The type of the messages.</typeparam>
    public class TcpSource<T> : TcpSimpleSource<T>, IProducer<T>
    {
        private readonly Pipeline pipeline;
        private readonly bool useSourceOriginatingTimes;

        /// <summary>
        /// Initializes a new instance of the <see cref="TcpSource{T}"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="address">The address of the remote server.</param>
        /// <param name="port">The port on which to connect.</param>
        /// <param name="deserializer">The deserializer to use to deserialize messages.</param>
        /// <param name="deallocator">An optional deallocator for the data.</param>
        /// <param name="useSourceOriginatingTimes">An optional parameter indicating whether to use originating times from the source received over the network or to re-timestamp with the current pipeline time upon receiving.</param>
        /// <param name="name">An optional name for the component.</param>
        public TcpSource(
            Pipeline pipeline,
            string address,
            int port,
            IFormatDeserializer deserializer,
            Action<T> deallocator = null,
            bool useSourceOriginatingTimes = true,
            string name = nameof(TcpSource<T>))
            : base(address, port, deserializer, deallocator, name)
        {
            this.pipeline = pipeline;
            this.useSourceOriginatingTimes = useSourceOriginatingTimes;
            this.Out = pipeline.CreateEmitter<T>(this, nameof(this.Out));
            this.OnMessageRecieved = this.Messagehandler;
        }

        /// <inheritdoc/>
        public Emitter<T> Out { get; }

        /// <inheritdoc/>
        public override void Start(Action<DateTime> notifyCompletionTime)
        {
            this.SetEndTime(this.Out.Pipeline.ReplayDescriptor.End);
            base.Start(notifyCompletionTime);
        }

        private void Messagehandler(T message, DateTime dateTime)
        {
            if (this.useSourceOriginatingTimes)
            {
                this.Out.Post(message, dateTime);
            }
            else
            {
                this.Out.Post(message, this.pipeline.GetCurrentTime());
            }
        }
    }
}

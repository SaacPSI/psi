// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Interop.Transport
{
    using System;
    using Microsoft.Psi;
    using Microsoft.Psi.Interop.Serialization;

    /// <summary>
    /// Component that reads and deserializes messages from a UDP socket.
    /// </summary>
    /// <typeparam name="T">The type of the messages.</typeparam>
    public class UdpSource<T> : UdpSimpleSource<T>, IProducer<T>
    {
        private readonly Pipeline pipeline;
        private readonly bool useSourceOriginatingTimes;

        /// <summary>
        /// Initializes a new instance of the <see cref="UdpSource{T}"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="port">The local port on which to listen.</param>
        /// <param name="deserializer">The deserializer to use to deserialize messages.</param>
        /// <param name="deallocator">An optional deallocator for the data.</param>
        /// <param name="useSourceOriginatingTimes">An optional parameter indicating whether to use originating times received from the source over the network or to re-timestamp with the current pipeline time upon receiving.</param>
        /// <param name="name">An optional name for the component.</param>
        public UdpSource(
            Pipeline pipeline,
            int port,
            IFormatDeserializer deserializer,
            Action<T> deallocator = null,
            bool useSourceOriginatingTimes = true,
            string name = nameof(UdpSource<T>))
            : base(port, deserializer, deallocator, name)
        {
            this.pipeline = pipeline;
            this.useSourceOriginatingTimes = useSourceOriginatingTimes;
            this.Out = pipeline.CreateEmitter<T>(this, nameof(this.Out));
            this.OnMessageReceived = this.MessageHandler;
        }

        /// <inheritdoc/>
        public Emitter<T> Out { get; }

        /// <inheritdoc/>
        public override void Start(Action<DateTime> notifyCompletionTime)
        {
            this.SetEndTime(this.Out.Pipeline.ReplayDescriptor.End);
            base.Start(notifyCompletionTime);
        }

        private void MessageHandler(T message, DateTime dateTime)
        {
            if (this.useSourceOriginatingTimes)
            {
                if (!dateTime.Equals(this.Out.LastEnvelope.OriginatingTime))
                {
                    this.Out.Post(message, dateTime);
                }
            }
            else
            {
                this.Out.Post(message, this.pipeline.GetCurrentTime());
            }
        }
    }
}

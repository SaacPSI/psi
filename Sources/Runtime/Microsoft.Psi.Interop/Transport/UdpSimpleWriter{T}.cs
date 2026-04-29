// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Interop.Transport
{
    using System;
    using System.Diagnostics;
    using System.Net;
    using System.Net.Sockets;
    using Microsoft.Psi.Interop.Serialization;

    /// <summary>
    /// Component that serializes and writes messages to a remote host over UDP.
    /// </summary>
    /// <typeparam name="T">The type of the messages.</typeparam>
    public class UdpSimpleWriter<T> : ISimpleWriter<T>
    {
        private readonly IFormatSerializer serializer;
        private readonly string name;
        private UdpClient udpClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="UdpSimpleWriter{T}"/> class.
        /// </summary>
        /// <param name="port">The destination port.</param>
        /// <param name="serializer">The serializer to use to serialize messages.</param>
        /// <param name="isBrodcast">If the writer is broadcasting.</param>
        /// <param name="name">An optional name for the component.</param>
        public UdpSimpleWriter(
            int port,
            IFormatSerializer serializer,
            bool isBrodcast = false,
            string name = nameof(UdpSimpleWriter<T>))
        {
            this.serializer = serializer;
            this.name = name;
            this.Port = port;
            this.udpClient = new UdpClient();
            if (isBrodcast)
            {
                this.udpClient.EnableBroadcast = true;
                this.udpClient.Connect(IPAddress.Broadcast, port);
            }
            else
            {
                this.udpClient.Connect(IPAddress.Any, port);
            }
        }

        /// <summary>
        /// Gets the destination port.
        /// </summary>
        public int Port { get; private set; }

        /// <inheritdoc/>
        public void Dispose()
        {
            this.udpClient?.Dispose();
            this.udpClient = null;
        }

        /// <inheritdoc/>
        public override string ToString() => this.name;

        /// <summary>
        /// Serialize and send the message over UDP.
        /// </summary>
        /// <param name="message">The data to be sent.</param>
        /// <param name="envelope">The time data of the message.</param>
        public void Receive(T message, Envelope envelope)
        {
            (var bytes, int offset, int count) = this.serializer.SerializeMessage(message, envelope.OriginatingTime);

            var frame = new byte[sizeof(int) + count];
            BitConverter.GetBytes(count).CopyTo(frame, 0);
            Buffer.BlockCopy(bytes, offset, frame, sizeof(int), count);

            try
            {
                this.udpClient.Send(frame, frame.Length);
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"UdpSimpleWriter Exception: {ex.Message}");
            }
        }
    }
}

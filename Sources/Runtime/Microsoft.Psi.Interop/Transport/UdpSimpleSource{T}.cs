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
    using Microsoft.Psi.Components;
    using Microsoft.Psi.Interop.Serialization;

    /// <summary>
    /// Component that reads and deserializes messages from a UDP socket.
    /// </summary>
    /// <typeparam name="T">The type of the messages.</typeparam>
    public class UdpSimpleSource<T> : ISourceComponent, IDisposable
    {
        private readonly int port;
        private readonly IFormatDeserializer deserializer;
        private readonly Action<T> deallocator;
        private readonly string name;
        private UdpClient udpClient;
        private Thread readerThread;
        private Action<DateTime> completed;
        private DateTime endTime = DateTime.MaxValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="UdpSimpleSource{T}"/> class.
        /// </summary>
        /// <param name="port">The local port on which to listen.</param>
        /// <param name="deserializer">The deserializer to use to deserialize messages.</param>
        /// <param name="deallocator">An optional deallocator for the data.</param>
        /// <param name="name">An optional name for the component.</param>
        public UdpSimpleSource(
            int port,
            IFormatDeserializer deserializer,
            Action<T> deallocator = null,
            string name = nameof(UdpSimpleSource<T>))
        {
            this.port = port;
            this.deserializer = deserializer;
            this.deallocator = deallocator ?? (d =>
            {
                if (d is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            });
            this.name = name;
        }

        /// <summary>
        /// Definition of delegate for handling incoming messages.
        /// </summary>
        /// <param name="message">The incoming message.</param>
        /// <param name="dateTime">The timestamp of the message.</param>
        public delegate void OnMessageReceivedDelegate(T message, DateTime dateTime);

        /// <summary>
        /// Gets or sets an event on message received.
        /// </summary>
        public OnMessageReceivedDelegate OnMessageReceived { get; set; }

        /// <inheritdoc/>
        public void Dispose() => this.udpClient?.Close();

        /// <inheritdoc/>
        public virtual void Start(Action<DateTime> notifyCompletionTime)
        {
            this.completed = notifyCompletionTime;
            this.udpClient = new UdpClient(this.port);
            this.readerThread = new Thread(this.ReadDatagrams) { IsBackground = true };
            this.readerThread.Start();
        }

        /// <inheritdoc/>
        public void Stop(DateTime finalOriginatingTime, Action notifyCompleted)
        {
            this.endTime = finalOriginatingTime;
            this.udpClient?.Close();
            this.readerThread?.Join();
            notifyCompleted();
        }

        /// <inheritdoc/>
        public override string ToString() => this.name;

        /// <summary>
        /// Sets the end time for the source component.
        /// </summary>
        /// <param name="time">End time to set.</param>
        protected void SetEndTime(DateTime time)
        {
            this.endTime = time;
        }

        private void ReadDatagrams()
        {
            var remoteEp = new IPEndPoint(IPAddress.Any, 0);
            var lastTimestamp = DateTime.MinValue;

            try
            {
                while (true)
                {
                    var datagram = this.udpClient.Receive(ref remoteEp);

                    if (datagram.Length < sizeof(int))
                    {
                        continue;
                    }

                    var frameLength = BitConverter.ToInt32(datagram, 0);
                    if (datagram.Length < sizeof(int) + frameLength)
                    {
                        continue;
                    }

                    (var message, var originatingTime) = this.deserializer.DeserializeMessage(datagram, sizeof(int), frameLength);

                    if (originatingTime > this.endTime)
                    {
                        break;
                    }

                    lastTimestamp = originatingTime;
                    this.OnMessageReceived?.Invoke((T)message, originatingTime);
                    this.deallocator((T)message);
                }
            }
            catch (SocketException)
            {
                // Socket closed by Stop()
            }
            catch (ObjectDisposedException)
            {
                // Socket disposed
            }
            finally
            {
                this.completed?.Invoke(lastTimestamp);
            }
        }
    }
}

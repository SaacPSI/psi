// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Interop.Transport
{
    using System;

    /// <summary>
    /// Interface for components that can receive messages to be sent to a remote host.
    /// </summary>
    /// <typeparam name="T">The type of the messages.</typeparam>
    public interface ISimpleWriter<T> : IDisposable
    {
        /// <summary>
        /// Serialize and send the message.
        /// </summary>
        /// <param name="message">The data to be sent.</param>
        /// <param name="envelope">The time data of the message.</param>
        public void Receive(T message, Envelope envelope);
    }
}

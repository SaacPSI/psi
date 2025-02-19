// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Represents a stream visualization object.
    /// </summary>
    public interface IActivableStreamVisualizationObject
    {
        /// <summary>
        /// Gets or sets a value indicating whether the source of the stream visualization object is active.
        /// </summary>
        bool IsActive { get; set; }

        /// <summary>
        /// Gets the name of the stream visualization object.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the source for the stream data, or null if the visualization object is not currently bound to a source.
        /// </summary>
        StreamSource StreamSource { get; }

        /// <summary>
        /// Clean the internal attributes in order to exit proprely.
        /// </summary>
        void Dispose();
    }
}

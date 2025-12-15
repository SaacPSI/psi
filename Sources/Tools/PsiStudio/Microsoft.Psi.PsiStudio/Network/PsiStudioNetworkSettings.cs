// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.PsiStudio
{
    /// <summary>
    /// Implements temporary network settings for Psi Studio.
    /// </summary>
    public class PsiStudioNetworkSettings
    {
        /// <summary>
        /// Gets or sets a value indicating whether gets or sets the RendzeVous broadcast of dataset is active.
        /// </summary>
        public bool IsActive { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether gets or sets audio streams are broadcasted.
        /// </summary>
        public bool IsAudio { get; set; } = false;

        /// <summary>
        /// Gets or sets the name of the process, if empty the rendez takes the session name as process name.
        /// </summary>
        public string ProcessNameOverriding { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether gets or sets if the exporter will be TCPWriter or RemoteExporter.
        /// </summary>
        public bool UseTcpWriter { get; set; } = true;

        /// <summary>
        /// Gets or sets address to use for endpoint.
        /// </summary>
        public string EndpointAddress { get; set; } = "localhost";

        /// <summary>
        /// Gets or sets the port of the rendezVous.
        /// </summary>
        public int RendezVousPort { get; set; } = 13331;

        /// <summary>
        /// Gets or sets the port of the rendezVous address, if empty the network will instanciate a rendezVous else a client.
        /// </summary>
        public string RendezVousAddress { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the starting port for exporters.
        /// </summary>
        public int ExporterStartingPort { get; set; } = 15551;

        /// <summary>
        /// Gets or sets the name of the process of the incoming command of replay for PsiStudio, if empty source will not be used.
        /// </summary>
        public string CommandProcessName { get; set; } = "PsiStudioCommand";
    }
}

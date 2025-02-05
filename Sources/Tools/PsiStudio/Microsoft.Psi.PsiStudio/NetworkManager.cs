// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.PsiStudio
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.Psi.Components;
    using Microsoft.Psi.Data;
    using Microsoft.Psi.Interop.Rendezvous;
    using Microsoft.Psi.Interop.Serialization;
    using Microsoft.Psi.Interop.Transport;
    using Microsoft.Psi.Visualization;
    using Microsoft.Psi.Visualization.Data;
    using Microsoft.Psi.Visualization.ViewModels;

    /// <summary>
    /// Implements temporary network settings for Psi Studio.
    /// </summary>
    internal class NetworkManager : IDisposable
    {
        private RendezvousServer server;
        private string lastProcessName;
        private int currentPort;
        private List<IDisposable> networkStreams;

        /// <summary>
        /// Initializes a new instance of the <see cref="NetworkManager"/> class.
        /// </summary>
        public NetworkManager()
        {
            this.Settings = new PsiStudioNetworkSettings();
            this.currentPort = this.Settings.ExporterStartingPort;
            this.networkStreams = new List<IDisposable>();

            // Listen for events that occur when the DatasetViewModel change
            VisualizationContext.Instance.PropertyChanged += this.DatasetViewModelChanged;

            // Listen for events that occur when the session change
            VisualizationContext.Instance.DatasetViewModel.PropertyChanged += this.CurrentSessionChanged;
        }

        /// <summary>
        /// Gets the current network settings.
        /// </summary>
        public PsiStudioNetworkSettings Settings { get; private set; }

        /// <summary>
        /// Update the settings.
        /// </summary>
        /// <param name="newSettings">The new network settings to update.</param>
        public void UpdateSettings(PsiStudioNetworkSettings newSettings)
        {
            if (newSettings.IsActive != this.Settings.IsActive)
            {
                this.server.Stop();
                if (newSettings.IsActive)
                {
                    this.server = new RendezvousServer(newSettings.RendezVousPort);
                    this.server.Start();
                }
            }

            this.Settings = newSettings;
        }

        /// <summary>
        /// Stop RendezVous Server.
        /// </summary>
        public void Dispose()
        {
            this.server.Dispose();
            foreach (IDisposable networkStream in this.networkStreams)
            {
                networkStream.Dispose();
            }
        }

        private void DatasetViewModelChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(VisualizationContext.Instance.DatasetViewModel))
            {
                return;
            }

            VisualizationContext.Instance.DatasetViewModel.PropertyChanged += this.CurrentSessionChanged;
            this.UpdateStreams();
        }

        private void CurrentSessionChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(VisualizationContext.Instance.DatasetViewModel.CurrentSessionViewModel))
            {
                return;
            }

            this.UpdateStreams();
        }

        private void UpdateStreams()
        {
            if (VisualizationContext.Instance.DatasetViewModel != null)
            {
                SessionViewModel currentSessionViewModel = VisualizationContext.Instance.DatasetViewModel.CurrentSessionViewModel;
                if (currentSessionViewModel != null)
                {
                    this.server?.Rendezvous.TryRemoveProcess(this.lastProcessName);
                    Rendezvous.Process process = new Rendezvous.Process(this.Settings.ProcessNameOverriding.Length > 0 ? this.Settings.ProcessNameOverriding : VisualizationContext.Instance.DatasetViewModel.CurrentSessionViewModel.Name);
                    foreach (var partition in currentSessionViewModel.Session.Partitions)
                    {
                        var partitionVM = currentSessionViewModel.PartitionViewModels.FirstOrDefault(x => x.Name == partition.Name);
                        if (partitionVM == default)
                        {
                            continue;
                        }

                        foreach (var streamMetadata in partition.AvailableStreams)
                        {
                            // StreamSource source = partitionVM.CreateStreamSource(new StreamBinding(streamMetadata.StoreName, partitionVM.Name), , );
                            // var tcpSimpleWriter = typeof(TcpSimpleWriter<>).MakeGenericType([Type.GetType(streamMetadata.TypeName)]).
                            //    GetConstructor([typeof(int), typeof(IFormatSerializer), typeof(string)]).
                            //    Invoke([this.currentPort, , null]);
                            // IDisposable networkedVisu = (IDisposable)typeof(NetworkedStreamValueVisualisationObject<>).MakeGenericType([Type.GetType(streamMetadata.TypeName)]).
                            //    GetConstructor([typeof(TcpSimpleWriter<>), typeof(StreamSource)]).
                            //    Invoke([tcpSimpleWriter, source]);
                            // this.networkStreams.Add(networkedVisu);
                            process.AddEndpoint(new Rendezvous.TcpSourceEndpoint(this.Settings.EndpointAddress, this.currentPort));
                            this.currentPort++;
                        }
                    }

                    this.server?.Rendezvous.TryAddProcess(process);
                    this.lastProcessName = process.Name;
                }
            }
        }
    }
}

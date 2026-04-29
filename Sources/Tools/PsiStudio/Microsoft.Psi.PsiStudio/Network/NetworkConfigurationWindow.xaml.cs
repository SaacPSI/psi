// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Windows
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows;
    using Microsoft.Psi.PsiStudio;
    using Microsoft.Psi.Remoting;

    /// <summary>
    /// Interaction logic for NetworkConfigurationWindow.xaml.
    /// </summary>
    public partial class NetworkConfigurationWindow : Window
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NetworkConfigurationWindow"/> class.
        /// </summary>
        /// <param name="owner">The owner of this window.</param>
        /// <param name="args">Previous network settings.</param>
        public NetworkConfigurationWindow(Window owner, PsiStudioNetworkSettings args = null)
        {
            this.InitializeComponent();

            this.NetworkSettings = args ?? new PsiStudioNetworkSettings();
            this.IsNetworkActive.IsChecked = this.NetworkSettings.IsActive;
            this.IsAudioActive.IsChecked = this.NetworkSettings.IsAudio;
            this.IsUsingRemoteExporters.IsChecked = this.NetworkSettings.UseRemoteExporters;
            this.UpdateTransportKind(this.NetworkSettings.UseRemoteExporters);
            this.TransportKindComboBox.SelectedItem = this.NetworkSettings.TransportType;
            this.EndpointAddress.Text = this.NetworkSettings.EndpointAddress;
            this.RendezVousPort.Text = this.NetworkSettings.RendezVousPort.ToString();
            this.RendezVousAddress.Text = this.NetworkSettings.RendezVousAddress;
            this.ExporterStartingPort.Text = this.NetworkSettings.ExporterStartingPort.ToString();
            this.ProcessNameOverriding.Text = this.NetworkSettings.ProcessNameOverriding;
            this.CommandProcessName.Text = this.NetworkSettings.CommandProcessName;
            this.Owner = owner;

            this.DataContext = this;
        }

        /// <summary>
        /// Gets or sets the directory to search for layout files.
        /// </summary>
        public PsiStudioNetworkSettings NetworkSettings { get; set; }

        private void SaveButtonClick(object sender, RoutedEventArgs e)
        {
            this.NetworkSettings.IsActive = (bool)this.IsNetworkActive.IsChecked;
            this.NetworkSettings.IsAudio = (bool)this.IsAudioActive.IsChecked;
            this.NetworkSettings.UseRemoteExporters = (bool)this.IsUsingRemoteExporters.IsChecked;
            this.NetworkSettings.TransportType = (TransportKind)this.TransportKindComboBox.SelectedItem;
            this.NetworkSettings.EndpointAddress = this.EndpointAddress.Text;
            int port;
            if (!int.TryParse(this.RendezVousPort.Text, out port))
            {
                // Display validation errors
                new MessageBoxWindow(
                    this.Owner,
                    "Settings Error",
                    "Failed to parse RendezVousPort",
                    cancelButtonText: null).ShowDialog();
                e.Handled = false;
                return;
            }

            this.NetworkSettings.RendezVousPort = port;
            this.NetworkSettings.RendezVousAddress = this.RendezVousAddress.Text;
            if (!int.TryParse(this.ExporterStartingPort.Text, out port))
            {
                // Display validation errors
                new MessageBoxWindow(
                    this.Owner,
                    "Settings Error",
                    "Failed to parse ExporterStartingPort",
                    cancelButtonText: null).ShowDialog();
                e.Handled = false;
                return;
            }

            this.NetworkSettings.ExporterStartingPort = port;
            this.NetworkSettings.ProcessNameOverriding = this.ProcessNameOverriding.Text;
            this.NetworkSettings.CommandProcessName = this.CommandProcessName.Text;
            this.DialogResult = true;
            this.Close();
        }

        private void RemoteExporterIsChecked(object sender, RoutedEventArgs e)
        {
            this.UpdateTransportKind((bool)this.IsUsingRemoteExporters.IsChecked);
        }

        private void UpdateTransportKind(bool isChecked)
        {
            List<TransportKind> transportKinds = Enum.GetValues(typeof(TransportKind)).Cast<TransportKind>().ToList();
            if (isChecked)
            {
                transportKinds.Remove(TransportKind.UdpBroadcast);
            }
            else
            {
                transportKinds.Remove(TransportKind.NamedPipes);
            }

            this.TransportKindComboBox.ItemsSource = transportKinds;
        }
    }
}
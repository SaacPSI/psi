// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.PsiStudio
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Windows;
    using Microsoft.Psi.Audio;
    using Microsoft.Psi.Interop.Rendezvous;
    using Microsoft.Psi.Interop.Serialization;
    using Microsoft.Psi.Interop.Transport;
    using Microsoft.Psi.Visualization;
    using Microsoft.Psi.Visualization.Data;
    using Microsoft.Psi.Visualization.Navigation;
    using Microsoft.Psi.Visualization.ViewModels;
    using Microsoft.Psi.Visualization.VisualizationObjects;
    using Microsoft.Psi.Visualization.Windows;

    /// <summary>
    /// Implements temporary network settings for Psi Studio.
    /// </summary>
    internal class NetworkStreamsManager : IDisposable
    {
        /// <summary>
        /// Reserved name for PsiTudioEvent RendezVous process.
        /// </summary>
        public const string PsiStudioProcess = "PsiStudio";

        private readonly string audioTypeName = typeof(AudioBuffer).ToString();
        private readonly Navigator navigator;
        private TcpSimpleWriter<PsiStudioNetworkInfo> psiStudioWriter;
        private TcpSimpleSource<PsiStudioNetworkInfo> psiStudioSource;
        private RendezvousRelay rendezVous;
        private string lastProcessName;
        private string activeSessionName;
        private int currentPort;
        private List<IActivableStreamVisualizationObject> networkStreams;
        private Rendezvous.Process audioProcess;

        /// <summary>
        /// Initializes a new instance of the <see cref="NetworkStreamsManager"/> class.
        /// </summary>
        /// <param name="navigator">The navigator to register on curser mode change event.</param>
        public NetworkStreamsManager(Navigator navigator)
        {
            this.Settings = new PsiStudioNetworkSettings();
            this.currentPort = this.Settings.ExporterStartingPort + 1;
            this.networkStreams = new List<IActivableStreamVisualizationObject>();
            this.audioProcess = null;

            // Listen for events that occur when the DatasetViewModel change
            VisualizationContext.Instance.PropertyChanged += this.DatasetViewModelChanged;

            // Listen for events that occur when the session change
            VisualizationContext.Instance.DatasetViewModel.PropertyChanged += this.CurrentSessionChanged;

            this.navigator = navigator;
            this.navigator.CursorModeChanged += this.NavigatorCursorModeChanged;
            this.navigator.PropertyChanged += this.NavigatorPropertyChanged;
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
            this.Settings = newSettings;
            this.StopRendezVous();
            if (this.Settings.IsActive && this.StartRendezVous())
            {
                this.GeneratePsiStudioProcess();
                this.UpdateStreams();

                if (this.Settings.IsAudio)
                {
                    this.navigator.PlayNewAudioStream += this.OnAudioStreamAdded;
                }
                else
                {
                    this.navigator.PlayNewAudioStream -= this.OnAudioStreamAdded;
                }
            }

            this.currentPort = this.Settings.ExporterStartingPort + 1;
        }

        /// <summary>
        /// Add to audio stream a TCPWriter.
        /// </summary>
        /// <param name="pipeline">The audio pipeline from the navigator.</param>
        /// <param name="audioStream">The audio stream.</param>
        /// <param name="streamMetadata">The metadata of the audio stream.</param>
        public void OnAudioStreamAdded(Pipeline pipeline, IProducer<AudioBuffer> audioStream, StreamSource streamMetadata)
        {
            TcpWriter<AudioBuffer> exporter = new TcpWriter<AudioBuffer>(pipeline, this.currentPort, Format.PsiFormatAudioBuffer.GetFormat());
            audioStream.PipeTo(exporter);
            pipeline.PipelineRun += this.AudioPipelineRun;
            pipeline.PipelineCompleted += this.AudioPipelineCompleted;
            this.audioProcess = this.audioProcess ?? new Rendezvous.Process($"{this.activeSessionName}-AudioProcess");
            this.audioProcess.AddEndpoint(new Rendezvous.TcpSourceEndpoint(this.Settings.EndpointAddress, this.currentPort, new Rendezvous.Stream(streamMetadata.StreamName, this.audioTypeName)));
            this.currentPort++;
        }

        /// <summary>
        /// Update the settings.
        /// </summary>
        /// <param name="state">The new state for network.</param>
        public void Activate(bool state = true)
        {
            foreach (IActivableStreamVisualizationObject networkStream in this.networkStreams)
            {
                networkStream.IsActive = state;
            }
        }

        /// <summary>
        /// Stop RendezVous Server.
        /// </summary>
        public void Dispose()
        {
            this.Clean();
            this.StopRendezVous();
            this.psiStudioWriter?.Dispose();
            this.psiStudioSource?.Dispose();
        }

        private bool StartRendezVous()
        {
            if (this.Settings.RendezVousAddress == string.Empty)
            {
                RendezvousServer server = new RendezvousServer(this.Settings.RendezVousPort);
                this.rendezVous = server;
                server.Start();
            }
            else
            {
                RendezvousClient client = new RendezvousClient(this.Settings.RendezVousAddress, this.Settings.RendezVousPort);
                MessageBoxWindow waitingMessageBox = new MessageBoxWindow(Application.Current.MainWindow, "RendezvousClient", "\nWaiting the server...\n\n", null, "Cancel");
                SynchronizationContext currentContext = SynchronizationContext.Current;
                Thread clientStartThread = new Thread(new ThreadStart( () =>
                {
                    client.Start();
                    currentContext.Send(_ => waitingMessageBox.DialogResult = true, null);
                }));
                clientStartThread.Start();
                bool? modalResult = waitingMessageBox.ShowDialog();
                this.Settings.IsActive = modalResult != null ? (bool)modalResult : false;
                if (this.Settings.IsActive == false)
                {
                    try
                    {
                        client.Dispose();
                    }
                    catch (Exception)
                    {
                    }
                }
                else
                {
                    this.rendezVous = client;
                }

                clientStartThread.Abort();
                return this.Settings.IsActive;
            }

            return true;
        }

        private void StopRendezVous()
        {
            (this.rendezVous as RendezvousServer)?.Stop();
            (this.rendezVous as RendezvousClient)?.Stop();
        }

        private void NavigatorCursorModeChanged(object sender, CursorModeChangedEventArgs e)
        {
            if (!this.Settings.IsActive)
            {
                return;
            }

            this.Activate(e.NewValue == CursorMode.Playback);
            PsiStudioNetworkInfo.PsiStudioNetworkEvent evt = e.NewValue == CursorMode.Playback ? PsiStudioNetworkInfo.PsiStudioNetworkEvent.Playing : PsiStudioNetworkInfo.PsiStudioNetworkEvent.Stopping;
            TimeInterval interval;
            if (evt == PsiStudioNetworkInfo.PsiStudioNetworkEvent.Stopping)
            {
                interval = new TimeInterval(this.navigator.DataRange.AsTimeInterval.Left, this.navigator.Cursor);
            }
            else
            {
                interval = this.navigator.DataRange.AsTimeInterval;
            }

            this.psiStudioWriter.Receive(new PsiStudioNetworkInfo(evt, interval, this.navigator.PlaySpeed, this.activeSessionName), new Envelope(this.navigator.Cursor, DateTime.UtcNow, 0, 0));
        }

        private void PlaybackRequestFromNetwork(PsiStudioNetworkInfo info, DateTime time)
        {
            switch (info.Event)
            {
                case PsiStudioNetworkInfo.PsiStudioNetworkEvent.Playing:
                    Application.Current.Dispatcher.Invoke(new Action(() =>
                    {
                        this.navigator.PlaySpeed = info.PlaySpeed;
                        this.navigator.SetPlaybackCursorMode(info.Interval.Left < this.navigator.ViewRange.StartTime ? this.navigator.ViewRange.StartTime : info.Interval.Left, info.Interval.Right > this.navigator.ViewRange.EndTime ? this.navigator.ViewRange.EndTime : info.Interval.Right );
                    }));
                    break;
                case PsiStudioNetworkInfo.PsiStudioNetworkEvent.Stopping:
                    Application.Current.Dispatcher.Invoke(new Action(() =>
                    {
                        VisualizationContext.Instance.PlayOrPause(false);
                        this.navigator.SetManualCursorMode();
                    }));
                    break;
                case PsiStudioNetworkInfo.PsiStudioNetworkEvent.SpeedPlayback:
                    Application.Current.Dispatcher.Invoke(new Action(() =>
                    {
                        this.navigator.PlaySpeed = info.PlaySpeed;
                    }));
                    break;
            }
        }

        private void GeneratePsiStudioProcess()
        {
            if (this.rendezVous == null)
            {
                return;
            }

            Rendezvous.Process process = new Rendezvous.Process(PsiStudioProcess);
            this.psiStudioWriter = new TcpSimpleWriter<PsiStudioNetworkInfo>(this.Settings.ExporterStartingPort, PsiFormatPsiStudioNetworkInfo.GetFormat(), PsiStudioProcess);
            process.AddEndpoint(new Rendezvous.TcpSourceEndpoint(this.Settings.EndpointAddress, this.Settings.ExporterStartingPort, new Rendezvous.Stream(PsiStudioProcess, typeof(PsiStudioNetworkInfo))));
            this.rendezVous?.Rendezvous.TryAddProcess(process);
            if (this.Settings.CommandProcessName.Length > 0 && this.rendezVous != null)
            {
                this.rendezVous.Rendezvous.ProcessAdded += this.RendezvousProcessAdded;
            }
        }

        private void RendezvousProcessAdded(object sender, Rendezvous.Process e)
        {
            if (e.Name == this.Settings.CommandProcessName && this.psiStudioSource == null)
            {
                Rendezvous.TcpSourceEndpoint source = e.Endpoints.First() as Rendezvous.TcpSourceEndpoint;

                // Might add a typename check.
                if (source != null)
                {
                    this.psiStudioSource = new TcpSimpleSource<PsiStudioNetworkInfo>(source.Host, source.Port, PsiFormatPsiStudioNetworkInfo.GetFormat());
                    this.psiStudioSource.OnMessageRecieved = this.PlaybackRequestFromNetwork;
                    this.psiStudioSource.Start((time) => { });
                }
            }
        }

        private void Clean()
        {
            if (this.networkStreams.Count == 0)
            {
                return;
            }

            this.rendezVous?.Rendezvous.TryRemoveProcess(this.lastProcessName);
            foreach (IActivableStreamVisualizationObject networkStream in this.networkStreams)
            {
                networkStream.Dispose();
            }

            this.networkStreams.Clear();
            this.currentPort = this.Settings.ExporterStartingPort + 1;
        }

        private void DatasetViewModelChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(VisualizationContext.Instance.DatasetViewModel) || !this.Settings.IsActive)
            {
                return;
            }

            VisualizationContext.Instance.DatasetViewModel.PropertyChanged += this.CurrentSessionChanged;
            this.UpdateStreams();
        }

        private void CurrentSessionChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(VisualizationContext.Instance.DatasetViewModel.CurrentSessionViewModel) || !this.Settings.IsActive)
            {
                return;
            }

            this.UpdateStreams();
        }

        private void UpdateStreams()
        {
            if (this.rendezVous == null || !this.Settings.IsActive)
            {
                return;
            }

            this.Clean();
            if (VisualizationContext.Instance.DatasetViewModel != null)
            {
                SessionViewModel currentSessionViewModel = VisualizationContext.Instance.DatasetViewModel.CurrentSessionViewModel;
                if (currentSessionViewModel != null && !currentSessionViewModel.ContainsLivePartitions)
                {
                    this.activeSessionName = currentSessionViewModel.Name;
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
                            StreamSource source = partitionVM.CreateStreamSource(new StreamBinding(streamMetadata.Name, partitionVM.Name), null, null);
                            if (source == null)
                            {
                                continue;
                            }

                            Type streamType = Type.GetType(streamMetadata.TypeName);
                            Type format = null;
                            if (streamType == null)
                            {
                                var mapCheck = VisualizationContext.Instance.PluginMap.SerializationsMappings.Where(type => type.Key.AssemblyQualifiedName == streamMetadata.TypeName).ToList();

                                if (mapCheck.Count < 1)
                                {
                                    continue;
                                }

                                streamType = mapCheck.First().Key;
                                format = mapCheck.First().Value;
                            }
                            else if (streamType == typeof(AudioBuffer) || VisualizationContext.Instance.PluginMap.SerializationsMappings.TryGetValue(streamType, out format) == false)
                            {
                                continue;
                            }

                            var simpleWriter = this.GenerateSimpleWriter(streamType, format);

                            IActivableStreamVisualizationObject networkedVisu = (IActivableStreamVisualizationObject)typeof(NetworkedStreamValueVisualisationObject<>).MakeGenericType([streamType]).
                                GetConstructors()[0].Invoke([simpleWriter, source]);

                            this.networkStreams.Add(networkedVisu);
                            switch (this.Settings.TransportType)
                            {
                                case Remoting.TransportKind.UdpBroadcast:
                                case Remoting.TransportKind.Udp:
                                    process.AddEndpoint(new Rendezvous.UdpSourceEndpoint(this.Settings.EndpointAddress, this.currentPort, new Rendezvous.Stream(streamMetadata.Name, streamMetadata.TypeName)));
                                    break;
                                default:
                                case Remoting.TransportKind.Tcp:
                                    process.AddEndpoint(new Rendezvous.TcpSourceEndpoint(this.Settings.EndpointAddress, this.currentPort, new Rendezvous.Stream(streamMetadata.Name, streamMetadata.TypeName)));
                                    break;
                            }

                            this.currentPort++;
                        }
                    }

                    if (process.Endpoints.Count() > 0)
                    {
                        this.rendezVous?.Rendezvous.TryAddProcess(process);
                        this.lastProcessName = process.Name;
                    }
                }
            }
        }

        private object GenerateSimpleWriter(Type stream, Type serializer)
        {
            switch (this.Settings.TransportType)
            {
                case Remoting.TransportKind.UdpBroadcast:
                case Remoting.TransportKind.Udp:
                    return typeof(UdpSimpleWriter<>).MakeGenericType([stream]).
                               GetConstructor([typeof(int), typeof(IFormatSerializer), typeof(bool), typeof(string)]).
                               Invoke([this.currentPort, serializer.GetMethod("GetFormat").Invoke(null, null), this.Settings.TransportType == Remoting.TransportKind.UdpBroadcast, null]);
                default:
                case Remoting.TransportKind.Tcp:
                    return typeof(TcpSimpleWriter<>).MakeGenericType([stream]).
                               GetConstructor([typeof(int), typeof(IFormatSerializer), typeof(string)]).
                               Invoke([this.currentPort, serializer.GetMethod("GetFormat").Invoke(null, null), null]);
            }
        }

        private void AudioPipelineCompleted(object sender, PipelineCompletedEventArgs e)
        {
            Pipeline pipeline = sender as Pipeline;
            this.rendezVous?.Rendezvous.TryRemoveProcess(this.audioProcess);
            pipeline.PipelineRun -= this.AudioPipelineRun;
            pipeline.PipelineCompleted -= this.AudioPipelineCompleted;
            this.audioProcess = null;
        }

        private void AudioPipelineRun(object sender, PipelineRunEventArgs e)
        {
            if (this.audioProcess.Endpoints.Count() > 0)
            {
                this.rendezVous?.Rendezvous.TryAddProcess(this.audioProcess);
            }
        }

        private void NavigatorPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(Navigator.PlaySpeed):
                    if (!this.Settings.IsActive)
                    {
                        return;
                    }

                    this.psiStudioWriter.Receive(new PsiStudioNetworkInfo(PsiStudioNetworkInfo.PsiStudioNetworkEvent.SpeedPlayback, TimeInterval.Empty, this.navigator.PlaySpeed, this.activeSessionName), new Envelope(this.navigator.Cursor, DateTime.UtcNow, 0, 0));
                    break;
            }
        }
    }
}

using Microsoft.Psi;
using Microsoft.Psi.Data;
using Microsoft.Psi.Remoting;
using Microsoft.Psi.Interop.Rendezvous;
using Microsoft.Psi.Interop.Serialization;
using static Microsoft.Psi.Interop.Rendezvous.Rendezvous;
using System.Numerics;
using System.Net;
using System.IO;
using System.Windows;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Win32;
using System.Linq;

namespace PsiStudioTestPipeline
{
    internal class PsiFormatBoolean
    {
        public static Format<bool> GetFormat()
        {
            return new Format<bool>(WriteBoolean, ReadBoolean);
        }

        public static void WriteBoolean(bool boolean, BinaryWriter writer)
        {
            writer.Write(boolean);
        }

        public static bool ReadBoolean(BinaryReader reader)
        {
            return reader.ReadBoolean();
        }
    }

    internal class PsiFormaChar
    {
        public static Format<char> GetFormat()
        {
            return new Format<char>(WriteChar, ReadChar);
        }

        public static void WriteChar(char character, BinaryWriter writer)
        {
            writer.Write(character);
        }

        public static char ReadChar(BinaryReader reader)
        {
            return reader.ReadChar();
        }
    }

    internal class PsiFormatPositionAndOrientation
    {
        public static Format<Tuple<System.Numerics.Vector3, System.Numerics.Vector3>> GetFormat()
        {
            return new Format<Tuple<System.Numerics.Vector3, System.Numerics.Vector3>>(WritePositionOrientation, ReadPositionOrientation);
        }

        public static void WritePositionOrientation(Tuple<System.Numerics.Vector3, System.Numerics.Vector3> point3D, BinaryWriter writer)
        {
            writer.Write((double)point3D.Item1.X);
            writer.Write((double)point3D.Item1.Y);
            writer.Write((double)point3D.Item1.Z);
            writer.Write((double)point3D.Item2.X);
            writer.Write((double)point3D.Item2.Y);
            writer.Write((double)point3D.Item2.Z);
        }

        public static Tuple<System.Numerics.Vector3, System.Numerics.Vector3> ReadPositionOrientation(BinaryReader reader)
        {
            return new Tuple<System.Numerics.Vector3, System.Numerics.Vector3>(new System.Numerics.Vector3((float)reader.ReadDouble(), (float)reader.ReadDouble(), (float)reader.ReadDouble()),
                            new System.Numerics.Vector3((float)reader.ReadDouble(), (float)reader.ReadDouble(), (float)reader.ReadDouble()));
        }
    }

    internal class ConnectorInfo
    {
        public string SourceName;
        public string SessionName;
        public Type DataType;
        public dynamic Source;

        public ConnectorInfo(string sourceName, string sessionName, Type dataType, dynamic source)
        {
            SourceName = sourceName;
            SessionName = sessionName;
            DataType = dataType;
            Source = source;
        }
    }

    internal class RendezVousPipelineConfiguration
    {
        public string RendezVousHost = "localhost";
        public int RendezVousPort = 13331;
        public int ClockPort = 11510;
        public bool Diagnostics = false;
        public string DatasetPath = "";
        public string DatasetName = "";
        public string SessionName = "";
        public int NumberOfQuest = 1;
    }

    internal class RendezVousPipeline
    {
        public delegate void LogStatus(string log);
        private LogStatus Log;

        private RendezvousServer Server;
        private Dataset Dataset;
        private Pipeline Pipeline;
        private RendezVousPipelineConfiguration Configuration;
        private bool IsStarted;
        private bool IsPipelineRunning;
        private ushort ConnectedQuestNumber;
        private Dictionary<string, ConnectorInfo> Connectors;

        public RendezVousPipeline(RendezVousPipelineConfiguration? configuration, LogStatus? log)
        {
            Configuration = configuration ?? new RendezVousPipelineConfiguration();
            Log = log ?? ((log) => { Console.WriteLine(log); });
            Pipeline = Pipeline.Create(enableDiagnostics: Configuration.Diagnostics);
            Server = new RendezvousServer(Configuration.RendezVousPort);
            Connectors = new Dictionary<string, ConnectorInfo>();
            if (File.Exists(Configuration.DatasetPath + Configuration.DatasetName))
                Dataset = Dataset.Load(Configuration.DatasetPath + Configuration.DatasetName);
            else
                Dataset = new Dataset(Configuration.DatasetName, Configuration.DatasetPath + Configuration.DatasetName);
            IsStarted = IsPipelineRunning = false;
            ConnectedQuestNumber = 0;
        }

        private void AddedProcess(object? sender, Process process)
        {
            Log($"Process {process.Name}");

            // Temporary no subpipeline
            if (IsPipelineRunning)
                return;

            if (process.Name.Contains("Quest"))
            {
                CreateQuestSubPipeline(process);
                ConnectedQuestNumber++;
                if (ConnectedQuestNumber == Configuration.NumberOfQuest)
                {
                    Pipeline.RunAsync();
                    IsPipelineRunning = true;
                }
            }
            // not for demo
            else
            {
                switch (process.Name)
                {
                    case "KinectApplication":
                        //do stuff
                        break;
                }
            }
        }

        private void CreateQuestSubPipeline(Process process)
        {
            // Upgrade create subpipeline
            Session session = Dataset.AddEmptySession(Configuration.SessionName + process.Name);
            foreach (var endpoint in process.Endpoints)
            {
                if (endpoint is Rendezvous.TcpSourceEndpoint)
                {
                    TcpSourceEndpoint? source = endpoint as TcpSourceEndpoint;
                    if (source == null)
                        return;
                    foreach (var stream in endpoint.Streams)
                    {
                        Log($"\tStream {stream.StreamName}");
                        //TODOOOOOOOOOOOOOOOOOOOOOOOOO proper things
                        switch (stream.StreamName)
                        {
                            case "Left":
                            case "Right":
                            case "Player":
                            case "Black":
                            case "Yellow":
                            case "Red":
                            case "Purple":
                            case "Green":
                                Connection<Tuple<Vector3, Vector3>>(stream.StreamName, session, source, Pipeline, PsiFormatPositionAndOrientation.GetFormat());
                                break;
                            case "OutDigiCode":
                                Connection<char>(stream.StreamName, session, source, Pipeline, PsiFormaChar.GetFormat());
                                break;
                            case "IsSuccess":
                                Connection<bool>(stream.StreamName, session, source, Pipeline, PsiFormatBoolean.GetFormat());
                                break;
                        }
                    }
                }
            }
            Dataset.Save();
        }

        public bool RunPipeline()
        {
            if (ConnectedQuestNumber == Configuration.NumberOfQuest)
            {
                Pipeline.RunAsync();
                IsPipelineRunning = true;
            }
            return IsPipelineRunning;
        }

        public void Start()
        {
            if (IsStarted)
                return;
            var remoteClock = new RemoteClockExporter(Configuration.ClockPort);
            Server.Rendezvous.TryAddProcess(new Rendezvous.Process("ClockSynch", new[] { remoteClock.ToRendezvousEndpoint(Configuration.RendezVousHost) }));
            Server.Rendezvous.ProcessAdded += AddedProcess;
            Server.Error += (s, e) => { Log(e.Message); Log(e.HResult.ToString()); };
            Server.Start();
            Log("Server started!");
            IsStarted = true;
            //If subpipeline
            //Pipeline.RunAsync();
        }

        public void Stop()
        {
            if (!IsStarted)
                return;
            Server.Stop();
            if (IsPipelineRunning)
                Pipeline.Dispose();
            if (Dataset.HasUnsavedChanges)
                Dataset.Save();
            IsStarted = IsPipelineRunning = false;
        }

        private void Connection<T>(string name, Session session, TcpSourceEndpoint source, Pipeline p, Format<T> deserializer)
        {
            string sourceName = $"{session.Name}-{name}";
            var tcpSource = source.ToTcpSource<T>(p, deserializer, null, true, sourceName);
            //For debug
            tcpSource.Do((d, e) => { Log($"Recieve {sourceName} data @{e} : {d}"); });
            Connectors.Add(sourceName, new ConnectorInfo(name, session.Name, typeof(T), tcpSource)); //.BridgeTo(saacSubpipeline, sourceName)
            var store = PsiStore.Create(Pipeline, name, $"{Configuration.DatasetPath}/{session.Name}/");
            store.Write(tcpSource, name);
            session.AddPartitionFromPsiStoreAsync(name, $"{Configuration.DatasetPath}/{session.Name}/");
        }
    }

    /// <summary>
    /// Interaction logic for PipelineSetting.xaml
    /// </summary>
    public partial class PipelineSetting : Window, INotifyPropertyChanged, Microsoft.Psi.PsiStudio.IPsiStudioPipeline
    {
        private RendezVousPipelineConfiguration configuration;

        // UI
        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        private void SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (!EqualityComparer<T>.Default.Equals(field, value))
            {
                field = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion

        // LOG
        private string status = "";
        public string Status
        {
            get => status;
            set => SetProperty(ref status, value);
        }
        public void DelegateMethod(string status)
        {
            Status = status;
        }

        // RendezVousHost
        public string RendezVousHost
        {
            get => configuration.RendezVousHost;
            set => SetProperty(ref configuration.RendezVousHost, value);
        }
        public void DelegateMethodSynchServerIP(string ip)
        {
            configuration.RendezVousHost = ip;
        }

        // RendezVousPort
        public int RendezVousPort
        {
            get => configuration.RendezVousPort;
            set => SetProperty(ref configuration.RendezVousPort, value);
        }
        public void DelegateMethodSynchServerPort(int port)
        {
            configuration.RendezVousPort = port;
        }

        // ClockPort
        public int ClockPort
        {
            get => configuration.ClockPort;
            set => SetProperty(ref configuration.ClockPort, value);
        }
        public void DelegateMethodClockPort(int port)
        {
            configuration.ClockPort = port;
        }

        // DatasetPath
        public string DatasetPath
        {
            get => configuration.DatasetPath;
            set => SetProperty(ref configuration.DatasetPath, value);
        }
        public void DelegateMethodDatasetPath(string path)
        {
            configuration.DatasetPath = path;
        }

        // DatasetName
        public string DatasetName
        {
            get => configuration.DatasetName;
            set => SetProperty(ref configuration.DatasetName, value);
        }
        public void DelegateMethodDatasetName(string path)
        {
            configuration.DatasetName = path;
        }

        // SessionName
        public string SessionName
        {
            get => configuration.SessionName;
            set => SetProperty(ref configuration.SessionName, value);
        }
        public void DelegateMethodSessionName(string path)
        {
            configuration.SessionName = path;
        }

        // ClockPort
        public int NumberOfQuest
        {
            get => configuration.NumberOfQuest;
            set => SetProperty(ref configuration.NumberOfQuest, value);
        }
        public void DelegateMethodNumberOfQuest(int port)
        {
            configuration.NumberOfQuest = port;
        }

        private RendezVousPipeline server;

        public PipelineSetting()
        {
            DataContext = this;
            configuration = new RendezVousPipelineConfiguration();
            configuration.RendezVousHost = Dns.GetHostEntry(Dns.GetHostName()).AddressList[0].ToString();
            configuration.DatasetPath = "D:/Stores/Unity/";
            configuration.DatasetName = "Unity.pds";

            InitializeComponent();
        }

        public string GetDataset()
        {
            return configuration.DatasetPath + configuration.DatasetName;
        }

        public void RunPipeline()
        {
            RunPipeline();
        }

        public void StopPipeline()
        {
            server.Stop();
        }

        private void BtnStopClick(object sender, RoutedEventArgs e)
        {
            server.Stop();
        }

        private void BtnStartClick(object sender, RoutedEventArgs e)
        {
            status = "";
            server = new RendezVousPipeline(configuration,(log) => { Status += $"{log}\n"; });
            server.Start();
        }

        private void BtnBrowseNameClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                DatasetPath = openFileDialog.FileName.Substring(0, openFileDialog.FileName.IndexOf(openFileDialog.SafeFileName));
                DatasetName = openFileDialog.SafeFileName;
            }
        }

        public string GetLayout()
        {
            return "{\"LayoutVersion\":5.0,\"Layout\":{\"$id\":\"1\",\"Panels\":[{\"$id\":\"2\",\"$type\":\"Microsoft.Psi.Visualization.VisualizationPanels.InstantVisualizationContainer,Microsoft.Psi.Visualization.Windows\",\"Panels\":[{\"$id\":\"3\",\"$type\":\"Microsoft.Psi.Visualization.VisualizationPanels.XYVisualizationPanel,Microsoft.Psi.Visualization.Windows\",\"AxisComputeMode\":0,\"XAxis\":{\"$id\":\"4\",\"maximum\":640.0,\"minimum\":0.0},\"YAxis\":{\"$id\":\"5\",\"maximum\":480.0,\"minimum\":0.0},\"ViewportPadding\":\"1063.16666666667,0,1063.16666666667,0\",\"CompatiblePanelTypes\":[2],\"DefaultCursorEpsilonNegMs\":500,\"DefaultCursorEpsilonPosMs\":0,\"RelativeWidth\":100,\"Name\":\"2DPanel\",\"Visible\":true,\"Height\":400.0,\"BackgroundColor\":\"#FF252526\",\"Width\":2659.0,\"VisualizationObjects\":[{\"$id\":\"6\",\"$type\":\"Microsoft.Psi.Visualization.VisualizationObjects.ImageVisualizationObject,Microsoft.Psi.Visualization.Windows\",\"HorizontalFlip\":false,\"StreamBinding\":{\"$id\":\"7\",\"PartitionName\":\"Webcam\",\"SourceStreamName\":\"Image\",\"StreamName\":\"Image\",\"VisualizerStreamAdapterArguments\":[],\"VisualizerSummarizerArguments\":[],\"VisualizerStreamAdapterTypeName\":\"Microsoft.Psi.Visualization.Adapters.EncodedImageToImageAdapter,Microsoft.Psi.Visualization.Windows,Version=0.18.72.1,Culture=neutral,PublicKeyToken=null\"},\"Name\":\"Image\",\"Visible\":true,\"CursorEpsilonPosMs\":0,\"CursorEpsilonNegMs\":500}]}],\"Name\":\"InstantVisualizationContainer\",\"Visible\":true,\"Height\":400.0,\"CompatiblePanelTypes\":[],\"BackgroundColor\":\"#FF252526\",\"Width\":400.0,\"VisualizationObjects\":[]}]}}";
        }
    }
}

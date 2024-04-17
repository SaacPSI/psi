using System.Windows;
using Microsoft.Psi;
using Microsoft.Psi.PsiStudio;
using Microsoft.Psi.Media;
using Microsoft.Psi.Data;
using Microsoft.Psi.Imaging;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.IO;
using Microsoft.Win32;
using SharpDX;
using System.Data;

namespace PsiStudioTestPipeline
{
    /// <summary>
    /// Interaction logic for PipelineSetting.xaml
    /// </summary>
    public partial class PipelineSetting : Window, INotifyPropertyChanged, IPsiStudioPipeline
    {
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
        // DatasetPath
        private string datasetPath;
        public string DatasetPath
        {
            get => datasetPath;
            set => SetProperty(ref datasetPath, value);
        }
        public void DelegateMethodDatasetPath(string path)
        {
            datasetPath = path;
        }

        // DatasetName
        private string datasetName;
        public string DatasetName
        {
            get => datasetName;
            set => SetProperty(ref datasetName, value);
        }
        public void DelegateMethodDatasetName(string path)
        {
            datasetName = path;
        }

        // SessionName
        private string sessionName;
        public string SessionName
        {
            get => sessionName;
            set => SetProperty(ref sessionName, value);
        }
        public void DelegateMethodSessionName(string path)
        {
            sessionName = path;
        }

        private Pipeline pipeline;

        public PipelineSetting()
        {
            InitializeComponent();
            DataContext = this;
        }

        public string GetDataset()
        {
            return Path.Combine(DatasetPath, DatasetName);
        }

        public void RunPipeline()
        {
            if (DatasetName == null || DatasetPath == null || SessionName == null)
                throw new Exception("Argument(s) missing!");
            pipeline = Pipeline.Create("WebcamAndStore");
            string datasetPath = Path.Combine(DatasetPath, DatasetName);
            Dataset dataset;
            if (File.Exists(datasetPath))
                dataset = Dataset.Load(datasetPath);
            else
                dataset = new Dataset(DatasetName, Path.Combine(DatasetPath, DatasetName), true);
            var session = dataset.AddEmptySession(SessionName);
            // Create the webcam component
            var webcam = new MediaCapture(pipeline, 640, 480, 30, true);

            // Create the store component
            var store = PsiStore.Create(pipeline, "Webcam", DatasetPath);
        
            session.AddPartitionFromPsiStoreAsync("Webcam", DatasetPath);
            // Write incoming images in the store at 'Image' track
            store.Write(webcam.Out.EncodeJpeg(), "Image");
            store.Write(webcam.Audio, "Audio");

            dataset.Save();
            // Start the pipeline running
            pipeline.RunAsync();
        }

        public void StopPipeline()
        {
            pipeline.Dispose();
            pipeline = null;
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

        private void BtnOkClick(object sender, RoutedEventArgs e)
        {
            this.Hide();
        }

        public string GetLayout()
        {
            return "{\"LayoutVersion\":5.0,\"Layout\":{\"$id\":\"1\",\"Panels\":[{\"$id\":\"2\",\"$type\":\"Microsoft.Psi.Visualization.VisualizationPanels.InstantVisualizationContainer,Microsoft.Psi.Visualization.Windows\",\"Panels\":[{\"$id\":\"3\",\"$type\":\"Microsoft.Psi.Visualization.VisualizationPanels.XYVisualizationPanel,Microsoft.Psi.Visualization.Windows\",\"AxisComputeMode\":0,\"CompatiblePanelTypes\":[2],\"DefaultCursorEpsilonNegMs\":500,\"DefaultCursorEpsilonPosMs\":0,\"RelativeWidth\":100,\"Name\":\"2DPanel\",\"Visible\":true,\"Height\":400.0,\"BackgroundColor\":\"#FF252526\",\"VisualizationObjects\":[{\"$id\":\"4\",\"$type\":\"Microsoft.Psi.Visualization.VisualizationObjects.ImageVisualizationObject,Microsoft.Psi.Visualization.Windows\",\"HorizontalFlip\":false,\"StreamBinding\":{\"$id\":\"5\",\"PartitionName\":\"Webcam\",\"SourceStreamName\":\"Image\",\"StreamName\":\"Image\",\"VisualizerStreamAdapterArguments\":[],\"VisualizerSummarizerArguments\":[],\"VisualizerStreamAdapterTypeName\":\"Microsoft.Psi.Visualization.Adapters.EncodedImageToImageAdapter,Microsoft.Psi.Visualization.Windows,Version=0.18.72.1,Culture=neutral,PublicKeyToken=null\"},\"Name\":\"Image\",\"Visible\":true,\"CursorEpsilonPosMs\":0,\"CursorEpsilonNegMs\":500}]}],\"CompatiblePanelTypes\":[],\"Name\":\"InstantVisualizationContainer\",\"Visible\":true,\"Height\":400.0,\"BackgroundColor\":\"#FF252526\",\"VisualizationObjects\":[]},{\"$id\":\"6\",\"$type\":\"Microsoft.Psi.Visualization.VisualizationPanels.TimelineVisualizationPanel,Microsoft.Psi.Visualization.Windows\",\"VisualizationObjects\":[{\"$id\":\"7\",\"$type\":\"Microsoft.Psi.Visualization.VisualizationObjects.AudioVisualizationObject,Microsoft.Psi.Visualization.Windows\",\"Channel\":0,\"PlayDisplayChannelOnly\":false,\"Color\":\"#FFD3D3D3\",\"LineWidth\":1.0,\"InterpolationStyle\":0,\"MarkerColor\":\"#FFD3D3D3\",\"MarkerSize\":4.0,\"MarkerStyle\":0,\"RangeColor\":\"#FFD3D3D3\",\"RangeWidth\":1.0,\"VisualizationInterval\":0,\"LegendFormat\":\"\",\"StreamBinding\":{\"$id\":\"8\",\"PartitionName\":\"Webcam\",\"SourceStreamName\":\"Audio\",\"StreamName\":\"Audio\",\"VisualizerStreamAdapterArguments\":[],\"VisualizerSummarizerArguments\":[],\"SummarizerTypeName\":\"Microsoft.Psi.Visualization.Summarizers.AudioSummarizer,Microsoft.Psi.Visualization.Windows,Version=0.19.100.1,Culture=neutral,PublicKeyToken=null\"},\"Name\":\"Audio\",\"Visible\":true,\"CursorEpsilonPosMs\":0,\"CursorEpsilonNegMs\":500}],\"AxisComputeMode\":0,\"ShowLegend\":false,\"ShowTimeTicks\":false,\"Threshold\":{\"$id\":\"9\",\"ThresholdType\":0,\"ThresholdValue\":0.0,\"Opacity\":0.25},\"CompatiblePanelTypes\":[0],\"Name\":\"TimelinePanel\",\"Visible\":true,\"Height\":70.0,\"BackgroundColor\":\"#FF252526\"}]}}";
        }
    }
}

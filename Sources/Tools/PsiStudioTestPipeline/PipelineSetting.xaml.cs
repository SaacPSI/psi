using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using Microsoft.Psi;
using Microsoft.Psi.PsiStudio;
using Microsoft.Psi.Media;
using Microsoft.Psi.Data;
using Microsoft.Psi.Imaging;

namespace PsiStudioTestPipeline
{
    /// <summary>
    /// Interaction logic for PipelineSetting.xaml
    /// </summary>
    public partial class PipelineSetting : Window, IPsiStudioPipeline
    {
        public string PathName { get; set; } = "D:\\Stores\\Webcam\\webcam.pds";

        private Pipeline pipeline;

        public PipelineSetting()
        {
            InitializeComponent();
        }

        public string GetDataset()
        {
            return PathName;
        }

        public void RunPipeline()
        {
            pipeline = Pipeline.Create("WebcamAndStore");
            var dataset = new Dataset("WebcamDD", PathName, true);
            var session = dataset.CreateSession("Webcam");
            // Create the webcam component
            var webcam = new MediaCapture(pipeline, 640, 480, 30);

            // Create the store component
            var store = PsiStore.Create(pipeline, "Webcam", "D:\\Stores");
            session.AddPsiStorePartition("Webcam", "D:\\Stores");
            // Write incoming images in the store at 'Image' track
            store.Write(webcam.Out.EncodeJpeg(), "Image");
            // store.Write(webcam.Audio, "Audio");

            dataset.Save();
            // Start the pipeline running
            pipeline.RunAsync();
        }

        public void StopPipeline()
        {
            pipeline.Dispose();
            pipeline = null;
        }

        public string GetLayout()
        {
           return "{\"LayoutVersion\":5.0,\"Layout\":{\"$id\":\"1\",\"Panels\":[{\"$id\":\"2\",\"$type\":\"Microsoft.Psi.Visualization.VisualizationPanels.InstantVisualizationContainer,Microsoft.Psi.Visualization.Windows\",\"Panels\":[{\"$id\":\"3\",\"$type\":\"Microsoft.Psi.Visualization.VisualizationPanels.XYVisualizationPanel,Microsoft.Psi.Visualization.Windows\",\"AxisComputeMode\":0,\"XAxis\":{\"$id\":\"4\",\"maximum\":640.0,\"minimum\":0.0},\"YAxis\":{\"$id\":\"5\",\"maximum\":480.0,\"minimum\":0.0},\"ViewportPadding\":\"1063.16666666667,0,1063.16666666667,0\",\"CompatiblePanelTypes\":[2],\"DefaultCursorEpsilonNegMs\":500,\"DefaultCursorEpsilonPosMs\":0,\"RelativeWidth\":100,\"Name\":\"2DPanel\",\"Visible\":true,\"Height\":400.0,\"BackgroundColor\":\"#FF252526\",\"Width\":2659.0,\"VisualizationObjects\":[{\"$id\":\"6\",\"$type\":\"Microsoft.Psi.Visualization.VisualizationObjects.ImageVisualizationObject,Microsoft.Psi.Visualization.Windows\",\"HorizontalFlip\":false,\"StreamBinding\":{\"$id\":\"7\",\"PartitionName\":\"Webcam\",\"SourceStreamName\":\"Image\",\"StreamName\":\"Image\",\"VisualizerStreamAdapterArguments\":[],\"VisualizerSummarizerArguments\":[],\"VisualizerStreamAdapterTypeName\":\"Microsoft.Psi.Visualization.Adapters.EncodedImageToImageAdapter,Microsoft.Psi.Visualization.Windows,Version=0.18.72.1,Culture=neutral,PublicKeyToken=null\"},\"Name\":\"Image\",\"Visible\":true,\"CursorEpsilonPosMs\":0,\"CursorEpsilonNegMs\":500}]}],\"Name\":\"InstantVisualizationContainer\",\"Visible\":true,\"Height\":400.0,\"CompatiblePanelTypes\":[],\"BackgroundColor\":\"#FF252526\",\"Width\":400.0,\"VisualizationObjects\":[]}]}}";
        }
    }
}

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
    }
}

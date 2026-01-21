
using Microsoft.Psi.Interop.Rendezvous;
using Microsoft.Psi;
using Microsoft.Psi.Media;
using Microsoft.Psi.Media_Interop;

namespace TestingConsole
{
    public class Program
    {
        static void Main(string[] args)
        {
            //RendezvousServer server = new RendezvousServer(13331);
            //server.Start();
            //server.Rendezvous.ProcessAdded += (_, process) =>
            //{
            //    Console.WriteLine($"Process added: {process.Name}");
            //};

            //RendezvousClient client = new RendezvousClient("localhost", 13331);
            //client.Start();
            //int i = 0;
            //while (true)
            //{
            //    Console.WriteLine("Press");
            //    Console.ReadLine();
            //    client.Rendezvous.TryAddProcess(new Rendezvous.Process($"test{i++}"));
            //}

            List<string> cameras = MediaCapture.GetAvailableCameras();
            List<string> CameraCaptureFormat = new List<string>();

            var vals = MediaCapture.GetAvailableFormats(cameras.First());
            foreach (CaptureFormat format in vals)
            {
                CameraCaptureFormat.Add($"{format.nWidth}x{format.nHeight}@{format.nFrameRateNumerator}");
            }
        }
    }
}

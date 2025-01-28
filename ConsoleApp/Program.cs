using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Psi.Interop.Rendezvous;

namespace ConsoleApp
{
    internal class Program
    {
        static void Main(string[] args)
        {

            RendezvousServer rendezVous = new RendezvousServer();
            rendezVous.Rendezvous.ProcessAdded += ProcessAdded;
            rendezVous.Rendezvous.ProcessRemoved += ProcessRemoved;
            rendezVous.Error += (s, e) => { Console.WriteLine(e.Message); Console.WriteLine(e.HResult.ToString()); };
            rendezVous.Start();

            Console.WriteLine("Press any key.");
            Console.ReadLine();
        }

        private static void ProcessRemoved(object sender, Rendezvous.Process e)
        {
            Console.WriteLine($"Process removed {e.Name}");
        }

        private static void ProcessAdded(object sender, Rendezvous.Process e)
        {
            Console.WriteLine($"Process added {e.Name}");
        }
    }
}

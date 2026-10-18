using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel;
using WCFTablicServer;

namespace WCFTablicClient
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var factory = CreateFactory(new CallbackHandler());
            ITablicService proxy = factory.CreateChannel();

            Console.Write("Ime: ");
            string ime = Console.ReadLine();
            Console.Write("1 - igrac, 2 - posmatrac: ");
            string mode = Console.ReadLine();

            OperationResult result = mode == "2"
                ? proxy.PrijaviSeKaoPosmatrac(ime)
                : proxy.PrijaviSeZaIgru(ime);

            Console.WriteLine(result.Message);
            Console.WriteLine(proxy.VratiStanje());
            Console.WriteLine("Komande: /state, baci KARTA, nosi KARTA TALON1 TALON2, /q");

            while (true)
            {
                Console.Write("> ");
                string line = Console.ReadLine();
                if (line == "/q")
                {
                    break;
                }

                if (line == "/state")
                {
                    Console.WriteLine(proxy.VratiStanje());
                    continue;
                }

                string[] parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2 && parts[0] == "baci")
                {
                    Print(proxy.BaciKartu(parts[1]));
                }
                else if (parts.Length >= 3 && parts[0] == "nosi")
                {
                    Print(proxy.Nosi(parts[1], parts.Skip(2).ToList()));
                }
                else
                {
                    Console.WriteLine("Nepoznata komanda.");
                }
            }

            ((IClientChannel)proxy).Close();
            factory.Close();
        }

        private static DuplexChannelFactory<ITablicService> CreateFactory(ITablicCallback callback)
        {
            var binding = new WSDualHttpBinding(WSDualHttpSecurityMode.None);
            int port = 20000 + (Process.GetCurrentProcess().Id % 20000);
            binding.ClientBaseAddress = new Uri("http://localhost:" + port + "/WCFTablicClient/");
            var endpoint = new EndpointAddress("http://localhost:55104/TablicService.svc");
            return new DuplexChannelFactory<ITablicService>(callback, binding, endpoint);
        }

        private static void Print(OperationResult result)
        {
            Console.WriteLine(result.IsError ? "Greska: " + result.Message : result.Message);
        }
    }

    public class CallbackHandler : ITablicCallback
    {
        public void Obavestenje(string tekst)
        {
            Console.WriteLine();
            Console.WriteLine(tekst);
        }
    }
}

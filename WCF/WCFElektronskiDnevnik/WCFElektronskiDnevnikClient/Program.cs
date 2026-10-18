using System;
using System.Diagnostics;
using System.ServiceModel;
using WCFElektronskiDnevnikServer;

namespace WCFElektronskiDnevnikClient
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var factory = CreateFactory(new CallbackHandler());
            IDnevnikService proxy = factory.CreateChannel();

            Console.WriteLine("1 - Profesor");
            Console.WriteLine("2 - Roditelj");
            Console.Write("Mod: ");
            string mode = Console.ReadLine();

            IspisiUcenike(proxy);

            if (mode == "1")
            {
                Console.Write("Ime profesora: ");
                string ime = Console.ReadLine();
                Console.Write("Predmet: ");
                string predmet = Console.ReadLine();
                Console.WriteLine(proxy.RegistrujProfesora(ime, predmet).Message);
                Console.WriteLine(proxy.DodajUcenikaNaPredmet(1).Message);
                Console.WriteLine(proxy.DajOcenu(1, 5).Message);
            }
            else
            {
                Console.Write("Prezime roditelja: ");
                string prezime = Console.ReadLine();
                Console.WriteLine(proxy.RegistrujRoditelja(prezime).Message);
                Console.WriteLine(proxy.DodajDete(1).Message);

                Console.WriteLine("Ocene za dete 1:");
                foreach (Ocena ocena in proxy.VratiOceneZaDete(1, "SVI"))
                {
                    Console.WriteLine(ocena.Ucenik + " | " + ocena.Predmet + " | " + ocena.Vrednost + " | " + ocena.Profesor);
                }
            }

            ((IClientChannel)proxy).Close();
            factory.Close();

            Console.WriteLine("Pritisni Enter za izlaz.");
            Console.ReadLine();
        }

        private static DuplexChannelFactory<IDnevnikService> CreateFactory(IDnevnikCallback callback)
        {
            var binding = new WSDualHttpBinding(WSDualHttpSecurityMode.None);
            int port = 20000 + (Process.GetCurrentProcess().Id % 20000);
            binding.ClientBaseAddress = new Uri("http://localhost:" + port + "/WCFElektronskiDnevnikClient/");
            var endpoint = new EndpointAddress("http://localhost:55105/DnevnikService.svc");
            return new DuplexChannelFactory<IDnevnikService>(callback, binding, endpoint);
        }

        private static void IspisiUcenike(IDnevnikService proxy)
        {
            Console.WriteLine("Ucenici:");
            foreach (Ucenik u in proxy.VratiUcenike())
            {
                Console.WriteLine(u.Id + ". " + u.Ime + " " + u.Prezime);
            }
        }
    }

    public class CallbackHandler : IDnevnikCallback
    {
        public void Obavestenje(string tekst)
        {
            Console.WriteLine();
            Console.WriteLine("[DNEVNIK] " + tekst);
        }
    }
}

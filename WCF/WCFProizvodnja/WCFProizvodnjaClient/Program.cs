using System;
using System.Diagnostics;
using System.ServiceModel;
using WCFProizvodnjaServer;

namespace WCFProizvodnjaClient
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var callback = new CallbackHandler();
            var factory = CreateFactory(callback);
            IProizvodnjaService proxy = factory.CreateChannel();

            Console.Write("Korisnik: ");
            string korisnik = Console.ReadLine();
            Console.WriteLine(proxy.RegistrujKorisnika(korisnik).Message);

            IspisiProizvode(proxy);
            Console.WriteLine(proxy.DodajProizvod("Novi proizvod " + DateTime.Now.ToString("HHmmss")).Message);
            IspisiProizvode(proxy);

            Console.WriteLine(proxy.ZakljucajProizvod(1).Message);
            Console.WriteLine(proxy.PovecajProizvodnju(1, 25).Message);
            IspisiProizvode(proxy);

            ((IClientChannel)proxy).Close();
            factory.Close();

            Console.WriteLine("Pritisni Enter za izlaz.");
            Console.ReadLine();
        }

        private static DuplexChannelFactory<IProizvodnjaService> CreateFactory(IProizvodnjaCallback callback)
        {
            var binding = new WSDualHttpBinding(WSDualHttpSecurityMode.None);
            int port = 20000 + (Process.GetCurrentProcess().Id % 20000);
            binding.ClientBaseAddress = new Uri("http://localhost:" + port + "/WCFProizvodnjaClient/");
            var endpoint = new EndpointAddress("http://localhost:55103/ProizvodnjaService.svc");
            return new DuplexChannelFactory<IProizvodnjaService>(callback, binding, endpoint);
        }

        private static void IspisiProizvode(IProizvodnjaService proxy)
        {
            Console.WriteLine();
            Console.WriteLine("Proizvodi:");
            foreach (Proizvod p in proxy.VratiProizvode())
            {
                Console.WriteLine(p.Id + ". " + p.Naziv + " | ukupno: " + p.ProizvedenaKolicina + " | lock: " + p.ZakljucaoKorisnik);
            }
        }
    }

    public class CallbackHandler : IProizvodnjaCallback
    {
        public void Obavestenje(string tekst)
        {
            Console.WriteLine();
            Console.WriteLine("[OBAVESTENJE] " + tekst);
        }
    }
}

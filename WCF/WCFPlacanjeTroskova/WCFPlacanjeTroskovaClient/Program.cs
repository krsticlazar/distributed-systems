using System;
using System.ServiceModel;
using WCFPlacanjeTroskovaServer;

namespace WCFPlacanjeTroskovaClient
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var factory = new ChannelFactory<IPlacanjeService>("BasicHttpBinding_IPlacanjeService");
            IPlacanjeService proxy = factory.CreateChannel();

            Console.WriteLine("WCF placanje troskova demo");
            IspisiRacune(proxy);

            OperationResult noviRacun = proxy.DodajRacun("100-3", "Voda", 2500);
            Console.WriteLine("Dodaj racun: " + noviRacun.Message);

            PlacanjeResult placanje = proxy.PlatiTrosak("100-1", 1200, "Junski racun za struju");
            Console.WriteLine("Placanje: " + placanje.Message);
            if (!placanje.IsError)
            {
                Console.WriteLine("Novi bilans: " + placanje.Racun.Bilans);
            }

            PlacanjeResult neuspesno = proxy.PlatiTrosak("100-2", 99999, "Prevelik iznos");
            Console.WriteLine("Neuspesno placanje: " + neuspesno.Message);

            Console.WriteLine();
            Console.WriteLine("Listing za 100-1:");
            foreach (Placanje p in proxy.ListingPlacanja("100-1"))
            {
                Console.WriteLine(p.Id + ". " + p.Vreme + " " + p.Iznos + " - " + p.Opis);
            }

            ((IClientChannel)proxy).Close();
            factory.Close();

            Console.WriteLine("Pritisni Enter za izlaz.");
            Console.ReadLine();
        }

        private static void IspisiRacune(IPlacanjeService proxy)
        {
            Console.WriteLine("Racuni:");
            foreach (Racun r in proxy.VratiRacune())
            {
                Console.WriteLine(r.BrojRacuna + " | " + r.Naziv + " | bilans: " + r.Bilans);
            }
        }
    }
}

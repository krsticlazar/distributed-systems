using System;
using System.Collections.Generic;
using System.ServiceModel;
using WCFKvizServer;

namespace WCFKvizClient
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var factory = new ChannelFactory<IKvizService>("BasicHttpBinding_IKvizService");
            IKvizService proxy = factory.CreateChannel();

            Console.WriteLine("WCF kviz demo");
            IspisiPitanja(proxy);

            var dodavanje = proxy.DodajPitanje(new PitanjeInput
            {
                Tekst = "Koji binding se koristi za callback?",
                Opcije = new[] { "basicHttpBinding", "wsDualHttpBinding", "mexHttpBinding" },
                TacanOdgovor = 2
            });
            Console.WriteLine("Dodaj pitanje: " + dodavanje.Message);

            var izmena = proxy.IzmeniPitanje(1, new PitanjeInput
            {
                Tekst = "Koji binding je najjednostavniji za osnovni HTTP WCF servis?",
                Opcije = new[] { "basicHttpBinding", "wsHttpBinding", "wsDualHttpBinding" },
                TacanOdgovor = 1
            });
            Console.WriteLine("Izmeni pitanje: " + izmena.Message);

            List<Odgovor> odgovori = new List<Odgovor>();
            foreach (PitanjeZaKviz pitanje in proxy.VratiPitanjaZaKviz())
            {
                Console.WriteLine();
                Console.WriteLine(pitanje.Id + ". " + pitanje.Tekst);
                for (int i = 0; i < pitanje.Opcije.Length; i++)
                {
                    Console.WriteLine((i + 1) + ") " + pitanje.Opcije[i]);
                }

                Console.Write("Odgovor: ");
                int izbor;
                if (!int.TryParse(Console.ReadLine(), out izbor))
                {
                    izbor = 0;
                }

                odgovori.Add(new Odgovor { PitanjeId = pitanje.Id, OdabraniOdgovor = izbor });
            }

            RezultatKviza rezultat = proxy.ValidirajOdgovore(odgovori);
            Console.WriteLine();
            Console.WriteLine("Rezultat: " + rezultat.TacnihOdgovora + "/" + rezultat.UkupnoPitanja);
            Console.WriteLine("Procenat: " + rezultat.Procenat.ToString("0.00") + "%");

            ((IClientChannel)proxy).Close();
            factory.Close();

            Console.WriteLine("Pritisni Enter za izlaz.");
            Console.ReadLine();
        }

        private static void IspisiPitanja(IKvizService proxy)
        {
            Console.WriteLine("Administracija - trenutna pitanja:");
            foreach (PitanjeAdmin pitanje in proxy.VratiPitanjaZaAdministraciju())
            {
                Console.WriteLine(pitanje.Id + ". " + pitanje.Tekst + " (tacan: " + pitanje.TacanOdgovor + ")");
            }
        }
    }
}

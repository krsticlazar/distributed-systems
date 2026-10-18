
using System;
using System.Linq;
using WCFIspitnaPrijavaClient.ServiceReferenceIspitnaPrijava;

namespace WCFIspitnaPrijavaClient
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var proxy = new IspitnaPrijavaServiceClient();

            while (true)
            {
                Console.WriteLine();
                Console.WriteLine("Elektronska studentska sluzba");
                Console.WriteLine("1 - Prikazi sve ispite");
                Console.WriteLine("2 - Vrati ispitnu prijavu studenta");
                Console.WriteLine("3 - Dodaj ispit u prijavu");
                Console.WriteLine("4 - Obrisi ispit iz prijave");
                Console.WriteLine("5 - Formatirani nazivi ispita za studenta");
                Console.WriteLine("q - Izlaz");
                Console.Write("Izbor: ");

                string izbor = Console.ReadLine();

                if (izbor == "q")
                {
                    break;
                }

                switch (izbor)
                {
                    case "1":
                        PrikaziSveIspite(proxy);
                        break;
                    case "2":
                        VratiPrijavu(proxy);
                        break;
                    case "3":
                        DodajIspit(proxy);
                        break;
                    case "4":
                        ObrisiIspit(proxy);
                        break;
                    case "5":
                        VratiFormatiranTekst(proxy);
                        break;
                    default:
                        Console.WriteLine("Nepoznata opcija.");
                        break;
                }
            }

            proxy.Close();
        }

        private static void PrikaziSveIspite(IspitnaPrijavaServiceClient proxy)
        {
            Console.WriteLine();
            Console.WriteLine("Dostupni ispiti:");

            foreach (Ispit ispit in proxy.VratiIspite())
            {
                Console.WriteLine(ispit.Id + ". " + ispit.Naziv + " - " + ispit.Profesor);
            }
        }

        private static void VratiPrijavu(IspitnaPrijavaServiceClient proxy)
        {
            Console.Write("Broj indeksa: ");
            string brojIndeksa = Console.ReadLine();

            IspitnaPrijava prijava = proxy.VratiIspitnuPrijavu(brojIndeksa);
            if (prijava == null)
            {
                Console.WriteLine("Student ne postoji.");
                return;
            }

            Console.WriteLine();
            Console.WriteLine(prijava.BrojIndeksa + " - " + prijava.ImePrezime);

            if (prijava.Ispiti == null || !prijava.Ispiti.Any())
            {
                Console.WriteLine("Nema prijavljenih ispita.");
                return;
            }

            foreach (Ispit ispit in prijava.Ispiti)
            {
                Console.WriteLine(ispit.Id + ". " + ispit.Naziv);
            }
        }

        private static void DodajIspit(IspitnaPrijavaServiceClient proxy)
        {
            Console.Write("Broj indeksa: ");
            string brojIndeksa = Console.ReadLine();

            Console.Write("ID ispita: ");
            int ispitId = ReadInt();

            OperationResult result = proxy.DodajIspitUPrijavu(brojIndeksa, ispitId);
            PrintResult(result);
        }

        private static void ObrisiIspit(IspitnaPrijavaServiceClient proxy)
        {
            Console.Write("Broj indeksa: ");
            string brojIndeksa = Console.ReadLine();

            Console.Write("ID ispita: ");
            int ispitId = ReadInt();

            OperationResult result = proxy.ObrisiIspitIzPrijave(brojIndeksa, ispitId);
            PrintResult(result);
        }

        private static void VratiFormatiranTekst(IspitnaPrijavaServiceClient proxy)
        {
            Console.Write("Broj indeksa: ");
            string brojIndeksa = Console.ReadLine();

            Console.WriteLine();
            Console.WriteLine(proxy.VratiFormatiraneNaziveIspita(brojIndeksa));
        }

        private static int ReadInt()
        {
            int value;
            if (!int.TryParse(Console.ReadLine(), out value))
            {
                return 0;
            }

            return value;
        }

        private static void PrintResult(OperationResult result)
        {
            Console.WriteLine(result.IsError ? "Greska: " + result.Message : result.Message);
        }
    }
}


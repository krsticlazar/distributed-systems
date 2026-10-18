using ConsoleSokovi.ServiceReferenceSokovi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleSokovi
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var proxy = new SokoviClient();

            proxy.dodaj(new Sastojak() { Naziv = "Voda", Gustina = 1000, Zapremina = 100 });
            proxy.dodaj(new Sastojak() { Naziv = "Secer", Gustina = 1400, Zapremina = 1 });
            proxy.dodaj(new Sastojak() { Naziv = "ExtractCocaCola", Gustina = 10000, Zapremina = 0.5M });

            Stanje s = proxy.VratiStanje();
            Console.WriteLine($"Gustina: {s.Gustina}, {s.Zauzece}%");

            proxy.flasiraj(50);

            proxy.isprazni();

            var promene = proxy.Promene();
            foreach(var p in promene )
            {
                switch(p.TipPromena)
                {
                    case "Dodavanje":
                        Console.WriteLine($"{p.Datum} :: {p.TipPromena} :: {p.Sastojak.Naziv}, {p.Sastojak.Zapremina}, {p.Sastojak.Gustina}");
                        break;
                    case "Flasiranje":
                        Console.WriteLine($"{p.Datum} :: {p.TipPromena} :: {p.Zapremina}");
                        break;
                    case "Praznjenje":
                        Console.WriteLine($"{p.Datum} :: {p.TipPromena}");
                        break;

                }
            }
        }
    }
}

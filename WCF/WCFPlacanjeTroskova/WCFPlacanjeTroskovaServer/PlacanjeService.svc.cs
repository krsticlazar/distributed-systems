using System;
using System.Collections.Generic;
using System.Linq;

namespace WCFPlacanjeTroskovaServer
{
    public class PlacanjeService : IPlacanjeService
    {
        private static readonly object LockObj = new object();
        private static readonly List<Racun> Racuni = new List<Racun>
        {
            new Racun { BrojRacuna = "100-1", Naziv = "Struja", Bilans = 5000 },
            new Racun { BrojRacuna = "100-2", Naziv = "Internet", Bilans = 3000 }
        };
        private static readonly List<Placanje> Placanja = new List<Placanje>();
        private static int nextPaymentId = 1;

        public OperationResult DodajRacun(string brojRacuna, string naziv, decimal pocetniBilans)
        {
            if (string.IsNullOrWhiteSpace(brojRacuna))
            {
                return Error("Broj racuna je obavezan.");
            }

            if (string.IsNullOrWhiteSpace(naziv))
            {
                return Error("Naziv racuna je obavezan.");
            }

            if (pocetniBilans < 0)
            {
                return Error("Pocetni bilans ne moze biti negativan.");
            }

            lock (LockObj)
            {
                if (Racuni.Any(r => r.BrojRacuna == brojRacuna))
                {
                    return Error("Racun sa tim brojem vec postoji.");
                }

                Racuni.Add(new Racun
                {
                    BrojRacuna = brojRacuna,
                    Naziv = naziv,
                    Bilans = pocetniBilans
                });
            }

            return Ok("Racun je dodat.");
        }

        public List<Racun> VratiRacune()
        {
            lock (LockObj)
            {
                return Racuni.Select(CloneRacun).ToList();
            }
        }

        public PlacanjeResult PlatiTrosak(string brojRacuna, decimal iznos, string opis)
        {
            if (iznos <= 0)
            {
                return PaymentError("Iznos mora biti veci od nule.");
            }

            lock (LockObj)
            {
                Racun racun = Racuni.FirstOrDefault(r => r.BrojRacuna == brojRacuna);
                if (racun == null)
                {
                    return PaymentError("Broj racuna nije validan.");
                }

                if (racun.Bilans < iznos)
                {
                    return PaymentError("Nema dovoljno novca na racunu.");
                }

                racun.Bilans -= iznos;
                Placanje placanje = new Placanje
                {
                    Id = nextPaymentId++,
                    BrojRacuna = brojRacuna,
                    Iznos = iznos,
                    Opis = opis,
                    Vreme = DateTime.Now
                };
                Placanja.Add(placanje);

                return new PlacanjeResult
                {
                    IsError = false,
                    Message = "Placanje je uspesno.",
                    Racun = CloneRacun(racun),
                    Placanje = ClonePlacanje(placanje)
                };
            }
        }

        public List<Placanje> ListingPlacanja(string brojRacuna)
        {
            lock (LockObj)
            {
                return Placanja
                    .Where(p => p.BrojRacuna == brojRacuna)
                    .Select(ClonePlacanje)
                    .ToList();
            }
        }

        private static Racun CloneRacun(Racun r)
        {
            return new Racun { BrojRacuna = r.BrojRacuna, Naziv = r.Naziv, Bilans = r.Bilans };
        }

        private static Placanje ClonePlacanje(Placanje p)
        {
            return new Placanje
            {
                Id = p.Id,
                BrojRacuna = p.BrojRacuna,
                Iznos = p.Iznos,
                Opis = p.Opis,
                Vreme = p.Vreme
            };
        }

        private static OperationResult Ok(string message)
        {
            return new OperationResult { IsError = false, Message = message };
        }

        private static OperationResult Error(string message)
        {
            return new OperationResult { IsError = true, Message = message };
        }

        private static PlacanjeResult PaymentError(string message)
        {
            return new PlacanjeResult { IsError = true, Message = message };
        }
    }
}

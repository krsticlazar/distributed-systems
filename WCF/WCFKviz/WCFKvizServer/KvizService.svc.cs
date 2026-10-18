using System.Collections.Generic;
using System.Linq;

namespace WCFKvizServer
{
    public class KvizService : IKvizService
    {
        private static readonly object LockObj = new object();
        private static readonly List<PitanjeAdmin> Pitanja = new List<PitanjeAdmin>
        {
            new PitanjeAdmin
            {
                Id = 1,
                Tekst = "Koji binding je najjednostavniji za osnovni WCF servis?",
                Opcije = new[] { "basicHttpBinding", "wsDualHttpBinding", "netTcpBinding" },
                TacanOdgovor = 1
            },
            new PitanjeAdmin
            {
                Id = 2,
                Tekst = "Koji atribut oznacava metodu dostupnu klijentu?",
                Opcije = new[] { "DataMember", "OperationContract", "ServiceBehavior" },
                TacanOdgovor = 2
            }
        };
        private static int nextId = 3;

        public OperationResult DodajPitanje(PitanjeInput pitanje)
        {
            OperationResult validation = Validate(pitanje);
            if (validation.IsError)
            {
                return validation;
            }

            lock (LockObj)
            {
                Pitanja.Add(new PitanjeAdmin
                {
                    Id = nextId++,
                    Tekst = pitanje.Tekst,
                    Opcije = pitanje.Opcije,
                    TacanOdgovor = pitanje.TacanOdgovor
                });
            }

            return Ok("Pitanje je dodato.");
        }

        public OperationResult IzmeniPitanje(int id, PitanjeInput pitanje)
        {
            OperationResult validation = Validate(pitanje);
            if (validation.IsError)
            {
                return validation;
            }

            lock (LockObj)
            {
                PitanjeAdmin existing = Pitanja.FirstOrDefault(p => p.Id == id);
                if (existing == null)
                {
                    return Error("Pitanje sa zadatim ID-em ne postoji.");
                }

                existing.Tekst = pitanje.Tekst;
                existing.Opcije = pitanje.Opcije;
                existing.TacanOdgovor = pitanje.TacanOdgovor;
            }

            return Ok("Pitanje je izmenjeno.");
        }

        public List<PitanjeAdmin> VratiPitanjaZaAdministraciju()
        {
            lock (LockObj)
            {
                return Pitanja.Select(CloneAdmin).ToList();
            }
        }

        public List<PitanjeZaKviz> VratiPitanjaZaKviz()
        {
            lock (LockObj)
            {
                return Pitanja.Select(p => new PitanjeZaKviz
                {
                    Id = p.Id,
                    Tekst = p.Tekst,
                    Opcije = p.Opcije.ToArray()
                }).ToList();
            }
        }

        public RezultatKviza ValidirajOdgovore(List<Odgovor> odgovori)
        {
            if (odgovori == null)
            {
                return new RezultatKviza
                {
                    IsError = true,
                    ErrorMessage = "Lista odgovora nije poslata."
                };
            }

            lock (LockObj)
            {
                int tacno = 0;
                foreach (PitanjeAdmin pitanje in Pitanja)
                {
                    Odgovor odgovor = odgovori.FirstOrDefault(o => o.PitanjeId == pitanje.Id);
                    if (odgovor != null && odgovor.OdabraniOdgovor == pitanje.TacanOdgovor)
                    {
                        tacno++;
                    }
                }

                return new RezultatKviza
                {
                    IsError = false,
                    UkupnoPitanja = Pitanja.Count,
                    TacnihOdgovora = tacno,
                    Procenat = Pitanja.Count == 0 ? 0 : tacno * 100m / Pitanja.Count
                };
            }
        }

        private static OperationResult Validate(PitanjeInput pitanje)
        {
            if (pitanje == null)
            {
                return Error("Pitanje nije poslato.");
            }

            if (string.IsNullOrWhiteSpace(pitanje.Tekst))
            {
                return Error("Tekst pitanja je obavezan.");
            }

            if (pitanje.Opcije == null || pitanje.Opcije.Length < 2)
            {
                return Error("Pitanje mora imati bar dve opcije.");
            }

            if (pitanje.TacanOdgovor < 1 || pitanje.TacanOdgovor > pitanje.Opcije.Length)
            {
                return Error("Tacan odgovor mora biti redni broj postojece opcije.");
            }

            return Ok("Validno.");
        }

        private static PitanjeAdmin CloneAdmin(PitanjeAdmin p)
        {
            return new PitanjeAdmin
            {
                Id = p.Id,
                Tekst = p.Tekst,
                Opcije = p.Opcije.ToArray(),
                TacanOdgovor = p.TacanOdgovor
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
    }
}

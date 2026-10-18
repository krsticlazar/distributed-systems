using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;

namespace WCFProizvodnjaServer
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerSession, ConcurrencyMode = ConcurrencyMode.Reentrant)]
    public class ProizvodnjaService : IProizvodnjaService
    {
        private static readonly object LockObj = new object();
        private static readonly List<Proizvod> Proizvodi = new List<Proizvod>
        {
            new Proizvod { Id = 1, Naziv = "Sok od jabuke", ProizvedenaKolicina = 100 },
            new Proizvod { Id = 2, Naziv = "Sok od visnje", ProizvedenaKolicina = 80 }
        };
        private static readonly Dictionary<string, IProizvodnjaCallback> Korisnici =
            new Dictionary<string, IProizvodnjaCallback>();
        private static int nextId = 3;

        private string korisnik;

        public OperationResult RegistrujKorisnika(string ime)
        {
            if (string.IsNullOrWhiteSpace(ime))
            {
                return Error("Ime korisnika je obavezno.");
            }

            korisnik = ime.Trim();
            IProizvodnjaCallback callback = OperationContext.Current.GetCallbackChannel<IProizvodnjaCallback>();

            lock (LockObj)
            {
                Korisnici[korisnik] = callback;
            }

            return Ok("Korisnik je registrovan: " + korisnik);
        }

        public OperationResult DodajProizvod(string naziv)
        {
            if (!IsRegistered())
            {
                return Error("Korisnik nije registrovan.");
            }

            if (string.IsNullOrWhiteSpace(naziv))
            {
                return Error("Naziv proizvoda je obavezan.");
            }

            Proizvod proizvod;
            lock (LockObj)
            {
                proizvod = new Proizvod { Id = nextId++, Naziv = naziv.Trim(), ProizvedenaKolicina = 0 };
                Proizvodi.Add(proizvod);
            }

            NotifyAll("Dodat je novi proizvod: " + proizvod.Naziv + " (dodao: " + korisnik + ")");
            return Ok("Proizvod je dodat.");
        }

        public List<Proizvod> VratiProizvode()
        {
            lock (LockObj)
            {
                return Proizvodi.Select(Clone).ToList();
            }
        }

        public OperationResult ZakljucajProizvod(int proizvodId)
        {
            if (!IsRegistered())
            {
                return Error("Korisnik nije registrovan.");
            }

            lock (LockObj)
            {
                Proizvod proizvod = Find(proizvodId);
                if (proizvod == null)
                {
                    return Error("Proizvod ne postoji.");
                }

                if (!string.IsNullOrEmpty(proizvod.ZakljucaoKorisnik) && proizvod.ZakljucaoKorisnik != korisnik)
                {
                    return Error("Proizvod trenutno menja korisnik: " + proizvod.ZakljucaoKorisnik);
                }

                proizvod.ZakljucaoKorisnik = korisnik;
            }

            return Ok("Proizvod je zakljucan za korisnika: " + korisnik);
        }

        public OperationResult PovecajProizvodnju(int proizvodId, int kolicina)
        {
            if (!IsRegistered())
            {
                return Error("Korisnik nije registrovan.");
            }

            if (kolicina <= 0)
            {
                return Error("Kolicina mora biti veca od nule.");
            }

            string naziv;
            int ukupno;
            lock (LockObj)
            {
                Proizvod proizvod = Find(proizvodId);
                if (proizvod == null)
                {
                    return Error("Proizvod ne postoji.");
                }

                if (proizvod.ZakljucaoKorisnik != korisnik)
                {
                    return Error("Pre izmene moras zakljucati proizvod.");
                }

                proizvod.ProizvedenaKolicina += kolicina;
                proizvod.ZakljucaoKorisnik = null;
                naziv = proizvod.Naziv;
                ukupno = proizvod.ProizvedenaKolicina;
            }

            NotifyAll(korisnik + " je povecao proizvodnju za " + naziv + ". Ukupno: " + ukupno);
            return Ok("Proizvodnja je evidentirana.");
        }

        public OperationResult OtkljucajProizvod(int proizvodId)
        {
            lock (LockObj)
            {
                Proizvod proizvod = Find(proizvodId);
                if (proizvod == null)
                {
                    return Error("Proizvod ne postoji.");
                }

                if (proizvod.ZakljucaoKorisnik == korisnik)
                {
                    proizvod.ZakljucaoKorisnik = null;
                }
            }

            return Ok("Proizvod je otkljucan.");
        }

        private bool IsRegistered()
        {
            return !string.IsNullOrWhiteSpace(korisnik);
        }

        private static Proizvod Find(int id)
        {
            return Proizvodi.FirstOrDefault(p => p.Id == id);
        }

        private static Proizvod Clone(Proizvod p)
        {
            return new Proizvod
            {
                Id = p.Id,
                Naziv = p.Naziv,
                ProizvedenaKolicina = p.ProizvedenaKolicina,
                ZakljucaoKorisnik = p.ZakljucaoKorisnik
            };
        }

        private static void NotifyAll(string text)
        {
            List<IProizvodnjaCallback> callbacks;
            lock (LockObj)
            {
                callbacks = Korisnici.Values.ToList();
            }

            foreach (IProizvodnjaCallback callback in callbacks)
            {
                try
                {
                    callback.Obavestenje(text);
                }
                catch (CommunicationException)
                {
                }
                catch (TimeoutException)
                {
                }
            }
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

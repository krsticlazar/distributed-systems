using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;

namespace WCFElektronskiDnevnikServer
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerSession, ConcurrencyMode = ConcurrencyMode.Reentrant)]
    public class DnevnikService : IDnevnikService
    {
        private static readonly object LockObj = new object();
        private static readonly List<Ucenik> Ucenici = new List<Ucenik>
        {
            new Ucenik { Id = 1, Ime = "Ana", Prezime = "Petrovic" },
            new Ucenik { Id = 2, Ime = "Marko", Prezime = "Jovanovic" },
            new Ucenik { Id = 3, Ime = "Lena", Prezime = "Nikolic" }
        };
        private static readonly Dictionary<string, HashSet<int>> Predmeti =
            new Dictionary<string, HashSet<int>>();
        private static readonly Dictionary<string, HashSet<int>> DecaRoditelja =
            new Dictionary<string, HashSet<int>>();
        private static readonly Dictionary<string, IDnevnikCallback> Roditelji =
            new Dictionary<string, IDnevnikCallback>();
        private static readonly List<Ocena> Ocene = new List<Ocena>();

        private string profesorIme;
        private string predmet;
        private string roditeljPrezime;

        public OperationResult RegistrujProfesora(string ime, string predmetProfesora)
        {
            if (string.IsNullOrWhiteSpace(ime) || string.IsNullOrWhiteSpace(predmetProfesora))
            {
                return Error("Ime profesora i predmet su obavezni.");
            }

            profesorIme = ime.Trim();
            predmet = predmetProfesora.Trim();

            lock (LockObj)
            {
                if (!Predmeti.ContainsKey(predmet))
                {
                    Predmeti[predmet] = new HashSet<int>();
                }
            }

            return Ok("Profesor je registrovan za predmet: " + predmet);
        }

        public OperationResult RegistrujRoditelja(string prezime)
        {
            if (string.IsNullOrWhiteSpace(prezime))
            {
                return Error("Prezime roditelja je obavezno.");
            }

            roditeljPrezime = prezime.Trim();
            IDnevnikCallback callback = OperationContext.Current.GetCallbackChannel<IDnevnikCallback>();

            lock (LockObj)
            {
                Roditelji[roditeljPrezime] = callback;
                if (!DecaRoditelja.ContainsKey(roditeljPrezime))
                {
                    DecaRoditelja[roditeljPrezime] = new HashSet<int>();
                }
            }

            return Ok("Roditelj je registrovan: " + roditeljPrezime);
        }

        public List<Ucenik> VratiUcenike()
        {
            lock (LockObj)
            {
                return Ucenici.Select(Clone).ToList();
            }
        }

        public OperationResult DodajUcenikaNaPredmet(int ucenikId)
        {
            if (string.IsNullOrWhiteSpace(predmet))
            {
                return Error("Prvo registruj profesora.");
            }

            lock (LockObj)
            {
                if (!Ucenici.Any(u => u.Id == ucenikId))
                {
                    return Error("Ucenik ne postoji.");
                }

                Predmeti[predmet].Add(ucenikId);
            }

            return Ok("Ucenik je dodat na predmet " + predmet + ".");
        }

        public OperationResult DajOcenu(int ucenikId, int ocena)
        {
            if (string.IsNullOrWhiteSpace(predmet))
            {
                return Error("Prvo registruj profesora.");
            }

            if (ocena < 1 || ocena > 5)
            {
                return Error("Ocena mora biti od 1 do 5.");
            }

            Ocena novaOcena;
            List<IDnevnikCallback> callbacks;

            lock (LockObj)
            {
                Ucenik ucenik = Ucenici.FirstOrDefault(u => u.Id == ucenikId);
                if (ucenik == null)
                {
                    return Error("Ucenik ne postoji.");
                }

                if (!Predmeti.ContainsKey(predmet) || !Predmeti[predmet].Contains(ucenikId))
                {
                    return Error("Ucenik nije dodat na predmet profesora.");
                }

                novaOcena = new Ocena
                {
                    UcenikId = ucenikId,
                    Ucenik = ucenik.Ime + " " + ucenik.Prezime,
                    Predmet = predmet,
                    Vrednost = ocena,
                    Profesor = profesorIme,
                    Vreme = DateTime.Now
                };
                Ocene.Add(novaOcena);

                callbacks = Roditelji
                    .Where(r => DecaRoditelja.ContainsKey(r.Key) && DecaRoditelja[r.Key].Contains(ucenikId))
                    .Select(r => r.Value)
                    .ToList();
            }

            foreach (IDnevnikCallback callback in callbacks)
            {
                TryNotify(callback, "Nova ocena: " + novaOcena.Ucenik + ", " + novaOcena.Predmet + " = " + novaOcena.Vrednost);
            }

            return Ok("Ocena je upisana.");
        }

        public OperationResult DodajDete(int ucenikId)
        {
            if (string.IsNullOrWhiteSpace(roditeljPrezime))
            {
                return Error("Prvo registruj roditelja.");
            }

            lock (LockObj)
            {
                if (!Ucenici.Any(u => u.Id == ucenikId))
                {
                    return Error("Ucenik ne postoji.");
                }

                DecaRoditelja[roditeljPrezime].Add(ucenikId);
            }

            return Ok("Dete je dodato roditelju.");
        }

        public List<Ocena> VratiOceneZaDete(int ucenikId, string predmetFilter)
        {
            lock (LockObj)
            {
                IEnumerable<Ocena> query = Ocene.Where(o => o.UcenikId == ucenikId);
                if (!string.IsNullOrWhiteSpace(predmetFilter) && predmetFilter != "SVI")
                {
                    query = query.Where(o => o.Predmet == predmetFilter);
                }

                return query.Select(Clone).ToList();
            }
        }

        private static void TryNotify(IDnevnikCallback callback, string message)
        {
            try
            {
                callback.Obavestenje(message);
            }
            catch (CommunicationException)
            {
            }
            catch (TimeoutException)
            {
            }
        }

        private static Ucenik Clone(Ucenik u)
        {
            return new Ucenik { Id = u.Id, Ime = u.Ime, Prezime = u.Prezime };
        }

        private static Ocena Clone(Ocena o)
        {
            return new Ocena
            {
                UcenikId = o.UcenikId,
                Ucenik = o.Ucenik,
                Predmet = o.Predmet,
                Vrednost = o.Vrednost,
                Profesor = o.Profesor,
                Vreme = o.Vreme
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

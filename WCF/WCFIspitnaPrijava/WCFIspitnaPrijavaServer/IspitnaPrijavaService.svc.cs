using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WCFIspitnaPrijavaServer
{

    public class IspitnaPrijavaService : IIspitnaPrijavaService
    {
        private static readonly object LockObj = new object();

        private static readonly List<Ispit> SviIspiti = new List<Ispit>
        {
            new Ispit { Id = 1, Naziv = "Matematika 1", Profesor = "Prof. Marjanovic" },
            new Ispit { Id = 2, Naziv = "Algoritmi i programiranje", Profesor = "Prof. Jankovic" },
            new Ispit { Id = 3, Naziv = "Baze podataka", Profesor = "Prof. Nikolic" },
            new Ispit { Id = 4, Naziv = "Distribuirani sistemi", Profesor = "Prof. Milovanovic" }
        };

        private static readonly Dictionary<string, IspitnaPrijava> Prijave =
            new Dictionary<string, IspitnaPrijava>
            {
                {
                    "001",
                    new IspitnaPrijava
                    {
                        BrojIndeksa = "001",
                        ImePrezime = "Ana",
                        Ispiti = new List<Ispit>
                        {
                            new Ispit { Id = 1, Naziv = "Matematika 1", Profesor = "Prof. Marjanovic" }
                        }
                    }
                },
                {
                    "002",
                    new IspitnaPrijava
                    {
                        BrojIndeksa = "002",
                        ImePrezime = "Lazar",
                        Ispiti = new List<Ispit>()
                    }
                }
            };

        public IspitnaPrijava VratiIspitnuPrijavu(string brojIndeksa)
        {
            lock (LockObj)
            {
                IspitnaPrijava prijava = FindPrijava(brojIndeksa);
                return prijava == null ? null : ClonePrijava(prijava);
            }
        }

        public OperationResult DodajIspitUPrijavu(string brojIndeksa, int ispitId)
        {
            if (string.IsNullOrWhiteSpace(brojIndeksa))
            {
                return Error("Broj indeksa je obavezan.");
            }

            lock (LockObj)
            {
                IspitnaPrijava prijava = FindPrijava(brojIndeksa);
                if (prijava == null)
                {
                    return Error("Student sa zadatim brojem indeksa ne postoji.");
                }

                Ispit ispit = SviIspiti.FirstOrDefault(i => i.Id == ispitId);
                if (ispit == null)
                {
                    return Error("Ispit ne postoji.");
                }

                bool vecPrijavljen = prijava.Ispiti.Any(i => i.Id == ispitId);
                if (vecPrijavljen)
                {
                    return Error("Ispit je vec dodat u prijavu studenta.");
                }

                prijava.Ispiti.Add(CloneIspit(ispit));
            }

            return Ok("Ispit je dodat u ispitnu prijavu.");
        }

        public OperationResult ObrisiIspitIzPrijave(string brojIndeksa, int ispitId)
        {
            lock (LockObj)
            {
                IspitnaPrijava prijava = FindPrijava(brojIndeksa);
                if (prijava == null)
                {
                    return Error("Student sa zadatim brojem indeksa ne postoji.");
                }

                Ispit ispit = prijava.Ispiti.FirstOrDefault(i => i.Id == ispitId);
                if (ispit == null)
                {
                    return Error("Ispit nije pronadjen u prijavi studenta.");
                }

                prijava.Ispiti.Remove(ispit);
            }

            return Ok("Ispit je obrisan iz ispitne prijave.");
        }

        public string VratiFormatiraneNaziveIspita(string brojIndeksa)
        {
            lock (LockObj)
            {
                IspitnaPrijava prijava = FindPrijava(brojIndeksa);
                if (prijava == null)
                {
                    return "Student sa zadatim brojem indeksa ne postoji.";
                }

                var sb = new StringBuilder();
                sb.AppendLine("Ispitna prijava za " + prijava.BrojIndeksa + " - " + prijava.ImePrezime);

                if (prijava.Ispiti.Count == 0)
                {
                    sb.AppendLine("Student nema prijavljene ispite.");
                    return sb.ToString();
                }

                for (int i = 0; i < prijava.Ispiti.Count; i++)
                {
                    sb.AppendLine((i + 1) + ". " + prijava.Ispiti[i].Naziv);
                }

                return sb.ToString();
            }
        }

        public List<Ispit> VratiIspite()
        {
            lock (LockObj)
            {
                return SviIspiti.Select(CloneIspit).ToList();
            }
        }

        private static IspitnaPrijava FindPrijava(string brojIndeksa)
        {
            if (string.IsNullOrWhiteSpace(brojIndeksa))
            {
                return null;
            }

            IspitnaPrijava prijava;
            return Prijave.TryGetValue(brojIndeksa.Trim(), out prijava)
                ? prijava
                : null;
        }

        private static IspitnaPrijava ClonePrijava(IspitnaPrijava prijava)
        {
            return new IspitnaPrijava
            {
                BrojIndeksa = prijava.BrojIndeksa,
                ImePrezime = prijava.ImePrezime,
                Ispiti = prijava.Ispiti.Select(CloneIspit).ToList()
            };
        }

        private static Ispit CloneIspit(Ispit ispit)
        {
            return new Ispit
            {
                Id = ispit.Id,
                Naziv = ispit.Naziv,
                Profesor = ispit.Profesor
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
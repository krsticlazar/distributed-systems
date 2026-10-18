using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ServiceModel;

namespace WcfVremeServer
{
    public class VremeServis : IVremeServis
    {
        public DateTime TrenutnoVreme()
        {
            Repository.Instance.EvidentirajPoziv();
            return DateTime.Now;
        }

        public DateTime VremePokretanja()
        {
            Repository.Instance.EvidentirajPoziv();
            return Repository.Instance.VremePokretanja;
        }

        public RezultatSabiranja Saberi(ZahtevSabiranje zahtev)
        {
            Repository.Instance.EvidentirajPoziv();

            if (zahtev == null)
            {
                throw new FaultException<GreskaSabiranja>(
                    new GreskaSabiranja
                    {
                        Sifra = "ZAHTEV_NIJE_POSLAT",
                        Poruka = "Zahtev za sabiranje nije poslat."
                    },
                    "Zahtev za sabiranje nije poslat.");
            }

            if (zahtev.A < 0 || zahtev.B < 0)
            {
                throw new FaultException<GreskaSabiranja>(
                    new GreskaSabiranja
                    {
                        Sifra = "NEGATIVAN_BROJ",
                        Poruka = "Sabiranje negativnih brojeva nije dozvoljeno u ovom primeru."
                    },
                    "Neispravan zahtev za sabiranje.");
            }

            return new RezultatSabiranja
            {
                A = zahtev.A,
                B = zahtev.B,
                Zbir = zahtev.A + zahtev.B,
                VremeObrade = DateTime.Now
            };
        }

        public string Opis()
        {
            Repository.Instance.EvidentirajPoziv();

            return "Server je pokrenut u " +
                   Repository.Instance.VremePokretanja +
                   ". Broj poziva: " +
                   Repository.Instance.BrojPoziva;
        }
    }
}

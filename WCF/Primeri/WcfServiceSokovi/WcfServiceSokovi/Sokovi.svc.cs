using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;

namespace WcfServiceSokovi
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "Service1" in code, svc and config file together.
    // NOTE: In order to launch WCF Test Client for testing this service, please select Service1.svc or Service1.svc.cs at the Solution Explorer and start debugging.
    public class Sokovi : ISokovi
    {


        public void dodaj(Sastojak s)
        {
            Repository.Instance.Promene.Add(
                new Promena()
                {
                    Datum = DateTime.Now,
                    Sastojak = s,
                    TipPromena = "Dodavanje",
                });

            decimal masa = Repository.Instance.Masa;
            masa += s.Gustina * s.Zapremina;
            Repository.Instance.Zapremina += s.Zapremina;
            Repository.Instance.Gustina = masa / Repository.Instance.Zapremina;
        }

        public void flasiraj(decimal z)
        {
            Repository.Instance.Promene.Add(
                new Promena()
                {
                    Datum = DateTime.Now,
                    TipPromena = "Flasiranje",
                    Zapremina = z,
                });
            Repository.Instance.Zapremina-= z;
            if (Repository.Instance.Zapremina < 0)
                Repository.Instance.Zapremina = 0;
        }

        public void isprazni()
        {
            Repository.Instance.Promene.Add(
               new Promena()
               {
                   Datum = DateTime.Now,
                   TipPromena = "Praznjenje",
               });
            Repository.Instance.Zapremina = 0M;
        }

        public IList<Promena> Promene()
        {
            return Repository.Instance.Promene;
        }

        public Stanje VratiStanje()
        {
            return new Stanje()
            {
                Gustina = Repository.Instance.Gustina,
                Zauzece = Repository.Instance.Zapremina / Repository.Instance.MaxZapremina * 100,
            };
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WcfServiceSokovi
{
    public class Repository
    {
        private static Repository _instance;
        private static object locker = true;

        private Repository()
        {
            Promene = new List<Promena>();
            MaxZapremina = 1000;
            Zapremina = 0;
            Gustina = 0;
        }

        public static Repository Instance
        {
            get
            {
                lock (locker)
                {
                    if (_instance == null)
                        _instance = new Repository();
                }
                return _instance;
            }
        }

        public IList<Promena> Promene { get; set; }
        public decimal Zapremina { get; set; }
        public decimal MaxZapremina { get; set; }

        public decimal Gustina { get; set; }
        public decimal Masa { get { return Instance.Gustina * Instance.Zapremina; } }
    }
}
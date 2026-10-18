using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WcfVremeServer
{
    public class Repository
    {
        private static readonly object LockObj = new object();
        private static Repository instance;

        private Repository()
        {
            VremePokretanja = DateTime.Now;
            BrojPoziva = 0;
        }

        public static Repository Instance
        {
            get
            {
                lock (LockObj)
                {
                    if (instance == null)
                    {
                        instance = new Repository();
                    }

                    return instance;
                }
            }
        }

        public DateTime VremePokretanja { get; private set; }

        public int BrojPoziva { get; private set; }

        public void EvidentirajPoziv()
        {
            lock (LockObj)
            {
                BrojPoziva++;
            }
        }
    }
}

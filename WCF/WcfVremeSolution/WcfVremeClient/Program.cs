using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using WcfVremeClient.ServiceReferenceVreme;

namespace WcfVremeClient
{
    internal class Program
    {
        static void Main(string[] args)
        {
            VremeServisClient proxy = null;

            try
            {
                proxy = new VremeServisClient();

                Console.WriteLine("Trenutno vreme na serveru: " + proxy.TrenutnoVreme());
                Console.WriteLine("Server pamti pokretanje: " + proxy.VremePokretanja());

                var rezultat = proxy.Saberi(new ZahtevSabiranje
                {
                    A = 12,
                    B = 8
                });

                Console.WriteLine(rezultat.A + " + " + rezultat.B + " = " + rezultat.Zbir);
                Console.WriteLine("Obradjeno u: " + rezultat.VremeObrade);
                Console.WriteLine(proxy.Opis());

                Console.WriteLine();
                Console.WriteLine("Primer FaultException:");

                try
                {
                    proxy.Saberi(new ZahtevSabiranje
                    {
                        A = -5,
                        B = 3
                    });
                }
                catch (FaultException<GreskaSabiranja> ex)
                {
                    Console.WriteLine("Server je vratio kontrolisanu gresku.");
                    Console.WriteLine("Sifra: " + ex.Detail.Sifra);
                    Console.WriteLine("Poruka: " + ex.Detail.Poruka);
                }

                proxy.Close();
            }
            catch (FaultException ex)
            {
                Console.WriteLine("WCF greska: " + ex.Message);
                if (proxy != null)
                {
                    proxy.Abort();
                }
            }
            catch (CommunicationException ex)
            {
                Console.WriteLine("Problem u komunikaciji sa servisom: " + ex.Message);
                if (proxy != null)
                {
                    proxy.Abort();
                }
            }
            catch (TimeoutException ex)
            {
                Console.WriteLine("Isteklo je vreme cekanja na servis: " + ex.Message);
                if (proxy != null)
                {
                    proxy.Abort();
                }
            }

            Console.WriteLine("Pritisni Enter za izlaz.");
            Console.ReadLine();
        }
    }
}


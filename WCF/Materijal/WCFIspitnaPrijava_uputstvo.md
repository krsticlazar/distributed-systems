# WCFIspitnaPrijava: elektronska studentska sluzba

Ovo uputstvo je za zadatak:

> Koriscenjem WCF tehnologije implementirati sistem koji omogucava studentima da prijavljuju ispite putem korisnickog servisa elektronske studentske sluzbe.

Ovo nije callback zadatak. Klijent poziva server, server vraca rezultat. Zato koristimo obican `basicHttpBinding`.

## 1. Sta pravis

Solution neka se zove:

```text
WCFIspitnaPrijava
```

U njemu napravi:

```text
WCFIspitnaPrijava
  WCFIspitnaPrijavaServer     WCF Service Application (.NET Framework 4.8)
  WCFIspitnaPrijavaClient     Console App (.NET Framework 4.8)
```

Server predstavlja elektronsku studentsku sluzbu.

Klijent predstavlja konzolnu aplikaciju preko koje student poziva servis.

## 2. Sta sistem treba da radi

Zadatak trazi metode koje:

1. vracaju ispitnu prijavu datog studenta;
2. dodaju ispit u ispitnu prijavu studenta;
3. brisu ispit iz ispitne prijave studenta;
4. vracaju nazive ispita u obliku formatiranog teksta za izabranog studenta;
5. vracaju listu svih ispita.

Prevedeno u WCF operacije:

```csharp
IspitnaPrijava VratiIspitnuPrijavu(string brojIndeksa);
OperationResult DodajIspitUPrijavu(string brojIndeksa, int ispitId);
OperationResult ObrisiIspitIzPrijave(string brojIndeksa, int ispitId);
string VratiFormatiraneNaziveIspita(string brojIndeksa);
List<Ispit> VratiIspite();
```

## 3. Kako aplikacija radi

Server cuva:

- listu dostupnih ispita;
- ispitne prijave studenata.

Student se identifikuje preko broja indeksa, npr:

```text
001
002
```

Ispit se identifikuje preko `Id`, npr:

```text
1 - Matematika 1
2 - Algoritmi i programiranje
3 - Baze podataka
```

Kada klijent zeli da prijavi ispit, salje:

```text
brojIndeksa + ispitId
```

Server proverava:

- da student postoji;
- da ispit postoji;
- da ispit nije vec prijavljen.

Tek onda dodaje ispit u prijavu.

Kod brisanja server proverava:

- da student postoji;
- da je taj ispit stvarno u njegovoj prijavi.

## 4. Server fajlovi

U server projektu koristi ova imena:

```text
WCFIspitnaPrijavaServer
  IIspitnaPrijavaService.cs
  IspitnaPrijavaService.svc
  IspitnaPrijavaService.svc.cs
  Web.config
```

Ako Visual Studio napravi `IService1.cs` i `Service1.svc`, mozes ih obrisati ili preimenovati. Bitno je da se imena poklapaju u `.svc` fajlu, servisnoj klasi i `Web.config`.

## 5. IIspitnaPrijavaService.cs

Ovo je servisni ugovor i modeli koji se prenose izmedju klijenta i servera.

```csharp
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.ServiceModel;

namespace WCFIspitnaPrijavaServer
{
    [ServiceContract]
    public interface IIspitnaPrijavaService
    {
        [OperationContract]
        IspitnaPrijava VratiIspitnuPrijavu(string brojIndeksa);

        [OperationContract]
        OperationResult DodajIspitUPrijavu(string brojIndeksa, int ispitId);

        [OperationContract]
        OperationResult ObrisiIspitIzPrijave(string brojIndeksa, int ispitId);

        [OperationContract]
        string VratiFormatiraneNaziveIspita(string brojIndeksa);

        [OperationContract]
        List<Ispit> VratiIspite();
    }

    [DataContract]
    public class Ispit
    {
        [DataMember]
        public int Id { get; set; }

        [DataMember]
        public string Naziv { get; set; }

        [DataMember]
        public string Profesor { get; set; }
    }

    [DataContract]
    public class IspitnaPrijava
    {
        [DataMember]
        public string BrojIndeksa { get; set; }

        [DataMember]
        public string ImePrezime { get; set; }

        [DataMember]
        public List<Ispit> Ispiti { get; set; }
    }

    [DataContract]
    public class OperationResult
    {
        [DataMember]
        public bool IsError { get; set; }

        [DataMember]
        public string Message { get; set; }
    }
}
```

Sta je bitno:

- `IIspitnaPrijavaService` je servisni ugovor.
- `[ServiceContract]` oznacava da je interfejs WCF servis.
- Svaka metoda koju klijent treba da vidi ima `[OperationContract]`.
- `Ispit`, `IspitnaPrijava` i `OperationResult` su `[DataContract]` klase.
- Svaki property koji treba da se prenese ima `[DataMember]`.

## 6. Objasnjenje metoda

### `VratiIspitnuPrijavu`

Prima broj indeksa i vraca celu prijavu tog studenta.

Ako student ima prijavljene ispite, klijent dobija:

```text
broj indeksa
ime i prezime
listu prijavljenih ispita
```

Ako student ne postoji, u ovoj verziji vracamo `null`, a klijent to proverava.

### `DodajIspitUPrijavu`

Prima:

```text
brojIndeksa
ispitId
```

Server mora da proveri:

- da broj indeksa nije prazan;
- da student postoji;
- da ispit postoji;
- da taj ispit nije vec dodat u prijavu.

Vraca `OperationResult`.

### `ObrisiIspitIzPrijave`

Prima:

```text
brojIndeksa
ispitId
```

Server mora da proveri:

- da student postoji;
- da je taj ispit stvarno u njegovoj prijavi.

Ako jeste, uklanja ga iz liste prijavljenih ispita.

### `VratiFormatiraneNaziveIspita`

Vraca tekst spreman za ispis.

Primer:

```text
Ispitna prijava za 001 - Ana
1. Matematika 1
2. Algoritmi i programiranje
```

Ova metoda postoji zato sto zadatak posebno trazi nazive ispita u obliku formatiranog teksta.

### `VratiIspite`

Vraca sve dostupne ispite koje student moze da prijavi.

## 7. IspitnaPrijavaService.svc

Sadrzaj `.svc` fajla:

```aspx
<%@ ServiceHost Language="C#" Debug="true" Service="WCFIspitnaPrijavaServer.IspitnaPrijavaService" CodeBehind="IspitnaPrijavaService.svc.cs" %>
```

Ovo mora da pokazuje na pravu servisnu klasu:

```text
WCFIspitnaPrijavaServer.IspitnaPrijavaService
```

Ako ovde ostane `Service1`, servis nece raditi kako ocekujes.

## 8. IspitnaPrijavaService.svc.cs

Ovo je implementacija servisa.

```csharp
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
```

Sta je bitno:

- `SviIspiti` je lista svih ispita koje student moze da prijavi.
- `Prijave` je dictionary gde je kljuc broj indeksa.
- `static` podaci glume bazu za potrebe lab vezbe.
- `LockObj` stiti zajednicko stanje ako vise klijenata pozove servis u isto vreme.
- `ClonePrijava` i `CloneIspit` vracaju kopije objekata, ne interne reference.
- Server radi validaciju, ne klijent.

## 9. Web.config za server

U `Web.config` ostavi `appSettings`, `system.web` i `system.webServer`, a `system.serviceModel` podesi ovako:

```xml
<system.serviceModel>
  <behaviors>
    <serviceBehaviors>
      <behavior name="IspitnaPrijavaServiceBehavior">
        <serviceMetadata httpGetEnabled="true" httpsGetEnabled="true"/>
        <serviceDebug includeExceptionDetailInFaults="false"/>
      </behavior>
    </serviceBehaviors>
  </behaviors>

  <services>
    <service name="WCFIspitnaPrijavaServer.IspitnaPrijavaService"
             behaviorConfiguration="IspitnaPrijavaServiceBehavior">
      <endpoint address=""
                binding="basicHttpBinding"
                contract="WCFIspitnaPrijavaServer.IIspitnaPrijavaService" />

      <endpoint address="mex"
                binding="mexHttpBinding"
                contract="IMetadataExchange" />
    </service>
  </services>

  <serviceHostingEnvironment aspNetCompatibilityEnabled="true"
                             multipleSiteBindingsEnabled="true" />
</system.serviceModel>
```

Najbitnije:

- koristi se `basicHttpBinding`, jer nema callback-a;
- `service name` mora biti puna putanja do servisne klase;
- `contract` mora biti servisni interfejs `IIspitnaPrijavaService`;
- MEX endpoint sluzi da klijent moze da doda Service Reference.

## 10. Dodavanje Service Reference u klijent

U primerima sa casa klijent uglavnom ne pravi rucno `ChannelFactory`, nego koristi `Service Reference`, pa se dobije generisana klasa tipa:

```csharp
IspitnaPrijavaServiceClient
```

Zato i ovde koristi taj stil.

1. Pokreni server projekat.
2. Otvori u browseru:

```text
http://localhost:PORT/IspitnaPrijavaService.svc
```

3. Ako se servis otvori, idi u client projekat.
4. Desni klik na `WCFIspitnaPrijavaClient`.
5. Izaberi:

```text
Add > Service Reference
```

ili:

```text
Add > Connected Service > Microsoft WCF Web Service Reference Provider
```

6. Unesi adresu servisa.
7. Namespace stavi:

```text
ServiceReferenceIspitnaPrijava
```

8. Klikni `Go`.
9. Ako imas dugme `Advanced`, mozes za `Collection type` da izaberes `System.Collections.Generic.List`. Ako ne diras nista, Visual Studio moze generisati nizove. Kod ispod radi i sa nizom i sa listom.
10. Klikni `OK` / `Finish`.

Ako posle toga promenis `IIspitnaPrijavaService` ili data modele, obavezno uradi:

```text
Update Service Reference
```

## 11. Program.cs u klijentu

Ovo je konzolni klijent sa menijem. Zameni ceo sadrzaj fajla `Program.cs` ovim kodom.

```csharp
using System;
using System.Linq;
using WCFIspitnaPrijavaClient.ServiceReferenceIspitnaPrijava;

namespace WCFIspitnaPrijavaClient
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var proxy = new IspitnaPrijavaServiceClient();

            while (true)
            {
                Console.WriteLine();
                Console.WriteLine("Elektronska studentska sluzba");
                Console.WriteLine("1 - Prikazi sve ispite");
                Console.WriteLine("2 - Vrati ispitnu prijavu studenta");
                Console.WriteLine("3 - Dodaj ispit u prijavu");
                Console.WriteLine("4 - Obrisi ispit iz prijave");
                Console.WriteLine("5 - Formatirani nazivi ispita za studenta");
                Console.WriteLine("q - Izlaz");
                Console.Write("Izbor: ");

                string izbor = Console.ReadLine();

                if (izbor == "q")
                {
                    break;
                }

                switch (izbor)
                {
                    case "1":
                        PrikaziSveIspite(proxy);
                        break;
                    case "2":
                        VratiPrijavu(proxy);
                        break;
                    case "3":
                        DodajIspit(proxy);
                        break;
                    case "4":
                        ObrisiIspit(proxy);
                        break;
                    case "5":
                        VratiFormatiranTekst(proxy);
                        break;
                    default:
                        Console.WriteLine("Nepoznata opcija.");
                        break;
                }
            }

            proxy.Close();
        }

        private static void PrikaziSveIspite(IspitnaPrijavaServiceClient proxy)
        {
            Console.WriteLine();
            Console.WriteLine("Dostupni ispiti:");

            foreach (Ispit ispit in proxy.VratiIspite())
            {
                Console.WriteLine(ispit.Id + ". " + ispit.Naziv + " - " + ispit.Profesor);
            }
        }

        private static void VratiPrijavu(IspitnaPrijavaServiceClient proxy)
        {
            Console.Write("Broj indeksa: ");
            string brojIndeksa = Console.ReadLine();

            IspitnaPrijava prijava = proxy.VratiIspitnuPrijavu(brojIndeksa);
            if (prijava == null)
            {
                Console.WriteLine("Student ne postoji.");
                return;
            }

            Console.WriteLine();
            Console.WriteLine(prijava.BrojIndeksa + " - " + prijava.ImePrezime);

            if (prijava.Ispiti == null || !prijava.Ispiti.Any())
            {
                Console.WriteLine("Nema prijavljenih ispita.");
                return;
            }

            foreach (Ispit ispit in prijava.Ispiti)
            {
                Console.WriteLine(ispit.Id + ". " + ispit.Naziv);
            }
        }

        private static void DodajIspit(IspitnaPrijavaServiceClient proxy)
        {
            Console.Write("Broj indeksa: ");
            string brojIndeksa = Console.ReadLine();

            Console.Write("ID ispita: ");
            int ispitId = ReadInt();

            OperationResult result = proxy.DodajIspitUPrijavu(brojIndeksa, ispitId);
            PrintResult(result);
        }

        private static void ObrisiIspit(IspitnaPrijavaServiceClient proxy)
        {
            Console.Write("Broj indeksa: ");
            string brojIndeksa = Console.ReadLine();

            Console.Write("ID ispita: ");
            int ispitId = ReadInt();

            OperationResult result = proxy.ObrisiIspitIzPrijave(brojIndeksa, ispitId);
            PrintResult(result);
        }

        private static void VratiFormatiranTekst(IspitnaPrijavaServiceClient proxy)
        {
            Console.Write("Broj indeksa: ");
            string brojIndeksa = Console.ReadLine();

            Console.WriteLine();
            Console.WriteLine(proxy.VratiFormatiraneNaziveIspita(brojIndeksa));
        }

        private static int ReadInt()
        {
            int value;
            if (!int.TryParse(Console.ReadLine(), out value))
            {
                return 0;
            }

            return value;
        }

        private static void PrintResult(OperationResult result)
        {
            Console.WriteLine(result.IsError ? "Greska: " + result.Message : result.Message);
        }
    }
}
```

Najbitnije:

- `IspitnaPrijavaServiceClient` je generisana proxy klasa iz Service Reference-a.
- Namespace mora da bude isti kao namespace koji si uneo kod dodavanja reference: `ServiceReferenceIspitnaPrijava`.
- Ako promenis server ugovor, uradi `Update Service Reference`.
- `Any()` radi i ako Visual Studio generise `Ispiti` kao niz i ako ih generise kao listu.

## 12. App.config u klijentu

Visual Studio ce uglavnom sam dodati ovaj deo posle Service Reference-a. Treba da izgleda slicno ovome:

```xml
<system.serviceModel>
  <bindings>
    <basicHttpBinding>
      <binding name="BasicHttpBinding_IIspitnaPrijavaService" />
    </basicHttpBinding>
  </bindings>

  <client>
    <endpoint address="http://localhost:PORT/IspitnaPrijavaService.svc"
              binding="basicHttpBinding"
              bindingConfiguration="BasicHttpBinding_IIspitnaPrijavaService"
              contract="ServiceReferenceIspitnaPrijava.IIspitnaPrijavaService"
              name="BasicHttpBinding_IIspitnaPrijavaService" />
  </client>
</system.serviceModel>
```

Ako servis radi na portu `55106`, onda adresa treba da bude:

```xml
address="http://localhost:55106/IspitnaPrijavaService.svc"
```

Najcesca greska je da server radi na jednom portu, a `App.config` pokazuje na drugi.

## 13. Kako testiras

1. Pokreni server.
2. Otvori browser:

```text
http://localhost:PORT/IspitnaPrijavaService.svc
```

Ako se otvori WCF stranica, server radi.

3. U klijentu uradi `Update Service Reference`.
4. Pokreni klijent.

Prvo prikazi sve ispite:

```text
1 - Prikazi sve ispite
```

Ocekivano:

```text
1. Matematika 1 - Prof. Marjanovic
2. Algoritmi i programiranje - Prof. Jankovic
3. Baze podataka - Prof. Nikolic
4. Distribuirani sistemi - Prof. Milovanovic
```

Zatim proveri prijavu za Anu:

```text
2 - Vrati ispitnu prijavu studenta
Broj indeksa: 001
```

Ocekivano:

```text
001 - Ana
1. Matematika 1
```

Dodaj Algoritmi i programiranje:

```text
3 - Dodaj ispit u prijavu
Broj indeksa: 001
ID ispita: 2
```

Ocekivano:

```text
Ispit je dodat u ispitnu prijavu.
```

Probaj opet da dodas isti ispit:

```text
3 - Dodaj ispit u prijavu
Broj indeksa: 001
ID ispita: 2
```

Ocekivano:

```text
Greska: Ispit je vec dodat u prijavu studenta.
```

Prikazi formatiran tekst:

```text
5 - Formatirani nazivi ispita za studenta
Broj indeksa: 001
```

Ocekivano:

```text
Ispitna prijava za 001 - Ana
1. Matematika 1
2. Algoritmi i programiranje
```

Obrisi Algoritmi i programiranje:

```text
4 - Obrisi ispit iz prijave
Broj indeksa: 001
ID ispita: 2
```

Ocekivano:

```text
Ispit je obrisan iz ispitne prijave.
```

## 14. Najcesce greske

Ako se server ne otvara:

- proveri `IspitnaPrijavaService.svc`;
- proveri `Service="WCFIspitnaPrijavaServer.IspitnaPrijavaService"`;
- proveri `service name` u `Web.config`;
- proveri `contract` u endpointu.

Ako klijent ne vidi metode:

- pokreni server;
- uradi `Update Service Reference`;
- proveri da li u `Program.cs` koristis `using WCFIspitnaPrijavaClient.ServiceReferenceIspitnaPrijava;`;
- proveri da li se proxy klasa zove `IspitnaPrijavaServiceClient`.

Ako puca konekcija:

- proveri port u `App.config`;
- proveri da li je server projekat stvarno pokrenut;
- proveri da li endpoint koristi `basicHttpBinding`.

Ako se prijavljeni ispiti ne cuvaju:

- proveri da li su liste `static`;
- proveri da li ne restartujes server izmedju testova.

## 15. Sta treba da znas za ispit

Ako dobijes ovaj zadatak, redosled razmisljanja je:

1. Ovo nije callback zadatak.
2. Koristim `basicHttpBinding`.
3. Pravim servisni interfejs `IIspitnaPrijavaService`.
4. Pravim data modele `Ispit`, `IspitnaPrijava`, `OperationResult`.
5. Server cuva dostupne ispite i prijave studenata.
6. Dodavanje i brisanje rade validaciju na serveru.
7. Klijent preko generisanog `IspitnaPrijavaServiceClient` proxy-ja poziva metode servisa.
8. `VratiFormatiraneNaziveIspita` vraca gotov tekst jer zadatak to eksplicitno trazi.

Najbitnije recenice za usmeno:

```text
Ovo je obican request/response WCF servis, zato koristim basicHttpBinding.
Student se identifikuje brojem indeksa, ispit ID-em.
Server cuva listu ispita i prijave, i na serveru proverava da li student i ispit postoje.
```

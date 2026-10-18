# WCF priprema: servis i console klijent

Ovaj dokument sluzi kao praktican redosled za pravljenje osnovne WCF aplikacije u jednom solution-u: jedan projekat je server, drugi je console klijent. Primer je namerno jednostavan: servis vraca trenutno vreme servera, pamti vreme prvog pokretanja kroz zajednicki repozitorijum i radi jednostavno sabiranje.

Najvaznije pravilo: za ovaj predmet koristi WCF u `.NET Framework` projektu. Nemoj praviti `.NET`, `.NET Core`, `.NET 5+` server projekat za klasicni WCF servis. Za klijenta na ispitu takodje najjednostavnije koristi `Console App (.NET Framework)`.

## Kratko po slajdovima

1. `Distribuirani sistemi / WCF`

WCF je Microsoft tehnologija za pravljenje distribuiranih aplikacija. Ideja je da jedna aplikacija izlozi servis, a druga aplikacija ga poziva preko mreze kao da poziva obicnu metodu.

2. `Sadrzaj`

Tri glavne teme su uvod, API i primer. Za ispit je prakticno najbitnije da znas sablon: ugovor, implementacija, host, endpoint, service reference, proxy poziv.

3. `Interoperabilnost`

WCF je pravljen da radi kroz standarde web servisa. Zbog toga klijent ne mora nuzno biti C#, ali na vezbama i ispitu je najlakse da i server i klijent budu C#.

4. `WEB service standardi`

SOAP je format poruka. WSDL opisuje servis i metode koje klijent moze da vidi. WS-Security, WS-ReliableMessaging i transakcioni standardi su dodatni slojevi za ozbiljnije sisteme. Za osnovni primer uglavnom ti trebaju SOAP i WSDL.

5. `Kreiranje WCF servisa`

Servis se sastoji iz klase koja implementira metode, host procesa u kome servis radi i endpoint-a preko kog klijent pristupa servisu. U ovom primeru host je WCF Service Application preko IIS Express-a u Visual Studio-u.

6. `Primer servisa`

`[ServiceContract]` oznacava interfejs koji se vidi spolja. `[OperationContract]` oznacava metodu koja se vidi klijentu. Ako metoda nema `[OperationContract]`, klijent je nece dobiti u proxy klasi.

7. `DataContract / DataMember`

Kada saljes slozen tip, napravi posebnu klasu i oznaci je sa `[DataContract]`. Svako polje ili property koji treba da se prenese oznaci sa `[DataMember]`. Ako property nema `[DataMember]`, ne racunaj da ce biti poslat klijentu.

8. `Interfejs i implementacija`

Interfejs opisuje sta servis nudi, a servisna klasa implementira kako to radi. Klijent ne treba da referencira servisni projekat direktno, nego treba da doda `Service Reference` i koristi generisani proxy.

9. `Definisanje endpoint-a`

Endpoint je kombinacija `address + binding + contract`. `address` govori gde je servis, `binding` kako se komunicira, a `contract` koji interfejs se izlozi. Za pocetak koristi `basicHttpBinding`.

10. `Kreiranje WCF klijenta`

Klijent dodaje service reference, Visual Studio generise proxy klasu, a onda se poziva npr. `VremeServisClient proxy = new VremeServisClient(); proxy.TrenutnoVreme();`.

## Sta instalirati i proveriti

U Visual Studio Installer-u proveri da je instalirana WCF komponenta:

1. Otvori `Visual Studio Installer`.
2. Izaberi svoju verziju Visual Studio-a i klikni `Modify`.
3. Idi na `Individual components`.
4. Nadji i ukljuci `Windows Communication Foundation`.
5. Potvrdi sa `Modify`.

Ako u VS26 ne vidis potpuno iste nazive menija, trazi isto znacenje: WCF template, .NET Framework projekat i Add Service Reference.

Koristi ove tipove projekata:

- Server: `WCF Service Application (.NET Framework)`.
- Klijent: `Console App (.NET Framework)`.
- Target framework: najbolje `.NET Framework 4.8` ili `.NET Framework 4.8.1`, ako je dostupno.

Nemoj izabrati:

- `Console App` koji je `.NET 8`, `.NET 9`, `.NET 10` ili slicno.
- ASP.NET Core server za ovaj osnovni WCF primer.
- Direktan `Project Reference` sa klijenta na server kao zamenu za `Service Reference`.

## Redosled kreiranja solution-a

1. `File > New > Project`.
2. Izaberi `Blank Solution`, ako postoji. Nazovi ga `WcfVremeSolution`.
3. Desni klik na solution, pa `Add > New Project`.
4. Izaberi `WCF Service Application (.NET Framework)`.
5. Nazovi server projekat `WcfVremeServer`.
6. Desni klik na solution, pa `Add > New Project`.
7. Izaberi `Console App (.NET Framework)`.
8. Nazovi klijent projekat `WcfVremeClient`.

Na kraju treba da imas:

```text
WcfVremeSolution
  WcfVremeServer
  WcfVremeClient
```

## Server projekat

WCF Service Application obicno napravi default fajlove kao `IService1.cs`, `Service1.svc`, `Service1.svc.cs` i `Web.config`. Mozes ih preimenovati ili obrisati i dodati nove. Bitno je da imena u `.svc`, implementaciji i `Web.config` budu uskladjena.

Za ovaj primer koristi ova imena:

```text
WcfVremeServer
  IVremeServis.cs
  Repository.cs
  VremeServis.svc
  VremeServis.svc.cs
  Web.config
```

### IVremeServis.cs

Ovaj fajl je ugovor servisa. Tu pises sta klijent sme da pozove.

```csharp
using System;
using System.Runtime.Serialization;
using System.ServiceModel;

namespace WcfVremeServer
{
    [ServiceContract]
    public interface IVremeServis
    {
        [OperationContract]
        DateTime TrenutnoVreme();

        [OperationContract]
        DateTime VremePokretanja();

        [OperationContract]
        RezultatSabiranja Saberi(ZahtevSabiranje zahtev);

        [OperationContract]
        string Opis();
    }

    [DataContract]
    public class ZahtevSabiranje
    {
        [DataMember]
        public int A { get; set; }

        [DataMember]
        public int B { get; set; }
    }

    [DataContract]
    public class RezultatSabiranja
    {
        [DataMember]
        public int A { get; set; }

        [DataMember]
        public int B { get; set; }

        [DataMember]
        public int Zbir { get; set; }

        [DataMember]
        public DateTime VremeObrade { get; set; }
    }
}
```

Sta je bitno:

- `IVremeServis` je public interfejs.
- `[ServiceContract]` kaze da je ovo WCF ugovor.
- `[OperationContract]` mora da stoji iznad svake metode koju klijent treba da vidi.
- `ZahtevSabiranje` i `RezultatSabiranja` su slozeni tipovi, zato imaju `[DataContract]`.
- Svaki property koji se salje preko mreze ima `[DataMember]`.

### Repository.cs

Ovaj fajl cuva stanje koje zivi dok server radi. To je korisno jer je default WCF ponasanje cesto po pozivu, pa servisna klasa sama po sebi nije dobro mesto za zajednicko stanje.

```csharp
using System;

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
```

Sta je bitno:

- Konstruktor je `private`, pa niko spolja ne moze da pravi nove instance.
- `Instance` vraca jednu zajednicku instancu.
- `lock` sprecava da dva klijenta istovremeno naprave ili menjaju stanje na los nacin.
- Ovo je dobar obrazac za ispitne zadatke gde servis pamti listu pitanja, racune, proizvode, ocene ili istoriju promena.

### VremeServis.svc

Ovo je host fajl. On govori WCF-u koja klasa predstavlja servis.

```aspx
<%@ ServiceHost Language="C#" Debug="true" Service="WcfVremeServer.VremeServis" CodeBehind="VremeServis.svc.cs" %>
```

Sta je bitno:

- `Service="WcfVremeServer.VremeServis"` mora da se poklopi sa namespace-om i imenom servisne klase.
- Ako preimenujes klasu ili namespace, promeni i ovaj fajl.
- Ako ovde ostane staro `Service1`, klijent nece gadjati tvoj novi servis.

### VremeServis.svc.cs

Ovo je implementacija. Tu pises stvarnu logiku.

```csharp
using System;
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
                throw new FaultException("Zahtev za sabiranje nije poslat.");
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
```

Sta je bitno:

- Klasa `VremeServis` implementira `IVremeServis`.
- Potpis svake metode mora da se poklopi sa interfejsom.
- Stanje se ne cuva u obicnom polju servisne klase, nego u `Repository.Instance`.
- `FaultException` je osnovni nacin da servis vrati WCF gresku klijentu.

### Web.config

Kada kreiras `WCF Service Application that is hosted in IIS/WAS` za `.NET Framework 4.8`, Visual Studio ce napraviti `Web.config` koji vec ima ove celine:

- `appSettings`
- `system.web`
- `system.serviceModel`
- `system.webServer`

Ne diraj `appSettings`, `system.web` i `system.webServer`. Tu vec stoji `targetFramework="4.8"` i to je dobro.

Menjas samo deo od:

```xml
<system.serviceModel>
```

do:

```xml
</system.serviceModel>
```

Taj ceo `system.serviceModel` deo zameni ovim:

```xml
<system.serviceModel>
  <behaviors>
    <serviceBehaviors>
      <behavior name="VremeServisBehavior">
        <serviceMetadata httpGetEnabled="true" httpsGetEnabled="true" />
        <serviceDebug includeExceptionDetailInFaults="false" />
      </behavior>
    </serviceBehaviors>
  </behaviors>

  <services>
    <service name="WcfVremeServer.VremeServis"
             behaviorConfiguration="VremeServisBehavior">
      <endpoint address=""
                binding="basicHttpBinding"
                contract="WcfVremeServer.IVremeServis" />

      <endpoint address="mex"
                binding="mexHttpBinding"
                contract="IMetadataExchange" />
    </service>
  </services>

  <serviceHostingEnvironment aspNetCompatibilityEnabled="true"
                             multipleSiteBindingsEnabled="true" />
</system.serviceModel>
```

Drugim recima, iz default fajla zadrzavas ovo:

```xml
<appSettings>
  <add key="aspnet:UseTaskFriendlySynchronizationContext" value="true" />
</appSettings>
<system.web>
  <compilation debug="true" targetFramework="4.8" />
  <httpRuntime targetFramework="4.8"/>
</system.web>
```

i ovo:

```xml
<system.webServer>
  <modules runAllManagedModulesForAllRequests="true"/>
  <directoryBrowse enabled="true"/>
</system.webServer>
```

A u `system.serviceModel` dodajes eksplicitno:

- `behavior name="VremeServisBehavior"` da bi servis mogao da se poveze sa tim podesavanjem.
- `serviceMetadata httpGetEnabled="true"` da bi klijent mogao da procita WSDL/metapodatke.
- `services/service` sa tacnim imenom servisne klase.
- obican endpoint sa `basicHttpBinding`.
- `mex` endpoint za metapodatke.

Default deo:

```xml
<protocolMapping>
  <add binding="basicHttpsBinding" scheme="https" />
</protocolMapping>
```

mozes da obrises u ovom primeru. On mapira `https` na `basicHttpsBinding`, a ovde pravimo jednostavan lokalni HTTP primer sa eksplicitnim `basicHttpBinding` endpointom. Ako ostane, uglavnom nece smetati, ali za ispitni/lab primer je cistije da konfiguracija bude eksplicitna i kratka.

Za ovaj primer koristi `http://localhost:PORT/VremeServis.svc` adresu kada dodajes Service Reference u klijentu. Ako Visual Studio automatski otvori `https://...`, za vezbu samo promeni adresu u `http://...` sa odgovarajucim portom. Ako bas zelis HTTPS varijantu, onda endpoint binding treba da bude `basicHttpsBinding`, a mex endpoint `mexHttpsBinding`.

Sta je bitno:

- `service name` mora da bude puno ime servisne klase: `namespace.klasa`.
- `contract` mora da bude puno ime interfejsa: `namespace.interfejs`.
- `basicHttpBinding` je najjednostavniji binding za osnovni primer.
- `mexHttpBinding` i `serviceMetadata` omogucavaju da Visual Studio procita metapodatke i napravi service reference.
- Ako kasnije radis sesije, verovatnije ces koristiti `wsHttpBinding` i `SessionMode`.

## Test servera

1. Desni klik na `VremeServis.svc`.
2. Izaberi `Set As Start Page`, ako postoji.
3. Pokreni server sa `F5`.
4. Browser treba da otvori adresu nalik:

```text
http://localhost:12345/VremeServis.svc
```

Ako se otvori stranica koja kaze da je servis napravljen i prikazuje WSDL link, server je ziv. Zapamti port, jer klijent koristi tu adresu.

Ako dobijes gresku:

- Proveri da li je projekat `.NET Framework`.
- Proveri da li se `Service` u `.svc` fajlu poklapa sa klasom.
- Proveri da li `Web.config` ima tacan `service name` i `contract`.
- Proveri da li svaka metoda u interfejsu ima `[OperationContract]`.

## Dodavanje Service Reference u klijentu

Ovo je deo koji je bitan da ne pomesas sa obicnom referencom.

1. Server mora da radi.
2. Desni klik na projekat `WcfVremeClient`.
3. Izaberi `Add > Service Reference`.
4. Ako postoji `Discover`, klikni `Discover`.
5. Ako `Discover` ne nadje servis, unesi adresu rucno:

```text
http://localhost:12345/VremeServis.svc
```

6. Klikni `Go`.
7. Kao namespace upisi:

```text
ServiceReferenceVreme
```

8. Klikni `OK`.

Visual Studio ce dodati proxy kod i podesavanja u `App.config`. Klijent treba da koristi taj generisani proxy. Ne dodaj direktno referencu na serverski projekat kao zamenu za service reference.

Kada promenis interfejs na serveru:

1. Pokreni server ponovo.
2. Desni klik na `ServiceReferenceVreme`.
3. Izaberi `Update Service Reference`.
4. Tek onda ce klijent videti nove metode i tipove.

## Client Program.cs

U `WcfVremeClient` zameni `Program.cs` ovim:

```csharp
using System;
using WcfVremeClient.ServiceReferenceVreme;

namespace WcfVremeClient
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var proxy = new VremeServisClient();

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

            Console.WriteLine("Pritisni Enter za izlaz.");
            Console.ReadLine();
        }
    }
}
```

Ako `VremeServisClient` nije tacno ime, otvori `Service References > ServiceReferenceVreme > Reference.cs` i vidi kako se zove generisana proxy klasa. Najcesce je ime izvedeno iz imena servisa ili interfejsa.

## Pokretanje oba projekta

Najjednostavnije za vezbu:

1. Pokreni server projekat.
2. Ostavi ga da radi.
3. Pokreni klijent projekat.

Ako hoces iz jednog `F5`:

1. Desni klik na solution.
2. `Properties`.
3. `Startup Project`.
4. Izaberi `Multiple startup projects`.
5. Za `WcfVremeServer` stavi `Start`.
6. Za `WcfVremeClient` stavi `Start`.
7. Server treba da bude iznad klijenta ili bar da se startuje pre klijenta.

## Sta treba da razumes iz primera

WCF ima cetiri glavna dela:

- `Contract`: interfejs oznacen sa `[ServiceContract]`.
- `Operation`: metoda oznacena sa `[OperationContract]`.
- `Data`: klase oznacene sa `[DataContract]` i property-ji sa `[DataMember]`.
- `Endpoint`: `address + binding + contract` u konfiguraciji.

Klijent ne zove server direktno. Klijent zove proxy klasu koju je Visual Studio napravio iz WSDL metapodataka. Zato je `Service Reference` centralni korak.

## PerCall, PerSession i Single

Ovo je bitno za zadatke gde servis nesto pamti.

- `PerCall`: za svaki poziv moze nova instanca servisa. Dobro za operacije bez stanja.
- `PerSession`: jedna instanca po klijentskoj sesiji. Dobro ako svaki klijent ima svoje stanje.
- `Single`: jedna instanca servisa za sve klijente. Dobro za zajednicko stanje, ali treba paziti na konkurentne pozive.

Za ispit je cesto najstabilnije da zajednicke podatke cuvas u posebnoj singleton/repository klasi, kao u ovom dokumentu. Tada servis moze da ostane jednostavan, a stanje ostaje zajednicko dok host radi.

Ako tekst zadatka kaze:

- "svaki korisnik ima svoje stanje" - razmisli o sesiji.
- "svi korisnici vide isto stanje" - koristi zajednicki repository ili singleton.
- "operacija samo obradi i vrati rezultat" - ne treba stanje.

## FaultContract ukratko

Ako treba da klijent dobije strukturisanu gresku, pravi se posebna data contract klasa:

```csharp
[DataContract]
public class ServerskaGreska
{
    [DataMember]
    public string Poruka { get; set; }
}
```

Na metodu u interfejsu dodajes:

```csharp
[OperationContract]
[FaultContract(typeof(ServerskaGreska))]
RezultatSabiranja Saberi(ZahtevSabiranje zahtev);
```

U implementaciji bacas:

```csharp
throw new FaultException<ServerskaGreska>(
    new ServerskaGreska { Poruka = "Neispravan zahtev." });
```

Na klijentu hvatas:

```csharp
try
{
    var rezultat = proxy.Saberi(zahtev);
}
catch (FaultException<ServerskaGreska> ex)
{
    Console.WriteLine(ex.Detail.Poruka);
}
```

Za osnovne zadatke ovo cesto nije neophodno, ali je vazno da prepoznas kada se trazi validacija i jasna serverska greska.

## Kako se ovo preslikava na ispitni zadatak

Skoro svaki zadatak mozes da rastavis ovako:

1. Iz teksta izvuci operacije koje klijent treba da poziva.
2. Te operacije stavi u WCF interfejs.
3. Sve slozene podatke napravi kao `[DataContract]` klase.
4. Implementaciju metoda pisi u servisnoj klasi.
5. Zajednicke liste i stanje cuvaj u repository klasi.
6. U `Web.config` proveri endpoint.
7. U klijentu dodaj ili update-uj `Service Reference`.
8. U console aplikaciji napravi meni koji zove proxy metode.

Primer za kviz:

- `DodajPitanje(Pitanje pitanje)`
- `IzmeniPitanje(int id, Pitanje pitanje)`
- `List<Pitanje> VratiPitanja()`
- `RezultatKviza Validiraj(List<Odgovor> odgovori)`

Primer za racune:

- `DodajRacun(Racun racun)`
- `List<Racun> VratiRacune()`
- `void Plati(string brojRacuna, decimal iznos)`
- `List<Placanje> Listing(string brojRacuna)`

Primer za proizvode:

- `DodajProizvod(Proizvod proizvod)`
- `List<Proizvod> VratiProizvode()`
- `void RegistrujProizvodnju(int proizvodId, decimal kolicina)`

## Najcesce greske

- Napravljen je `.NET` ili `.NET Core` projekat umesto `.NET Framework`.
- Klijent ima obican project reference na server umesto `Service Reference`.
- Metoda postoji u implementaciji, ali nije u interfejsu.
- Metoda jeste u interfejsu, ali nema `[OperationContract]`.
- Klasa se salje kroz WCF, ali nema `[DataContract]`.
- Property se salje kroz WCF, ali nema `[DataMember]`.
- Promenjen je server, ali nije uradjen `Update Service Reference`.
- `.svc` fajl pokazuje na staru servisnu klasu.
- `Web.config` ima pogresan `service name` ili `contract`.
- Server nije pokrenut kada dodajes service reference.
- Port u client `App.config` ne odgovara portu pokrenutog servera.

## Mini priprema za callback

Callback je sledeci korak kada klijent ne treba stalno da pita server da li se nesto desilo. Umesto toga, klijent se prijavi, a server ga kasnije pozove nazad.

Za callback se pojavljuju novi pojmovi:

- callback interfejs koji implementira klijent,
- duplex komunikacija,
- binding koji podrzava callback, cesto `wsDualHttpBinding` ili `netTcpBinding`,
- na klijentu se proxy pravi sa callback instancom.

Za ovaj cas je dovoljno da dobro razumes obican smer: klijent poziva servis preko proxy-ja. Callback je isti sistem ugovora i endpoint-a, samo komunikacija ide i nazad ka klijentu.

## Brza kontrolna lista

Pre nego sto kazes da primer radi, proveri:

- Server je `.NET Framework` WCF projekat.
- Klijent je `Console App (.NET Framework)`.
- Server ima interfejs sa `[ServiceContract]`.
- Svaka javna operacija za klijenta ima `[OperationContract]`.
- Slozeni tipovi imaju `[DataContract]` i `[DataMember]`.
- `.svc` pokazuje na pravu servisnu klasu.
- `Web.config` ima endpoint za pravi contract.
- Server se otvara u browseru preko `.svc` adrese.
- Klijent ima `Service Reference`, ne samo obican reference.
- Posle promene servera uradjen je `Update Service Reference`.
- Proxy se pravi u klijentu i metode se pozivaju preko njega.

## Korisni zvanicni linkovi

- Microsoft Learn: WCF servis u Visual Studio-u: https://learn.microsoft.com/en-us/visualstudio/data-tools/walkthrough-creating-a-simple-wcf-service-in-windows-forms
- Microsoft Learn: WCF Web Service Reference Provider: https://learn.microsoft.com/en-us/dotnet/core/additional-tools/wcf-web-service-reference-guide

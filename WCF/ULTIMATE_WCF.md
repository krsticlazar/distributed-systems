# Ultimativni WCF za ispit

Ovaj dokument je napravljen prema:

- `Materijal/beleske 1.txt`;
- `Materijal/beleske 2.txt`;
- `Materijal/4 WCF.md`;
- postojecim WCF uputstvima i primerima;
- svim Markdown blanketima iz foldera `Blanketi`.

Tekstovi zadataka su prepisani uz normalizovana slova, jer su pojedini Markdown blanketi nastali iz PDF-a sa losim encoding-om.

Najbitniji lokalni primeri za obrasce u resenjima:

- `Primeri/WcfServiceSokovi` - obican servis i generisani `SokoviClient` proxy;
- `Primeri/01_Vreme` - obican Service Reference klijent;
- `Primeri/T2CB` - full-duplex kalkulator, callback i `InstanceContext`;
- `Primeri/Chat` - cuvanje callback kanala vise korisnika;
- `Materijal/WCFCalculatorDuplex_uputstvo.md` - sredjen kalkulatorski callback obrazac;
- `Materijal/WCFChat_uputstvo.md` - sredjen callback/broadcast obrazac.

## Kako se WCF radi u ispitnoj svesci

Prema beleskama, na ispitu se ne pise ceo Visual Studio projekat.

Najcesce treba napisati:

1. slozene tipove kao `[DataContract]`;
2. njihove podatke kao `[DataMember]`;
3. servisni interfejs sa `[ServiceContract]`;
4. trazene metode sa `[OperationContract]`;
5. implementaciju servisa i stanje koje servis pamti;
6. minimalni `<service>` i `<endpoint>` deo konfiguracije ako je trazen;
7. minimalni klijentski poziv preko generisanog Service Reference proxy-ja;
8. callback ugovor, `InstanceContext` i `wsDualHttpBinding` samo kada zadatak trazi callback/full-duplex.

Obicno se ne pisu:

- `using` direktive;
- `.csproj`, solution i Visual Studio koraci;
- ceo `Web.config`;
- generisani `Reference.cs`;
- kompletan `.svc` fajl;
- `try/catch`, osim kada zadatak izricito trazi fault/gresku;
- validacije i pomocne klase koje nisu bitne za zahtev zadatka.

Klijent se na papiru pise u stilu primera sa vezbi:

```csharp
ServisClient proxy = new ServisClient();
proxy.Metoda();
```

Za callback:

```csharp
ServisClient proxy =
    new ServisClient(new InstanceContext(new CallbackHandler()));
```

Ne treba rucno pisati generisani proxy.

## Dva klijentska proxy obrasca

Profesorovi primeri koriste proxy koji Visual Studio generise kada se doda `Service Reference`.

### 1. Obican proxy

Koristi se kada samo klijent poziva server:

```csharp
SkladisteServiceClient proxy =
    new SkladisteServiceClient();

var rezultat = proxy.Metoda();
proxy.Close();
```

Ovo se koristi za:

- obican request/response;
- `basicHttpBinding`;
- sesijski servis bez callback-a, uz odgovarajuci `wsHttpBinding`.

### 2. Duplex proxy

Koristi se kada server mora da poziva klijenta kroz callback:

```csharp
CallbackHandler callback = new CallbackHandler();
InstanceContext context = new InstanceContext(callback);

CalculatorDuplexClient proxy =
    new CalculatorDuplexClient(context);

var rezultat = proxy.Metoda();
proxy.Close();
```

`CallbackHandler` implementira callback interfejs:

```csharp
public class CallbackHandler : ICalculatorCallback
{
    public void IzrazPromenjen(string izraz)
    {
        Console.WriteLine(izraz);
    }
}
```

Najkraca razlika:

```text
Obican servis:  new ServisClient()
Duplex servis:  new ServisClient(new InstanceContext(new CallbackHandler()))
```

Ovo nisu rucno napisani proxy-ji. Obe `...Client` klase generise `Service Reference`.

### Sinhroni i asinhroni poziv nisu dva razlicita proxy-ja

Isti generisani proxy moze ponuditi:

```csharp
proxy.Metoda();
proxy.MetodaAsync();
```

Prvi poziv ceka rezultat, dok asinhroni poziv ne blokira klijentsku nit dok se operacija izvrsava. Ako tekst zadatka ne trazi asinhroni poziv, na ispitu je dovoljan obican sinhroni poziv.

`IsOneWay = true` je treca, odvojena stvar: operacija tada nema povratnu vrednost i pozivalac ne ceka poslovni odgovor.

## ABC endpoint-a

Svaki endpoint ima tri kljucna dela:

- `address` - gde je servis dostupan;
- `binding` - kako se komunicira;
- `contract` - koji servisni interfejs endpoint iznosi.

Najbitniji izbor binding-a:

| Situacija | Binding |
|---|---|
| Obican request/response bez sesije | `basicHttpBinding` |
| Stanje posebno za svakog klijenta, bez callback-a | `wsHttpBinding` |
| Callback/full-duplex | `wsDualHttpBinding` |

## Pregled svih Markdown blanketa

| Blanket | WCF zadatak |
|---|---|
| `DS April 2026 NP.md` | Zadatak 17: full-duplex kalkulator |
| `DS Jun 2024 NP.md` | Zadatak 4: zakup skladista |
| `DS Kol I 2024.md` | Nema WCF zadatka |
| `DS Kol I 2026.md` | Nema WCF zadatka |
| `JUN 26` - fotografija blanketa | Zadatak 19: sesijski kalkulator sa tipiziranim fault-om |

Kolokvijumski blanketi iz 2024. i 2026. sadrze gRPC, RMI i MPI zadatke, ali ne i WCF. Zbog toga za njih nije izmisljeno WCF resenje.

# Stvarni zadatak 1: zakup skladista

## Originalni tekst zadatka

> Koristeci WCF kreirati sistem za zakup skladista. Potrebno je da servis podrzava sledece funkcionalnosti:
>
> - Zakup skladista (proslediti Vlasnika: ime, prezime i JMBG, i skladiste: IdSkladista, pocetak zakupa, kraj zakupa, cena).
> - Vraca listu svih aktivnih skladista zadatog vlasnika.
> - Vraca listu svih vlasnika aktivnih skladista.
> - Vraca listu svih skladista i istoriju njihovih vlasnika (zakupa).
>
> Obavezno izdvojiti interfejs, implementaciju, `web.config` (dovoljan je samo deo za setovanje servisa) i klijentsku stranu koja demonstrira rad servisa.

## Analiza zadatka

### Trazene operacije

Servis mora da ima cetiri operacije:

1. evidentiranje novog zakupa;
2. vracanje aktivnih skladista jednog vlasnika;
3. vracanje vlasnika svih trenutno aktivnih zakupa;
4. vracanje kompletne istorije zakupa.

### Trazene klase

Potrebna su tri slozena tipa:

- `Vlasnik` - ime, prezime i JMBG;
- `Skladiste` - ID, pocetak, kraj i cena zakupa;
- `Zakup` - spaja jednog vlasnika i jedno skladiste.

`Zakup` je koristan jer istorija mora da pamti ko je koristio skladiste, u kom periodu i po kojoj ceni.

### Nacin komunikacije

Ovo je obican request/response servis:

```text
klijent -> servis -> rezultat
```

Nema callback-a, nema stanja posebnog za jednu klijentsku sesiju i nema razloga za `wsDualHttpBinding`.

Svi klijenti treba da vide istu evidenciju zakupa. Zato je stanje zajednicko. Najjednostavnije ispitno resenje je:

```csharp
[ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
```

i jedna lista zakupa u servisu.

Za endpoint je dovoljan:

```text
basicHttpBinding
```

## Kljucni WCF kod za svesku

### Ugovori podataka i servisni ugovor

```csharp
namespace Jun2024
{
    [DataContract]
    public class Vlasnik
    {
        [DataMember]
        public string Ime { get; set; }

        [DataMember]
        public string Prezime { get; set; }

        [DataMember]
        public string Jmbg { get; set; }
    }

    [DataContract]
    public class Skladiste
    {
        [DataMember]
        public int IdSkladista { get; set; }

        [DataMember]
        public DateTime PocetakZakupa { get; set; }

        [DataMember]
        public DateTime KrajZakupa { get; set; }

        [DataMember]
        public decimal Cena { get; set; }
    }

    [DataContract]
    public class Zakup
    {
        [DataMember]
        public Vlasnik Vlasnik { get; set; }

        [DataMember]
        public Skladiste Skladiste { get; set; }
    }

    [ServiceContract]
    public interface ISkladisteService
    {
        [OperationContract]
        bool ZakupiSkladiste(Vlasnik vlasnik, Skladiste skladiste);

        [OperationContract]
        List<Skladiste> AktivnaSkladista(string jmbg);

        [OperationContract]
        List<Vlasnik> VlasniciAktivnihSkladista();

        [OperationContract]
        List<Zakup> SvaSkladistaIIstorijaZakupa();
    }
}
```

### Implementacija servisa

```csharp
namespace Jun2024
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class SkladisteService : ISkladisteService
    {
        private readonly List<Zakup> zakupi = new List<Zakup>();

        public bool ZakupiSkladiste(Vlasnik vlasnik, Skladiste skladiste)
        {
            if (vlasnik == null || skladiste == null)
                return false;

            if (skladiste.PocetakZakupa >= skladiste.KrajZakupa)
                return false;

            bool postojiPreklapanje = zakupi.Any(z =>
                z.Skladiste.IdSkladista == skladiste.IdSkladista &&
                skladiste.PocetakZakupa < z.Skladiste.KrajZakupa &&
                z.Skladiste.PocetakZakupa < skladiste.KrajZakupa);

            if (postojiPreklapanje)
                return false;

            zakupi.Add(new Zakup
            {
                Vlasnik = vlasnik,
                Skladiste = skladiste
            });

            return true;
        }

        public List<Skladiste> AktivnaSkladista(string jmbg)
        {
            DateTime sada = DateTime.Now;

            return zakupi
                .Where(z => z.Vlasnik.Jmbg == jmbg &&
                            z.Skladiste.PocetakZakupa <= sada &&
                            z.Skladiste.KrajZakupa >= sada)
                .Select(z => z.Skladiste)
                .ToList();
        }

        public List<Vlasnik> VlasniciAktivnihSkladista()
        {
            DateTime sada = DateTime.Now;

            return zakupi
                .Where(z => z.Skladiste.PocetakZakupa <= sada &&
                            z.Skladiste.KrajZakupa >= sada)
                .Select(z => z.Vlasnik)
                .GroupBy(v => v.Jmbg)
                .Select(g => g.First())
                .ToList();
        }

        public List<Zakup> SvaSkladistaIIstorijaZakupa()
        {
            return zakupi.ToList();
        }
    }
}
```

## Minimalni serverski `Web.config`

Zadatak izricito trazi samo deo za setovanje servisa:

```xml
<system.serviceModel>
  <services>
    <service name="Jun2024.SkladisteService">
      <endpoint address=""
                binding="basicHttpBinding"
                contract="Jun2024.ISkladisteService" />
    </service>
  </services>
</system.serviceModel>
```

`service name` pokazuje na implementacionu klasu.

`contract` pokazuje na interfejs, ne na implementaciju.

`address=""` znaci da je endpoint na osnovnoj adresi `.svc` servisa.

## Minimalna klijentska demonstracija

Pretpostavlja se da je dodat Service Reference i da je Visual Studio generisao `SkladisteServiceClient`.

```csharp
SkladisteServiceClient proxy = new SkladisteServiceClient();

Vlasnik vlasnik = new Vlasnik
{
    Ime = "Ana",
    Prezime = "Petrovic",
    Jmbg = "0101000735001"
};

Skladiste skladiste = new Skladiste
{
    IdSkladista = 1,
    PocetakZakupa = DateTime.Today,
    KrajZakupa = DateTime.Today.AddMonths(1),
    Cena = 50000
};

Console.WriteLine(proxy.ZakupiSkladiste(vlasnik, skladiste));

foreach (Skladiste s in proxy.AktivnaSkladista(vlasnik.Jmbg))
    Console.WriteLine(s.IdSkladista);

foreach (Vlasnik v in proxy.VlasniciAktivnihSkladista())
    Console.WriteLine(v.Ime + " " + v.Prezime);

foreach (Zakup z in proxy.SvaSkladistaIIstorijaZakupa())
    Console.WriteLine(z.Skladiste.IdSkladista + " - " + z.Vlasnik.Jmbg);

proxy.Close();
```

Klijent poziva sve cetiri metode, sto tekst zadatka trazi.

## Detaljno objasnjenje

### Zasto `[DataContract]`?

`Vlasnik`, `Skladiste` i `Zakup` nisu prosti tipovi. WCF mora da zna kako da ih serijalizuje i posalje kroz SOAP/XML poruku.

### Zasto `[DataMember]`?

Samo oznaceni podaci ulaze u ugovor podataka. Bez `[DataMember]` neki property moze postojati u C# klasi, ali ne mora biti prenet klijentu.

### Zasto poseban `Zakup`?

`Skladiste` opisuje ID i period/cenu, a `Vlasnik` opisuje osobu. Istorija je veza izmedju njih. Vise elemenata sa istim `IdSkladista`, ali razlicitim periodima i vlasnicima, predstavlja istoriju tog skladista.

### Zasto `Single`?

Evidencija zakupa je zajednicka svim klijentima. Jedna servisna instanca cuva jednu zajednicku listu.

Drugo prihvatljivo jednostavno resenje bilo bi da lista bude `static`, ali nije potrebno koristiti oba pristupa.

### Zasto nema `SessionMode.Required`?

Zadatak ne trazi posebnu prijavu ili korpu za svakog klijenta. Klijent samo prosledjuje JMBG kada trazi podatke.

### Zasto nema callback-a?

Server ne treba sam da obavestava klijenta. Sve operacije pokrece klijent.

### Zasto `basicHttpBinding`?

Nema callback-a i nema WCF sesije. Najjednostavniji request/response binding je dovoljan.

### Zasto se proverava preklapanje?

Jedno skladiste ne treba da bude izdato dvema osobama u preklapajucem periodu. Ovo je kratka poslovna validacija. Ako nema vremena, bitniji su ugovori i trazene metode.

## Sta obavezno napisati i naglasiti

- Tri data contract klase: `Vlasnik`, `Skladiste`, `Zakup`.
- Tacno cetiri trazene operacije.
- Jednu zajednicku listu zakupa.
- Proveru aktivnog zakupa preko pocetka, kraja i `DateTime.Now`.
- Istoriju kao sve sacuvane zakupne zapise.
- `InstanceContextMode.Single` ili jasno naglasenu `static` zajednicku listu.
- `basicHttpBinding`.
- `service name` je implementacija, `contract` je interfejs.
- Klijent mora demonstrirati sve metode.

# Stvarni zadatak 2: full-duplex kalkulator

## Originalni tekst zadatka

> Koristeci WCF kreirati full-duplex sistem kalkulatora. Korisnik u svojoj sesiji moze da obrise trenutno racunanje, doda broj, oduzme broj, pomnozi brojem i podeli rezultat prosledjenim brojem. Svaka operacija se odmah izvrsava nad rezultatom (prethodni rezultat) i smesta u rezultat. Svaka operacija vraca rezultat izvrsene operacije. Servis po izvrsenju operacije poziva klijenta i prosledjuje mu do tog momenta kreirani izraz (na primer: izraz `2+3-5*7`).
>
> Obavezno izdvojiti interfejs, implementaciju, `web.config` (dovoljan je samo deo za setovanje servisa i na klijentskoj strani deo za callback) i klijentsku stranu koja demonstrira rad servisa. Klijentska strana mora pozvati sve metode servisa i prikazati njihov rezultat ako postoji.

## Analiza zadatka

### Trazene servisne operacije

Potrebno je pet operacija:

1. `Clear` - brise trenutno racunanje;
2. `Add` - dodaje broj na trenutni rezultat;
3. `Subtract` - oduzima broj;
4. `Multiply` - mnozi trenutni rezultat;
5. `Divide` - deli trenutni rezultat.

Tekst izricito kaze da svaka operacija vraca rezultat. Zato servisne metode nisu `IsOneWay = true`.

### Trazena callback operacija

Server posle operacije poziva klijenta i salje izraz. Zato postoji poseban callback ugovor:

```csharp
public interface ICalculatorCallback
{
    [OperationContract(IsOneWay = true)]
    void IzrazPromenjen(string izraz);
}
```

Callback ne vraca vrednost, pa moze biti `IsOneWay = true`.

### Stanje

Svaki korisnik ima:

- svoj trenutni rezultat;
- svoj izraz.

To nije zajednicko stanje. Zato ide:

```csharp
SessionMode.Required
InstanceContextMode.PerSession
```

Polja `rezultat` i `izraz` nisu `static`.

### Nacin komunikacije

Komunikacija ide u oba smera:

```text
klijent -> servisna operacija
servis -> callback sa izrazom
```

Zato je potreban:

```text
wsDualHttpBinding
```

## Kljucni WCF kod za svesku

### Servisni i callback ugovor

```csharp
namespace April2026
{
    [ServiceContract(
        SessionMode = SessionMode.Required,
        CallbackContract = typeof(ICalculatorCallback))]
    public interface ICalculatorDuplex
    {
        [OperationContract]
        double Clear();

        [OperationContract]
        double Add(double broj);

        [OperationContract]
        double Subtract(double broj);

        [OperationContract]
        double Multiply(double broj);

        [OperationContract]
        double Divide(double broj);
    }

    public interface ICalculatorCallback
    {
        [OperationContract(IsOneWay = true)]
        void IzrazPromenjen(string izraz);
    }
}
```

Ovde nema `[DataContract]`, jer se salju samo `double` i `string`, koji su vec poznati WCF-u.

### Implementacija servisa

```csharp
namespace April2026
{
    [ServiceBehavior(
        InstanceContextMode = InstanceContextMode.PerSession,
        ConcurrencyMode = ConcurrencyMode.Reentrant)]
    public class CalculatorService : ICalculatorDuplex
    {
        private double rezultat = 0;
        private string izraz = "0";

        private ICalculatorCallback Callback
        {
            get
            {
                return OperationContext.Current
                    .GetCallbackChannel<ICalculatorCallback>();
            }
        }

        private void PosaljiIzraz()
        {
            Callback.IzrazPromenjen(izraz);
        }

        public double Clear()
        {
            rezultat = 0;
            izraz = "0";
            PosaljiIzraz();
            return rezultat;
        }

        public double Add(double broj)
        {
            rezultat += broj;
            izraz += "+" + broj;
            PosaljiIzraz();
            return rezultat;
        }

        public double Subtract(double broj)
        {
            rezultat -= broj;
            izraz += "-" + broj;
            PosaljiIzraz();
            return rezultat;
        }

        public double Multiply(double broj)
        {
            rezultat *= broj;
            izraz += "*" + broj;
            PosaljiIzraz();
            return rezultat;
        }

        public double Divide(double broj)
        {
            if (broj == 0)
            {
                PosaljiIzraz();
                return rezultat;
            }

            rezultat /= broj;
            izraz += "/" + broj;
            PosaljiIzraz();
            return rezultat;
        }
    }
}
```

Provera deljenja nulom je minimalna. Zadatak ne trazi `FaultContract`, pa ne treba uvoditi dodatnu fault klasu.

## Minimalni serverski `Web.config`

```xml
<system.serviceModel>
  <bindings>
    <wsDualHttpBinding>
      <binding name="DuplexCalculatorBinding">
        <security mode="None" />
      </binding>
    </wsDualHttpBinding>
  </bindings>

  <services>
    <service name="April2026.CalculatorService">
      <endpoint address=""
                binding="wsDualHttpBinding"
                bindingConfiguration="DuplexCalculatorBinding"
                contract="April2026.ICalculatorDuplex" />
    </service>
  </services>
</system.serviceModel>
```

Endpoint contract je servisni interfejs `ICalculatorDuplex`, ne callback interfejs.

## Klijentski callback deo konfiguracije

Pretpostavljeni namespace Service Reference-a je `CalculatorReference`.

```xml
<system.serviceModel>
  <bindings>
    <wsDualHttpBinding>
      <binding name="DuplexCalculatorBinding"
               clientBaseAddress="http://localhost:8010/CalculatorClient/">
        <security mode="None" />
      </binding>
    </wsDualHttpBinding>
  </bindings>

  <client>
    <endpoint address="http://localhost:PORT/CalculatorService.svc"
              binding="wsDualHttpBinding"
              bindingConfiguration="DuplexCalculatorBinding"
              contract="CalculatorReference.ICalculatorDuplex"
              name="DuplexCalculatorBinding" />
  </client>
</system.serviceModel>
```

`clientBaseAddress` je adresa na kojoj klijent prima callback. U svesci je dovoljno naglasiti da svaki istovremeno pokrenuti klijent mora imati razlicitu callback adresu/port.

## Minimalna klijentska demonstracija

```csharp
public class CallbackHandler : ICalculatorCallback
{
    public void IzrazPromenjen(string izraz)
    {
        Console.WriteLine("Izraz: " + izraz);
    }
}

CalculatorDuplexClient proxy =
    new CalculatorDuplexClient(
        new InstanceContext(new CallbackHandler()));

Console.WriteLine("Clear = " + proxy.Clear());
Console.WriteLine("Add = " + proxy.Add(2));
Console.WriteLine("Add = " + proxy.Add(3));
Console.WriteLine("Subtract = " + proxy.Subtract(5));
Console.WriteLine("Multiply = " + proxy.Multiply(7));
Console.WriteLine("Divide = " + proxy.Divide(2));

proxy.Close();
```

Ovaj kod:

- implementira callback;
- prosledjuje callback objekat kroz `InstanceContext`;
- poziva svih pet metoda;
- ispisuje svaki povratni rezultat;
- prima izraz nezavisnim pozivom sa servera.

## Detaljno objasnjenje

### Zasto `CallbackContract`?

Servisni ugovor opisuje sta klijent poziva na serveru. `CallbackContract` opisuje sta server sme da pozove na klijentu.

### Zasto `SessionMode.Required`?

Callback zahteva vezu sa konkretnim klijentom, a kalkulator mora da pamti rezultat konkretnog korisnika.

### Zasto `PerSession`?

Svaki klijent dobija posebnu servisnu instancu. Zbog toga svaki korisnik ima svoja polja `rezultat` i `izraz`.

Ako bi ta polja bila `static`, svi klijenti bi delili isti kalkulator, sto je suprotno tekstu zadatka.

### Zasto `Reentrant`?

Servis tokom obrade poziva klijenta nazad. `Reentrant` je odgovarajuci concurrency rezim za jednostavan callback obrazac obradjen na vezbama.

### Zasto servisne metode nisu `IsOneWay`?

Tekst zadatka trazi da svaka operacija vrati rezultat. One-way operacija mora biti `void` i ne vraca rezultat.

### Zasto callback jeste `IsOneWay`?

Klijent samo prima tekst izraza. Server ne ocekuje povratnu vrednost od callback metode.

### Zasto nema liste callbackova?

Kalkulator vraca izraz istom klijentu koji je pozvao operaciju. Dovoljan je callback kanal trenutne sesije.

Lista ili dictionary callbackova bi bili potrebni za chat ili broadcast obavestenja drugim klijentima.

### Zasto `wsDualHttpBinding`?

`basicHttpBinding` ne podrzava ovaj full-duplex obrazac. `wsDualHttpBinding` omogucava klijent-server i server-klijent komunikaciju.

### Kako se dobija callback kanal?

```csharp
OperationContext.Current
    .GetCallbackChannel<ICalculatorCallback>();
```

`OperationContext.Current` predstavlja trenutni WCF poziv. Iz njega server dobija kanal ka klijentu koji je pozvao operaciju.

## Sta obavezno napisati i naglasiti

- `CallbackContract = typeof(ICalculatorCallback)`.
- `SessionMode.Required`.
- Pet servisnih operacija, sve vracaju `double`.
- Callback operacija prima `string izraz`.
- Samo callback ima `IsOneWay = true`.
- `PerSession` i nestaticka polja `rezultat` i `izraz`.
- `GetCallbackChannel<ICalculatorCallback>()`.
- Callback posle svake uspesne operacije.
- `wsDualHttpBinding` na serveru i klijentu.
- Klijent pravi `new CalculatorDuplexClient(new InstanceContext(...))`.
- Klijent poziva svih pet metoda i stampa njihove rezultate.

# Stvarni zadatak 3: JUN 26 - sesijski kalkulator sa fault-om

## Originalni tekst zadatka

> Koristeci WCF kreirajte sistem kalkulatora. Korisnik u svojoj sesiji moze da obrise trenutno racunanje, doda broj, oduzme broj, pomnozi brojem i podeli rezultat prosledjenim brojem. Svaka operacija se odmah izvrsava nad rezultatom (prethodni rezultat) i smesta u rezultat. Operacija vraca rezultat izvrsenja i do tog momenta kreirani izraz (na primer: izraz `2+3-5*7`). Predvideti prosledjivanje izuzetka klijentu. Obavezno izdvojiti interfejs, implementaciju, `web.config` (dovoljan je samo deo za setovanje) i klijentsku stranu koja demonstrira rad servisa. Klijentska strana mora pozvati sve metode servisa i prikazati njihov rezultat ako postoji.

## Analiza zadatka

### Operacije

Potrebno je pet servisnih operacija:

1. brisanje trenutnog racunanja;
2. dodavanje broja;
3. oduzimanje broja;
4. mnozenje brojem;
5. deljenje prosledjenim brojem.

Svaka operacija mora vratiti dva podatka:

- trenutni brojcani rezultat;
- do tada formirani izraz.

Zato se pravi jedan slozeni povratni tip `StanjeKalkulatora`.

### Stanje i sesija

Tekst izricito kaze da korisnik racuna "u svojoj sesiji". Svaki klijent mora imati svoj rezultat i izraz:

```csharp
SessionMode = SessionMode.Required
InstanceContextMode = InstanceContextMode.PerSession
```

Polja `rezultat` i `izraz` nisu `static`.

Za sesiju bez callback-a koristi se:

```text
wsHttpBinding
```

### Prosledjivanje izuzetka

Najprirodnija greska kalkulatora je deljenje nulom. Za kontrolisano prosledjivanje greske klijentu koriste se:

- `[DataContract]` klasa `Greska`;
- `[FaultContract(typeof(Greska))]` na operaciji `Podeli`;
- `FaultException<Greska>` u implementaciji;
- `catch (FaultException<Greska>)` na klijentu.

### Nema callback-a

Ovaj tekst ne trazi da server naknadno poziva klijenta. Rezultat i izraz se vracaju kao povratna vrednost iste operacije.

Zato ne treba pisati:

- `CallbackContract`;
- callback interfejs;
- `InstanceContext` u klijentu;
- `wsDualHttpBinding`.

## Kljucni WCF kod za svesku

### Ugovori podataka i servisni ugovor

```csharp
namespace Jun2026
{
    [DataContract]
    public class StanjeKalkulatora
    {
        [DataMember]
        public double Rezultat { get; set; }

        [DataMember]
        public string Izraz { get; set; }
    }

    [DataContract]
    public class Greska
    {
        [DataMember]
        public string Poruka { get; set; }
    }

    [ServiceContract(SessionMode = SessionMode.Required)]
    public interface IKalkulator
    {
        [OperationContract]
        StanjeKalkulatora Obrisi();

        [OperationContract]
        StanjeKalkulatora Dodaj(double broj);

        [OperationContract]
        StanjeKalkulatora Oduzmi(double broj);

        [OperationContract]
        StanjeKalkulatora Pomnozi(double broj);

        [OperationContract]
        [FaultContract(typeof(Greska))]
        StanjeKalkulatora Podeli(double broj);
    }
}
```

### Implementacija servisa

```csharp
namespace Jun2026
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerSession)]
    public class KalkulatorService : IKalkulator
    {
        private double rezultat = 0;
        private string izraz = "0";

        private StanjeKalkulatora VratiStanje()
        {
            return new StanjeKalkulatora
            {
                Rezultat = rezultat,
                Izraz = izraz
            };
        }

        public StanjeKalkulatora Obrisi()
        {
            rezultat = 0;
            izraz = "0";
            return VratiStanje();
        }

        public StanjeKalkulatora Dodaj(double broj)
        {
            izraz = rezultat == 0 && izraz == "0"
                ? broj.ToString()
                : izraz + "+" + broj;

            rezultat += broj;
            return VratiStanje();
        }

        public StanjeKalkulatora Oduzmi(double broj)
        {
            rezultat -= broj;
            izraz += "-" + broj;
            return VratiStanje();
        }

        public StanjeKalkulatora Pomnozi(double broj)
        {
            rezultat *= broj;
            izraz += "*" + broj;
            return VratiStanje();
        }

        public StanjeKalkulatora Podeli(double broj)
        {
            if (broj == 0)
            {
                throw new FaultException<Greska>(
                    new Greska
                    {
                        Poruka = "Deljenje nulom nije dozvoljeno."
                    },
                    "Greska kalkulatora");
            }

            rezultat /= broj;
            izraz += "/" + broj;
            return VratiStanje();
        }
    }
}
```

## Minimalni serverski `Web.config`

```xml
<system.serviceModel>
  <services>
    <service name="Jun2026.KalkulatorService">
      <endpoint address=""
                binding="wsHttpBinding"
                contract="Jun2026.IKalkulator" />
    </service>
  </services>
</system.serviceModel>
```

`wsHttpBinding` je potreban zbog WCF sesije. `basicHttpBinding` nije odgovarajuci za `SessionMode.Required`.

## Minimalna klijentska demonstracija

Pretpostavlja se da je dodat Service Reference i generisan `KalkulatorServiceClient`.

```csharp
public class Program
{
    private static void Ispisi(StanjeKalkulatora stanje)
    {
        Console.WriteLine(
            "Rezultat: " + stanje.Rezultat +
            ", izraz: " + stanje.Izraz);
    }

    public static void Main()
    {
        KalkulatorServiceClient proxy =
            new KalkulatorServiceClient();

        Ispisi(proxy.Obrisi());
        Ispisi(proxy.Dodaj(2));
        Ispisi(proxy.Dodaj(3));
        Ispisi(proxy.Oduzmi(5));
        Ispisi(proxy.Pomnozi(7));
        Ispisi(proxy.Podeli(2));

        try
        {
            Ispisi(proxy.Podeli(0));
        }
        catch (FaultException<Greska> ex)
        {
            Console.WriteLine(ex.Detail.Poruka);
        }

        proxy.Close();
    }
}
```

Klijent je obican proxy:

```csharp
new KalkulatorServiceClient()
```

Nema `InstanceContext`, jer nema callback-a.

## Detaljno objasnjenje

### Zasto `StanjeKalkulatora`?

Jedna metoda treba da vrati i broj i izraz. WCF metoda ima jednu povratnu vrednost, pa se oba podatka pakuju u jedan data contract.

### Zasto `Greska`?

Zadatak izricito trazi prosledjivanje izuzetka klijentu. Tipizirani fault je kontrolisan deo WCF ugovora, za razliku od nekontrolisanog serverskog exception-a.

### Zasto je `FaultContract` samo na `Podeli`?

U ovom jednostavnom kalkulatoru deljenje nulom je operacija koja prirodno proizvodi trazenu gresku. Ako bi zadatak naveo jos poslovnih gresaka, `FaultContract` bi se stavio i na odgovarajuce operacije.

### Zasto `PerSession`?

Jedna servisna instanca cuva rezultat i izraz jednog klijenta tokom njegove sesije. Drugi klijent dobija drugu instancu i svoje stanje.

### Zasto `wsHttpBinding`?

Potreban je session-aware binding, ali komunikacija ide samo klijent-server-klijent kroz povratnu vrednost. Nema server-to-client callback poziva.

### Zasto nije duplex?

Servis ne poziva klijentsku callback metodu. Izraz stize zajedno sa rezultatom kao `StanjeKalkulatora`.

## Sta obavezno napisati i naglasiti

- `StanjeKalkulatora` sa `Rezultat` i `Izraz`.
- `Greska` sa `Poruka`.
- Pet trazenih operacija sa uskladjenim povratnim tipom.
- `[FaultContract(typeof(Greska))]` na `Podeli`.
- `FaultException<Greska>` pri deljenju nulom.
- `SessionMode.Required`.
- `InstanceContextMode.PerSession`.
- Nestaticka polja `rezultat` i `izraz`.
- `wsHttpBinding`, ne `basicHttpBinding` i ne `wsDualHttpBinding`.
- Obican generisani proxy `new KalkulatorServiceClient()`.
- Klijent poziva sve metode, stampa rezultat i izraz i hvata fault.

# Novi ispitni zadatak 1: korpa po sesiji

## Tekst zadatka

> Koristeci WCF kreirati sistem elektronske prodavnice u kojem svaki klijent u svojoj sesiji ima posebnu korpu. Proizvod ima ID, naziv, cenu i kolicinu. Servis treba da omoguci dodavanje proizvoda, brisanje proizvoda po ID-u, vracanje sadrzaja korpe, racunanje ukupne cene i praznjenje korpe. Izdvojiti interfejs, implementaciju, potreban endpoint i minimalni klijentski poziv.

## Ispitno resenje

```csharp
namespace NoviZadatak1
{
    [DataContract]
    public class Proizvod
    {
        [DataMember]
        public int Id { get; set; }

        [DataMember]
        public string Naziv { get; set; }

        [DataMember]
        public decimal Cena { get; set; }

        [DataMember]
        public int Kolicina { get; set; }
    }

    [ServiceContract(SessionMode = SessionMode.Required)]
    public interface IKorpaService
    {
        [OperationContract]
        void Dodaj(Proizvod proizvod);

        [OperationContract]
        bool Obrisi(int id);

        [OperationContract]
        List<Proizvod> VratiKorpu();

        [OperationContract]
        decimal UkupnaCena();

        [OperationContract]
        void Isprazni();
    }

    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerSession)]
    public class KorpaService : IKorpaService
    {
        private readonly List<Proizvod> korpa = new List<Proizvod>();

        public void Dodaj(Proizvod proizvod)
        {
            korpa.Add(proizvod);
        }

        public bool Obrisi(int id)
        {
            Proizvod proizvod = korpa.FirstOrDefault(p => p.Id == id);
            if (proizvod == null)
                return false;

            korpa.Remove(proizvod);
            return true;
        }

        public List<Proizvod> VratiKorpu()
        {
            return korpa.ToList();
        }

        public decimal UkupnaCena()
        {
            return korpa.Sum(p => p.Cena * p.Kolicina);
        }

        public void Isprazni()
        {
            korpa.Clear();
        }
    }
}
```

```xml
<system.serviceModel>
  <services>
    <service name="NoviZadatak1.KorpaService">
      <endpoint address=""
                binding="wsHttpBinding"
                contract="NoviZadatak1.IKorpaService" />
    </service>
  </services>
</system.serviceModel>
```

```csharp
KorpaServiceClient proxy = new KorpaServiceClient();

proxy.Dodaj(new Proizvod
{
    Id = 1,
    Naziv = "Tastatura",
    Cena = 5000,
    Kolicina = 2
});

Console.WriteLine(proxy.UkupnaCena());
proxy.Obrisi(1);
proxy.Isprazni();
proxy.Close();
```

## Objasnjenje

Korpa pripada jednoj sesiji, pa ugovor zahteva sesiju, a servis je `PerSession`.

Lista nije `static`, jer razliciti klijenti ne smeju deliti istu korpu.

Koristi se `wsHttpBinding`, jer je potrebna sesija, ali nema callback-a.

`basicHttpBinding` nije odgovarajuci za ovaj WCF session obrazac, a `wsDualHttpBinding` je nepotreban jer server ne poziva klijenta.

# Novi ispitni zadatak 2: rezervacija ucionica sa fault-om

## Tekst zadatka

> Koristeci WCF kreirati sistem za rezervaciju ucionica. Rezervacija sadrzi ID, oznaku ucionice, pocetak, kraj i ime nastavnika. Servis omogucava rezervisanje termina, otkazivanje rezervacije i vracanje svih aktivnih rezervacija. Ako se novi termin preklapa sa postojecim terminom iste ucionice, servis mora klijentu vratiti tipiziranu WCF gresku koja sadrzi opis problema. Izdvojiti interfejs, implementaciju, endpoint i minimalnu klijentsku demonstraciju.

## Ispitno resenje

```csharp
namespace NoviZadatak2
{
    [DataContract]
    public class Rezervacija
    {
        [DataMember]
        public int Id { get; set; }

        [DataMember]
        public string Ucionica { get; set; }

        [DataMember]
        public DateTime Pocetak { get; set; }

        [DataMember]
        public DateTime Kraj { get; set; }

        [DataMember]
        public string Nastavnik { get; set; }
    }

    [DataContract]
    public class Greska
    {
        [DataMember]
        public string Opis { get; set; }
    }

    [ServiceContract]
    public interface IRezervacijaService
    {
        [OperationContract]
        [FaultContract(typeof(Greska))]
        bool Rezervisi(Rezervacija rezervacija);

        [OperationContract]
        bool Otkazi(int id);

        [OperationContract]
        List<Rezervacija> AktivneRezervacije();
    }

    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class RezervacijaService : IRezervacijaService
    {
        private readonly List<Rezervacija> rezervacije =
            new List<Rezervacija>();

        public bool Rezervisi(Rezervacija nova)
        {
            bool preklapanje = rezervacije.Any(r =>
                r.Ucionica == nova.Ucionica &&
                nova.Pocetak < r.Kraj &&
                r.Pocetak < nova.Kraj);

            if (preklapanje)
            {
                throw new FaultException<Greska>(
                    new Greska
                    {
                        Opis = "Ucionica je zauzeta u trazenom terminu."
                    },
                    "Rezervacija nije uspela.");
            }

            rezervacije.Add(nova);
            return true;
        }

        public bool Otkazi(int id)
        {
            Rezervacija r = rezervacije.FirstOrDefault(x => x.Id == id);
            if (r == null)
                return false;

            rezervacije.Remove(r);
            return true;
        }

        public List<Rezervacija> AktivneRezervacije()
        {
            return rezervacije
                .Where(r => r.Kraj >= DateTime.Now)
                .ToList();
        }
    }
}
```

```xml
<service name="NoviZadatak2.RezervacijaService">
  <endpoint address=""
            binding="basicHttpBinding"
            contract="NoviZadatak2.IRezervacijaService" />
</service>
```

```csharp
RezervacijaServiceClient proxy = new RezervacijaServiceClient();

try
{
    proxy.Rezervisi(new Rezervacija
    {
        Id = 1,
        Ucionica = "A1",
        Pocetak = DateTime.Today.AddHours(10),
        Kraj = DateTime.Today.AddHours(12),
        Nastavnik = "Petar Petrovic"
    });
}
catch (FaultException<Greska> ex)
{
    Console.WriteLine(ex.Detail.Opis);
}

foreach (Rezervacija r in proxy.AktivneRezervacije())
    Console.WriteLine(r.Ucionica);

proxy.Otkazi(1);
proxy.Close();
```

## Objasnjenje

`Greska` je data contract zato sto se njen sadrzaj prenosi klijentu.

`[FaultContract(typeof(Greska))]` ide na operaciju koja moze vratiti tu tipiziranu gresku.

`try/catch` je ovde potreban jer ga tekst zadatka posredno zahteva tipiziranom WCF greskom. U obicnim zadacima bez fault zahteva ne treba ga pisati.

Stanje je zajednicko, pa se koristi `Single`. Callback i sesija nisu potrebni. Zato je dovoljan `basicHttpBinding`.

# Novi ispitni zadatak 3: proizvodnja sa obavestenjima

## Tekst zadatka

> Koristeci WCF kreirati full-duplex sistem za evidentiranje proizvodnje. Korisnik se registruje imenom, moze da doda novi proizvod i da vrati listu proizvoda. Proizvod ima ID, naziv i proizvedenu kolicinu. Kada bilo koji korisnik doda proizvod, svi registrovani korisnici moraju odmah dobiti obavestenje sa nazivom novog proizvoda. Izdvojiti servisni i callback interfejs, implementaciju, potreban endpoint i minimalni klijentski kod.

## Ispitno resenje

```csharp
namespace NoviZadatak3
{
    [DataContract]
    public class Proizvod
    {
        [DataMember]
        public int Id { get; set; }

        [DataMember]
        public string Naziv { get; set; }

        [DataMember]
        public int Kolicina { get; set; }
    }

    public interface IProizvodnjaCallback
    {
        [OperationContract(IsOneWay = true)]
        void Obavestenje(string tekst);
    }

    [ServiceContract(
        SessionMode = SessionMode.Required,
        CallbackContract = typeof(IProizvodnjaCallback))]
    public interface IProizvodnjaService
    {
        [OperationContract]
        bool Registruj(string ime);

        [OperationContract]
        bool DodajProizvod(Proizvod proizvod);

        [OperationContract]
        List<Proizvod> VratiProizvode();
    }

    [ServiceBehavior(
        InstanceContextMode = InstanceContextMode.PerSession,
        ConcurrencyMode = ConcurrencyMode.Reentrant)]
    public class ProizvodnjaService : IProizvodnjaService
    {
        private static readonly List<Proizvod> proizvodi =
            new List<Proizvod>();

        private static readonly Dictionary<string, IProizvodnjaCallback>
            korisnici =
            new Dictionary<string, IProizvodnjaCallback>();

        private string korisnickoIme;

        public bool Registruj(string ime)
        {
            if (string.IsNullOrWhiteSpace(ime))
                return false;

            korisnickoIme = ime;
            korisnici[ime] = OperationContext.Current
                .GetCallbackChannel<IProizvodnjaCallback>();

            return true;
        }

        public bool DodajProizvod(Proizvod proizvod)
        {
            if (proizvod == null || korisnickoIme == null)
                return false;

            proizvodi.Add(proizvod);

            foreach (IProizvodnjaCallback callback in korisnici.Values)
                callback.Obavestenje(
                    "Dodat proizvod: " + proizvod.Naziv);

            return true;
        }

        public List<Proizvod> VratiProizvode()
        {
            return proizvodi.ToList();
        }
    }
}
```

```xml
<system.serviceModel>
  <bindings>
    <wsDualHttpBinding>
      <binding name="ProizvodnjaDuplex">
        <security mode="None" />
      </binding>
    </wsDualHttpBinding>
  </bindings>

  <services>
    <service name="NoviZadatak3.ProizvodnjaService">
      <endpoint address=""
                binding="wsDualHttpBinding"
                bindingConfiguration="ProizvodnjaDuplex"
                contract="NoviZadatak3.IProizvodnjaService" />
    </service>
  </services>
</system.serviceModel>
```

```csharp
public class CallbackHandler : IProizvodnjaCallback
{
    public void Obavestenje(string tekst)
    {
        Console.WriteLine(tekst);
    }
}

ProizvodnjaServiceClient proxy =
    new ProizvodnjaServiceClient(
        new InstanceContext(new CallbackHandler()));

proxy.Registruj("Ana");
proxy.DodajProizvod(new Proizvod
{
    Id = 1,
    Naziv = "Sok",
    Kolicina = 100
});

foreach (Proizvod p in proxy.VratiProizvode())
    Console.WriteLine(p.Naziv);

proxy.Close();
```

## Objasnjenje

Jedan callback nije dovoljan zato sto svi registrovani korisnici treba da prime obavestenje.

Dictionary povezuje korisnicko ime sa callback kanalom:

```csharp
korisnici[ime] =
    OperationContext.Current
        .GetCallbackChannel<IProizvodnjaCallback>();
```

`PerSession` cuva ime konkretnog klijenta, dok su proizvodi i dictionary `static` jer ih dele sve sesije.

Samo callback metoda je `IsOneWay = true`, jer ne vraca rezultat. `DodajProizvod` vraca `bool`, pa ne sme biti one-way.

Za produkcioni kod trebalo bi dodati `lock` i uklanjanje mrtvih callback kanala, ali to nije sustina ovog ispitnog zadatka.

## Puskica

```csharp
// Slozeni podatak koji putuje kroz WCF.
[DataContract]
public class Podatak
{
    [DataMember]
    public int Id { get; set; }

    [DataMember]
    public string Naziv { get; set; }
}

// Obican request/response ugovor.
[ServiceContract]
public interface IServis
{
    [OperationContract]
    bool Dodaj(Podatak podatak);

    [OperationContract]
    List<Podatak> VratiSve();
}

// Single + obicna lista predstavlja zajednicko stanje.
[ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
public class Servis : IServis
{
    private readonly List<Podatak> podaci = new List<Podatak>();

    public bool Dodaj(Podatak podatak)
    {
        if (podatak == null)
            return false;

        podaci.Add(podatak);
        return true;
    }

    public List<Podatak> VratiSve()
    {
        return podaci.ToList();
    }
}

// Programatski host pokazuje vezu service-address-binding-contract.
public class HostPrimer
{
    public static void Pokreni()
    {
        Uri address = new Uri("http://localhost:8000/Servis");
        ServiceHost host = new ServiceHost(typeof(Servis), address);
        BasicHttpBinding binding = new BasicHttpBinding();

        host.AddServiceEndpoint(typeof(IServis), binding, "");
        host.Open();
    }
}

// U projektu se obicno koristi generisani Service Reference proxy.
public class ClientPrimer
{
    public static void Pozovi()
    {
        ServisClient proxy = new ServisClient();
        proxy.Dodaj(new Podatak { Id = 1, Naziv = "Primer" });
        Podatak[] rezultat = proxy.VratiSve();
        proxy.Close();
    }
}

// Za stanje po klijentu: SessionMode.Required + PerSession + wsHttpBinding.
[ServiceContract(SessionMode = SessionMode.Required)]
public interface ISesijskiServis
{
    [OperationContract]
    int Uvecaj();
}

[ServiceBehavior(InstanceContextMode = InstanceContextMode.PerSession)]
public class SesijskiServis : ISesijskiServis
{
    private int vrednost;

    public int Uvecaj()
    {
        return ++vrednost;
    }
}

// Callback ugovor: server poziva klijenta.
public interface IDuplexCallback
{
    [OperationContract(IsOneWay = true)]
    void Obavestenje(string tekst);
}

[ServiceContract(
    SessionMode = SessionMode.Required,
    CallbackContract = typeof(IDuplexCallback))]
public interface IDuplexServis
{
    [OperationContract]
    bool Posalji(string tekst);
}

[ServiceBehavior(
    InstanceContextMode = InstanceContextMode.PerSession,
    ConcurrencyMode = ConcurrencyMode.Reentrant)]
public class DuplexServis : IDuplexServis
{
    private IDuplexCallback Callback
    {
        get
        {
            return OperationContext.Current
                .GetCallbackChannel<IDuplexCallback>();
        }
    }

    public bool Posalji(string tekst)
    {
        Callback.Obavestenje(tekst);
        return true;
    }
}

public class CallbackHandler : IDuplexCallback
{
    public void Obavestenje(string tekst)
    {
        Console.WriteLine(tekst);
    }
}

public class DuplexClientPrimer
{
    public static void Pozovi()
    {
        InstanceContext context =
            new InstanceContext(new CallbackHandler());

        DuplexServisClient proxy =
            new DuplexServisClient(context);

        proxy.Posalji("Poruka");
        proxy.Close();
    }
}

// Binding izbor.
public class BindingPrimer
{
    public void KreirajBindinge()
    {
        BasicHttpBinding basic = new BasicHttpBinding();
        WSHttpBinding session = new WSHttpBinding();
        WSDualHttpBinding duplex = new WSDualHttpBinding();
    }
}
```

Za papir je cesci XML endpoint:

```xml
<service name="Namespace.Servis">
  <endpoint address=""
            binding="basicHttpBinding"
            contract="Namespace.IServis" />
</service>
```

Promena samo binding-a:

```xml
<!-- Bez sesije i callback-a -->
binding="basicHttpBinding"

<!-- PerSession, bez callback-a -->
binding="wsHttpBinding"

<!-- Callback/full-duplex -->
binding="wsDualHttpBinding"
```

## Bare Minimum

### DS Jun 2024 NP - zakup skladista

#### Obavezno

1. `Vlasnik`:

```csharp
[DataContract]
public class Vlasnik
{
    [DataMember] public string Ime { get; set; }
    [DataMember] public string Prezime { get; set; }
    [DataMember] public string Jmbg { get; set; }
}
```

2. `Skladiste`:

```csharp
[DataContract]
public class Skladiste
{
    [DataMember] public int IdSkladista { get; set; }
    [DataMember] public DateTime PocetakZakupa { get; set; }
    [DataMember] public DateTime KrajZakupa { get; set; }
    [DataMember] public decimal Cena { get; set; }
}
```

3. Veza za istoriju:

```csharp
[DataContract]
public class Zakup
{
    [DataMember] public Vlasnik Vlasnik { get; set; }
    [DataMember] public Skladiste Skladiste { get; set; }
}
```

4. Tacne trazene operacije:

```csharp
[ServiceContract]
public interface ISkladisteService
{
    [OperationContract]
    bool ZakupiSkladiste(Vlasnik v, Skladiste s);

    [OperationContract]
    List<Skladiste> AktivnaSkladista(string jmbg);

    [OperationContract]
    List<Vlasnik> VlasniciAktivnihSkladista();

    [OperationContract]
    List<Zakup> SvaSkladistaIIstorijaZakupa();
}
```

5. Jedna zajednicka evidencija:

```csharp
[ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
public class SkladisteService : ISkladisteService
{
    private List<Zakup> zakupi = new List<Zakup>();
}
```

6. Aktivno znaci:

```csharp
z.Skladiste.PocetakZakupa <= DateTime.Now &&
z.Skladiste.KrajZakupa >= DateTime.Now
```

7. Minimalni endpoint:

```xml
<service name="Jun2024.SkladisteService">
  <endpoint address=""
            binding="basicHttpBinding"
            contract="Jun2024.ISkladisteService" />
</service>
```

8. Klijent mora pozvati sve cetiri operacije preko:

```csharp
SkladisteServiceClient proxy = new SkladisteServiceClient();
```

#### Samo ako tekst dodatno zahteva

- MEX endpoint.
- Celi `Web.config`.
- `.svc` direktivu.
- `FaultContract`.
- `try/catch`.
- callback.
- sesiju po klijentu.
- detaljnu proveru preklapanja termina.

### DS April 2026 NP - full-duplex kalkulator

#### Obavezno

1. Servisni ugovor mora imati oba parametra:

```csharp
[ServiceContract(
    SessionMode = SessionMode.Required,
    CallbackContract = typeof(ICalculatorCallback))]
```

2. Operacije moraju vratiti rezultat:

```csharp
[OperationContract] double Clear();
[OperationContract] double Add(double broj);
[OperationContract] double Subtract(double broj);
[OperationContract] double Multiply(double broj);
[OperationContract] double Divide(double broj);
```

Ne stavljati `IsOneWay = true` na ove metode, jer vracaju `double`.

3. Callback:

```csharp
public interface ICalculatorCallback
{
    [OperationContract(IsOneWay = true)]
    void IzrazPromenjen(string izraz);
}
```

4. Stanje po sesiji:

```csharp
[ServiceBehavior(
    InstanceContextMode = InstanceContextMode.PerSession,
    ConcurrencyMode = ConcurrencyMode.Reentrant)]
public class CalculatorService : ICalculatorDuplex
{
    private double rezultat;
    private string izraz = "0";
}
```

5. Posle svake operacije:

```csharp
rezultat += broj;
izraz += "+" + broj;

OperationContext.Current
    .GetCallbackChannel<ICalculatorCallback>()
    .IzrazPromenjen(izraz);

return rezultat;
```

6. Server endpoint:

```xml
<service name="April2026.CalculatorService">
  <endpoint address=""
            binding="wsDualHttpBinding"
            contract="April2026.ICalculatorDuplex" />
</service>
```

7. Klijentski callback obrazac:

```csharp
public class CallbackHandler : ICalculatorCallback
{
    public void IzrazPromenjen(string izraz)
    {
        Console.WriteLine(izraz);
    }
}

CalculatorDuplexClient proxy =
    new CalculatorDuplexClient(
        new InstanceContext(new CallbackHandler()));
```

8. Klijent mora pozvati:

```csharp
proxy.Clear();
proxy.Add(2);
proxy.Subtract(1);
proxy.Multiply(5);
proxy.Divide(2);
```

i ispisati svaki vraceni rezultat.

9. U client config-u mora postojati `wsDualHttpBinding` endpoint ka servisnom ugovoru.

#### Samo ako tekst dodatno zahteva

- `DataContract` klase, jer ovde se koriste samo `double` i `string`.
- Dictionary callbackova, jer kalkulator odgovara samo trenutnom klijentu.
- `FaultContract` za deljenje nulom.
- MEX endpoint.
- ceo `Web.config`.
- rucno pisanje generisanog proxy-ja.
- `ChannelFactory` ili `DuplexChannelFactory`; profesorov obrazac je generisani Service Reference klijent.

### JUN 26 - sesijski kalkulator sa fault-om

#### Obavezno

1. Povratni tip koji objedinjuje oba trazena podatka:

```csharp
[DataContract]
public class StanjeKalkulatora
{
    [DataMember]
    public double Rezultat { get; set; }

    [DataMember]
    public string Izraz { get; set; }
}
```

2. Tipizirana greska:

```csharp
[DataContract]
public class Greska
{
    [DataMember]
    public string Poruka { get; set; }
}
```

3. Sesijski servis i tacne operacije:

```csharp
[ServiceContract(SessionMode = SessionMode.Required)]
public interface IKalkulator
{
    [OperationContract]
    StanjeKalkulatora Obrisi();

    [OperationContract]
    StanjeKalkulatora Dodaj(double broj);

    [OperationContract]
    StanjeKalkulatora Oduzmi(double broj);

    [OperationContract]
    StanjeKalkulatora Pomnozi(double broj);

    [OperationContract]
    [FaultContract(typeof(Greska))]
    StanjeKalkulatora Podeli(double broj);
}
```

4. Stanje jednog klijenta:

```csharp
[ServiceBehavior(InstanceContextMode = InstanceContextMode.PerSession)]
public class KalkulatorService : IKalkulator
{
    private double rezultat;
    private string izraz = "0";
}
```

Polja ne smeju biti `static`.

5. Svaka operacija menja oba polja i vraca:

```csharp
return new StanjeKalkulatora
{
    Rezultat = rezultat,
    Izraz = izraz
};
```

6. Deljenje nulom:

```csharp
if (broj == 0)
{
    throw new FaultException<Greska>(
        new Greska
        {
            Poruka = "Deljenje nulom nije dozvoljeno."
        });
}
```

7. Endpoint mora podrzati sesiju:

```xml
<service name="Jun2026.KalkulatorService">
  <endpoint address=""
            binding="wsHttpBinding"
            contract="Jun2026.IKalkulator" />
</service>
```

8. Klijent koristi obican Service Reference proxy:

```csharp
KalkulatorServiceClient proxy =
    new KalkulatorServiceClient();
```

Nema `InstanceContext`, jer ovo nije callback zadatak.

9. Klijent mora:

- pozvati `Obrisi`, `Dodaj`, `Oduzmi`, `Pomnozi` i `Podeli`;
- ispisati `Rezultat` i `Izraz`;
- uhvatiti `FaultException<Greska>` i ispisati `ex.Detail.Poruka`.

#### Ne pisati jer zadatak to ne trazi

- `CallbackContract`.
- Callback interfejs.
- `GetCallbackChannel`.
- `IsOneWay = true`.
- `wsDualHttpBinding`.
- `DuplexChannelFactory`.
- `new InstanceContext(...)`.
- Dictionary callbackova.

Najbitnija razlika:

```text
April 2026: full-duplex, izraz stize callback-om, wsDualHttpBinding.
JUN 26: rezultat i izraz stizu kroz return, fault za gresku, wsHttpBinding.
```

# WCFProizvodnja - detaljno objasnjenje

Ovaj projekat je prvi od ovih pet zadataka gde obican request/response nije dovoljan. U tekstu zadatka pise da svi korisnici treba da dobiju obavestenje kada se doda novi proizvod. To znaci da server mora sam da pozove klijente. To je WCF callback, odnosno duplex komunikacija.

Zadatak trazi:

- korisnik moze da doda proizvod;
- svi korisnici dobijaju obavestenje da je dodat novi proizvod;
- korisnik moze da vidi sve proizvode;
- korisnik moze da poveca proizvedenu kolicinu;
- samo jedan korisnik sme u jednom trenutku da menja jedan proizvod.

Zato projekat kombinuje dve teme:

- callback obavestenja;
- zakljucavanje proizvoda zbog konkurentnog pristupa.

## Struktura solutiona

Solution se zove `WCFProizvodnja`.

Ima dva projekta:

- `WCFProizvodnjaServer` - WCF servis;
- `WCFProizvodnjaClient` - konzolni klijent.

Backend fajlovi:

- `IProizvodnjaService.cs` - servisni ugovor, callback ugovor i data modeli;
- `ProizvodnjaService.svc` - host fajl;
- `ProizvodnjaService.svc.cs` - implementacija servisa;
- `Web.config` - server konfiguracija za `wsDualHttpBinding`.

Client fajlovi:

- `Program.cs` - pravi duplex proxy i implementira callback handler;
- `App.config` - sadrzi WCF endpoint konfiguraciju za duplex binding.

## Najbitnija razlika u odnosu na kviz i placanje

Kviz i placanje rade ovako:

```text
klijent -> server -> odgovor
```

Proizvodnja radi ovako:

```text
klijent -> server
server -> jedan ili vise klijenata
```

Zbog toga ne koristimo `basicHttpBinding`, nego `wsDualHttpBinding`.

## `IProizvodnjaService.cs` - servisni i callback ugovor

Na vrhu je servisni ugovor:

```csharp
[ServiceContract(SessionMode = SessionMode.Required,
                 CallbackContract = typeof(IProizvodnjaCallback))]
public interface IProizvodnjaService
```

Ovo ima tri vazna dela.

### `ServiceContract`

Oznacava da je `IProizvodnjaService` WCF servisni ugovor.

### `SessionMode = SessionMode.Required`

Za callback i duplex scenarije je bitno da postoji sesija izmedju klijenta i servera.

U ovoj aplikaciji servis pamti korisnika u polju:

```csharp
private string korisnik;
```

To ima smisla samo ako ista sesija predstavlja istog klijenta.

### `CallbackContract = typeof(IProizvodnjaCallback)`

Ovo govori WCF-u: klijent mora da implementira callback interfejs `IProizvodnjaCallback`, jer server moze da ga pozove.

Drugim recima:

- `IProizvodnjaService` su metode koje klijent poziva na serveru;
- `IProizvodnjaCallback` su metode koje server poziva na klijentu.

## Operacije u `IProizvodnjaService`

### `RegistrujKorisnika(string ime)`

Prvi poziv koji klijent treba da uradi.

Server tada:

- zapamti ime korisnika u sesiji;
- uzme callback kanal tog klijenta;
- ubaci callback u recnik registrovanih korisnika.

Bez registracije server ne zna kome pripada callback.

### `DodajProizvod(string naziv)`

Dodaje novi proizvod i posle toga obavestava sve registrovane klijente.

Ovo je tipicno mesto za callback.

### `VratiProizvode()`

Obican request/response poziv. Klijent trazi listu, server vraca listu.

### `ZakljucajProizvod(int proizvodId)`

Ovo je uvedeno zbog uslova da samo jedan korisnik sme da menja proizvod.

Korisnik mora prvo da zakljuca proizvod, pa tek onda da poveca proizvodnju.

### `PovecajProizvodnju(int proizvodId, int kolicina)`

Povecava proizvedenu kolicinu, ali samo ako je taj proizvod zakljucao isti korisnik.

Kolicina je prirastaj, ne nova ukupna vrednost.

Primer:

- trenutno 100;
- korisnik unese 25;
- novo stanje je 125.

### `OtkljucajProizvod(int proizvodId)`

Ako korisnik odustane, moze da oslobodi proizvod.

U implementaciji se proizvod otkljucava i posle uspesnog povecanja proizvodnje.

## `IProizvodnjaCallback`

```csharp
public interface IProizvodnjaCallback
{
    [OperationContract(IsOneWay = true)]
    void Obavestenje(string tekst);
}
```

Ovo je callback ugovor. Njega implementira klijent.

Kada server pozove:

```csharp
callback.Obavestenje("Dodat je novi proizvod...");
```

na klijentu se izvrsi metoda:

```csharp
public void Obavestenje(string tekst)
```

### Zasto `IsOneWay = true`?

`IsOneWay = true` znaci da server salje obavestenje i ne ceka povratnu vrednost.

To je dobro za notifikacije:

- server samo javlja informaciju;
- klijent ne mora da vrati odgovor;
- manja je sansa da callback napravi blokiranje.

Ako callback metoda treba nesto da vrati serveru, onda ne bi bila `IsOneWay = true`. Ali za obavestenja je to pravilno.

## DataContract klase

### `Proizvod`

Sadrzi:

- `Id`;
- `Naziv`;
- `ProizvedenaKolicina`;
- `ZakljucaoKorisnik`.

`ZakljucaoKorisnik` je bitno polje jer klijent moze da vidi da li neko trenutno menja proizvod.

### `OperationResult`

Sadrzi:

- `IsError`;
- `Message`.

Koristi se za poslovne rezultate: registrovanje, dodavanje, zakljucavanje, povecanje.

## `ProizvodnjaService.svc`

```aspx
<%@ ServiceHost Language="C#" Debug="true" Service="WCFProizvodnjaServer.ProizvodnjaService" CodeBehind="ProizvodnjaService.svc.cs" %>
```

Ovaj fajl povezuje URL `.svc` fajla sa klasom `ProizvodnjaService`.

Server adresa je:

```text
http://localhost:55103/ProizvodnjaService.svc
```

## `ProizvodnjaService.svc.cs` - implementacija

Klasa ima vazan atribut:

```csharp
[ServiceBehavior(InstanceContextMode = InstanceContextMode.PerSession,
                 ConcurrencyMode = ConcurrencyMode.Reentrant)]
public class ProizvodnjaService : IProizvodnjaService
```

### `InstanceContextMode.PerSession`

Za svaku klijentsku sesiju pravi se posebna instanca servisa.

Zato polje:

```csharp
private string korisnik;
```

moze da pamti ime korisnika za tu sesiju.

Da je servis `Single`, svi klijenti bi delili isto polje `korisnik`, sto bi bilo pogresno.

### `ConcurrencyMode.Reentrant`

Kod callback scenarija server tokom jedne operacije moze da pozove klijenta. `Reentrant` dozvoljava takav tok bez tipicnog deadlock problema kada se koristi callback.

Prakticno za ispit: kod duplex servisa sa callbackom cesto ces videti `ConcurrencyMode.Reentrant`.

## Static stanje

Servis ima:

```csharp
private static readonly List<Proizvod> Proizvodi
private static readonly Dictionary<string, IProizvodnjaCallback> Korisnici
```

`Proizvodi` su zajednicka lista proizvoda.

`Korisnici` je recnik:

```text
ime korisnika -> callback kanal
```

Ovo je odgovor na pitanje: zasto lista/recnik callbackova, a ne samo jedan callback?

Zato sto zadatak trazi da svi korisnici dobiju obavestenje. Ako server cuva samo jedan callback, mogao bi da obavesti samo poslednjeg registrovanog klijenta. Za vise klijenata mora postojati kolekcija callbackova.

## `RegistrujKorisnika`

Ova metoda radi tri bitne stvari:

1. validira ime;
2. pamti ime u polju `korisnik`;
3. uzima callback kanal:

```csharp
IProizvodnjaCallback callback =
    OperationContext.Current.GetCallbackChannel<IProizvodnjaCallback>();
```

Zatim ga cuva:

```csharp
Korisnici[korisnik] = callback;
```

Od tog trenutka server moze da pozove tog klijenta.

## `DodajProizvod`

Prvo proverava:

- da je korisnik registrovan;
- da naziv nije prazan.

Zatim u `lock` bloku dodaje proizvod.

Posle izlaska iz `lock` bloka poziva:

```csharp
NotifyAll(...)
```

Bitno: callback se poziva van kriticnog dela koliko god je moguce. Ne zelis dugo da drzis `lock` dok klijenti primaju poruke.

## `VratiProizvode`

Vraca kopije proizvoda:

```csharp
return Proizvodi.Select(Clone).ToList();
```

Kopiranje je dobar stil zbog zastite interne liste.

## `ZakljucajProizvod`

Logika:

1. korisnik mora biti registrovan;
2. proizvod mora postojati;
3. ako je `ZakljucaoKorisnik` prazan, trenutni korisnik ga zakljucava;
4. ako je vec zakljucao isti korisnik, to je dozvoljeno;
5. ako je zakljucao neko drugi, vraca se greska.

Ovo resava uslov "samo jedan korisnik moze da menja jedan proizvod".

## `PovecajProizvodnju`

Logika:

1. korisnik mora biti registrovan;
2. kolicina mora biti veca od nule;
3. proizvod mora postojati;
4. proizvod mora biti zakljucan bas za tog korisnika;
5. kolicina se uvecava;
6. proizvod se otkljucava;
7. salje se obavestenje svima.

Ovo je pravilno jer se ne dozvoljava da neko preskoci zakljucavanje.

## `NotifyAll`

```csharp
private static void NotifyAll(string text)
```

Ova metoda uzima sve callbackove iz recnika `Korisnici` i svakom klijentu poziva:

```csharp
callback.Obavestenje(text);
```

Poziv je u `try/catch` bloku, jer neki klijent moze biti ugasen ili kanal moze biti prekinut.

U takvom slucaju ne zelimo da jedan mrtav klijent obori ceo servis.

## `Web.config` - server

Ovaj projekat koristi `wsDualHttpBinding`.

### Binding

```xml
<wsDualHttpBinding>
  <binding name="WSDualHttpBinding_IProizvodnjaService">
    <security mode="None"/>
  </binding>
</wsDualHttpBinding>
```

`wsDualHttpBinding` je potreban jer komunikacija ide u oba smera:

- klijent poziva server;
- server poziva callback na klijentu.

`security mode="None"` je pojednostavljenje za lokalni lab.

### Service endpoint

```xml
<endpoint address=""
          binding="wsDualHttpBinding"
          bindingConfiguration="WSDualHttpBinding_IProizvodnjaService"
          contract="WCFProizvodnjaServer.IProizvodnjaService" />
```

Ovo mora da se poklopi sa klijentskim bindingom.

### MEX endpoint

```xml
<endpoint address="mex" binding="mexHttpBinding" contract="IMetadataExchange" />
```

Sluzi za metadata i Service Reference.

## `Program.cs` - klijent

Klijent ima dve uloge:

1. poziva server;
2. implementira callback koji server moze da pozove.

### Callback handler

```csharp
public class CallbackHandler : IProizvodnjaCallback
{
    public void Obavestenje(string tekst)
    {
        Console.WriteLine("[OBAVESTENJE] " + tekst);
    }
}
```

Ovo je kod koji se izvrsava kada server pozove callback.

### Duplex factory

Klijent ne koristi obican `ChannelFactory`, nego:

```csharp
DuplexChannelFactory<IProizvodnjaService>
```

Zato sto uz proxy mora da prosledi i callback objekat.

```csharp
var callback = new CallbackHandler();
var factory = CreateFactory(callback);
IProizvodnjaService proxy = factory.CreateChannel();
```

### `ClientBaseAddress`

U kodu se pravi:

```csharp
binding.ClientBaseAddress = new Uri("http://localhost:" + port + "/WCFProizvodnjaClient/");
```

Kod `wsDualHttpBinding` klijent mora imati adresu na kojoj server moze da ga pozove nazad.

Port se racuna iz `Process.GetCurrentProcess().Id` da bi vise pokrenutih klijenata moglo da ima razlicite callback adrese.

To je vazno za testiranje vise konzolnih klijenata.

### Tok u klijentu

1. Unese se ime korisnika.
2. Klijent pozove `RegistrujKorisnika`.
3. Klijent ispise proizvode.
4. Klijent pozove `DodajProizvod`.
5. Server pozove callback `Obavestenje` kod svih registrovanih klijenata.
6. Klijent zakljuca proizvod 1.
7. Klijent poveca proizvodnju za 25.
8. Server opet obavesti sve klijente.

## `App.config` - klijent

Sadrzi endpoint:

```xml
<endpoint address="http://localhost:55103/ProizvodnjaService.svc"
          binding="wsDualHttpBinding"
          bindingConfiguration="WSDualHttpBinding_IProizvodnjaService"
          contract="WCFProizvodnjaServer.IProizvodnjaService"
          name="WSDualHttpBinding_IProizvodnjaService" />
```

U ovom projektu klijent u `Program.cs` programatski pravi binding i endpoint, jer tako lakse dobija jedinstveni `ClientBaseAddress` za callback.

Ipak, `App.config` je ostavljen da se jasno vidi kako bi endpoint izgledao u konfiguraciji.

## Sta moras da znas za kviz/usmeni

- Callback znaci da server poziva metodu na klijentu.
- `CallbackContract` se stavlja u `[ServiceContract]`.
- Callback interfejs implementira klijent.
- Server callback kanal dobija preko `OperationContext.Current.GetCallbackChannel<T>()`.
- Za vise korisnika cuva se lista ili recnik callbackova.
- `IsOneWay = true` je dobro za obavestenja koja ne vracaju rezultat.
- `wsDualHttpBinding` se koristi za duplex/callback.
- `basicHttpBinding` nije dovoljan za callback.
- `PerSession` je bitan kada servis pamti stanje konkretnog klijenta.
- `Reentrant` je cest kod callback scenarija.
- Lock po proizvodu je poslovna logika, nije isto sto i C# `lock`.

## Najcesce greske

- Koristi se `basicHttpBinding` za callback.
- Zaboravi se `CallbackContract`.
- Klijent ne implementira callback interfejs.
- Server cuva samo jedan callback, pa obavestenje dobija samo poslednji korisnik.
- Pozove se `PovecajProizvodnju` bez `ZakljucajProizvod`.
- Ne proveri se da li je proizvod zakljucao isti korisnik.
- Callback se poziva dok se nepotrebno dugo drzi `lock`.
- Klijent nema validan `ClientBaseAddress`.


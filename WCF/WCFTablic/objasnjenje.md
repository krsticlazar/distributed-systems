# WCFTablic - detaljno objasnjenje

Ovo je najkompleksniji zadatak iz pripreme jer nije obican CRUD i nije samo prost callback. Ovde server glumi centralni sistem koji vodi celu igru.

Zadatak trazi:

- korisnik se prijavljuje da zeli da igra;
- centralni sistem uparuje dva igraca;
- server deli karte;
- posle svakog poteza prikazuje se stanje table;
- klijenti prijavljuju poteze;
- server proverava legalnost poteza;
- u jednom trenutku postoji samo jedna aktivna partija;
- ostali korisnici mogu da posmatraju partiju.

Ovo znaci da server mora da cuva zajednicko stanje igre i da obavestava igrace/posmatrace kada se stanje promeni. Zato je ovo duplex WCF aplikacija sa callbackom.

## Struktura solutiona

Solution se zove `WCFTablic`.

Ima dva projekta:

- `WCFTablicServer` - WCF servis;
- `WCFTablicClient` - konzolni klijent.

Backend fajlovi:

- `ITablicService.cs` - servisni ugovor, callback ugovor i rezultat operacije;
- `TablicService.svc` - host fajl;
- `TablicService.svc.cs` - kompletna logika igre;
- `Web.config` - konfiguracija za duplex servis.

Client fajlovi:

- `Program.cs` - klijent za igraca ili posmatraca;
- `App.config` - WCF endpoint konfiguracija.

## Zasto callback?

Bez callbacka bi klijent stalno morao da pita server:

```text
Da li je protivnik odigrao?
Da li se promenio talon?
Da li sam na potezu?
```

Sa callbackom server sam salje novo stanje kada se nesto desi:

- drugi igrac se prikljucio;
- partija je pocela;
- neko je bacio kartu;
- neko je nosio karte;
- stanje se promenilo.

Zbog toga se koristi `wsDualHttpBinding`.

## `ITablicService.cs` - servisni ugovor

```csharp
[ServiceContract(SessionMode = SessionMode.Required,
                 CallbackContract = typeof(ITablicCallback))]
public interface ITablicService
```

Ovo znaci:

- servis zahteva sesiju;
- servis ima callback ugovor;
- klijent mora da implementira `ITablicCallback`.

### Zasto `SessionMode.Required`?

Servis u poljima instance pamti:

```csharp
private string name;
private bool observer;
```

To su podaci za konkretnog klijenta. Jedna sesija = jedan prijavljeni korisnik.

Bez sesije bi bilo teze znati ko je pozvao `BaciKartu` ili `Nosi`.

## Operacije u `ITablicService`

### `PrijaviSeZaIgru(string ime)`

Korisnik se prijavljuje kao igrac.

Server:

- zapamti njegovo ime;
- uzme njegov callback kanal;
- doda ga u kolekciju igraca;
- ako nema drugog igraca, stavi ga kao `waitingPlayer`;
- ako vec postoji igrac koji ceka, pokrece partiju.

### `PrijaviSeKaoPosmatrac(string ime)`

Korisnik se prijavljuje kao posmatrac.

Posmatrac dobija stanje preko callbacka, ali ne sme da odigra potez.

### `BaciKartu(string karta)`

Igrac baca kartu iz ruke na talon.

Server mora da proveri:

- da postoji aktivna partija;
- da pozivalac nije posmatrac;
- da je taj igrac na potezu;
- da igrac stvarno ima tu kartu.

### `Nosi(string kartaIzRuke, List<string> karteSaTalona)`

Igrac pokusava da pokupi karte sa talona kartom iz ruke.

U ovoj implementaciji pravilo je pojednostavljeno:

```text
vrednost karte iz ruke == zbir vrednosti izabranih karata sa talona
```

Primer:

```text
2P nosi 2T
```

ili:

```text
7H nosi 3P 4K
```

Ako zbir nije jednak, potez nije legalan.

### `VratiStanje()`

Vraca tekstualno stanje za trenutnog korisnika.

Igrac vidi svoje karte.

Posmatrac ne vidi privatne karte igraca, nego vidi talon, redosled poteza i rezultat.

## `ITablicCallback`

```csharp
public interface ITablicCallback
{
    [OperationContract(IsOneWay = true)]
    void Obavestenje(string tekst);
}
```

Ovo je metoda koju server poziva na klijentu.

`IsOneWay = true` znaci:

- server posalje obavestenje;
- ne ceka rezultat;
- metoda ne vraca vrednost.

Za stanje igre ovo je dobro, jer server samo prosledjuje informaciju.

## `OperationResult`

```csharp
[DataContract]
public class OperationResult
```

Sadrzi:

- `IsError`;
- `Message`;
- `State`.

`Message` je kratka poruka o operaciji.

`State` moze da sadrzi stanje igre posle operacije.

## `TablicService.svc`

```aspx
<%@ ServiceHost Language="C#" Debug="true" Service="WCFTablicServer.TablicService" CodeBehind="TablicService.svc.cs" %>
```

Ovo povezuje `.svc` adresu sa klasom `TablicService`.

Server endpoint:

```text
http://localhost:55104/TablicService.svc
```

## `TablicService.svc.cs` - implementacija igre

Klasa ima:

```csharp
[ServiceBehavior(InstanceContextMode = InstanceContextMode.PerSession,
                 ConcurrencyMode = ConcurrencyMode.Reentrant)]
```

### `PerSession`

Svaki klijent dobija svoju instancu servisa.

Zato instancna polja:

```csharp
private string name;
private bool observer;
```

predstavljaju trenutnog klijenta.

### `Reentrant`

Servis koristi callback. Kada server poziva klijenta nazad, `Reentrant` pomaze da duplex komunikacija ne zapne u tipicnom callback deadlock scenariju.

## Static stanje servisa

```csharp
private static readonly Dictionary<string, ITablicCallback> Players
private static readonly Dictionary<string, ITablicCallback> Observers
private static string waitingPlayer;
private static Game activeGame;
```

Ovo je centralno stanje igre.

### `Players`

Mapa:

```text
ime igraca -> callback kanal
```

Server mora da cuva callback za svakog igraca da bi mu slao novo stanje.

### `Observers`

Mapa:

```text
ime posmatraca -> callback kanal
```

Posmatraci takodje dobijaju obavestenja, ali nisu igraci.

### `waitingPlayer`

Pamti prvog igraca koji ceka protivnika.

Ako prvi igrac pozove `PrijaviSeZaIgru`, nema partije, pa on ceka.

Kada drugi igrac pozove `PrijaviSeZaIgru`, server ih uparuje.

### `activeGame`

Predstavlja trenutno aktivnu partiju.

Posto zadatak kaze da samo jedna partija sme biti aktivna, dovoljan je jedan `activeGame`.

Da je dozvoljeno vise partija, morali bismo imati listu ili recnik partija.

## `PrijaviSeZaIgru`

Tok:

1. proveri da ime nije prazno;
2. zapamti ime u `name`;
3. `observer = false`;
4. uzme callback kanal preko `OperationContext.Current.GetCallbackChannel<ITablicCallback>()`;
5. doda igraca u `Players`;
6. ako vec postoji `activeGame`, vrati gresku;
7. ako nema igraca koji ceka, postavi `waitingPlayer`;
8. ako postoji igrac koji ceka, pozove `StartGame`.

Posle promene stanja poziva se:

```csharp
NotifyAll();
```

Tako svi dobijaju novo stanje.

## `PrijaviSeKaoPosmatrac`

Slicno kao registracija igraca, ali:

- `observer = true`;
- callback se cuva u `Observers`;
- posmatrac ne ulazi u `waitingPlayer`;
- posmatrac ne moze da pocne partiju.

## `StartGame`

Ova metoda pravi novu partiju.

Radi:

- kreira i mesa spil;
- postavlja 4 karte na talon;
- deli po 6 karata igracima;
- inicijalizuje rezultat;
- postavlja ko je prvi na potezu.

Spil se pravi u `CreateDeck`.

U ovoj implementaciji karte su stringovi:

```text
1P, 2P, 3P...
```

Slovo predstavlja boju, broj predstavlja vrednost.

## `BaciKartu`

Pre poteza zove se:

```csharp
ValidateTurn(karta)
```

Ako validacija prodje:

- karta se uklanja iz ruke igraca;
- dodaje se na talon;
- postavlja se `LastMessage`;
- menja se igrac na potezu preko `SwitchTurn`;
- poziva se `NotifyAll`.

## `Nosi`

Ovo je slozeniji potez.

Provere:

1. validan je red na potezu;
2. igrac ima kartu koju zeli da koristi;
3. lista karata sa talona nije prazna;
4. svaka navedena karta stvarno postoji na talonu;
5. zbir vrednosti karata sa talona jednak je vrednosti karte iz ruke.

Ako sve prodje:

- karta iz ruke se uklanja;
- izabrane karte se uklanjaju sa talona;
- rezultat igraca se uvecava za broj pokupljenih karata + 1;
- red prelazi na drugog igraca;
- svi dobijaju obavestenje.

## `ValidateTurn`

Ovo je centralna validacija poteza.

Proverava:

- da partija postoji;
- da pozivalac nije posmatrac;
- da pozivalac jeste igrac u aktivnoj partiji;
- da je bas on na potezu;
- da u ruci ima kartu koju prijavljuje.

Ovo mora biti na serveru. Klijent nikada ne sme sam da odlucuje da je potez legalan.

## `SwitchTurn`

Menja igraca na potezu.

Ako su obe ruke prazne, dodaje poruku da je partija zavrsena i ispisuje rezultat.

U ovoj verziji nema deljenja novih ruku posle 6 karata, jer je cilj WCF struktura i osnovna validacija. Za lab je bitno da umes da objasnis da je matematika/pravila igre pojednostavljena.

## `NotifyAll`

Ovo je najbitnija callback metoda u servisu.

Server napravi listu svih meta:

- igraci iz `Players`;
- posmatraci iz `Observers`.

Za svaku metu poziva:

```csharp
target.Item2.Obavestenje(StateFor(target.Item1, target.Item3));
```

Bitno: ne salje svima isti tekst.

Za igraca `StateFor` moze da ukljuci njegove karte.

Za posmatraca `StateFor` kaze da posmatra partiju.

Ovo je bitno jer ne sme svaki klijent da vidi isto privatno stanje.

## `StateFor`

Pravi tekstualno stanje igre.

Ako nema aktivne partije:

- ili nema nikoga;
- ili ceka prvi igrac.

Ako ima aktivne partije, ispisuje:

- poslednju poruku;
- talon;
- ko je na potezu;
- imena igraca;
- rezultat;
- privatne karte ako je korisnik igrac;
- poruku za posmatraca ako korisnik nije igrac.

## `CardValue`

Iz stringa karte izvlaci broj sa pocetka.

Primer:

- `2T` daje 2;
- `14K` daje 14.

Ovo je pojednostavljena vrednost karte za validaciju poteza.

## `CreateDeck`

Pravi listu karata.

Boje su:

```csharp
string[] suits = { "P", "K", "H", "T" };
```

Za svaku boju pravi vrednosti od 1 do 14.

Na kraju mesa karte:

```csharp
return cards.OrderBy(c => Guid.NewGuid()).ToList();
```

Ovo nije najbolji nacin za ozbiljnu igru, ali je dovoljno za lab primer.

## `Game` unutrasnja klasa

Cuva stanje aktivne partije:

- `Player1`;
- `Player2`;
- `Turn`;
- `Table`;
- `Hands`;
- `Scores`;
- `LastMessage`.

Ona nije `[DataContract]` jer se ne salje klijentu kao objekat. To je interna serverska klasa.

## `Web.config` - server

Koristi se `wsDualHttpBinding`.

```xml
<wsDualHttpBinding>
  <binding name="WSDualHttpBinding_ITablicService">
    <security mode="None"/>
  </binding>
</wsDualHttpBinding>
```

Ovo je potrebno za callback.

Glavni endpoint:

```xml
<endpoint address=""
          binding="wsDualHttpBinding"
          bindingConfiguration="WSDualHttpBinding_ITablicService"
          contract="WCFTablicServer.ITablicService" />
```

MEX endpoint:

```xml
<endpoint address="mex" binding="mexHttpBinding" contract="IMetadataExchange" />
```

MEX je za metadata/Service Reference, ne za igru.

## `Program.cs` - klijent

Klijent pravi duplex proxy:

```csharp
var factory = CreateFactory(new CallbackHandler());
ITablicService proxy = factory.CreateChannel();
```

Zato sto server mora da moze da pozove klijenta nazad.

### Izbor moda

Korisnik bira:

- `1` - igrac;
- `2` - posmatrac.

Ako je igrac:

```csharp
proxy.PrijaviSeZaIgru(ime)
```

Ako je posmatrac:

```csharp
proxy.PrijaviSeKaoPosmatrac(ime)
```

### Komande

Klijent podrzava:

- `/state` - rucno trazi stanje;
- `baci KARTA` - baca kartu;
- `nosi KARTA TALON1 TALON2` - nosi karte;
- `/q` - izlaz.

Primer:

```text
baci 7H
nosi 2P 2T
nosi 7H 3P 4K
```

### Callback handler

```csharp
public class CallbackHandler : ITablicCallback
{
    public void Obavestenje(string tekst)
    {
        Console.WriteLine();
        Console.WriteLine(tekst);
    }
}
```

Ovo se izvrsi kada server pozove callback.

### `ClientBaseAddress`

U `CreateFactory` se pravi:

```csharp
binding.ClientBaseAddress = new Uri("http://localhost:" + port + "/WCFTablicClient/");
```

Kod `wsDualHttpBinding`, klijent mora imati adresu za callback.

Port se racuna iz process ID-a da bi vise konzolnih klijenata moglo da radi paralelno.

## `App.config` - klijent

Endpoint:

```xml
<endpoint address="http://localhost:55104/TablicService.svc"
          binding="wsDualHttpBinding"
          bindingConfiguration="WSDualHttpBinding_ITablicService"
          contract="WCFTablicServer.ITablicService"
          name="WSDualHttpBinding_ITablicService" />
```

U ovom klijentu binding se pravi programatski zbog `ClientBaseAddress`, ali config pokazuje koju adresu i binding servis koristi.

## Sta posebno nauciti

- Ovo je zadatak na koji treba najvise obratiti paznju.
- Server vodi centralno stanje igre.
- Klijent samo prijavljuje nameru, server proverava legalnost.
- Callback se koristi da se stanje posalje igracima i posmatracima.
- Mora postojati kolekcija callbackova, ne samo jedan callback.
- Posmatrac ima callback, ali nema pravo poteza.
- `activeGame` ogranicava sistem na jednu partiju.
- `waitingPlayer` resava uparivanje.
- `StateFor` pokazuje da razliciti klijenti mogu dobiti razlicit prikaz istog stanja.

## Najcesce greske

- Dozvolis da posmatrac odigra potez.
- Ne proveris da li je igrac na potezu.
- Verujes klijentu da ima kartu.
- Cuvas samo jedan callback.
- Svim klijentima saljes iste privatne karte.
- Koristis `basicHttpBinding` za callback.
- Zaboravis `ClientBaseAddress`.
- Pokusas da podrzis vise partija, a zadatak trazi samo jednu.


# WCFElektronskiDnevnik - detaljno objasnjenje

Ovaj projekat je duplex WCF aplikacija sa dve uloge: profesor i roditelj. Najbitnija stvar je da callback ovde nije chat poruka, nego poslovno obavestenje: roditelj treba da dobije informaciju kada njegovo dete dobije ocenu.

Zadatak trazi:

- korisnik bira profesorski ili roditeljski mod;
- profesor daje ocenu uceniku za svoj predmet;
- profesor unosi ucenike koji slusaju njegov predmet;
- roditelj prijavljuje koja su njegova deca;
- roditelj moze da vidi ocene deteta za jedan ili sve predmete;
- roditelj dobija obavestenje kada dete dobije ocenu.

Ovo je vazan primer jer pokazuje da callback ne mora biti broadcast svima. Server treba da obavesti samo roditelje cija deca su ocenjena.

## Struktura solutiona

Solution se zove `WCFElektronskiDnevnik`.

Ima dva projekta:

- `WCFElektronskiDnevnikServer` - WCF servis;
- `WCFElektronskiDnevnikClient` - konzolni klijent.

Backend fajlovi:

- `IDnevnikService.cs` - servisni ugovor, callback ugovor i data modeli;
- `DnevnikService.svc` - host fajl;
- `DnevnikService.svc.cs` - implementacija dnevnika;
- `Web.config` - server konfiguracija za duplex binding.

Client fajlovi:

- `Program.cs` - konzolni klijent sa profesor/roditelj modom;
- `App.config` - endpoint i binding konfiguracija.

## Glavna ideja

Server cuva:

- listu ucenika;
- mapu predmeta i ucenika koji slusaju taj predmet;
- mapu roditelja i njihove dece;
- callback kanale roditelja;
- listu ocena.

Profesor menja stanje tako sto dodaje ucenike na predmet i upisuje ocene.

Roditelj cita stanje i prima callback obavestenje.

## Zasto callback?

Zato sto roditelj treba da dobije obavestenje kada profesor unese ocenu.

Bez callbacka bi roditelj morao stalno da poziva:

```text
VratiOceneZaDete(...)
```

i da proverava da li ima nova ocena.

Sa callbackom server sam pozove roditelja:

```text
Nova ocena: Ana Petrovic, Matematika = 5
```

## `IDnevnikService.cs` - servisni i callback ugovor

```csharp
[ServiceContract(SessionMode = SessionMode.Required,
                 CallbackContract = typeof(IDnevnikCallback))]
public interface IDnevnikService
```

Ovo znaci:

- servis zahteva sesiju;
- klijent mora da ima callback implementaciju;
- server moze da pozove metode iz `IDnevnikCallback`.

## Operacije u `IDnevnikService`

### `RegistrujProfesora(string ime, string predmet)`

Klijent se registruje kao profesor.

Server pamti u sesiji:

- ime profesora;
- predmet profesora.

Profesor posle toga moze da:

- doda ucenika na svoj predmet;
- da ocenu za svoj predmet.

### `RegistrujRoditelja(string prezime)`

Klijent se registruje kao roditelj.

Server:

- pamti prezime roditelja u sesiji;
- uzima callback kanal roditelja;
- cuva callback u recnik `Roditelji`.

Ovo je potrebno da bi server kasnije mogao da obavesti tog roditelja.

### `VratiUcenike()`

Vraca listu svih ucenika.

Koristi se u oba moda:

- profesor bira kome daje ocenu;
- roditelj bira koje dete prijavljuje.

### `DodajUcenikaNaPredmet(int ucenikId)`

Profesor dodaje ucenika na svoj predmet.

Bez ovoga profesor ne sme da da ocenu tom uceniku za taj predmet.

### `DajOcenu(int ucenikId, int ocena)`

Profesor daje ocenu.

Server proverava:

- da je profesor registrovan;
- da je ocena od 1 do 5;
- da ucenik postoji;
- da je ucenik dodat na profesorov predmet.

Ako je sve ispravno, server upisuje ocenu i obavestava roditelje tog ucenika.

### `DodajDete(int ucenikId)`

Roditelj prijavljuje da je konkretan ucenik njegovo dete.

Server posle toga zna da tog roditelja treba obavestiti kada taj ucenik dobije ocenu.

### `VratiOceneZaDete(int ucenikId, string predmet)`

Roditelj trazi ocene za dete.

Ako je `predmet` jednak `"SVI"`, vracaju se ocene iz svih predmeta.

Ako je prosledjen konkretan predmet, vracaju se samo ocene iz tog predmeta.

## `IDnevnikCallback`

```csharp
public interface IDnevnikCallback
{
    [OperationContract(IsOneWay = true)]
    void Obavestenje(string tekst);
}
```

Ovo je metoda koju server poziva na klijentu.

`IsOneWay = true` je pravilno jer:

- server samo salje obavestenje;
- ne ocekuje povratnu vrednost;
- callback je notifikacija, ne upit.

## DataContract klase

### `Ucenik`

Sadrzi:

- `Id`;
- `Ime`;
- `Prezime`.

Koristi se za prikaz liste ucenika.

### `Ocena`

Sadrzi:

- `UcenikId`;
- `Ucenik`;
- `Predmet`;
- `Vrednost`;
- `Profesor`;
- `Vreme`.

Ovo je kompletna informacija koju roditelj moze da vidi.

### `OperationResult`

Sadrzi:

- `IsError`;
- `Message`.

Koristi se za sve operacije koje treba da kazu da li su uspele.

## `DnevnikService.svc`

```aspx
<%@ ServiceHost Language="C#" Debug="true" Service="WCFElektronskiDnevnikServer.DnevnikService" CodeBehind="DnevnikService.svc.cs" %>
```

Ovo povezuje `.svc` adresu sa klasom `DnevnikService`.

Server endpoint:

```text
http://localhost:55105/DnevnikService.svc
```

## `DnevnikService.svc.cs` - implementacija

Klasa ima:

```csharp
[ServiceBehavior(InstanceContextMode = InstanceContextMode.PerSession,
                 ConcurrencyMode = ConcurrencyMode.Reentrant)]
```

### `PerSession`

Svaka klijentska sesija ima svoju instancu servisa.

Zato ova polja imaju smisla:

```csharp
private string profesorIme;
private string predmet;
private string roditeljPrezime;
```

Jedan klijent moze biti registrovan kao profesor, drugi kao roditelj.

### `Reentrant`

Servis koristi callback prema roditelju. `Reentrant` je uobicajen izbor za duplex callback scenarije.

## Static stanje

### `Ucenici`

Pocetna lista ucenika:

- Ana Petrovic;
- Marko Jovanovic;
- Lena Nikolic.

Ovo je zamena za bazu.

### `Predmeti`

```csharp
Dictionary<string, HashSet<int>>
```

Mapa:

```text
predmet -> skup ID-eva ucenika
```

Primer:

```text
Matematika -> { 1, 3 }
```

To znaci da ucenici 1 i 3 slusaju Matematiku.

### `DecaRoditelja`

```csharp
Dictionary<string, HashSet<int>>
```

Mapa:

```text
prezime roditelja -> skup ID-eva dece
```

Primer:

```text
Petrovic -> { 1 }
```

### `Roditelji`

```csharp
Dictionary<string, IDnevnikCallback>
```

Mapa:

```text
prezime roditelja -> callback kanal
```

Ovo je lista callbackova, ali organizovana po roditelju.

Ne cuva se samo jedan callback jer moze biti vise roditelja prijavljeno u isto vreme.

### `Ocene`

Lista svih upisanih ocena.

## `RegistrujProfesora`

Proverava da ime i predmet nisu prazni.

Zatim pamti:

```csharp
profesorIme = ime.Trim();
predmet = predmetProfesora.Trim();
```

U static recniku `Predmeti` pravi novi skup za predmet ako ne postoji.

Profesor nema callback u ovoj verziji, jer zadatak trazi obavestenje roditelju, ne profesoru.

## `RegistrujRoditelja`

Proverava prezime.

Uzima callback:

```csharp
IDnevnikCallback callback =
    OperationContext.Current.GetCallbackChannel<IDnevnikCallback>();
```

Cuva:

```csharp
Roditelji[roditeljPrezime] = callback;
```

Takodje obezbedjuje da postoji skup dece za tog roditelja.

## `VratiUcenike`

Vraca kopije ucenika:

```csharp
return Ucenici.Select(Clone).ToList();
```

Kopiranje je dobar stil jer server ne iznosi svoje interne objekte direktno.

## `DodajUcenikaNaPredmet`

Prvo proverava da je profesor registrovan:

```csharp
if (string.IsNullOrWhiteSpace(predmet))
```

Zatim proverava da ucenik postoji.

Ako postoji, dodaje njegov ID u:

```csharp
Predmeti[predmet]
```

Koristi se `HashSet<int>` da se isti ucenik ne duplira.

## `DajOcenu`

Ovo je centralna metoda.

Validacije:

1. profesor mora biti registrovan;
2. ocena mora biti od 1 do 5;
3. ucenik mora postojati;
4. ucenik mora biti dodat na predmet profesora.

Ako sve prodje, pravi se `Ocena` objekat i dodaje u listu `Ocene`.

Zatim server pronalazi roditelje koje treba obavestiti:

```csharp
callbacks = Roditelji
    .Where(r => DecaRoditelja.ContainsKey(r.Key) &&
                DecaRoditelja[r.Key].Contains(ucenikId))
    .Select(r => r.Value)
    .ToList();
```

Ovo znaci:

- prodji kroz registrovane roditelje;
- vidi da li taj roditelj ima dete sa tim `ucenikId`;
- ako ima, uzmi njegov callback.

Posle izlaska iz `lock` bloka, server salje obavestenje:

```csharp
TryNotify(callback, "Nova ocena: ...");
```

## Zasto se ne obavestavaju svi roditelji?

Zato sto zadatak kaze da roditelj dobija obavestenje za svoju decu.

Ako bi server obavestio sve roditelje, to bi bilo pogresno poslovno ponasanje.

Ovo je razlika u odnosu na chat/proizvodnju gde se cesto broadcastuje svima.

## `DodajDete`

Roditelj mora prvo da se registruje.

Zatim server proverava da ucenik postoji i dodaje njegov ID u:

```csharp
DecaRoditelja[roditeljPrezime]
```

Opet se koristi `HashSet<int>`.

## `VratiOceneZaDete`

Filtrira listu ocena po `UcenikId`.

Ako je predmet filter prazan ili `"SVI"`, vraca sve ocene za dete.

Ako je prosledjen konkretan predmet, radi dodatni filter:

```csharp
query = query.Where(o => o.Predmet == predmetFilter);
```

## `TryNotify`

Callback poziv je u `try/catch` bloku:

```csharp
try
{
    callback.Obavestenje(message);
}
catch (CommunicationException)
{
}
catch (TimeoutException)
{
}
```

Ako je roditelj zatvorio klijent ili pukla veza, servis ne treba da padne.

## `Web.config` - server

Koristi se `wsDualHttpBinding` jer server salje callback roditelju.

```xml
<wsDualHttpBinding>
  <binding name="WSDualHttpBinding_IDnevnikService">
    <security mode="None"/>
  </binding>
</wsDualHttpBinding>
```

Glavni endpoint:

```xml
<endpoint address=""
          binding="wsDualHttpBinding"
          bindingConfiguration="WSDualHttpBinding_IDnevnikService"
          contract="WCFElektronskiDnevnikServer.IDnevnikService" />
```

MEX endpoint:

```xml
<endpoint address="mex" binding="mexHttpBinding" contract="IMetadataExchange" />
```

MEX je za metadata/Service Reference.

## `Program.cs` - klijent

Klijent pravi duplex proxy:

```csharp
var factory = CreateFactory(new CallbackHandler());
IDnevnikService proxy = factory.CreateChannel();
```

Iako profesor u ovoj verziji ne prima callback, isti klijent moze da radi i kao roditelj, pa koristi duplex proxy.

### Profesor mod

Ako korisnik izabere `1`:

1. unosi ime profesora;
2. unosi predmet;
3. poziva `RegistrujProfesora`;
4. poziva `DodajUcenikaNaPredmet(1)`;
5. poziva `DajOcenu(1, 5)`.

To je demo tok.

Za ozbiljniji meni bi korisnik birao ucenika i ocenu iz konzole.

### Roditelj mod

Ako korisnik izabere `2`:

1. unosi prezime roditelja;
2. poziva `RegistrujRoditelja`;
3. poziva `DodajDete(1)`;
4. zove `VratiOceneZaDete(1, "SVI")`;
5. ispisuje ocene.

Ako je roditelj klijent pokrenut dok profesor upisuje ocenu za njegovo dete, roditelj dobija callback obavestenje.

### Callback handler

```csharp
public class CallbackHandler : IDnevnikCallback
{
    public void Obavestenje(string tekst)
    {
        Console.WriteLine("[DNEVNIK] " + tekst);
    }
}
```

Ova metoda se aktivira kada server pozove callback.

## `ClientBaseAddress`

U `CreateFactory`:

```csharp
binding.ClientBaseAddress = new Uri("http://localhost:" + port + "/WCFElektronskiDnevnikClient/");
```

Kod `wsDualHttpBinding`, klijent mora imati svoju adresu za callback.

Port se pravi na osnovu process ID-a da bi vise konzolnih klijenata moglo raditi istovremeno.

## `App.config` - klijent

Endpoint:

```xml
<endpoint address="http://localhost:55105/DnevnikService.svc"
          binding="wsDualHttpBinding"
          bindingConfiguration="WSDualHttpBinding_IDnevnikService"
          contract="WCFElektronskiDnevnikServer.IDnevnikService"
          name="WSDualHttpBinding_IDnevnikService" />
```

U kodu se endpoint pravi programatski zbog dinamickog callback porta, ali config pokazuje ispravan WCF oblik.

## Sta posebno nauciti

- Callback ne mora uvek ici svima.
- U dnevniku callback ide samo roditeljima cija deca su ocenjena.
- Profesor i roditelj su uloge u istoj servisnoj klasi.
- `PerSession` omogucava da servis pamti da li je klijent profesor ili roditelj.
- `HashSet<int>` je dobar za skup ID-eva bez duplikata.
- Server mora proveriti da li ucenik slusa predmet pre upisa ocene.
- `wsDualHttpBinding` je potreban zbog callback obavestenja.

## Najcesce greske

- Obavestis sve roditelje umesto samo roditelje tog deteta.
- Dozvolis ocenu bez `DodajUcenikaNaPredmet`.
- Dozvolis ocenu van opsega 1-5.
- Cuvas samo jedan roditeljski callback.
- Ne pozoves `RegistrujRoditelja`, pa server nema callback.
- Zaboravis `ClientBaseAddress`.
- Koristis `basicHttpBinding` i ocekujes callback.
- Zaboravis `[DataMember]` na `Vreme`, `Profesor` ili `Predmet`.


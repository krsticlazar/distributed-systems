# WCFKviz - detaljno objasnjenje

Ovaj projekat je najcistiji primer obicne WCF aplikacije bez callbacka. Klijent poziva server, server vrati odgovor i tu se komunikacija zavrsava. To je klasicni request/response model.

Zadatak trazi dve grupe funkcionalnosti:

- administracija pitanja: dodavanje i izmena pitanja;
- rad kviza: klijent preuzme pitanja, korisnik odgovara, klijent salje sve odgovore serveru, server racuna rezultat.

Najbitnija ideja: server cuva tacne odgovore, a klijent koji radi kviz ne sme da ih dobije. Zato postoje dve slicne, ali namerno razdvojene klase: `PitanjeAdmin` i `PitanjeZaKviz`.

## Struktura solutiona

Solution se zove `WCFKviz`.

U njemu su dva projekta:

- `WCFKvizServer` - WCF Service Application, hostuje servis `KvizService.svc`;
- `WCFKvizClient` - Console Application, poziva servis preko `ChannelFactory<IKvizService>`.

Backend fajlovi koje treba razumeti:

- `IKvizService.cs` - ugovor servisa i klase koje se salju kroz WCF;
- `KvizService.svc` - ulazna tacka servisa koju IIS Express hostuje;
- `KvizService.svc.cs` - stvarna implementacija metoda;
- `Web.config` - WCF konfiguracija servera, endpoint, binding i metadata.

Client fajlovi koje treba razumeti:

- `Program.cs` - pravi proxy i poziva metode servisa;
- `App.config` - konfigurise adresu servera i binding koji klijent koristi.

## Tok izvrsavanja

1. Pokrene se server projekat `WCFKvizServer`.
2. IIS Express hostuje adresu `http://localhost:55101/KvizService.svc`.
3. Pokrene se client projekat `WCFKvizClient`.
4. Klijent iz `App.config` procita endpoint po imenu `BasicHttpBinding_IKvizService`.
5. `ChannelFactory<IKvizService>` napravi proxy objekat.
6. Pozivi nad proxy objektom putuju do servera kao WCF pozivi.
7. Server izvrsi metodu u `KvizService.svc.cs` i vrati rezultat.

Ovde nema callbacka. Server nikada sam od sebe ne zove klijenta. Sve uvek pocinje od klijenta.

## `IKvizService.cs` - ugovor servisa

Ovo je najvazniji fajl za razumevanje WCF-a. On definise sta klijent sme da pozove.

```csharp
[ServiceContract]
public interface IKvizService
```

`[ServiceContract]` znaci: ovaj interfejs je WCF servisni ugovor. Bez toga WCF ne tretira interfejs kao servis.

Svaka metoda koja treba da bude dostupna klijentu mora imati:

```csharp
[OperationContract]
```

Ako metoda nema `[OperationContract]`, ona moze postojati u C# kodu, ali klijent je nece videti kao servisnu operaciju.

### Operacije

`DodajPitanje(PitanjeInput pitanje)`

- prima novo pitanje;
- server proverava da li je pitanje validno;
- ako jeste, dodaje ga u listu;
- vraca `OperationResult`.

`IzmeniPitanje(int id, PitanjeInput pitanje)`

- prima ID pitanja koje se menja;
- prima novi sadrzaj pitanja;
- ako ID ne postoji, vraca gresku;
- ako postoji, menja tekst, ponudjene opcije i tacan odgovor.

`VratiPitanjaZaAdministraciju()`

- vraca listu `PitanjeAdmin`;
- admin sme da vidi `TacanOdgovor`;
- koristi se kada korisnik bira koje pitanje zeli da izmeni.

`VratiPitanjaZaKviz()`

- vraca listu `PitanjeZaKviz`;
- ova klasa nema `TacanOdgovor`;
- to je namerno, jer korisnik koji radi kviz ne sme da zna resenje.

`ValidirajOdgovore(List<Odgovor> odgovori)`

- klijent salje sve odgovore odjednom;
- server za svako pitanje proverava da li postoji odgovor sa istim `PitanjeId`;
- server poredi `OdabraniOdgovor` sa `TacanOdgovor`;
- vraca broj tacnih, ukupan broj pitanja i procenat.

## DataContract i DataMember

Klase koje se salju izmedju klijenta i servera oznacene su sa:

```csharp
[DataContract]
```

Polja/properties koji treba da se stvarno prenesu oznaceni su sa:

```csharp
[DataMember]
```

Ako zaboravis `[DataMember]`, property postoji u C# klasi, ali nece biti serijalizovan kroz WCF poruku.

### `PitanjeInput`

Ovo je osnovni ulazni model za dodavanje i izmenu pitanja.

Sadrzi:

- `Tekst` - tekst pitanja;
- `Opcije` - ponudjeni odgovori;
- `TacanOdgovor` - redni broj tacne opcije, kod nas 1-based.

1-based znaci da je prva opcija broj 1, ne 0. To je lakse za unos iz konzole.

### `PitanjeAdmin`

Nasledjuje `PitanjeInput` i dodaje:

- `Id`.

Admin mora da zna ID pitanja da bi mogao da ga izmeni. Takodje vidi tacan odgovor.

### `PitanjeZaKviz`

Ovo je model za korisnika koji radi kviz.

Sadrzi:

- `Id`;
- `Tekst`;
- `Opcije`.

Ne sadrzi `TacanOdgovor`. To je sustina zadatka.

### `Odgovor`

Ovo je ono sto klijent salje nazad serveru.

Sadrzi:

- `PitanjeId` - na koje pitanje se odgovor odnosi;
- `OdabraniOdgovor` - redni broj izabrane opcije.

Server ne sme da se osloni na redosled liste, nego na `PitanjeId`.

### `RezultatKviza`

Ovo je odgovor servera posle validacije.

Sadrzi:

- `IsError`;
- `ErrorMessage`;
- `UkupnoPitanja`;
- `TacnihOdgovora`;
- `Procenat`.

Ovo je bolji model nego da metoda vrati samo broj, jer klijent dobija i status i rezultat.

### `OperationResult`

Koristi se za operacije koje nemaju poseban povratni model.

Sadrzi:

- `IsError` - da li se desila greska;
- `Message` - tekst za korisnika.

Na ispitu je ovo cest sablon: ne bacas exception za svaku poslovnu gresku, nego vratis objekat koji kaze da li je operacija uspela.

## `KvizService.svc`

Fajl ima samo jednu vaznu liniju:

```aspx
<%@ ServiceHost Language="C#" Debug="true" Service="WCFKvizServer.KvizService" CodeBehind="KvizService.svc.cs" %>
```

Ovo govori WCF hostu:

- servis je pisan u C#;
- konkretna servisna klasa je `WCFKvizServer.KvizService`;
- code-behind fajl je `KvizService.svc.cs`.

Kada u browseru otvoris:

```text
http://localhost:55101/KvizService.svc
```

WCF zna da iza te adrese stoji klasa `KvizService`.

## `KvizService.svc.cs` - implementacija

Ova klasa implementira `IKvizService`.

```csharp
public class KvizService : IKvizService
```

To znaci: sve metode iz interfejsa moraju ovde da postoje.

### Static lista pitanja

```csharp
private static readonly List<PitanjeAdmin> Pitanja
```

Lista je `static`, sto znaci da je dele sve instance servisa. Za lab i ispit je ovo dovoljno, jer ne koristimo bazu.

Da nije `static`, svaka nova instanca servisa mogla bi da ima praznu listu.

### `LockObj`

```csharp
private static readonly object LockObj = new object();
```

Koristi se zato sto vise klijenata moze istovremeno da pozove servis.

Kada se radi dodavanje, izmena ili citanje zajednicke liste, koristi se:

```csharp
lock (LockObj)
```

To sprecava da dva klijenta u isto vreme pokvare zajednicko stanje.

### `nextId`

`nextId` cuva sledeci ID za novo pitanje. Kada se doda pitanje, koristi se `nextId++`.

### `DodajPitanje`

Prvo se zove `Validate(pitanje)`.

Ako validacija vrati gresku, metoda odmah vraca tu gresku.

Ako je sve ispravno:

- ulazi se u `lock`;
- pravi se novi `PitanjeAdmin`;
- dodeljuje se ID;
- pitanje se dodaje u listu.

### `IzmeniPitanje`

Prvo validira novi sadrzaj pitanja.

Zatim u `lock` bloku trazi pitanje:

```csharp
PitanjeAdmin existing = Pitanja.FirstOrDefault(p => p.Id == id);
```

Ako ne postoji, vraca gresku.

Ako postoji, menja postojece polje, ne dodaje novo pitanje.

### `VratiPitanjaZaAdministraciju`

Vraca kopije pitanja:

```csharp
return Pitanja.Select(CloneAdmin).ToList();
```

Kopiranje je dobra praksa jer server ne vraca direktne reference na objekte iz svoje interne liste.

### `VratiPitanjaZaKviz`

Ovo je kljucno za bezbednost logike kviza.

Server pravi nove objekte `PitanjeZaKviz` i namerno ne popunjava tacan odgovor.

### `ValidirajOdgovore`

Ako je lista odgovora `null`, vraca se greska.

Ako postoji lista:

- za svako pitanje se nadje odgovor sa istim `PitanjeId`;
- ako odgovor postoji i vrednost je jednaka `TacanOdgovor`, broji se kao tacan;
- procenat se racuna kao `tacno * 100m / Pitanja.Count`.

`100m` znaci decimal literal, da rezultat bude decimalni.

### `Validate`

Pomocna metoda proverava:

- da pitanje nije `null`;
- da tekst nije prazan;
- da postoje bar dve opcije;
- da je tacan odgovor u opsegu od 1 do broja opcija.

Ovo je poslovna validacija i treba da stoji na serveru, jer klijentu ne verujemo.

## `Web.config` - serverska konfiguracija

Najvazniji deo je `system.serviceModel`.

### `serviceMetadata`

```xml
<serviceMetadata httpGetEnabled="true" httpsGetEnabled="true"/>
```

Omogucava da Visual Studio vidi metadata podatke servisa i napravi Service Reference.

Za lab je korisno da bude `true`. Za produkciju bi se pazljivije podesavalo.

### `serviceDebug`

```xml
<serviceDebug includeExceptionDetailInFaults="false"/>
```

Kada je `false`, server ne salje detalje exceptiona klijentu. To je bezbednije.

Za debug nekad ljudi stave `true`, ali na ispitu je bolje znati da `false` ne otkriva detalje.

### `services/service`

```xml
<service name="WCFKvizServer.KvizService" behaviorConfiguration="KvizServiceBehavior">
```

`name` mora da odgovara punom imenu servisne klase: namespace + class name.

Ako ovde pogresis, servis se nece pravilno hostovati.

### Glavni endpoint

```xml
<endpoint address=""
          binding="basicHttpBinding"
          contract="WCFKvizServer.IKvizService" />
```

Ovo znaci:

- `address=""` - servis je direktno na adresi `.svc` fajla;
- `binding="basicHttpBinding"` - obican HTTP request/response;
- `contract="WCFKvizServer.IKvizService"` - klijent vidi metode iz tog interfejsa.

Za ovaj zadatak je `basicHttpBinding` dovoljan jer nema callbacka.

### MEX endpoint

```xml
<endpoint address="mex" binding="mexHttpBinding" contract="IMetadataExchange" />
```

MEX sluzi za metadata exchange. Visual Studio ga koristi kada radis Add Service Reference.

MEX endpoint nije poslovna metoda servisa. On ne sluzi za kviz, nego za opis servisa.

## `Program.cs` - klijent

Klijent pravi proxy ovako:

```csharp
var factory = new ChannelFactory<IKvizService>("BasicHttpBinding_IKvizService");
IKvizService proxy = factory.CreateChannel();
```

Ime `BasicHttpBinding_IKvizService` mora da postoji u `App.config` endpoint konfiguraciji.

Posle toga klijent poziva metode kao da su lokalne:

```csharp
proxy.DodajPitanje(...)
proxy.IzmeniPitanje(...)
proxy.VratiPitanjaZaKviz()
proxy.ValidirajOdgovore(...)
```

Ali one nisu lokalne. Svaki poziv ide preko mreze do WCF servisa.

### Tok u klijentu

1. Ispise trenutna pitanja za administraciju.
2. Doda jedno novo pitanje.
3. Izmeni pitanje sa ID-em 1.
4. Preuzme pitanja za kviz.
5. Za svako pitanje ucita korisnikov odgovor iz konzole.
6. Salje celu listu odgovora serveru.
7. Ispisuje rezultat.
8. Zatvara proxy i factory.

Zatvaranje je bitno:

```csharp
((IClientChannel)proxy).Close();
factory.Close();
```

Time se korektno zatvara WCF kanal.

## `App.config` - klijentska konfiguracija

Najvazniji deo:

```xml
<endpoint address="http://localhost:55101/KvizService.svc"
          binding="basicHttpBinding"
          bindingConfiguration="BasicHttpBinding_IKvizService"
          contract="WCFKvizServer.IKvizService"
          name="BasicHttpBinding_IKvizService" />
```

Ovo mora da se poklopi sa serverom:

- `address` mora da pokazuje na pokrenut server;
- `binding` mora da bude isti tip bindinga kao na serveru;
- `contract` mora da odgovara servisnom ugovoru;
- `name` je ime po kome `ChannelFactory` nalazi endpoint.

Ako promenis port servera, moras promeniti i `address` u klijentu.

## Sta obavezno znati za usmeni/kratki kviz

- `[ServiceContract]` ide na interfejs servisa.
- `[OperationContract]` ide na metode koje klijent sme da pozove.
- `[DataContract]` ide na klase koje se prenose kroz WCF.
- `[DataMember]` ide na properties koji se serijalizuju.
- `basicHttpBinding` je dovoljan za obican request/response.
- Callback nije potreban za kviz.
- Server ne sme da salje tacan odgovor korisniku koji radi kviz.
- `Web.config` podesava server endpoint.
- `App.config` podesava client endpoint.
- MEX endpoint sluzi za Service Reference/metadata, ne za poslovnu logiku.

## Najcesce greske

- U `Web.config` pogresen `service name`.
- U `App.config` pogresan port.
- Metoda postoji u klasi, ali nema `[OperationContract]`.
- Klasa se salje kroz WCF, ali nema `[DataContract]`.
- Property nema `[DataMember]`, pa na klijentu stize prazan/default.
- Klijent dobije `PitanjeAdmin` umesto `PitanjeZaKviz`, pa vidi tacne odgovore.
- Validacija se radi samo na klijentu, umesto na serveru.


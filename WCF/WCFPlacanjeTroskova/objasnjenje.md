# WCFPlacanjeTroskova - detaljno objasnjenje

Ovaj projekat je primer obicne WCF request/response aplikacije sa poslovnom validacijom. Nema callbacka. Klijent trazi operaciju, server proverava stanje i vraca rezultat.

Zadatak trazi:

- dodavanje novog racuna;
- prikaz svih racuna;
- placanje troska;
- validaciju da racun postoji;
- validaciju da ima dovoljno novca;
- listing placanja za izabrani racun.

Ovo je dobar primer za ispit jer pokazuje da servis nije samo "pozovi metodu", nego centralno mesto gde mora da stoji poslovna logika.

## Struktura solutiona

Solution se zove `WCFPlacanjeTroskova`.

Ima dva projekta:

- `WCFPlacanjeTroskovaServer` - WCF servis;
- `WCFPlacanjeTroskovaClient` - konzolni klijent.

Backend fajlovi:

- `IPlacanjeService.cs` - servisni ugovor i data modeli;
- `PlacanjeService.svc` - host ulazna tacka;
- `PlacanjeService.svc.cs` - implementacija logike;
- `Web.config` - WCF endpoint, binding i metadata.

Client fajlovi:

- `Program.cs` - pravi proxy i demonstrira pozive;
- `App.config` - adresa i binding za server.

## Glavna ideja

Server cuva dve liste:

- listu racuna;
- listu placanja.

Racun ima bilans. Placanje sme da se izvrsi samo ako:

1. iznos je veci od nule;
2. broj racuna postoji;
3. racun ima dovoljno novca.

Klijent ne sme sam da odluci da je placanje validno. Klijent moze da prikaze podatke, ali server je autoritet.

## Tok izvrsavanja

1. Pokrene se `WCFPlacanjeTroskovaServer`.
2. WCF servis je dostupan na `http://localhost:55102/PlacanjeService.svc`.
3. Pokrene se `WCFPlacanjeTroskovaClient`.
4. Klijent pravi proxy preko `ChannelFactory<IPlacanjeService>`.
5. Klijent zove `VratiRacune`.
6. Klijent zove `DodajRacun`.
7. Klijent zove `PlatiTrosak`.
8. Server radi validaciju i menja bilans samo ako je placanje ispravno.
9. Klijent zove `ListingPlacanja` i ispisuje placanja za racun.

## `IPlacanjeService.cs` - servisni ugovor

```csharp
[ServiceContract]
public interface IPlacanjeService
```

Ovo je WCF ugovor. Sve sto klijent sme da vidi mora biti u ovom interfejsu.

Svaka javna servisna metoda ima:

```csharp
[OperationContract]
```

Ako metoda nema `[OperationContract]`, nece se pojaviti klijentu.

### `DodajRacun`

```csharp
OperationResult DodajRacun(string brojRacuna, string naziv, decimal pocetniBilans);
```

Prima osnovne podatke za novi racun.

Vraca `OperationResult`, jer nije potrebno vracati ceo racun. Dovoljno je reci da li je uspelo i zasto nije uspelo.

Validacije:

- broj racuna ne sme biti prazan;
- naziv ne sme biti prazan;
- pocetni bilans ne sme biti negativan;
- isti broj racuna ne sme vec postojati.

### `VratiRacune`

```csharp
List<Racun> VratiRacune();
```

Vraca sve racune. Koristi se da klijent moze da prikaze sta postoji.

### `PlatiTrosak`

```csharp
PlacanjeResult PlatiTrosak(string brojRacuna, decimal iznos, string opis);
```

Ovo je centralna metoda zadatka.

Vraca `PlacanjeResult`, a ne samo `OperationResult`, zato sto kod uspesnog placanja klijent treba da dobije:

- azurirani racun;
- podatke o placanju;
- poruku.

### `ListingPlacanja`

```csharp
List<Placanje> ListingPlacanja(string brojRacuna);
```

Vraca sva placanja za odabrani racun.

## DataContract klase

### `Racun`

```csharp
[DataContract]
public class Racun
```

Predstavlja racun.

Properties:

- `BrojRacuna`;
- `Naziv`;
- `Bilans`.

Sva tri imaju `[DataMember]`, sto znaci da ce se preneti do klijenta.

### `Placanje`

Predstavlja jedno izvrseno placanje.

Properties:

- `Id` - redni broj placanja;
- `BrojRacuna` - za koji racun je placanje;
- `Iznos`;
- `Opis`;
- `Vreme`.

`DateTime` se normalno prenosi kroz WCF ako je property oznacen sa `[DataMember]`.

### `OperationResult`

Sablon za poslovni rezultat:

- `IsError`;
- `Message`.

Ako je `IsError == true`, klijent treba da prikaze poruku kao gresku.

### `PlacanjeResult`

Nasledjuje `OperationResult` i dodaje:

- `Racun`;
- `Placanje`.

Ovo je korisno jer neuspesno placanje vrati samo gresku, a uspesno placanje vrati i novo stanje racuna.

## `PlacanjeService.svc`

Sadrzi `ServiceHost` direktivu:

```aspx
<%@ ServiceHost Language="C#" Debug="true" Service="WCFPlacanjeTroskovaServer.PlacanjeService" CodeBehind="PlacanjeService.svc.cs" %>
```

Ovo povezuje `.svc` URL sa klasom `PlacanjeService`.

Kada klijent gadja:

```text
http://localhost:55102/PlacanjeService.svc
```

WCF zna koju klasu treba da instancira i koju konfiguraciju da primeni.

## `PlacanjeService.svc.cs` - implementacija

```csharp
public class PlacanjeService : IPlacanjeService
```

Klasa implementira sve metode iz interfejsa.

### Static stanje

```csharp
private static readonly List<Racun> Racuni
private static readonly List<Placanje> Placanja
```

Liste su `static` zato sto predstavljaju zajednicko stanje servisa.

Za lab/ispit je to zamena za bazu. Kada se server restartuje, podaci se vracaju na pocetne vrednosti.

### `LockObj`

```csharp
private static readonly object LockObj = new object();
```

Koristi se kod svih operacija koje citaju ili menjaju zajednicke liste.

Bez lock-a dva klijenta bi mogla istovremeno da plate sa istog racuna i naprave pogresan bilans.

### `DodajRacun`

Metoda prvo radi validacije izvan `lock` bloka:

- da broj racuna nije prazan;
- da naziv nije prazan;
- da pocetni bilans nije negativan.

Zatim u `lock` bloku proverava da li racun vec postoji.

Ako ne postoji, dodaje novi `Racun`.

Bitno: provera duplikata i dodavanje moraju biti u istom `lock` bloku, jer je to jedna kriticna operacija.

### `VratiRacune`

Vraca kopije racuna:

```csharp
return Racuni.Select(CloneRacun).ToList();
```

Server ne vraca direktno objekte iz interne liste, nego kopije.

### `PlatiTrosak`

Ovo je najbitnija metoda.

Prvo proverava:

```csharp
if (iznos <= 0)
```

Iznos mora biti pozitivan.

Zatim u `lock` bloku:

1. trazi racun po broju;
2. ako racun ne postoji, vraca `"Broj racuna nije validan."`;
3. ako bilans nije dovoljan, vraca `"Nema dovoljno novca na racunu."`;
4. ako je sve ispravno, umanjuje bilans;
5. pravi objekat `Placanje`;
6. dodaje placanje u listu;
7. vraca `PlacanjeResult`.

Ovo je vazno: bilans se menja tek nakon svih provera.

### `ListingPlacanja`

Filtrira listu placanja:

```csharp
Placanja.Where(p => p.BrojRacuna == brojRacuna)
```

Vraca samo placanja za taj broj racuna.

### `CloneRacun` i `ClonePlacanje`

Pomocne metode prave kopije objekata.

To je dobar stil jer sprecava da interna server lista bude direktno izlozena.

### `Ok`, `Error`, `PaymentError`

Pomocne metode da se ne ponavlja isti kod za rezultate.

`PaymentError` vraca `PlacanjeResult`, jer metoda `PlatiTrosak` mora uvek da vrati taj tip.

## `Web.config` - server

Najvazniji deo je:

```xml
<system.serviceModel>
```

### Behavior

```xml
<behavior name="PlacanjeServiceBehavior">
  <serviceMetadata httpGetEnabled="true" httpsGetEnabled="true"/>
  <serviceDebug includeExceptionDetailInFaults="false"/>
</behavior>
```

`serviceMetadata` omogucava da klijent vidi WSDL/metadata.

`serviceDebug` kontrolise da li se detalji exceptiona salju klijentu.

### Service

```xml
<service name="WCFPlacanjeTroskovaServer.PlacanjeService"
         behaviorConfiguration="PlacanjeServiceBehavior">
```

`name` mora biti puno ime klase servisa.

### Endpoint

```xml
<endpoint address=""
          binding="basicHttpBinding"
          contract="WCFPlacanjeTroskovaServer.IPlacanjeService" />
```

Ovo je glavni endpoint.

Za ovaj zadatak je `basicHttpBinding` dovoljan, jer:

- nema callbacka;
- nema potrebe da server poziva klijenta;
- svaki poziv je klijent -> server -> odgovor.

### MEX endpoint

```xml
<endpoint address="mex" binding="mexHttpBinding" contract="IMetadataExchange" />
```

Sluzi da Visual Studio moze da generise proxy kod kada se radi Add Service Reference.

## `Program.cs` - klijent

Klijent pravi WCF kanal:

```csharp
var factory = new ChannelFactory<IPlacanjeService>("BasicHttpBinding_IPlacanjeService");
IPlacanjeService proxy = factory.CreateChannel();
```

`BasicHttpBinding_IPlacanjeService` mora da postoji kao `name` endpointa u `App.config`.

### Tok u klijentu

1. Pozove `VratiRacune` i ispise pocetne racune.
2. Pozove `DodajRacun("100-3", "Voda", 2500)`.
3. Pozove uspesno placanje:

```csharp
proxy.PlatiTrosak("100-1", 1200, "Junski racun za struju")
```

4. Ako nema greske, ispise novi bilans.
5. Pozove neuspesno placanje sa prevelikim iznosom.
6. Pozove `ListingPlacanja("100-1")`.
7. Zatvori proxy i factory.

Ovaj klijent je demo, ne kompletan meni. Ali pokriva sve operacije koje zadatak trazi.

## `App.config` - klijent

Najvazniji endpoint:

```xml
<endpoint address="http://localhost:55102/PlacanjeService.svc"
          binding="basicHttpBinding"
          bindingConfiguration="BasicHttpBinding_IPlacanjeService"
          contract="WCFPlacanjeTroskovaServer.IPlacanjeService"
          name="BasicHttpBinding_IPlacanjeService" />
```

Ovo mora da se poklopi sa serverom:

- adresa mora imati dobar port;
- binding mora biti `basicHttpBinding`;
- contract mora biti `IPlacanjeService`;
- name se koristi u `ChannelFactory`.

Ako promenis port servera u Visual Studio/IIS Express, promeni i ovu adresu.

## Sta se ovde uci

- Kako se pravi obican WCF servis bez callbacka.
- Kako se poslovne greske vracaju kroz `OperationResult`.
- Kako se koristi izvedeni rezultat `PlacanjeResult`.
- Kako server cuva zajednicko stanje.
- Zasto se koristi `lock`.
- Zasto validacija mora biti na serveru.
- Kako klijent zatvara WCF kanal.

## Pitanja koja profesor moze da postavi

**Zasto nema callbacka?**

Zato sto server nema potrebu da sam obavestava klijenta. Klijent pita, server odgovara.

**Zasto `basicHttpBinding`?**

Zato sto je dovoljan za osnovni HTTP request/response servis.

**Zasto se koristi `IsError` umesto exceptiona?**

Zato sto su "nema dovoljno novca" i "racun ne postoji" poslovne greske, ne sistemski pad servisa.

**Zasto je lista `static`?**

Da bi svi pozivi i sve instance servisa delili iste racune i placanja.

**Zasto se koristi `lock`?**

Zato sto vise klijenata moze u isto vreme da menja bilans istog racuna.

## Najcesce greske

- Validacija se radi samo u klijentu.
- Bilans se umanji pre provere da li ima dovoljno novca.
- `PlatiTrosak` vraca samo string pa klijent nema novo stanje racuna.
- Zaboravi se `[DataMember]` na `Bilans` ili `Vreme`.
- Port u `App.config` ne odgovara stvarnom portu servisa.
- `ChannelFactory` ime se ne poklapa sa `name` u endpointu.


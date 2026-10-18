# Ultimativna skripta - WCF kviz

Ovo je skracena skripta za brzo citanje pred test. Nema ponudjenih odgovora, nego samo pitanje i direktan odgovor.

## Teorijski uvod

WCF, odnosno Windows Communication Foundation, koristi se za pravljenje distribuiranih aplikacija. To znaci da jedan program, klijent, moze da poziva metode drugog programa, servisa, preko mreze ili lokalnog hostinga.

Najosnovniji WCF tok je:

```text
klijent -> servis -> odgovor
```

To je request/response komunikacija. Primeri za to su `WCFKviz` i `WCFPlacanjeTroskova`.

Kada server treba sam da pozove klijenta, koristi se callback. Tada tok izgleda ovako:

```text
klijent -> servis
servis -> klijent
```

To se zove duplex komunikacija. Primeri su `WCFChat`, `WCFCalculatorDuplex`, `WCFProizvodnja`, `WCFTablic` i `WCFElektronskiDnevnik`.

WCF servis se najcesce opisuje preko interfejsa. Interfejs koji predstavlja servis mora imati `[ServiceContract]`. Metode koje klijent sme da pozove moraju imati `[OperationContract]`.

Ako se kroz servis salje neka klasa, ona treba da ima `[DataContract]`, a property koji se stvarno prenosi treba da ima `[DataMember]`. Ako property nema `[DataMember]`, klijent ga nece dobiti kroz WCF poruku.

Svaki WCF endpoint se pamti kroz tri glavna dela:

```text
address + binding + contract
```

`address` govori gde se servis nalazi.  
`binding` govori kako se komunicira.  
`contract` govori koji interfejs je izlozen klijentu.

Za obican request/response koristi se `basicHttpBinding`. Za callback/duplex koristi se `wsDualHttpBinding`.

`mexHttpBinding` nije poslovni binding. On sluzi za metadata exchange, odnosno da Visual Studio moze da procita opis servisa i napravi/update-uje Service Reference.

`Web.config` je serverska konfiguracija. Tu se podesavaju servis, behavior, binding i endpoint.

`App.config` je klijentska konfiguracija. Tu klijent zna koju adresu, binding i contract da koristi.

Kod callbacka postoje dva interfejsa:

- servisni interfejs - metode koje klijent poziva na serveru;
- callback interfejs - metode koje server poziva na klijentu.

Callback se navodi ovako:

```csharp
[ServiceContract(SessionMode = SessionMode.Required,
                 CallbackContract = typeof(IMojCallback))]
```

Server callback kanal dobija ovako:

```csharp
OperationContext.Current.GetCallbackChannel<IMojCallback>();
```

Ako servis treba da obavesti vise klijenata, mora da cuva listu ili recnik callbackova. Jedan callback je dovoljan samo kada servis odgovara samo trenutnom klijentu, kao kod calculator duplex primera.

`IsOneWay = true` znaci da metoda ne ocekuje odgovor. To je dobro za obavestenja, npr. `Notify`, `Obavestenje`, `Equals`, `Equation`, `Error`.

`InstanceContextMode.PerSession` znaci da svaka klijentska sesija dobija svoju instancu servisa. To je korisno kada servis pamti stanje konkretnog klijenta, npr. ime korisnika, trenutni rezultat kalkulatora, ulogu roditelja/profesora.

`ConcurrencyMode.Reentrant` se cesto koristi kod callback servisa da bi servis mogao da pozove klijenta nazad bez tipicnog blokiranja u duplex komunikaciji.

Kod `wsDualHttpBinding` klijent cesto mora da ima `ClientBaseAddress`, jer server treba adresu na kojoj moze da pozove klijentski callback. Ako vise konzolnih klijenata radi odjednom, svaki treba razlicit callback port.

Najcesci razlog pucanja na `factory.CreateChannel()` je da server nije pokrenut na adresi koju klijent gadja, da port u `App.config` nije dobar, ili da callback klijent nema prava/adresu da otvori svoj callback endpoint.

## Brza mapa projekata

`WCFKviz` je obican request/response servis. Server cuva pitanja, admin vidi tacan odgovor, korisnik kviza ne vidi tacan odgovor. Klijent salje odgovore, server racuna rezultat.

`WCFPlacanjeTroskova` je obican request/response servis. Server cuva racune, bilans i placanja. Placanje sme da uspe samo ako racun postoji, iznos je pozitivan i ima dovoljno novca.

`WCFProizvodnja` je callback servis. Server cuva proizvode i callbackove korisnika. Kada se doda ili promeni proizvod, svi registrovani korisnici dobijaju obavestenje. Proizvod se zakljucava da ga ne menjaju dva korisnika istovremeno.

`WCFTablic` je callback servis sa centralnim stanjem igre. Server uparuje igrace, deli karte, cuva talon, proverava poteze i salje stanje igracima i posmatracima.

`WCFElektronskiDnevnik` je callback servis sa dve uloge. Profesor daje ocenu, roditelj prijavljuje decu i dobija obavestenje kada njegovo dete dobije ocenu.

`WCFChat` je callback servis gde server cuva korisnike i njihove callbackove. Poruka moze ici jednom korisniku ili svima.

`WCFCalculatorDuplex` je duplex kalkulator. Klijent salje operaciju, server menja rezultat i callbackom vraca rezultat, jednacinu ili gresku.

## Pitanja i odgovori

### Sta je WCF?

WCF je tehnologija za izradu distribuiranih aplikacija i servisa, gde klijent poziva metode servisa preko definisanog ugovora.

### Sta je servis u WCF-u?

Servis je serverski deo aplikacije koji iznosi metode dostupne klijentima.

### Sta je klijent u WCF-u?

Klijent je aplikacija koja pravi proxy i poziva metode servisa.

### Sta je `[ServiceContract]`?

To je atribut koji oznacava interfejs kao WCF servisni ugovor.

### Sta je `[OperationContract]`?

To je atribut koji oznacava metodu koja je dostupna klijentu kroz WCF.

### Sta ako metoda nema `[OperationContract]`?

Klijent je ne vidi kao servisnu operaciju, cak i ako metoda postoji u klasi.

### Sta je `[DataContract]`?

To je atribut koji oznacava klasu koja se prenosi kroz WCF.

### Sta je `[DataMember]`?

To je atribut koji oznacava property koji ce biti serijalizovan i poslat kroz WCF poruku.

### Sta ako property nema `[DataMember]`?

Nece biti prenet klijentu kroz WCF poruku.

### Sta je endpoint?

Endpoint je kombinacija adrese, bindinga i contracta.

### Sta je `address`?

Adresa na kojoj je servis dostupan.

### Sta je `binding`?

Nacin komunikacije: protokol, transport, sigurnost i da li postoji callback.

### Sta je `contract`?

Servisni interfejs koji endpoint iznosi klijentu.

### Koji binding ide za obican servis?

`basicHttpBinding`.

### Koji binding ide za callback/duplex?

`wsDualHttpBinding`.

### Cemu sluzi `mexHttpBinding`?

Sluzi za metadata exchange, najcesce za Add/Update Service Reference.

### Da li je MEX poslovna operacija?

Ne. MEX ne radi logiku aplikacije, nego opisuje servis.

### Sta je `.svc` fajl?

Ulazna tacka WCF servisa u IIS/IIS Express hostingu.

### Sta radi `ServiceHost` direktiva u `.svc` fajlu?

Povezuje `.svc` URL sa konkretnom servisnom klasom.

### Sta je `Web.config`?

Serverska konfiguracija WCF servisa.

### Sta je `App.config`?

Klijentska konfiguracija, najcesce adresa i binding servisa koji se poziva.

### Sta znaci `serviceMetadata httpGetEnabled="true"`?

Omogucava da metadata servisa bude dostupna preko HTTP-a.

### Sta znaci `includeExceptionDetailInFaults="false"`?

Detalji server exceptiona se ne salju klijentu, sto je bezbednije.

### Sta je `ChannelFactory<T>`?

Nacin da klijent napravi proxy na osnovu servisnog interfejsa.

### Sta je proxy?

Objekat preko koga klijent poziva servisne metode kao da su lokalne, iako poziv ide preko WCF-a.

### Kada treba Update Service Reference?

Kada promenis servisni ugovor ili data modele, a klijent koristi generisanu Service Reference.

### Da li mora Service Reference ako koristim `ChannelFactory<T>` i project reference?

Ne mora. Tada klijent direktno koristi isti servisni interfejs.

### Sta je callback?

Callback je kada server pozove metodu na klijentu.

### Gde se navodi callback ugovor?

U `[ServiceContract]`, preko `CallbackContract = typeof(...)`.

### Ko implementira callback interfejs?

Klijent.

### Ko poziva callback metode?

Server.

### Kako server dobija callback kanal?

Preko `OperationContext.Current.GetCallbackChannel<T>()`.

### Kada se callback stvarno aktivira?

Kada server eksplicitno pozove neku callback metodu.

### Da li je `SendState()` u kalkulatoru callback?

Ne direktno. `SendState()` je serverska pomocna metoda koja unutra poziva callback metode kao `Equals` i `Equation`.

### Sta znaci `IsOneWay = true`?

Metoda ne ocekuje povratnu vrednost i server ne ceka odgovor.

### Kada se koristi `IsOneWay = true`?

Kod obavestenja i notifikacija, kada je dovoljno samo poslati poruku klijentu.

### Da li `IsOneWay = true` metoda treba da vraca vrednost?

Ne. Uglavnom treba da bude `void`.

### Zasto se cuva lista callbackova?

Zato sto server moze imati vise klijenata koje treba obavestiti.

### Kada je dovoljan jedan callback?

Kada server komunicira samo sa trenutnim klijentom te sesije, kao kod calculator duplex primera.

### Zasto chat ima listu callbackova po korisniku?

Jer isti korisnik moze imati vise otvorenih klijenata/sesija.

### Sta je `InstanceContext` u duplex klijentu?

Objekat kojim klijent daje WCF-u instancu callback handlera.

### Sta je `DuplexChannelFactory<T>`?

Factory za pravljenje proxy-ja kod duplex servisa, gde klijent prosledjuje callback objekat.

### Zasto callback klijent treba `ClientBaseAddress`?

Zato sto server mora znati adresu na kojoj moze pozvati klijentov callback.

### Zasto vise klijenata treba razlicite callback portove?

Da dva procesa ne pokusaju da slusaju isti lokalni callback URL.

### Sta znaci `PerSession`?

Svaka klijentska sesija ima svoju instancu servisa.

### Zasto je `PerSession` bitan kod kalkulatora?

Svaki klijent ima svoj rezultat i svoju jednacinu.

### Zasto je `PerSession` bitan kod chata/proizvodnje/dnevnika/tablica?

Servis pamti podatke konkretnog klijenta, npr. ime, ulogu ili trenutno stanje sesije.

### Sta znaci `Reentrant`?

Omogucava servisnoj instanci da bezbednije udje u callback tok kod duplex komunikacije.

### Zasto callback poziv ide u `try/catch`?

Klijent moze biti ugasen ili veza moze puci, a server ne sme da padne zbog jednog klijenta.

### Koje greske su ceste kod callback kanala?

`CommunicationException` i `TimeoutException`.

### Sta je poslovna greska?

Ocekivana greska iz pravila zadatka, npr. nema dovoljno novca, pogresan potez, ocena nije 1-5.

### Da li je za poslovnu gresku uvek potreban exception?

Ne. Cesto je bolje vratiti `OperationResult` sa `IsError` i `Message`.

### Kada koristiti `FaultException`?

Kada servis zeli da klijentu prijavi WCF fault kao deo servisnog ugovora ili kontrolisane greske.

### Zasto se koristi `OperationResult`?

Da klijent dobije informaciju da li je operacija uspela i poruku za prikaz.

### Zasto se koristi `lock` u servisima?

Zato sto vise klijenata moze istovremeno da menja zajednicko stanje.

### Sta znaci `static` lista u servisu?

Listu dele sve instance servisa u istom procesu.

### Zasto se u lab primerima koristi `static` lista?

Kao jednostavna memorijska zamena za bazu.

### Sta se desi sa `static` listom kada restartujes server?

Podaci se vracaju na pocetno stanje definisano u kodu.

### Zasto server treba da radi validaciju?

Zato sto se klijentu ne veruje; server je autoritet za pravila sistema.

### Da li klijent sme da ima validaciju?

Da, ali samo kao pomoc korisniku. Server i dalje mora da validira.

### Zasto se vracaju kopije objekata?

Da se ne iznose direktne reference na interne objekte servisa.

### Zasto `WCFKviz` ne koristi callback?

Zato sto klijent trazi pitanja i salje odgovore, a server samo vraca rezultat.

### Zasto kviz ima `PitanjeAdmin` i `PitanjeZaKviz`?

Admin sme da vidi tacan odgovor, korisnik koji radi kviz ne sme.

### Sta radi `ValidirajOdgovore`?

Server poredi odgovore korisnika sa tacnim odgovorima i vraca broj tacnih i procenat.

### Zasto se odgovori vezuju preko `PitanjeId`?

Da server zna na koje pitanje se odgovor odnosi, nezavisno od redosleda liste.

### Zasto `WCFPlacanjeTroskova` ne koristi callback?

Zato sto klijent sam trazi dodavanje racuna, placanje i listing, a server samo odgovara.

### Sta mora da proveri `PlatiTrosak`?

Da je iznos pozitivan, da racun postoji i da ima dovoljno novca.

### Zasto bilans proverava server?

Zato sto klijent ne sme sam da odlucuje da li je placanje validno.

### Zasto `WCFProizvodnja` koristi callback?

Zato sto svi korisnici treba da dobiju obavestenje kada se doda ili promeni proizvod.

### Zasto `WCFProizvodnja` cuva recnik callbackova?

Da bi server mogao da obavesti sve registrovane korisnike.

### Sta radi `ZakljucajProizvod`?

Oznacava da konkretan korisnik trenutno ima pravo da menja dati proizvod.

### Koja je razlika izmedju C# `lock` i zakljucavanja proizvoda?

C# `lock` stiti memoriju tokom kratkog izvrsavanja, a zakljucavanje proizvoda je poslovno pravilo ko sme da menja proizvod.

### Kada `PovecajProizvodnju` sme da uspe?

Kada je korisnik registrovan, kolicina pozitivna i proizvod je zakljucao bas taj korisnik.

### Zasto je `WCFTablic` najtezi zadatak?

Zato sto kombinuje callback, centralno stanje, uparivanje igraca, posmatrace i validaciju poteza.

### Sta radi `waitingPlayer`?

Cuva prvog igraca koji ceka drugog igraca.

### Sta radi `activeGame`?

Cuva trenutno aktivnu partiju.

### Zasto postoji samo jedan `activeGame`?

Zato sto zadatak kaze da samo jedna partija moze biti aktivna u jednom trenutku.

### Da li posmatrac sme da odigra potez?

Ne. Posmatrac samo prima stanje.

### Ko proverava legalnost poteza u tablicu?

Server.

### Sta proverava `ValidateTurn`?

Da postoji partija, da korisnik nije posmatrac, da je na potezu i da ima prijavljenu kartu.

### Kako je pojednostavljeno pravilo za `Nosi`?

Zbir vrednosti izabranih karata sa talona mora biti jednak vrednosti karte iz ruke.

### Zasto `StateFor` ne vraca svima isti tekst?

Igrac sme da vidi svoje karte, posmatrac ne sme da vidi privatne karte igraca.

### Zasto `WCFElektronskiDnevnik` koristi callback?

Zato sto roditelj treba da dobije obavestenje kada njegovo dete dobije ocenu.

### Da li dnevnik treba da obavesti sve roditelje?

Ne. Treba obavestiti samo roditelje koji su prijavili to dete.

### Sta cuva `Predmeti`?

Mapu: predmet -> skup ID-eva ucenika koji slusaju predmet.

### Sta cuva `DecaRoditelja`?

Mapu: roditelj -> skup ID-eva njegove dece.

### Sta cuva `Roditelji`?

Mapu: roditelj -> callback kanal.

### Kada `DajOcenu` sme da uspe?

Kada je profesor registrovan, ocena je od 1 do 5, ucenik postoji i dodat je na predmet profesora.

### Sta znaci filter `"SVI"` kod ocena?

Vrati ocene iz svih predmeta za to dete.

### Sta je sustina chat primera?

Server cuva registrovane korisnike i njihove callbackove, pa prosledjuje poruke primaocima.

### Sta radi `MsgHistory` u chatu?

Vraca poruke iz zadatog vremenskog opsega koje pripadaju registrovanom korisniku.

### Sta je sustina calculator duplex primera?

Klijent salje operaciju, server menja rezultat i callbackom vraca rezultat, jednacinu ili gresku.

### Zasto calculator callback ima `Equals`, `Equation` i `Error`?

Server salje razlicite vrste informacija klijentu.

### Sta se desi kod deljenja nulom u calculator primeru?

Server pozove callback `Error`, ne menja rezultat i posalje trenutno stanje.

### Sta prvo proveriti ako obican client ne moze da pozove servis?

Da li server radi, da li je dobar port/adresa u `App.config`, i da li se binding/contract poklapaju.

### Sta prvo proveriti ako callback ne radi?

Da li je binding `wsDualHttpBinding`, da li postoji `CallbackContract`, da li klijent implementira callback i da li ima validan `ClientBaseAddress`.

### Sta prvo proveriti ako puca na `factory.CreateChannel()`?

Da li je server pokrenut na adresi koju klijent gadja i da li klijent ima ispravan endpoint/binding.

### Sta prvo proveriti ako duplex klijent puca odmah na startu?

Pokreni Visual Studio kao Administrator i proveri da callback port nije zauzet.

### Koje fajlove najcesce gledas kada nesto ne radi?

Na serveru `Web.config` i `.svc`; na klijentu `App.config` i `Program.cs`.

### Sta je najbitnija podela pred resavanje zadatka?

Prvo prepoznaj da li je zadatak obican request/response ili callback/duplex.

### Kada biras `basicHttpBinding`?

Kada server samo odgovara na klijentove zahteve.

### Kada biras `wsDualHttpBinding`?

Kada server treba sam da poziva klijenta, odnosno kada postoji callback.

### Sta je najbitnije zapamtiti za ispit?

Ugovor definise sta se poziva, binding definise kako se komunicira, a server uvek mora da cuva i validira pravo stanje sistema.


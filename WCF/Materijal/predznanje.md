# Predznanje za 2. termin: sesije, callback, duplex i matrice

Ovo je dopunjena verzija mojih beleski sa drugog termina. Poenta nije da ucim napamet svaku liniju, nego da tacno znam sta je WCF hteo da postigne u svakom primeru:

- sesija: server pamti stanje za jednog klijenta;
- callback: server moze da pozove klijenta nazad;
- duplex: i klijent i server imaju aktivne ugovore za komunikaciju;
- matrice: standardan WCF zadatak sa `DataContract`, `Result` klasom i obradom gresaka bez pucanja aplikacije.

## 1. Sta je profesor prvo pokusao sa sesijom

Krenuo je od ideje: "hocu da vidim koliko puta je servis startovan / koliko puta je instanca napravljena / sta se desava kada menjam instance mode".

Tu se mesaju dve stvari:

1. Koliko puta je napravljena servisna instanca.
2. Koliko dugo traje komunikaciona sesija izmedju klijenta i servera.

U WCF-u to nisu iste stvari.

### PerCall

`PerCall` znaci: za svaki poziv metode pravi se nova instanca servisne klase.

Primer:

```csharp
[ServiceBehavior(InstanceContextMode = InstanceContextMode.PerCall)]
public class VremeServis : IVremeServis
{
}
```

Ako klijent pozove:

```csharp
proxy.Metoda1();
proxy.Metoda2();
proxy.Metoda3();
```

server moze napraviti tri odvojene instance servisne klase. Zbog toga obicna polja u klasi nisu pouzdana za pamcenje stanja izmedju poziva.

### PerSession

`PerSession` znaci: za jednu WCF sesiju koristi se ista instanca servisne klase.

```csharp
[ServiceBehavior(InstanceContextMode = InstanceContextMode.PerSession)]
public class VremeServis : IVremeServis
{
}
```

Ali ovo samo po sebi nije dovoljno. Mora i ugovor da trazi sesiju:

```csharp
[ServiceContract(SessionMode = SessionMode.Required)]
public interface IVremeServis
{
}
```

I mora binding da podrzava sesiju.

### Zasto `basicHttpBinding` nije radio

Ovo je glavna lekcija iz haoticnog dela casa:

```xml
binding="basicHttpBinding"
```

je najjednostavniji binding i dobar je za prvi primer, ali ne podrzava WCF sesije na nacin koji treba za `SessionMode.Required`.

Zato, ako hocu sesije bez callback-a, koristim:

```xml
binding="wsHttpBinding"
```

Ako hocu callback / duplex, koristim:

```xml
binding="wsDualHttpBinding"
```

### Sta ide zajedno za sesiju

Za obicnu sesiju:

```csharp
[ServiceContract(SessionMode = SessionMode.Required)]
public interface IVremeServis
{
}
```

```csharp
[ServiceBehavior(InstanceContextMode = InstanceContextMode.PerSession)]
public class VremeServis : IVremeServis
{
}
```

```xml
<endpoint address=""
          binding="wsHttpBinding"
          contract="Namespace.IVremeServis" />
```

Ako ostane `basicHttpBinding`, moze da izgleda kao da je sve podeseno u kodu, ali WCF nema transport/protokol koji tu sesiju stvarno podrzava.

## 2. Deo iz `Web.config` koji profesor trazi na ispitu

U beleskama stoji:

```text
<service> ... </service> -> deo koji se trazi od nas na ispitu
```

To znaci da treba da znam endpoint konfiguraciju. Minimalno moram da umem da napisem:

```xml
<services>
  <service name="Namespace.ImeServisneKlase">
    <endpoint address=""
              binding="basicHttpBinding"
              contract="Namespace.ImeInterfejsa" />
  </service>
</services>
```

Za sesiju:

```xml
<endpoint address=""
          binding="wsHttpBinding"
          contract="Namespace.ImeInterfejsa" />
```

Za callback:

```xml
<endpoint address=""
          binding="wsDualHttpBinding"
          contract="Namespace.ImeInterfejsa" />
```

Tri najvaznije reci za endpoint su:

- `address`: gde je servis;
- `binding`: kojim protokolom se komunicira;
- `contract`: koji interfejs se izlozi klijentu.

To je ono staro pravilo: ABC endpoint-a.

## 3. Callback: sta je poenta

Kod obicnog WCF poziva klijent zove server:

```text
klijent -> server
```

Kod callback-a server moze da pozove klijenta nazad:

```text
klijent -> server
server -> klijent
```

Zato se callback zove i duplex komunikacija, jer komunikacija ide u oba smera.

U `Primeri/Chat/Chat/IChat.cs` vidi se kljucna linija:

```csharp
[ServiceContract(CallbackContract = typeof(IChatCallback), SessionMode = SessionMode.Required)]
public interface IChat
{
}
```

Ona znaci:

- `IChat` je ono sto klijent poziva na serveru;
- `IChatCallback` je ono sto server sme da pozove na klijentu;
- `SessionMode.Required` je prakticno obavezno za callback, jer server mora da zna kom klijentu vraca poruku.

Callback interfejs obicno ima sufiks `Callback`:

```csharp
public interface IChatCallback
{
    [OperationContract(IsOneWay = true)]
    void Notify(Message m);
}
```

`IsOneWay = true` znaci: poziv ne ceka povratnu vrednost. Server samo posalje poruku i nastavi dalje.

## 4. Najbitnija callback linija

U beleskama stoji:

```csharp
korisnici[name] = OperationContext.Current.GetCallbackChannel<IChatCallback>();
```

Ovo je sustina callback-a.

Razlozeno:

- `OperationContext.Current` predstavlja trenutni WCF poziv koji je stigao na server.
- Iz tog konteksta server moze da uzme callback kanal za klijenta koji ga je upravo pozvao.
- `GetCallbackChannel<IChatCallback>()` vraca objekat preko kog server moze da zove metode iz `IChatCallback`.
- `korisnici[name] = ...` pamti taj kanal pod korisnickim imenom.

Znaci, kada se korisnik registruje:

```csharp
public void Register(string name)
{
    var callback = OperationContext.Current.GetCallbackChannel<IChatCallback>();
    korisnici[name] = callback;
}
```

server je zapamtio:

```text
"Lazar" -> callback kanal ka Lazarovom klijentu
"Marko" -> callback kanal ka Markovom klijentu
```

Kasnije, kada Lazar posalje poruku Marku, server radi:

```csharp
korisnici["Marko"].Notify(poruka);
```

To je bukvalno server koji poziva klijenta.

## 5. Zasto nekad dictionary cuva listu callback-ova

U jednostavnoj verziji:

```csharp
Dictionary<string, IChatCallback> korisnici;
```

jedan username ima jedan callback kanal.

Problem: isti korisnik moze otvoriti dve aplikacije, dva prozora ili dva klijenta. Tada jedan username treba da ima vise aktivnih callback kanala.

Zato je bolja verzija:

```csharp
Dictionary<string, List<IChatCallback>> korisnici;
```

Tada `korisnici["Lazar"]` nije jedan kanal, nego lista kanala. Kada stigne poruka za Lazara, server moze da obavesti sve njegove aktivne klijente.

U `Primeri/Chat/Chat/Chat.svc.cs` profesor je krenuo ka toj ideji, ali primer nije zavrsen:

- dictionary je deklarisan kao `Dictionary<string, IChatCallback>`;
- kasnije se koristi kao da je `Dictionary<string, List<IChatCallback>>`;
- postoji poziv na `m.Neisporucena`, ali `Message` nema property `Neisporucena`;
- fali nekoliko zagrada i konstruktor liste nije zatvoren.

Znaci, primer je dobar za ideju, ali ne treba ga prepisivati 1:1.

## 6. Kako `Send` radi u chat primeru

Logika za `Send`:

1. Klijent se prvo registrovao i server zna njegov `rname`.
2. Klijent pozove `Send`.
3. Server upise:
   - vreme poruke;
   - ko salje poruku;
   - kome ide poruka;
   - tekst poruke.
4. Server doda poruku u istoriju.
5. Server preko callback-a obavesti primaoca ili sve korisnike.

Konceptualno:

```csharp
public void Send(Message m)
{
    m.Timestamp = DateTime.Now;
    m.From = rname;
    messages.Add(m);

    if (m.To == "SVI")
    {
        foreach (var user in korisnici)
        {
            user.Callback.Notify(m);
        }
    }
    else
    {
        korisnici[m.To].Notify(m);
    }
}
```

U stvarnom kodu treba paziti na:

- `lock`, jer vise klijenata moze istovremeno da salje poruke;
- mrtve callback kanale, jer klijent moze da ugasi aplikaciju;
- da se callback ne poziva dok se drzi lock, ako moze da se izbegne.

## 7. Sta znaci sinhrono, asinhrono i "hibridno" u tvojim beleskama

U beleskama je sustina dobra:

```text
posalji, primio si? to je to, ja sam slobodan
```

To je prakticno `IsOneWay = true`.

Kod klasicnog poziva:

```csharp
int r = proxy.Saberi(1, 2);
```

klijent ceka rezultat.

Kod one-way poziva:

```csharp
proxy.Send(message);
```

klijent ne dobija povratnu vrednost. On samo posalje poruku. Ako server kasnije zeli nesto da javi, to radi preko callback-a:

```csharp
callback.Notify(message);
```

Zato callback deluje kao hibrid:

- poziv do servera postoji;
- server primi zahtev;
- klijent ne ceka rezultat metode;
- odgovor ili obavestenje stize zasebnim pozivom nazad.

## 8. Klijent za callback

U `Primeri/Chat/FormChat/Form1.cs` vidi se kljucna linija:

```csharp
proxy = new ChatClient(new InstanceContext(this));
```

To znaci:

- `Form1` implementira `IChatCallback`;
- `this` je objekat koji zna da primi callback;
- `InstanceContext` pakuje taj objekat za WCF;
- `ChatClient` dobija informaciju gde da isporuci poruke koje server salje nazad.

U console aplikaciji ista ideja izgleda ovako:

```csharp
InstanceContext context = new InstanceContext(new CallbackHandler());
ChatClient proxy = new ChatClient(context);
```

`CallbackHandler` je obicna klasa koja implementira callback interfejs:

```csharp
public class CallbackHandler : IChatCallback
{
    public void Notify(Message m)
    {
        Console.WriteLine(m.Text);
    }
}
```

## 9. Config za callback

Za callback ne koristim `basicHttpBinding`.

Na serveru:

```xml
<bindings>
  <wsDualHttpBinding>
    <binding name="WSDualHttpBinding_IChat">
      <security mode="None"/>
    </binding>
  </wsDualHttpBinding>
</bindings>

<services>
  <service name="WCFChatServer.ChatService">
    <endpoint address=""
              binding="wsDualHttpBinding"
              bindingConfiguration="WSDualHttpBinding_IChat"
              contract="WCFChatServer.IChat" />
  </service>
</services>
```

Na klijentu, ako koristim generisani Service Reference, endpoint izgleda slicno:

```xml
<endpoint address="http://localhost:PORT/ChatService.svc"
          binding="wsDualHttpBinding"
          bindingConfiguration="WSDualHttpBinding_IChat"
          contract="ServiceReferenceChat.IChat"
          name="WSDualHttpBinding_IChat" />
```

Ako se pokrece vise konzolnih klijenata, moze se javiti problem oko callback adrese. Tada je prakticno podesiti `ClientBaseAddress` programatski, da svaki klijent dobije drugi lokalni port.

## 10. Microsoft calculator duplex primer

U beleskama stoji da cesto pada "full duplex service sa CalculatorDuplexCallback". To je direktno vezano za Microsoft Learn primer:

https://learn.microsoft.com/en-us/dotnet/framework/wcf/feature-details/duplex-services

Ideja:

- server ima kalkulator;
- klijent salje operacije: `AddTo`, `SubtractFrom`, `MultiplyBy`, `DivideBy`, `Clear`;
- server pamti trenutni rezultat po sesiji;
- server preko callback-a javlja novi rezultat i jednacinu.

Najbitnije iz tog primera:

```csharp
[ServiceContract(SessionMode = SessionMode.Required,
                 CallbackContract = typeof(ICalculatorDuplexCallback))]
public interface ICalculatorDuplex
{
}
```

Callback:

```csharp
public interface ICalculatorDuplexCallback
{
    [OperationContract(IsOneWay = true)]
    void Equals(double result);

    [OperationContract(IsOneWay = true)]
    void Equation(string equation);
}
```

Server:

```csharp
[ServiceBehavior(InstanceContextMode = InstanceContextMode.PerSession)]
public class CalculatorService : ICalculatorDuplex
{
}
```

Klijent:

```csharp
InstanceContext instanceContext = new InstanceContext(new CallbackHandler());
CalculatorDuplexClient client = new CalculatorDuplexClient(instanceContext);
```

To je isto pravilo kao chat. Samo se ne salju chat poruke, nego rezultati kalkulatora.

## 11. Lokalni primer `Primeri/T2CB`

U `Primeri/T2CB/T2CB/ICalculator.cs` postoji dobar kostur:

- `ICalculator`;
- `ICalculatorDuplexCallback`;
- `CallbackContract`;
- `SessionMode.Required`;
- `IsOneWay = true` za operacije.

U `Calculator.svc.cs` postoji dobra ideja:

- `result` se pamti po sesiji;
- `equation` pamti tekst operacije;
- posle svake operacije server poziva callback:

```csharp
Callback.Equals(result);
Callback.Equation(equation);
```

Sta bih doradio:

- ne bih radio klasicni exception za deljenje nulom;
- dodao bih callback metodu `Error(string message)`;
- za `DivideBy(0)` bih poslao `Callback.Error("Deljenje nulom nije dozvoljeno.")`;
- ne bih menjao `result` ako operacija nije validna.

To je bolje za vezbu, jer pokazuje da servis kontrolisano javlja gresku klijentu.

## 12. Matrice: sta se trazi u zadatku

Tekst zadatka kaze:

```text
Koristeci WCF napisati sistem za rad aritmetickih operacija nad matricama.
```

Operacije:

- setovanje matrice na serverskoj strani;
- sabiranje;
- mnozenje matrice skalarom;
- mnozenje matrice matricom;
- transponovanje matrice;
- preuzimanje matrice sa servisa.

Posebno bitno:

```text
Operacije kao jedan od operanada uzimaju matricu koja je setovana na serverskoj strani.
```

Znaci:

1. Klijent prvo posalje matricu serveru:

```csharp
proxy.Set(m);
```

2. Server tu matricu pamti.
3. Kasnije operacije koriste tu zapamcenu matricu kao prvi operand.

Primer:

```csharp
proxy.Set(A);
Result r = proxy.Add(B);
```

To znaci:

```text
r = A + B
```

Ako radim:

```csharp
Result r = proxy.Multiply(B);
```

to znaci:

```text
r = A * B
```

## 13. `Result` struktura u matricama

Zadatak jasno trazi zajednicki rezultat:

- flag da li je doslo do greske;
- opis greske;
- rezultat operacije ako je uspesno izvrsena.

Zato je dobra struktura:

```csharp
[DataContract]
public class Result
{
    [DataMember]
    public bool IsError { get; set; }

    [DataMember]
    public string ErrorMessage { get; set; }

    [DataMember]
    public Matrica Matrica { get; set; }
}
```

Ovo je prakticno zamena za exception u ovom zadatku. Ako dimenzije nisu dobre, servis ne puca, nego vrati:

```csharp
new Result
{
    IsError = true,
    ErrorMessage = "Za sabiranje matrice moraju imati isti broj redova i kolona.",
    Matrica = null
};
```

Klijent onda samo proveri:

```csharp
if (result.IsError)
{
    Console.WriteLine(result.ErrorMessage);
}
```

## 14. Sta je bilo lose u nezavrsenom `WCFServiceMatrice`

Pre dorade, projekat je imao ove probleme:

- `.svc` fajl je pokazivao na `WcfService1.Service1`, a stvarna klasa je `ServiceMatrice`;
- `Set` nije implementiran;
- `Take` nije implementiran;
- `Multiply` je racunao pogresno jer je koristio istu matricu i za operand i za rezultat;
- `Transpond` nije radio transponovanje, nego je sabirao simetricne elemente;
- kada serverska matrica nije setovana, neke metode su vracale rezultat bez jasne greske;
- klijent je bio prazan;
- config nije imao eksplicitno definisan servis i endpoint.

Zavrsena verzija sada ima:

- `IServiceMatrice` kao ugovor;
- `ServiceMatrice` kao implementaciju;
- `Matrica` sa `Rows`, `Columns`, `Values`;
- `Result` za sve operacije;
- `basicHttpBinding` endpoint;
- konzolni klijent koji zove svaku operaciju.

## 15. Sta obavezno znam za lab

Ako dobijem WCF zadatak na labu, redosled mi je:

1. Napravim `.NET Framework` solution, ne `.NET Core`.
2. Server je `WCF Service Application`.
3. Klijent je `Console App (.NET Framework)`.
4. U serveru prvo pisem interfejs:
   - `[ServiceContract]`;
   - `[OperationContract]`;
   - `[DataContract]`;
   - `[DataMember]`.
5. Zatim pisem servisnu klasu koja implementira interfejs.
6. Proverim `.svc`:
   - `Service="Namespace.ServisnaKlasa"`.
7. Proverim `Web.config`:
   - `service name`;
   - `endpoint binding`;
   - `contract`.
8. Ako je obican servis:
   - `basicHttpBinding`.
9. Ako je sesija:
   - `SessionMode.Required`;
   - `InstanceContextMode.PerSession`;
   - `wsHttpBinding`.
10. Ako je callback:
    - `CallbackContract`;
    - `SessionMode.Required`;
    - `wsDualHttpBinding`;
    - `InstanceContext` u klijentu.
11. U klijentu dodam `Service Reference` ili rucno napravim `ChannelFactory`.
12. Testiram redom:
    - server se otvara u browseru;
    - WSDL radi;
    - klijent moze da napravi proxy;
    - svaka operacija ima primer poziva;
    - greske se ispisuju kontrolisano.

## 16. Kratka mapa binding-a

```text
basicHttpBinding
  - najjednostavniji
  - dobar za prvi servis
  - ne za WCF sesije/callback

wsHttpBinding
  - koristi se za sesije
  - radi sa SessionMode.Required
  - nije duplex sam po sebi

wsDualHttpBinding
  - koristi se za callback/duplex
  - server moze da zove klijenta
  - klijent mora da ima InstanceContext
```

## 17. Kratka mapa atributa

```csharp
[ServiceContract]
```

Interfejs je WCF servisni ugovor.

```csharp
[ServiceContract(SessionMode = SessionMode.Required)]
```

Servis zahteva sesiju.

```csharp
[ServiceContract(CallbackContract = typeof(IMojCallback), SessionMode = SessionMode.Required)]
```

Servis je duplex i ima callback.

```csharp
[OperationContract]
```

Metoda se vidi klijentu.

```csharp
[OperationContract(IsOneWay = true)]
```

Metoda nema povratnu vrednost i pozivalac ne ceka odgovor.

```csharp
[ServiceBehavior(InstanceContextMode = InstanceContextMode.PerSession)]
```

Jedna servisna instanca po sesiji.

```csharp
[DataContract]
[DataMember]
```

Klase i property-ji koji se salju kroz WCF poruke.

## 18. Najbitnije recenice koje treba da mogu da kazem na ispitu

Callback je mehanizam u WCF-u gde servis, pored toga sto prima pozive od klijenta, moze da pozove metode na klijentu preko callback contract-a.

Duplex servis ima dva ugovora: servisni ugovor koji klijent poziva i callback ugovor koji server poziva na klijentu.

Za callback se koristi `CallbackContract` u `[ServiceContract]`, potreban je `SessionMode.Required`, a u konfiguraciji se najcesce koristi `wsDualHttpBinding`.

Klijent kod duplex servisa mora da prosledi `InstanceContext`, jer WCF mora da zna koji objekat na klijentu prima callback pozive.

`basicHttpBinding` je dobar za osnovni servis, ali nije dobar za callback i sesije koje se traze ovde.

Kod zadatka sa matricama, server pamti jednu matricu, a svaka operacija kao prvi operand koristi tu serversku matricu. Rezultat se vraca kroz zajednicku `Result` klasu, a greske se ne bacaju kao obican exception nego se vracaju kroz `IsError` i `ErrorMessage`.

## 19. TODO - kojim redom da ucim

1. Prvo ponovi `Materijal/kreiraj_projekat.md` i svoj `WcfVremeSolution`, jer je to osnova: contract, service, endpoint, service reference, proxy.
2. Zatim procitaj odeljke o sesiji u ovom fajlu: `PerCall`, `PerSession`, `SessionMode.Required`, `wsHttpBinding`.
3. Posle toga otvori `Materijal/WCFChat_uputstvo.md` i precrtaj sebi tok: `Register -> GetCallbackChannel -> Send -> Notify`.
4. Zatim prodji `Materijal/WCFCalculatorDuplex_uputstvo.md`, jer je to najcistiji duplex obrazac: service contract + callback contract + `InstanceContext`.
5. Posle callback-a otvori zavrseni `WCFServiceMatrice` projekat i pokreni server pa klijent.
6. Citaj `Materijal/matrice_objasnjenje.md` paralelno sa kodom: prvo `IServiceMatrice.cs`, zatim `ServiceMatrice.svc.cs`, zatim `Program.cs`.
7. Za lab vezbu vezbaj da sam napises kostur:
   - interfejs sa `[ServiceContract]`;
   - metode sa `[OperationContract]`;
   - podatke sa `[DataContract]` i `[DataMember]`;
   - `Web.config` endpoint;
   - klijentski proxy poziv.
8. Ako zapne callback, vrati se na tri linije:

```csharp
[ServiceContract(CallbackContract = typeof(IMojCallback),
                 SessionMode = SessionMode.Required)]
OperationContext.Current.GetCallbackChannel<IMojCallback>();
new MojClient(new InstanceContext(new CallbackHandler()));
```

To je srz drugog termina.

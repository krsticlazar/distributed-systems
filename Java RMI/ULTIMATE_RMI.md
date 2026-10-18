# Ultimativni Java RMI vodič za ispit

Ovaj dokument objedinjuje beleške, prezentacije, zadatke iz blanketa i obrasce iz postojećih projekata. Fokus je isključivo na onome što treba napisati u ispitnoj svesci. Kod je namerno jednostavan i prati stil sa računskih vežbi.

> **Pravilo za ceo dokument:** u ispitnoj svesci ne pišu se importi, kompletni `try-catch` blokovi, validacija unosa, meniji, konfiguracija ni kod za pokretanje procesa. Profesoru treba pokazati da pravilno prepoznaješ remote interfejs, remote implementaciju, serijalizovani tip, callback, serversko stanje, registry i ključne pozive klijenta.

## Teorijski uvod

Java RMI, odnosno **Remote Method Invocation**, omogućava da objekat iz jedne JVM poziva metode objekta koji se nalazi u drugoj JVM. Te JVM mogu da budu na istom ili na različitim računarima.

Osnovni učesnici su:

- **server**, koji kreira i čuva udaljene objekte;
- **klijent**, koji poziva metode udaljenih objekata;
- **RMI registry**, koji povezuje simboličko ime sa udaljenom referencom;
- **stub**, lokalni proxy na strani klijenta preko kog se obavlja udaljeni poziv.

Klijent ne dobija pravi serverski objekat. Poziv metode nad stubom RMI infrastruktura pakuje, šalje serveru, izvršava nad pravim objektom i vraća rezultat. Stub i serverski deo komunikacije ne pišu se ručno.

### Remote interfejs i implementacija

Remote interfejs predstavlja ugovor između klijenta i servera:

```java
public interface Servis extends Remote {
    public void metoda() throws RemoteException;
}
```

Pravila:

- interfejs nasleđuje `Remote`;
- svaka udaljena metoda ima `throws RemoteException`;
- klijent radi sa tipom interfejsa, nikada sa `ServisImpl`;
- tipovi parametara i povratnih vrednosti moraju biti primitivni, `Serializable` ili `Remote`.

Serverska implementacija najčešće izgleda ovako:

```java
public class ServisImpl extends UnicastRemoteObject implements Servis {
    public ServisImpl() throws RemoteException {
        super();
    }

    public synchronized void metoda() throws RemoteException {
    }
}
```

`UnicastRemoteObject` izvozi objekat, odnosno omogućava da prima udaljene pozive. Konstruktor zato prijavljuje `RemoteException`. Metoda implementacije mora biti `public`, a `synchronized` se koristi kada više klijenata može da menja isto serversko stanje.

### Registry, rebind i lookup

Server kreira registry i registruje glavni remote objekat:

```java
LocateRegistry.createRegistry(1099);
Naming.rebind("rmi://localhost:1099/Servis", new ServisImpl());
```

Klijent traži objekat pod istim imenom:

```java
Servis s = (Servis) Naming.lookup("rmi://localhost:1099/Servis");
```

URL je oblika:

```text
rmi://hostname:port/name
```

`1099` je podrazumevani port registra. Ako zadatak zada drugi port, mora se koristiti taj port. `bind` prvi put vezuje ime i baca izuzetak ako ono već postoji, dok `rebind` postavlja novu referencu i eventualno zamenjuje staru.

### Remote naspram Serializable objekta

Ako klijent treba da poziva metode nad pravim objektom koji ostaje na serveru, objekat je `Remote`. Prenosi se udaljena referenca.

Ako se objekat samo šalje kao podatak, implementira `Serializable`. Prenosi se kopija objekta, odnosno prenos po vrednosti.

Primeri:

- `Match` je `Remote` kada klijent poziva `addHomeGoal()` nad serverskom utakmicom;
- `Stadium` je `Serializable` kada se klijentu samo šalju naziv i grad;
- `Poruka` je `Serializable` kada se naslov i sadržaj prenose primaocu;
- primitivni tipovi i `String` prenose se po vrednosti.

### Callback

Callback je udaljeni poziv u obrnutom smeru: **server poziva klijenta**. Koristi se kada zadatak kaže da se klijent pretplaćuje, da mora odmah biti obavešten ili da server vraća rezultat čim nastane.

Postupak je:

1. Definiše se callback interfejs koji nasleđuje `Remote`.
2. Klijent pravi callback implementaciju koja nasleđuje `UnicastRemoteObject`.
3. Klijent prosleđuje callback referencu serveru kroz `subscribe` ili `register`.
4. Server čuva callback reference u kolekciji.
5. Kada nastane događaj, server poziva callback metode.

Callback uklanja potrebu da klijent neprekidno ispituje server, odnosno da radi polling.

### Šta se očekuje na papiru

Na ispitu treba prikazati:

- remote i callback interfejse;
- serializable tipove koji su bitni za prenos;
- početak implementacionih klasa i traženu poslovnu logiku;
- registry i `rebind`;
- `lookup` i ključne klijentske pozive;
- razlog zbog kog je objekat `Remote`, `Serializable` ili callback.

Ne treba pisati importe, validaciju, kompletne menije, `try-catch`, `Scanner`, `System.in.read()`, formatiranje ispisa i konfiguracione fajlove.

---

## Zadatak 1: Generisanje prostih brojeva - april 2026.

### Originalni tekst zadatka

> Korišćenjem Java RMI-a, napisati serversku aplikaciju koja za primljena 2 prirodna broja N i M, prosleđena od strane klijenta, ima za cilj da generiše sve proste brojeve između brojeva N i M, pri čemu svaki od elemenata vraća klijentu čim se element izgeneriše. Napisati klijentsku aplikaciju koja će minimalno demonstrirati funkcionisanje sistema.

### Kratka analiza zahteva

Glavni remote objekat je `Generator`. Brojevi `N` i `M` su obični `int` parametri i prenose se po vrednosti.

Najbitniji deo teksta je: **svaki element se vraća klijentu čim se izgeneriše**. Metoda zato ne treba da vrati `List<Integer>` tek na kraju. Potreban je callback kroz koji server šalje svaki pronađeni prost broj klijentu.

Potrebni delovi su:

- remote interfejs `Generator`;
- remote callback interfejs `Callback`;
- serverska implementacija `GeneratorImpl`;
- klijentska implementacija callback-a;
- registry na podrazumevanom portu `1099`.

### Kod za ispitnu svesku

#### Remote interfejsi

```java
public interface Generator extends Remote {
    public void generisiProsteBrojeve(int n, int m, Callback cb)
            throws RemoteException;
}

public interface Callback extends Remote {
    public void prostBrojGenerisan(int broj) throws RemoteException;
}
```

#### Serverska implementacija

```java
public class GeneratorImpl extends UnicastRemoteObject implements Generator {
    public GeneratorImpl() throws RemoteException {
        super();
    }

    public void generisiProsteBrojeve(int n, int m, Callback cb)
            throws RemoteException {
        for (int i = n; i <= m; i++) {
            if (prost(i)) {
                cb.prostBrojGenerisan(i);
            }
        }
    }

    private boolean prost(int broj) {
        if (broj < 2) {
            return false;
        }

        for (int i = 2; i * i <= broj; i++) {
            if (broj % i == 0) {
                return false;
            }
        }

        return true;
    }
}
```

#### Server

```java
LocateRegistry.createRegistry(1099);
Generator g = new GeneratorImpl();
Naming.rebind("rmi://localhost:1099/Generator", g);
```

#### Minimalni klijent i callback

```java
public class CallbackImpl extends UnicastRemoteObject implements Callback {
    public CallbackImpl() throws RemoteException {
        super();
    }

    public void prostBrojGenerisan(int broj) throws RemoteException {
        System.out.println(broj);
    }
}

Generator g = (Generator) Naming.lookup(
        "rmi://localhost:1099/Generator");
Callback cb = new CallbackImpl();
g.generisiProsteBrojeve(10, 30, cb);
```

### Detaljno objašnjenje

`Generator` nasleđuje `Remote` zato što njegovu metodu poziva klijent iz druge JVM. Metoda prijavljuje `RemoteException`, jer udaljeni poziv može da ne uspe zbog mreže, servera ili serijalizacije.

`Callback` je takođe remote interfejs. Njegova implementacija se nalazi kod klijenta, ali server dobija udaljenu referencu i poziva `prostBrojGenerisan`.

Povratni tip metode `generisiProsteBrojeve` je `void`. Rezultati se ne vraćaju standardnom povratnom vrednošću, nego pojedinačnim callback pozivima. Tako klijent dobija broj odmah kada ga server pronađe.

`GeneratorImpl` nasleđuje `UnicastRemoteObject`, čime njegova instanca postaje udaljeno dostupna. Metoda `prost` je privatna pomoćna metoda i ne pripada remote interfejsu, jer je klijent ne poziva.

Klijent prvo dobija stub `Generator` objekta preko `lookup`, zatim izvozi svoj `CallbackImpl` objekat i njegovu remote referencu šalje serveru kao argument.

### Obavezno napisati i naglasiti

- `Generator extends Remote`.
- `Callback extends Remote`.
- Obe udaljene metode imaju `throws RemoteException`.
- `GeneratorImpl extends UnicastRemoteObject`.
- Klijent implementira i izvozi callback objekat.
- Server za svaki prost broj poziva `cb.prostBrojGenerisan(i)`.
- Callback se koristi zato što se brojevi vraćaju jedan po jedan, odmah po generisanju.

---

## Zadatak 2: MQTT broker - jun 2024.

### Originalni tekst zadatka

> Korišćenjem Java RMI tehnologije napisati simulaciju MQTT brokera. MQTT broker je server koji razmenjuje poruke između klijenata korišćenjem topika. Topik predstavlja string na osnovu koga se poruke (definisane naslovom i sadržajem) filtriraju i dostavljaju određenim klijentima. Neophodno je implementirati metodu subscribe kako bi se klijent pretplatio na poruke pristigle na određeni topik, kao i metodu publish koja omogućava korisniku da pošalje poruku na željeni topik. Klijent ne kreira topik eksplicitno, već se topik, ukoliko ne postoji, kreira implicitno pozivom metode createTopic. Napisati serversku aplikaciju za hostovanje MQTT brokera i registrovati ga u RMI registar, kao i klijentsku aplikaciju koja će minimalno demonstrirati funkcionisanje istog.

### Kratka analiza zahteva

`Broker` je glavni remote objekat. `Poruka` sadrži samo naslov i sadržaj, pa se prenosi kao `Serializable` kopija.

Pretplaćeni klijent mora naknadno da primi poruku kada drugi klijent pozove `publish`. Zato je potreban callback interfejs `MqttClient`.

Server može da čuva pretplate u kolekciji:

```java
Map<String, List<MqttClient>>
```

Ključ je naziv topika, a vrednost lista callback referenci pretplaćenih klijenata. `createTopic` je interna serverska metoda i poziva se iz `subscribe` i `publish`, pa klijent ne kreira topik eksplicitno.

### Kod za ispitnu svesku

#### Serializable poruka

```java
public class Poruka implements Serializable {
    private String naslov;
    private String sadrzaj;

    public Poruka(String naslov, String sadrzaj) {
        this.naslov = naslov;
        this.sadrzaj = sadrzaj;
    }

    public String getNaslov() {
        return naslov;
    }

    public String getSadrzaj() {
        return sadrzaj;
    }
}
```

#### Remote interfejsi

```java
public interface Broker extends Remote {
    public void subscribe(String topic, MqttClient klijent)
            throws RemoteException;

    public void publish(String topic, Poruka poruka)
            throws RemoteException;
}

public interface MqttClient extends Remote {
    public void primiPoruku(String topic, Poruka poruka)
            throws RemoteException;
}
```

#### Serverska implementacija

```java
public class BrokerImpl extends UnicastRemoteObject implements Broker {
    private Map<String, List<MqttClient>> topici = new HashMap<>();

    public BrokerImpl() throws RemoteException {
        super();
    }

    private void createTopic(String topic) {
        topici.putIfAbsent(topic, new ArrayList<MqttClient>());
    }

    public synchronized void subscribe(String topic, MqttClient klijent)
            throws RemoteException {
        createTopic(topic);
        topici.get(topic).add(klijent);
    }

    public synchronized void publish(String topic, Poruka poruka)
            throws RemoteException {
        createTopic(topic);

        for (MqttClient klijent : topici.get(topic)) {
            klijent.primiPoruku(topic, poruka);
        }
    }
}
```

#### Server

```java
LocateRegistry.createRegistry(1099);
Broker broker = new BrokerImpl();
Naming.rebind("rmi://localhost:1099/Broker", broker);
```

#### Minimalni klijenti

```java
public class MqttClientImpl extends UnicastRemoteObject
        implements MqttClient {

    public MqttClientImpl() throws RemoteException {
        super();
    }

    public void primiPoruku(String topic, Poruka poruka)
            throws RemoteException {
        System.out.println(topic + ": "
                + poruka.getNaslov() + " - " + poruka.getSadrzaj());
    }
}

Broker broker = (Broker) Naming.lookup(
        "rmi://localhost:1099/Broker");

MqttClient cb = new MqttClientImpl();
broker.subscribe("sport", cb);

broker.publish("sport",
        new Poruka("Gol", "Domaci tim vodi 1:0"));
```

### Detaljno objašnjenje

`Poruka` je `Serializable` zato što klijent i server ne pozivaju njene metode udaljeno. Naslov i sadržaj kopiraju se između JVM procesa.

`Broker` je remote ugovor servera. `subscribe` prima callback referencu klijenta, dok `publish` prima serijalizovanu poruku.

`MqttClient` je callback interfejs. Klijent ga implementira i prosleđuje brokeru. Broker kasnije poziva `primiPoruku`, čime se poruka dostavlja u smeru server → klijent.

`Map<String, List<MqttClient>>` prirodno predstavlja MQTT pretplate. Svaki topik ima svoju listu klijenata. `putIfAbsent` kreira listu samo ako topik još ne postoji, što ispunjava zahtev o implicitnom kreiranju.

`publish` pronalazi listu za zadati topik i svakom pretplaćenom klijentu poziva callback metodu.

Klijent koji se pretplaćuje mora ostati aktivan da bi njegov callback objekat mogao da prima pozive. Na papiru nije potrebno pisati kod za čekanje.

### Obavezno napisati i naglasiti

- `Broker` je glavni remote interfejs.
- `MqttClient` je callback remote interfejs.
- `Poruka implements Serializable`.
- Pretplate se čuvaju kao topik → lista callback objekata.
- `createTopic` koristi `putIfAbsent` i poziva se interno.
- `subscribe` dodaje klijenta na odgovarajući topik.
- `publish` obaveštava samo klijente pretplaćene na dati topik.
- Callback je potreban jer poruke stižu nakon pretplate.

---

## Zadatak 3: Praćenje fudbalskih rezultata - kolokvijum I 2024.

### Originalni tekst zadatka

> Napisati RMI kôd koji implementira udaljeni pristup serveru za praćenje rezultata fudbalskih utakmica.
>
> Implementirati klasu Match sa atributima (int id, String homeTeam, String awayTeam, int homeGoals, int awayGoals, Stadium stadium – sa atributima String name i String city). Metode klase Match treba da omoguće korisniku da doda gol domaćem, odnosno gostujućem timu, i da preuzme stadion na kome se igra utakmica. Pored toga, potrebno je implemenirati udaljenu metodu koja će vratiti String literal koji će sadržati id i trenutni rezultat utakmice. Potrebno je omogućiti korisnicima da se pretplate na promenu rezultata utakmice. Callback interfejs sadrži popis udaljene metode resultChanged(int matchId).
>
> Klasa FootballScore treba omogućiti klijentima da pozovu metodu koja vraća String literal koji će sadržati identifikatore i rezultate svih utakmica koje postoje u sistemu, kao i metodu za preuzimanje jedne utakmice. U konstruktoru klase kreirati dva objekta klase Match, čije su vrednosti identifikatora 1 i 2, respektivno, i smestiti ih u odgovarajuću strukturu podataka.
>
> Implementirati serversku klasu Server koja kreira objekat klase FootballScore i upisuje ga u RMI registar na portu 4096. Kôd klijentske klase ClientUser treba da prikaže sve utakmice, naziv stadiona na kome se igra utakmica sa id-jem 2, da se pretplati na događaje utakmice sa id-jem 1, kao i da implementira Callback interfejs tako da korisnik bude u toku sa rezultatom utakmice. Kôd klijentske klase ClientAdmin treba da doda gol domaćem timu utakmice sa id-jem 1.

### Kratka analiza zahteva

`FootballScore` je glavni remote objekat i ulazna tačka sistema. On čuva utakmice i vraća jednu utakmicu na osnovu identifikatora.

`Match` takođe mora biti remote. Klijent dobija utakmicu od `FootballScore` i poziva metode `addHomeGoal`, `addAwayGoal`, `getStadium`, `getResult` i `subscribe` nad pravim serverskim objektom.

`Stadium` je `Serializable`, jer se klijentu šalju naziv i grad kao kopija. Klijent ne menja udaljeno stanje stadiona.

`Callback` je remote interfejs koji implementira `ClientUser`. `MatchImpl` čuva callback reference i poziva `resultChanged(id)` kada se promeni rezultat.

### Kod za ispitnu svesku

#### Serializable stadion

```java
public class Stadium implements Serializable {
    private String name;
    private String city;

    public Stadium(String name, String city) {
        this.name = name;
        this.city = city;
    }

    public String getName() {
        return name;
    }

    public String getCity() {
        return city;
    }
}
```

#### Remote interfejsi

```java
public interface Callback extends Remote {
    public void resultChanged(int matchId) throws RemoteException;
}

public interface Match extends Remote {
    public void addHomeGoal() throws RemoteException;
    public void addAwayGoal() throws RemoteException;
    public Stadium getStadium() throws RemoteException;
    public String getResult() throws RemoteException;
    public void subscribe(Callback cb) throws RemoteException;
}

public interface FootballScore extends Remote {
    public String getAllResults() throws RemoteException;
    public Match getMatch(int id) throws RemoteException;
}
```

#### Implementacija utakmice

```java
public class MatchImpl extends UnicastRemoteObject implements Match {
    private int id;
    private String homeTeam;
    private String awayTeam;
    private int homeGoals;
    private int awayGoals;
    private Stadium stadium;
    private List<Callback> callbacks = new ArrayList<>();

    public MatchImpl(int id, String homeTeam, String awayTeam,
            Stadium stadium) throws RemoteException {
        super();
        this.id = id;
        this.homeTeam = homeTeam;
        this.awayTeam = awayTeam;
        this.stadium = stadium;
    }

    public void addHomeGoal() throws RemoteException {
        homeGoals++;

        for (Callback cb : callbacks) {
            cb.resultChanged(id);
        }
    }

    public void addAwayGoal() throws RemoteException {
        awayGoals++;

        for (Callback cb : callbacks) {
            cb.resultChanged(id);
        }
    }

    public synchronized Stadium getStadium() throws RemoteException {
        return stadium;
    }

    public synchronized String getResult() throws RemoteException {
        return id + ": " + homeTeam + " " + homeGoals
                + " - " + awayGoals + " " + awayTeam;
    }

    public synchronized void subscribe(Callback cb)
            throws RemoteException {
        callbacks.add(cb);
    }
}
```

#### Glavni remote objekat

```java
public class FootballScoreImpl extends UnicastRemoteObject
        implements FootballScore {

    private Map<Integer, Match> matches = new LinkedHashMap<>();

    public FootballScoreImpl() throws RemoteException {
        super();
        matches.put(1, new MatchImpl(1, "Radnicki", "Partizan",
                new Stadium("Cair", "Nis")));
        matches.put(2, new MatchImpl(2, "Zvezda", "Vojvodina",
                new Stadium("Rajko Mitic", "Beograd")));
    }

    public synchronized String getAllResults() throws RemoteException {
        String rezultat = "";

        for (Match match : matches.values()) {
            rezultat += match.getResult() + "\n";
        }

        return rezultat;
    }

    public synchronized Match getMatch(int id) throws RemoteException {
        return matches.get(id);
    }
}
```

#### Server

```java
LocateRegistry.createRegistry(4096);
FootballScore fs = new FootballScoreImpl();
Naming.rebind("rmi://localhost:4096/FootballScore", fs);
```

#### ClientUser i njegov callback

```java
public class CallbackImpl extends UnicastRemoteObject
        implements Callback {

    private FootballScore footballScore;

    public CallbackImpl(FootballScore footballScore)
            throws RemoteException {
        super();
        this.footballScore = footballScore;
    }

    public void resultChanged(int matchId) throws RemoteException {
        Match match = footballScore.getMatch(matchId);
        System.out.println(match.getResult());
    }
}

FootballScore fs = (FootballScore) Naming.lookup(
        "rmi://localhost:4096/FootballScore");

System.out.println(fs.getAllResults());
System.out.println(fs.getMatch(2).getStadium().getName());

Callback cb = new CallbackImpl(fs);
fs.getMatch(1).subscribe(cb);
```

#### ClientAdmin

```java
FootballScore fs = (FootballScore) Naming.lookup(
        "rmi://localhost:4096/FootballScore");
Match match = fs.getMatch(1);
match.addHomeGoal();
```

### Detaljno objašnjenje

`FootballScore` služi za pronalaženje utakmica. Njegova metoda `getMatch` vraća tip `Match`, a ne `MatchImpl`, jer se preko mreže prenosi remote referenca definisana interfejsom.

`MatchImpl` ostaje na serveru. Kada `ClientAdmin` pozove `addHomeGoal`, menja se stvarno serversko polje `homeGoals`, a ne lokalna kopija.

`Stadium` se vraća po vrednosti. Promena dobijenog objekta na klijentu ne bi promenila stadion na serveru, što je prihvatljivo jer zadatak traži samo čitanje.

Svaki `MatchImpl` ima sopstvenu listu callback objekata. Tako se korisnik može pretplatiti baš na utakmicu sa id-jem `1`.

Kod dodavanja gola menja se rezultat, a zatim se poziva `resultChanged(id)` svakog pretplaćenog klijenta. Callback na osnovu id-ja ponovo traži utakmicu i ispisuje novi rezultat.

`LinkedHashMap` čuva utakmice po identifikatoru i zadržava redosled ubacivanja prilikom prikaza svih rezultata.

### Obavezno napisati i naglasiti

- `FootballScore` i `Match` su remote interfejsi.
- `Stadium implements Serializable`.
- `Callback extends Remote` i sadrži tačan potpis `resultChanged(int matchId)`.
- `FootballScoreImpl` u konstruktoru kreira dve utakmice sa id `1` i `2`.
- `MatchImpl` menja rezultat na serveru i obaveštava svoje pretplatnike.
- Registry mora biti na portu `4096`.
- `ClientUser` prikazuje sve utakmice, stadion utakmice `2` i pretplaćuje se na utakmicu `1`.
- `ClientAdmin` poziva `addHomeGoal()` nad utakmicom `1`.

---

## Zadatak 4: Praćenje cena akcija - kolokvijum I 2026.

### Originalni tekst zadatka

> Korišćenjem Java RMI tehnologije napisati simulaciju sistema za praćenje cena akcija. Implementirati klasu Stock sa atributima (int Id, String companyName, double price). Klasa treba da ima metodu koja vraća String literal sa Id-jem, nazivom kompanije i trenutnom cenom. Implementirati klasu StockService koja klijentima omogućava:
>
> a. Metodu koja vraća listu svih akcija u sistemu.  
> b. Metodu za promenu cene akcije po Id-ju.  
> c. Pretplatu klijenata na promene cene akcija. Callback interfejs sadrži potpis udaljene metode priceChanged(int Id, double newPrice).
>
> U konstruktoru klase StockService kreirati dva objekta klase Stock i smestiti ih u odgovarajuću strukturu podataka. Implementirati serversku klasu Server koja registruje StockService u RMI registar na portu 5099. Klijentska klasa Client treba da prikaže sve akcije, pretplati se na promene, implementira callback interfejs i ispiše obaveštenje pri promeni cene. Klijentska klasa AdminClient treba da promeni cenu jedne od akcija.

### Kratka analiza zahteva

`StockService` je glavni remote objekat. On vraća listu akcija, menja cenu po identifikatoru i registruje callback klijente.

`Stock` može i treba da bude `Serializable` u ovom rešenju. Tekst ne traži da klijent poziva udaljenu metodu za promenu direktno nad `Stock` objektom. Promena se vrši kroz `StockService.changePrice(id, newPrice)`, dok klijent listu akcija dobija kao skup kopija za prikaz.

Callback je obavezan jer klijent treba da bude obavešten kada administrator promeni cenu. Pretplata je globalna, a `priceChanged` prenosi identifikator akcije i novu cenu.

### Kod za ispitnu svesku

#### Serializable akcija

```java
public class Stock implements Serializable {
    private int id;
    private String companyName;
    private double price;

    public Stock(int id, String companyName, double price) {
        this.id = id;
        this.companyName = companyName;
        this.price = price;
    }

    public int getId() {
        return id;
    }

    public void setPrice(double price) {
        this.price = price;
    }

    public String getInfo() {
        return id + " " + companyName + " " + price;
    }
}
```

#### Remote interfejsi

```java
public interface Callback extends Remote {
    public void priceChanged(int id, double newPrice)
            throws RemoteException;
}

public interface StockService extends Remote {
    public List<Stock> getAllStocks() throws RemoteException;

    public void changePrice(int id, double newPrice)
            throws RemoteException;

    public void subscribe(Callback cb) throws RemoteException;
}
```

#### Serverska implementacija

```java
public class StockServiceImpl extends UnicastRemoteObject
        implements StockService {

    private List<Stock> stocks = new ArrayList<>();
    private List<Callback> callbacks = new ArrayList<>();

    public StockServiceImpl() throws RemoteException {
        super();
        stocks.add(new Stock(1, "Kompanija A", 100.0));
        stocks.add(new Stock(2, "Kompanija B", 200.0));
    }

    public synchronized List<Stock> getAllStocks()
            throws RemoteException {
        return new ArrayList<>(stocks);
    }

    public synchronized void subscribe(Callback cb)
            throws RemoteException {
        callbacks.add(cb);
    }

    public synchronized void changePrice(int id, double newPrice)
            throws RemoteException {
        for (Stock stock : stocks) {
            if (stock.getId() == id) {
                stock.setPrice(newPrice);

                for (Callback cb : callbacks) {
                    cb.priceChanged(id, newPrice);
                }

                return;
            }
        }
    }
}
```

#### Server

```java
LocateRegistry.createRegistry(5099);
StockService service = new StockServiceImpl();
Naming.rebind("rmi://localhost:5099/StockService", service);
```

#### Client i callback

```java
public class CallbackImpl extends UnicastRemoteObject
        implements Callback {

    public CallbackImpl() throws RemoteException {
        super();
    }

    public void priceChanged(int id, double newPrice)
            throws RemoteException {
        System.out.println("Akcija " + id
                + " ima novu cenu " + newPrice);
    }
}

StockService service = (StockService) Naming.lookup(
        "rmi://localhost:5099/StockService");

for (Stock stock : service.getAllStocks()) {
    System.out.println(stock.getInfo());
}

Callback cb = new CallbackImpl();
service.subscribe(cb);
```

#### AdminClient

```java
StockService service = (StockService) Naming.lookup(
        "rmi://localhost:5099/StockService");
service.changePrice(1, 125.5);
```

### Detaljno objašnjenje

`StockService` je remote zato što klijenti udaljeno pozivaju njegove metode. `StockServiceImpl` čuva jedino pravo stanje cena na serveru.

`Stock` je `Serializable`, pa `getAllStocks` vraća kopije akcija. Klijent ih koristi samo za prikaz. Čak i ako bi promenio cenu lokalne kopije, serversko stanje se ne bi promenilo.

Serverska cena menja se isključivo metodom `changePrice`. Metoda pronalazi akciju po id-ju, postavlja novu cenu i zatim poziva sve registrovane callback objekte.

`Callback` mora biti remote zato što ga server poziva u drugoj JVM. Parametri `int` i `double` prenose se po vrednosti.

Na ispitu je dovoljno pokazati da se nakon promene cene prolazi kroz listu callback objekata i poziva `priceChanged`.

Port `5099` i ime `StockService` moraju biti isti u `rebind` i `lookup`.

### Obavezno napisati i naglasiti

- `StockService extends Remote`.
- `Callback extends Remote` sa tačnim potpisom `priceChanged(int id, double newPrice)`.
- `Stock implements Serializable`.
- Konstruktor servisa kreira dve akcije.
- `getAllStocks` vraća listu akcija.
- `changePrice` menja cenu na serveru i poziva callback objekte.
- `Client` prikazuje akcije i registruje callback.
- `AdminClient` menja cenu jedne akcije.
- Registry radi na portu `5099`.

---

## Zadatak 5: Online enciklopedija - jun 2026.

### Originalni tekst zadatka

> Korišćenjem Java RMI tehnologije implementirati simulaciju online enciklopedije.
> Klasa `Encyclopedia` treba da čuva informacije o svim člancima koji postoje u
> sistemu. Potrebno je implementirati metodu `getArticlesTitles()` koja će korisniku
> vratiti `String` literal sa naslovima svih članaka u sistemu, kao i metodu
> `getArticleByTitle(String title)` koja korisniku treba vratiti članak sa prosleđenim
> naslovom.
>
> U konstruktoru klase `Encyclopedia` kreirati dva članka i smestiti ih u
> odgovarajuću strukturu podataka. Članak je definisan klasom `Article`
> (`private String title`, `private String content`,
> `private LocalDateTime lastUpdated`) koja treba da omogući korisniku da preuzme
> informacije o članku metodom `String info()`, kao i da promeni sadržaj članka
> metodom `updateContent(String newContent)`.
>
> Napisati serversku aplikaciju za hostovanje enciklopedije i registrovati je u RMI
> registar na portu `5000`, kao i klijentsku aplikaciju koja će minimalno
> demonstrirati funkcionisanje iste.

### Kratka analiza zahteva

`Encyclopedia` je glavni remote objekat preko kog klijent dobija naslove i pronalazi
članak. Članci se čuvaju u mapi jer se traže prema naslovu.

`Article` takođe mora biti remote objekat. Klijent nad dobijenim člankom poziva
`updateContent`, pa promena mora da se izvrši nad originalnim objektom na serveru.
Kada bi `Article` bio `Serializable`, klijent bi dobio kopiju i promenio samo lokalni
objekat.

Callback nije potreban jer server ne šalje spontana obaveštenja klijentu. Klijent
sam poziva metode enciklopedije i članka.

### Kod za ispitnu svesku

#### Remote interfejs članka

```java
public interface IArticle extends Remote {
    public String info() throws RemoteException;

    public void updateContent(String newContent)
            throws RemoteException;
}
```

#### Remote implementacija članka

```java
public class Article extends UnicastRemoteObject
        implements IArticle {

    private String title;
    private String content;
    private LocalDateTime lastUpdated;

    public Article(String title, String content)
            throws RemoteException {
        super();
        this.title = title;
        this.content = content;
        this.lastUpdated = LocalDateTime.now();
    }

    public synchronized String info() throws RemoteException {
        return title + "\n" + content + "\n" + lastUpdated;
    }

    public synchronized void updateContent(String newContent)
            throws RemoteException {
        content = newContent;
        lastUpdated = LocalDateTime.now();
    }
}
```

#### Remote interfejs enciklopedije

```java
public interface IEncyclopedia extends Remote {
    public String getArticlesTitles() throws RemoteException;

    public IArticle getArticleByTitle(String title)
            throws RemoteException;
}
```

#### Serverska implementacija enciklopedije

```java
public class Encyclopedia extends UnicastRemoteObject
        implements IEncyclopedia {

    private Map<String, IArticle> articles = new HashMap<>();

    public Encyclopedia() throws RemoteException {
        super();

        articles.put("Java RMI",
                new Article("Java RMI",
                        "Udaljeni pozivi metoda."));

        articles.put("Distribuirani sistemi",
                new Article("Distribuirani sistemi",
                        "Sistem sa vise povezanih racunara."));
    }

    public synchronized String getArticlesTitles()
            throws RemoteException {
        return String.join(", ", articles.keySet());
    }

    public synchronized IArticle getArticleByTitle(String title)
            throws RemoteException {
        return articles.get(title);
    }
}
```

#### Server

```java
public class Server {
    public static void main(String[] args) throws Exception {
        LocateRegistry.createRegistry(5000);

        IEncyclopedia encyclopedia = new Encyclopedia();

        Naming.rebind(
                "rmi://localhost:5000/Encyclopedia",
                encyclopedia);
    }
}
```

#### Klijent

```java
public class Client {
    public static void main(String[] args) throws Exception {
        IEncyclopedia encyclopedia =
                (IEncyclopedia) Naming.lookup(
                    "rmi://localhost:5000/Encyclopedia");

        System.out.println(encyclopedia.getArticlesTitles());

        IArticle article =
                encyclopedia.getArticleByTitle("Java RMI");

        System.out.println(article.info());
        article.updateContent("Novi sadrzaj clanka.");
        System.out.println(article.info());
    }
}
```

### Detaljno objašnjenje

`IEncyclopedia` predstavlja ugovor između klijenta i glavnog serverskog objekta.
Nasleđuje `Remote`, a obe udaljene metode prijavljuju `RemoteException`.

`Encyclopedia` nasleđuje `UnicastRemoteObject`, implementira udaljeni interfejs i u
konstruktoru kreira dva tražena članka. `Map` povezuje naslov sa udaljenom referencom
na članak, pa `getArticleByTitle` može direktno da pronađe odgovarajući objekat.

`IArticle` je poseban remote interfejs. Metoda `getArticleByTitle` zato vraća
`IArticle`, odnosno udaljenu referencu, a ne serijalizovanu kopiju članka.

`Article` čuva tražena privatna polja. `info` formira tekst sa svim informacijama, a
`updateContent` menja sadržaj i postavlja vreme poslednje izmene. Metode su
`synchronized` jer više klijenata može istovremeno pristupati istom članku.

Server kreira RMI registry na portu `5000`, pravi enciklopediju i registruje je pod
imenom `Encyclopedia`. Klijent koristi istu adresu u `lookup`, prikazuje naslove,
uzima jedan udaljeni članak, prikazuje ga, menja njegov sadržaj i ponovo ga prikazuje.

### Obavezno napisati i naglasiti

- `IEncyclopedia` i `IArticle` su `public Remote` interfejsi.
- Sve udaljene metode su `public` i imaju `throws RemoteException`.
- `Encyclopedia` i `Article` nasleđuju `UnicastRemoteObject`.
- `Encyclopedia` u konstruktoru kreira dva članka i čuva ih u mapi prema naslovu.
- `getArticlesTitles` vraća jedan `String` sa naslovima.
- `getArticleByTitle` vraća `IArticle`, odnosno udaljenu referencu.
- `Article` nije `Serializable`, jer `updateContent` mora menjati serverski objekat.
- `updateContent` menja i sadržaj i `lastUpdated`.
- Registry radi na portu `5000`.
- Klijent demonstrira `lookup`, ispis naslova, `info`, `updateContent` i ponovni
  `info`.

---

# Dodatni zadaci za samostalnu vežbu

## Zadatak 6: Elektronsko glasanje

### Tekst zadatka - prvo pokušaj sam

Korišćenjem Java RMI tehnologije napisati sistem za elektronsko glasanje. Glavni remote objekat `Glasanje` treba da omogući registrovanom biraču da glasa za kandidata prosleđivanjem jedinstvenog broja birača i identifikatora kandidata. Jedan birač može glasati samo jednom. Metoda vraća informaciju da li je glas uspešno evidentiran.

Omogućiti preuzimanje rezultata za jednog kandidata. Rezultat sadrži identifikator kandidata, broj glasova i trenutno mesto kandidata i prenosi se klijentu kao podatak. U konstruktoru serverske implementacije kreirati tri kandidata sa identifikatorima `1`, `2` i `3`. Napisati server koji registruje servis na portu `1099` i klijenta koji glasa i prikazuje rezultat kandidata.

### Rešenje za ispitnu svesku

#### Kratka analiza zahteva

`Glasanje` je jedini remote objekat. Klijent sve operacije izvršava kroz njega.

`Rezultat` je `Serializable`, jer predstavlja snapshot broja glasova i plasmana u trenutku poziva.

Callback nije potreban. Klijent dobija rezultat tek kada ga sam zatraži.

Za serversko stanje prirodno se koriste:

- `Map<Integer, Integer>` za kandidat → broj glasova;
- `Set<String>` za jedinstvene brojeve birača koji su već glasali.

#### Serializable rezultat

```java
public class Rezultat implements Serializable {
    private int kandidatId;
    private int brojGlasova;
    private int mesto;

    public Rezultat(int kandidatId, int brojGlasova, int mesto) {
        this.kandidatId = kandidatId;
        this.brojGlasova = brojGlasova;
        this.mesto = mesto;
    }

    public String toString() {
        return kandidatId + ": " + brojGlasova
                + " glasova, mesto " + mesto;
    }
}
```

#### Remote interfejs

```java
public interface Glasanje extends Remote {
    public boolean glasaj(String biracId, int kandidatId)
            throws RemoteException;

    public Rezultat vratiRezultat(int kandidatId)
            throws RemoteException;
}
```

#### Serverska implementacija

```java
public class GlasanjeImpl extends UnicastRemoteObject
        implements Glasanje {

    private Map<Integer, Integer> glasovi = new LinkedHashMap<>();
    private Set<String> glasali = new HashSet<>();

    public GlasanjeImpl() throws RemoteException {
        super();
        glasovi.put(1, 0);
        glasovi.put(2, 0);
        glasovi.put(3, 0);
    }

    public synchronized boolean glasaj(String biracId,
            int kandidatId) throws RemoteException {

        if (glasali.contains(biracId)
                || !glasovi.containsKey(kandidatId)) {
            return false;
        }

        glasali.add(biracId);
        glasovi.put(kandidatId, glasovi.get(kandidatId) + 1);
        return true;
    }

    public synchronized Rezultat vratiRezultat(int kandidatId)
            throws RemoteException {

        int broj = glasovi.get(kandidatId);
        int mesto = 1;

        for (int drugiBroj : glasovi.values()) {
            if (drugiBroj > broj) {
                mesto++;
            }
        }

        return new Rezultat(kandidatId, broj, mesto);
    }
}
```

#### Server i klijent

```java
LocateRegistry.createRegistry(1099);
Naming.rebind("rmi://localhost:1099/Glasanje",
        new GlasanjeImpl());
```

```java
Glasanje g = (Glasanje) Naming.lookup(
        "rmi://localhost:1099/Glasanje");

boolean uspeh = g.glasaj("BIRAC-100", 2);
System.out.println(uspeh);
System.out.println(g.vratiRezultat(2));
```

### Detaljno objašnjenje

`GlasanjeImpl` čuva pravo stanje glasanja na serveru. `synchronized` sprečava da dva istovremena poziva naruše proveru i upis glasa.

`Set` je odgovarajuća kolekcija za birače jer brzo proverava da li jedinstveni broj već postoji. Nakon uspešnog glasanja broj se dodaje u skup.

`Map` omogućava pronalaženje broja glasova po identifikatoru kandidata. Operacija `put(id, get(id) + 1)` povećava postojeću vrednost.

Plasman kandidata dobija se brojanjem kandidata koji imaju više glasova. `Rezultat` se zatim šalje klijentu po vrednosti.

Callback nije potreban jer tekst ne traži da server sam obavesti klijenta o promeni.

### Obavezno napisati i naglasiti

- `Glasanje extends Remote`.
- `Rezultat implements Serializable`.
- `Map` čuva glasove, a `Set` birače koji su već glasali.
- Metoda `glasaj` je `synchronized`.
- Isti birač ne može glasati dva puta.
- Serversko stanje se menja samo kroz remote servis.
- Callback nije potreban.

---

## Zadatak 7: Digitalna biblioteka

### Tekst zadatka - prvo pokušaj sam

Korišćenjem Java RMI tehnologije napraviti sistem digitalne biblioteke. Klasa `Book` predstavlja knjigu sa atributima `id`, `title`, `author` i `available`. Korisniku treba omogućiti da nad udaljenom knjigom pozove metode `borrow`, `returnBook` i `getInfo`. Metoda `borrow` vraća `false` ako je knjiga već iznajmljena.

Glavni remote objekat `Library` treba da vrati udaljenu referencu na knjigu po identifikatoru i listu podataka o svim knjigama. Podaci za prikaz jedne knjige predstavljeni su klasom `BookInfo` i prenose se po vrednosti. U konstruktoru serverske implementacije kreirati dve knjige. Registrovati biblioteku na portu `2099`. Klijent treba da prikaže knjige, preuzme knjigu sa id-jem `1`, iznajmi je i prikaže novo stanje.

### Rešenje za ispitnu svesku

#### Kratka analiza zahteva

`Library` je glavni remote objekat koji pronalazi knjige. `Book` je dodatni remote objekat jer klijent direktno poziva metode koje menjaju serversko stanje konkretne knjige.

`BookInfo` je `Serializable` snapshot za prikaz. Promena dobijenog `BookInfo` objekta ne sme da promeni serversku knjigu.

Callback nije potreban, jer server ne obaveštava klijenta samostalno.

#### Serializable podaci o knjizi

```java
public class BookInfo implements Serializable {
    private int id;
    private String title;
    private String author;
    private boolean available;

    public BookInfo(int id, String title, String author,
            boolean available) {
        this.id = id;
        this.title = title;
        this.author = author;
        this.available = available;
    }

    public String toString() {
        return id + " " + title + " " + author
                + " dostupna=" + available;
    }
}
```

#### Remote interfejsi

```java
public interface Book extends Remote {
    public boolean borrow() throws RemoteException;
    public void returnBook() throws RemoteException;
    public BookInfo getInfo() throws RemoteException;
}

public interface Library extends Remote {
    public Book getBook(int id) throws RemoteException;
    public List<BookInfo> getAllBooks() throws RemoteException;
}
```

#### Implementacija knjige

```java
public class BookImpl extends UnicastRemoteObject implements Book {
    private int id;
    private String title;
    private String author;
    private boolean available = true;

    public BookImpl(int id, String title, String author)
            throws RemoteException {
        super();
        this.id = id;
        this.title = title;
        this.author = author;
    }

    public synchronized boolean borrow() throws RemoteException {
        if (!available) {
            return false;
        }

        available = false;
        return true;
    }

    public synchronized void returnBook() throws RemoteException {
        available = true;
    }

    public synchronized BookInfo getInfo() throws RemoteException {
        return new BookInfo(id, title, author, available);
    }
}
```

#### Implementacija biblioteke

```java
public class LibraryImpl extends UnicastRemoteObject
        implements Library {

    private Map<Integer, Book> books = new LinkedHashMap<>();

    public LibraryImpl() throws RemoteException {
        super();
        books.put(1, new BookImpl(1, "Na Drini cuprija",
                "Ivo Andric"));
        books.put(2, new BookImpl(2, "Dervis i smrt",
                "Mesa Selimovic"));
    }

    public synchronized Book getBook(int id)
            throws RemoteException {
        return books.get(id);
    }

    public synchronized List<BookInfo> getAllBooks()
            throws RemoteException {
        List<BookInfo> rezultat = new ArrayList<>();

        for (Book book : books.values()) {
            rezultat.add(book.getInfo());
        }

        return rezultat;
    }
}
```

#### Server i klijent

```java
LocateRegistry.createRegistry(2099);
Naming.rebind("rmi://localhost:2099/Library",
        new LibraryImpl());
```

```java
Library library = (Library) Naming.lookup(
        "rmi://localhost:2099/Library");

for (BookInfo info : library.getAllBooks()) {
    System.out.println(info);
}

Book book = library.getBook(1);
System.out.println(book.borrow());
System.out.println(book.getInfo());
```

### Detaljno objašnjenje

`LibraryImpl` čuva remote knjige u mapi. Metoda `getBook` vraća interfejs `Book`, pa klijent dobija stub knjige, a ne serijalizovanu kopiju `BookImpl` objekta.

Kada klijent pozove `book.borrow()`, metoda se izvršava na `BookImpl` objektu na serveru. Zato svi naredni klijenti vide da knjiga više nije dostupna.

`BookInfo` je odvojen tip namenjen samo prikazu. `getInfo` svaki put pravi novi snapshot trenutnog stanja i vraća ga po vrednosti.

`synchronized` u `borrow` čini proveru i promenu `available` jednom atomskom operacijom. Dva klijenta zato ne mogu istovremeno uspešno iznajmiti istu knjigu.

### Obavezno napisati i naglasiti

- `Library` i `Book` su remote interfejsi.
- `BookInfo implements Serializable`.
- `Library.getBook` vraća `Book`, ne `BookImpl`.
- `BookImpl.borrow` atomski proverava i menja dostupnost.
- `getInfo` vraća serializable snapshot.
- Klijent menja pravo serversko stanje pozivom nad remote knjigom.
- Callback nije potreban.

---

## Zadatak 8: Sistem redova čekanja

### Tekst zadatka - prvo pokušaj sam

Korišćenjem Java RMI tehnologije napraviti sistem za upravljanje redom čekanja. Klijent poziva metodu `takeTicket`, prosleđuje svoje ime i callback objekat, a server mu vraća serijalizovani objekat `Ticket` sa brojem i imenom klijenta. Servis treba da omogući prikaz trenutnog reda i pozivanje sledećeg klijenta.

Kada administrator pozove `callNext`, server uklanja prvi tiket iz reda i preko callback metode `ticketCalled(int number)` obaveštava odgovarajućeg klijenta. Registrovati servis na portu `3099`. Napisati minimalni korisnički klijent koji uzima tiket i prima obaveštenje, kao i administratorski klijent koji poziva sledeći tiket.

### Rešenje za ispitnu svesku

#### Kratka analiza zahteva

`QueueService` je glavni remote objekat. `Ticket` je `Serializable`, jer se klijentu vraćaju broj i ime kao podaci.

`QueueCallback` je remote callback interfejs. Klijent ga prosleđuje kada uzima tiket, a server ga čuva dok taj tiket ne dođe na red.

Za red čekanja koristi se `Queue<Ticket>`, a za povezivanje broja tiketa sa klijentom `Map<Integer, QueueCallback>`.

#### Serializable tiket

```java
public class Ticket implements Serializable {
    private int number;
    private String clientName;

    public Ticket(int number, String clientName) {
        this.number = number;
        this.clientName = clientName;
    }

    public int getNumber() {
        return number;
    }

    public String toString() {
        return number + " - " + clientName;
    }
}
```

#### Remote interfejsi

```java
public interface QueueCallback extends Remote {
    public void ticketCalled(int number) throws RemoteException;
}

public interface QueueService extends Remote {
    public Ticket takeTicket(String clientName, QueueCallback cb)
            throws RemoteException;

    public String getQueue() throws RemoteException;

    public void callNext() throws RemoteException;
}
```

#### Serverska implementacija

```java
public class QueueServiceImpl extends UnicastRemoteObject
        implements QueueService {

    private Queue<Ticket> queue = new LinkedList<>();
    private Map<Integer, QueueCallback> callbacks = new HashMap<>();
    private int nextNumber = 1;

    public QueueServiceImpl() throws RemoteException {
        super();
    }

    public synchronized Ticket takeTicket(String clientName,
            QueueCallback cb) throws RemoteException {

        Ticket ticket = new Ticket(nextNumber++, clientName);
        queue.offer(ticket);
        callbacks.put(ticket.getNumber(), cb);
        return ticket;
    }

    public synchronized String getQueue() throws RemoteException {
        return queue.toString();
    }

    public synchronized void callNext() throws RemoteException {
        Ticket ticket = queue.poll();

        if (ticket == null) {
            return;
        }

        QueueCallback cb = callbacks.remove(ticket.getNumber());

        if (cb != null) {
            cb.ticketCalled(ticket.getNumber());
        }
    }
}
```

#### Server

```java
LocateRegistry.createRegistry(3099);
Naming.rebind("rmi://localhost:3099/QueueService",
        new QueueServiceImpl());
```

#### Korisnički klijent i callback

```java
public class QueueCallbackImpl extends UnicastRemoteObject
        implements QueueCallback {

    public QueueCallbackImpl() throws RemoteException {
        super();
    }

    public void ticketCalled(int number) throws RemoteException {
        System.out.println("Prozvan je tiket " + number);
    }
}

QueueService service = (QueueService) Naming.lookup(
        "rmi://localhost:3099/QueueService");

QueueCallback cb = new QueueCallbackImpl();
Ticket ticket = service.takeTicket("Marko", cb);
System.out.println(ticket);
```

#### Administratorski klijent

```java
QueueService service = (QueueService) Naming.lookup(
        "rmi://localhost:3099/QueueService");
service.callNext();
```

### Detaljno objašnjenje

`Ticket` se vraća kao kopija. Klijentu je potreban samo broj koji je dobio i njegovo ime, pa tiket nema razlog da bude remote objekat.

Callback jeste remote, jer server kasnije mora da pozove objekat koji živi u klijentskoj JVM.

`Queue.offer` dodaje tiket na kraj reda, a `Queue.poll` uklanja i vraća prvi tiket. Time se dobija FIFO ponašanje.

Mapa callback objekata omogućava da server, nakon što iz reda uzme tiket, pronađe tačno onog klijenta kog treba da obavesti.

`callNext` uzima prvi tiket, pronalazi njegov callback i obaveštava odgovarajućeg klijenta.

### Obavezno napisati i naglasiti

- `QueueService extends Remote`.
- `QueueCallback extends Remote`.
- `Ticket implements Serializable`.
- `takeTicket` prima callback i vraća tiket.
- `Queue` obezbeđuje FIFO redosled pomoću `offer` i `poll`.
- `Map` povezuje broj tiketa i callback klijenta.
- `callNext` uklanja prvi tiket i obaveštava pravog klijenta.
- Korisnički klijent mora ostati aktivan nakon registracije callback-a.

---

## Puskica

Sledeći blok objedinjuje obrasce koji se najčešće prilagođavaju konkretnom tekstu zadatka. Na ispitu se ne prepisuje ceo blok, već samo delovi potrebni za dati zadatak.

```java
// Server poziva ovaj remote interfejs na klijentu.
interface Callback extends Remote {
    void changed(int id, String novoStanje) throws RemoteException;
}

// Remote objekat ostaje na serveru i prenosi se njegova referenca.
interface UdaljeniObjekat extends Remote {
    void promeni(String novoStanje) throws RemoteException;
    String vratiStanje() throws RemoteException;
}

// Glavni remote servis je ulazna tacka sistema.
interface Servis extends Remote {
    UdaljeniObjekat vratiObjekat(int id) throws RemoteException;
    Podatak vratiPodatak(int id) throws RemoteException;
    List<Podatak> vratiSve() throws RemoteException;
    void register(Callback cb) throws RemoteException;
    void unregister(Callback cb) throws RemoteException;
}

// Serializable objekat se prenosi po vrednosti.
class Podatak implements Serializable {
    private int id;
    private String vrednost;

    Podatak(int id, String vrednost) {
        this.id = id;
        this.vrednost = vrednost;
    }

    int getId() {
        return id;
    }

    String getVrednost() {
        return vrednost;
    }

    public String toString() {
        return id + " " + vrednost;
    }
}

class UdaljeniObjekatImpl extends UnicastRemoteObject
        implements UdaljeniObjekat {

    private int id;
    private String stanje;
    private List<Callback> callbacks;

    UdaljeniObjekatImpl(int id, String stanje,
            List<Callback> callbacks) throws RemoteException {
        super();
        this.id = id;
        this.stanje = stanje;
        this.callbacks = callbacks;
    }

    public synchronized void promeni(String novoStanje)
            throws RemoteException {
        stanje = novoStanje;

        for (Callback cb : callbacks) {
            cb.changed(id, novoStanje);
        }
    }

    public synchronized String vratiStanje()
            throws RemoteException {
        return stanje;
    }
}

class ServisImpl extends UnicastRemoteObject implements Servis {
    private Map<Integer, UdaljeniObjekat> objekti =
            new LinkedHashMap<>();
    private Map<Integer, Podatak> podaci = new HashMap<>();
    private Map<String, List<Callback>> pretplate = new HashMap<>();
    private List<Callback> callbacks = new ArrayList<>();
    private Set<String> obradjeniKljucevi = new HashSet<>();
    private Queue<Podatak> red = new LinkedList<>();

    ServisImpl() throws RemoteException {
        super();

        objekti.put(1, new UdaljeniObjekatImpl(
                1, "pocetno", callbacks));
        podaci.put(1, new Podatak(1, "vrednost"));
    }

    public synchronized UdaljeniObjekat vratiObjekat(int id)
            throws RemoteException {
        return objekti.get(id);
    }

    public synchronized Podatak vratiPodatak(int id)
            throws RemoteException {
        Podatak p = podaci.get(id);
        return p == null ? null
                : new Podatak(p.getId(), p.getVrednost());
    }

    public synchronized List<Podatak> vratiSve()
            throws RemoteException {
        return new ArrayList<>(podaci.values());
    }

    public synchronized void register(Callback cb)
            throws RemoteException {
        callbacks.add(cb);
    }

    public synchronized void unregister(Callback cb)
            throws RemoteException {
        callbacks.remove(cb);
    }

    synchronized void operacijeNadMapom(int id, Podatak p) {
        podaci.put(id, p);
        podaci.putIfAbsent(id, p);
        boolean postoji = podaci.containsKey(id);
        Podatak pronadjen = podaci.get(id);

        for (Podatak element : podaci.values()) {
            System.out.println(element);
        }

        if (postoji && pronadjen != null) {
            podaci.remove(id);
        }
    }

    synchronized void subscribe(String topic, Callback cb) {
        pretplate.computeIfAbsent(
                topic, k -> new ArrayList<>()).add(cb);
    }

    synchronized void operacijeNadSkupom(String kljuc) {
        boolean prviPut = obradjeniKljucevi.add(kljuc);
        boolean postoji = obradjeniKljucevi.contains(kljuc);

        if (!prviPut && postoji) {
            obradjeniKljucevi.remove(kljuc);
        }
    }

    synchronized void operacijeNadRedom(Podatak p) {
        red.offer(p);
        Podatak prviBezUklanjanja = red.peek();
        Podatak prviSaUklanjanjem = red.poll();

        if (prviBezUklanjanja == prviSaUklanjanjem) {
            System.out.println("FIFO");
        }
    }
}

// Ova implementacija zivi u klijentskoj JVM.
class CallbackImpl extends UnicastRemoteObject
        implements Callback {

    CallbackImpl() throws RemoteException {
        super();
    }

    public void changed(int id, String novoStanje)
            throws RemoteException {
        System.out.println(id + " " + novoStanje);
    }
}

class ServerSablon {
    public static void main(String[] args) throws Exception {
        LocateRegistry.createRegistry(1099);
        Servis servis = new ServisImpl();

        // bind se koristi kada ime jos nije registrovano.
        Naming.bind(
                "rmi://localhost:1099/ServisPrviPut", servis);

        // rebind postavlja ili zamenjuje postojecu registraciju.
        Naming.rebind(
                "rmi://localhost:1099/Servis", servis);
    }
}

class KlijentSablon {
    public static void main(String[] args) throws Exception {
        Servis servis = (Servis) Naming.lookup(
                "rmi://localhost:1099/Servis");

        Callback cb = new CallbackImpl();
        servis.register(cb);

        UdaljeniObjekat objekat = servis.vratiObjekat(1);
        objekat.promeni("novo");
        System.out.println(objekat.vratiStanje());

        Podatak kopija = servis.vratiPodatak(1);
        List<Podatak> sveKopije = servis.vratiSve();

        System.out.println(kopija);
        System.out.println(sveKopije);

        servis.unregister(cb);
    }
}
```

## Bare minimum

Ovo je kostur koji donosi sigurne poene. Svaki prikazani `public` tip se u pravom
projektu nalazi u posebnom `.java` fajlu, ali se u ispitnoj svesci pisu samo trazeni
delovi. Menjaju se imena, parametri, povratni tipovi i poslovna logika. Importi,
`try/catch`, unos podataka, meniji i konfiguracija se ne pisu.

```java
// Pise se samo ako se slozeni objekat prenosi po vrednosti.
public class Podatak implements Serializable {
    private String vrednost;

    public Podatak(String vrednost) {
        this.vrednost = vrednost;
    }

    public String getVrednost() {
        return vrednost;
    }
}

// Pise se samo ako server treba naknadno da obavestava klijenta.
public interface Callback extends Remote {
    public void obavesti(String poruka) throws RemoteException;
}

public interface Servis extends Remote {
    public String operacija(Podatak podatak) throws RemoteException;

    // Ove dve metode se dodaju samo kada zadatak trazi callback.
    public void registruj(Callback callback) throws RemoteException;
    public void odjavi(Callback callback) throws RemoteException;
}

public class ServisImpl extends UnicastRemoteObject implements Servis {
    private List<Callback> callbacks = new ArrayList<>();

    public ServisImpl() throws RemoteException {
        super();
    }

    public synchronized String operacija(Podatak podatak)
            throws RemoteException {
        String rezultat = podatak.getVrednost();

        // Ovaj deo se dodaje samo kada zadatak trazi callback.
        for (Callback callback : callbacks) {
            callback.obavesti(rezultat);
        }

        return rezultat;
    }

    public synchronized void registruj(Callback callback)
            throws RemoteException {
        callbacks.add(callback);
    }

    public synchronized void odjavi(Callback callback)
            throws RemoteException {
        callbacks.remove(callback);
    }
}

// Callback implementacija se nalazi kod klijenta.
public class CallbackImpl extends UnicastRemoteObject
        implements Callback {

    public CallbackImpl() throws RemoteException {
        super();
    }

    public void obavesti(String poruka) throws RemoteException {
        System.out.println(poruka);
    }
}

public class Server {
    public static void main(String[] args) throws Exception {
        LocateRegistry.createRegistry(1099);
        Servis servis = new ServisImpl();
        Naming.rebind("rmi://localhost:1099/Servis", servis);
    }
}

public class Klijent {
    public static void main(String[] args) throws Exception {
        Servis servis = (Servis) Naming.lookup(
                "rmi://localhost:1099/Servis");

        // Ove dve linije se dodaju samo kada zadatak trazi callback.
        Callback callback = new CallbackImpl();
        servis.registruj(callback);

        String rezultat = servis.operacija(new Podatak("vrednost"));

        // Odjava se pise samo ako je potrebna.
        servis.odjavi(callback);
    }
}
```

Obavezno naglasiti:

- Glavni interfejs je `public`, nasledjuje `Remote`, a svaka njegova metoda je
  `public` i prijavljuje `RemoteException`.
- Implementacija je `public`, nasledjuje `UnicastRemoteObject`, implementira udaljeni
  interfejs i ima `public` konstruktor koji prijavljuje `RemoteException`.
- Sve implementirane udaljene metode moraju biti `public`.
- `synchronized` se koristi kada vise klijenata menja ili cita zajednicko stanje.
- Server kreira registry, pravi implementaciju i registruje je pomocu `rebind`.
- Klijent pomocu `Naming.lookup` dobija udaljenu referencu tipa remote interfejsa,
  a zatim nad njom poziva udaljene metode.
- Ime u `rebind` i `lookup` mora biti isto. Umesto `localhost`, klijent na drugom
  racunaru navodi IP adresu ili naziv serverskog racunara.
- Remote interfejs i svi tipovi koji se pojavljuju u potpisima udaljenih metoda
  moraju biti poznati i serveru i klijentu.
- Objekat koji se salje po vrednosti je `public` klasa koja implementira
  `Serializable`.
- Callback interfejs je takodje `public Remote` interfejs, dok njegova klijentska
  implementacija nasledjuje `UnicastRemoteObject`.
- Kod callback-a klijent prvo radi `lookup`, zatim kreira svoj callback objekat i
  prosledjuje ga serveru metodom za registraciju.
- Server cuva callback reference i poziva callback metodu kada nastane dogadjaj koji
  je opisan u zadatku.

Redosled koji mozes uvek da pratis:

1. Napisati `public` remote interfejs sa `extends Remote` i `throws RemoteException`.
2. Napisati `public` implementaciju sa `extends UnicastRemoteObject` i
   `implements Servis`.
3. U metodi implementacije napisati samo logiku koju zadatak zahteva.
4. Na serveru napisati `createRegistry`, kreiranje objekta i `rebind`.
5. Na klijentu napisati `lookup`, dobijanje reference i jedan udaljeni poziv.
6. Ako se prenosi objekat po vrednosti, dodati `public Serializable` klasu.
7. Ako se trazi obavestavanje, dodati `public Callback`, njegovu klijentsku
   implementaciju, registraciju i serverski poziv callback metode.

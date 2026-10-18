# Uputstvo za odgovor u ispitnoj svesci - Java RMI

Ovaj fajl se prosledjuje AI-u zajedno sa tekstom ili slikom Java RMI zadatka. Cilj nije da AI napravi kompletan projekat, foldere i fajlove, nego da napise tacno ono sto treba prepisati u ispitnu svesku.

Odgovor treba da bude ispitni: kratak, konkretan, sa najbitnijim interfejsima, klasama i metodama. Ne treba pisati sve sto bi postojalo u kompletnom projektu.

## Glavno pravilo

AI treba da odgovori kao da student resava zadatak na papiru.

Zato ne treba pisati:

- strukturu foldera;
- `.classpath`, `.project`, `.gitignore`;
- komande za pokretanje;
- detaljan meni;
- unos preko `Scanner`, osim ako je bas deo demonstracije;
- sav boilerplate `try-catch`;
- kompletne gettere i settere ako nisu bitni za RMI logiku;
- nepotrebno dugacak kod koji ne nosi poene.

Treba pisati:

- koji objekat je glavni remote objekat;
- koji objekti su dodatni remote objekti;
- koji objekti su `Serializable`;
- da li postoji callback i zasto;
- remote interfejse;
- minimalne klase sa bitnim atributima;
- implementacije najbitnijih metoda;
- registraciju servera u RMI registry;
- klijentski `lookup` i minimalnu demonstraciju trazenu u zadatku.

## Ako je zadatak poslat kao slika

Prvo procitaj tekst sa slike i zakljuci sta zadatak trazi. Ako je deo slike nejasan, nemoj izmisljati komplikovan sistem. Napravi najjednostavnije RMI resenje koje odgovara vidljivom tekstu.

U odgovoru prvo napisi kratak zakljucak:

- glavni remote interfejs je `...`;
- dodatni remote objekti su `...`;
- serializable klase su `...`;
- callback postoji ili ne postoji;
- server se registruje na portu `...`, ako je port naveden.

## Format odgovora koji AI treba da vrati

Odgovor uvek organizuj ovako:

```text
# Resenje za ispitnu svesku

## Zakljucak iz teksta zadatka

## 1. Remote interfejsi

## 2. Serializable klase

## 3. Serverske implementacije

## 4. Server

## 5. Klijent

## Kratko objasnjenje

## Sta ne moras da pises u svesci
```

Ako neki deo ne postoji, npr. nema callback-a, taj deo ne izmisljaj. Samo kratko napisi da callback nije potreban.

## Kako prepoznati sta je Remote

Objekat treba da bude `Remote` ako klijent treba da poziva metode nad pravim objektom koji ostaje na serveru.

Tipicni primeri:

- `Kviz` je remote jer klijent preko njega trazi pitanje i salje odgovor.
- `Eksponat` je remote jer klijent povecava cenu stvarnog eksponata na serveru.
- `Match` je remote jer klijent dodaje gol stvarnoj utakmici na serveru.
- `Korisnik` moze biti remote ako klijent direktno radi nad udaljenim korisnickim racunom.
- `Broker`, `EBanka`, `FootballScore`, `EStudSluzba` su glavni remote objekti sistema.

Remote interfejs obavezno:

- `extends Remote`;
- svaka metoda ima `throws RemoteException`;
- ako vraca objekat nad kojim se dalje pozivaju udaljene metode, vraca remote interfejs, ne `Impl` klasu.

Minimalni oblik:

```java
import java.rmi.Remote;
import java.rmi.RemoteException;

public interface Servis extends Remote {
    void metoda() throws RemoteException;
}
```

## Kako prepoznati sta je Serializable

Objekat treba da bude `Serializable` ako se samo prenosi kao podatak i klijent radi sa kopijom.

Tipicni primeri:

- `Pitanje` je `Serializable` jer se klijentu salje tekst pitanja.
- `Stanje` je `Serializable` jer predstavlja presek stanja racuna.
- `Stadium` je `Serializable` jer se samo procita naziv i grad stadiona.
- `Poruka` je `Serializable` jer broker salje naslov i sadrzaj poruke.
- `KlijentAukcije` je `Serializable` ako se samo salju podaci o klijentu.

Minimalni oblik:

```java
import java.io.Serializable;

public class Podatak implements Serializable {
    private String vrednost;

    public Podatak(String vrednost) {
        this.vrednost = vrednost;
    }

    public String getVrednost() {
        return vrednost;
    }
}
```

Na ispitu `serialVersionUID` moze da se izostavi ako se ne trazi eksplicitno.

## Kada se koristi callback

Callback se koristi kada server treba naknadno da pozove klijenta.

Signali u tekstu zadatka:

- "svaki element vraca klijentu cim se izgenerise";
- "korisnik se pretplacuje";
- "klijent treba da bude obavesten";
- "klijent treba da bude u toku sa promenama";
- "server dostavlja poruke pretplacenim klijentima";
- "callback interfejs sadrzi metodu".

Tada se pise callback remote interfejs:

```java
import java.rmi.Remote;
import java.rmi.RemoteException;

public interface Callback extends Remote {
    void promena(int id) throws RemoteException;
}
```

Klijent implementira callback:

```java
import java.rmi.RemoteException;
import java.rmi.server.UnicastRemoteObject;

public class CallbackImpl extends UnicastRemoteObject implements Callback {
    public CallbackImpl() throws RemoteException {
        super();
    }

    public void promena(int id) throws RemoteException {
        System.out.println("Promena: " + id);
    }
}
```

Server cuva callback reference:

```java
private ArrayList<Callback> callbacks = new ArrayList<>();

public synchronized void subscribe(Callback cb) throws RemoteException {
    callbacks.add(cb);
}
```

Server obavestava klijente:

```java
for (Callback cb : callbacks) {
    cb.promena(id);
}
```

Ako callback metoda ponovo zove server, bolje je prvo napraviti kopiju liste callback-a, pa tek onda obavestavati klijente, da se ne drzi `synchronized` lock tokom callback poziva.

## Serverska implementacija

Remote implementacija najcesce:

- `extends UnicastRemoteObject`;
- `implements RemoteInterfejs`;
- konstruktor ima `throws RemoteException`;
- metode koje menjaju zajednicko stanje treba da budu `public synchronized`.

Minimalni oblik:

```java
import java.rmi.RemoteException;
import java.rmi.server.UnicastRemoteObject;

public class ServisImpl extends UnicastRemoteObject implements Servis {
    public ServisImpl() throws RemoteException {
        super();
    }

    public synchronized void metoda() throws RemoteException {
        // bitna logika zadatka
    }
}
```

Bitno za ispit: u interfejsu pises samo tip metode, npr. `void metoda()`, a u implementaciji mora da stoji `public`. Ako metoda menja deljeno stanje, dodaj i `synchronized`.

## Server

Server deo u svesci treba da pokaze da znas da napravis registry i registrujes remote objekat.

Ako port nije naveden, koristi `1099`. Ako tekst zadatka navodi port, koristi taj port.

Minimalno:

```java
import java.rmi.Naming;
import java.rmi.registry.LocateRegistry;

public class Server {
    public static void main(String[] args) {
        try {
            LocateRegistry.createRegistry(1099);
            Naming.rebind("rmi://localhost:1099/Servis", new ServisImpl());
        } catch (Exception e) {
            e.printStackTrace();
        }
    }
}
```

Ako je port, na primer, `4096`:

```java
LocateRegistry.createRegistry(4096);
Naming.rebind("rmi://localhost:4096/FootballScore", new FootballScoreImpl());
```

Na ispitu nije presudno da pises `System.in.read()`, poruke tipa "Server pokrenut" i slicno.

## Klijent

Klijent deo treba da pokaze:

- `Naming.lookup`;
- kastovanje na remote interfejs;
- minimalne pozive metoda koje zadatak trazi;
- callback objekat ako postoji pretplata ili obavestavanje.

Minimalno:

```java
import java.rmi.Naming;

public class Klijent {
    public static void main(String[] args) {
        try {
            Servis servis = (Servis) Naming.lookup("rmi://localhost:1099/Servis");
            servis.metoda();
        } catch (Exception e) {
            e.printStackTrace();
        }
    }
}
```

Ako postoji callback:

```java
Servis servis = (Servis) Naming.lookup("rmi://localhost:1099/Servis");
Callback cb = new CallbackImpl();
servis.subscribe(cb);
```

## Kako analizirati zadatak

Prvo izvuci imenice:

- glavni sistem je obicno glavni remote interfejs, npr. `Broker`, `FootballScore`, `EBanka`;
- objekti koji se menjaju na serveru su remote, npr. `Match`, `Eksponat`, `Korisnik`;
- podaci koji se samo salju su `Serializable`, npr. `Poruka`, `Stadium`, `Stanje`.

Zatim izvuci glagole:

- "dodaj", "uplati", "skini", "povecaj", "prijavi" su metode koje menjaju stanje;
- "vrati", "preuzmi", "prikazi" su metode koje citaju stanje;
- "pretplati" znaci da se cuva callback;
- "obavesti" ili "vraca cim se izgenerise" znaci callback.

Na kraju odredi sta se pise:

1. Remote interfejs glavnog sistema.
2. Dodatni remote interfejsi, ako server vraca udaljene objekte.
3. Callback interfejs, ako server obavestava klijenta.
4. Serializable klase za podatke.
5. Serverske implementacije samo sa bitnom logikom.
6. `Server` sa `createRegistry` i `rebind`.
7. `Klijent` sa `lookup` i minimalnim pozivima.

## Sta tacno ne treba siriti u odgovoru

Ne trositi vreme na:

- kompletan unos i validaciju;
- nepotrebne konstruktore;
- sve gettere i settere;
- formatiranje ispisa;
- vise test primera;
- opis foldera;
- pokretanje iz terminala;
- objasnjenja sta je RMI generalno, osim jedne kratke recenice ako treba.

## Primer stila odgovora

Ako zadatak kaze da server generise proste brojeve i svaki broj vraca klijentu cim se izgenerise, odgovor treba da kaze:

```text
Ovo je callback zadatak, jer server ne vraca celu listu odjednom nego obavestava klijenta za svaki pronadjeni prost broj.
```

Zatim se pise:

```java
public interface Generator extends Remote {
    void generisi(int n, int m, Callback cb) throws RemoteException;
}

public interface Callback extends Remote {
    void prostBroj(int broj) throws RemoteException;
}
```

Pa samo bitna implementacija:

```java
public void generisi(int n, int m, Callback cb) throws RemoteException {
    for (int i = n; i <= m; i++) {
        if (prost(i)) {
            cb.prostBroj(i);
        }
    }
}
```

To je nivo detalja koji se ocekuje za ispitnu svesku: dovoljno da se vidi RMI struktura i logika zadatka, bez kompletnog projekta.

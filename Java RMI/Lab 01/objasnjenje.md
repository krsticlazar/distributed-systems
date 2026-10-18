# Matematicki kviz - objasnjenje

## Ideja zadatka

Klijent preko RMI-ja pristupa kvizu na serveru. Server cuva tri pitanja, tacne odgovore, trenutni indeks pitanja i broj poena.

## Struktura

- `Kviz` je glavni remote interfejs. Klijent preko njega pokrece kviz, trazi pitanje, salje odgovor i cita broj poena.
- `KvizImpl` je serverska implementacija. Ona cuva niz pitanja, niz tacnih odgovora, indeks trenutnog pitanja i poene.
- `Pitanje` je `Serializable`, jer se pitanje salje klijentu kao kopija podataka.
- `PitanjeImpl` cuva tekst pitanja i ponudjene odgovore.
- `Server` registruje `KvizImpl` pod imenom `Kviz`.
- `Klijent` radi `lookup`, prikazuje pitanja i salje odgovore.

## Sta ide na ispitu

Najbitnije je prepoznati da je samo `Kviz` remote objekat. `Pitanje` ne mora da bude remote zato sto klijent samo cita tekst pitanja, pa je dovoljno da se pitanje prenese po vrednosti kao `Serializable`.

Minimalna logika `KvizImpl`:

- `pocetak()` postavlja poene na 0 i indeks na 0;
- `vratiPitanje()` vraca pitanje na trenutnom indeksu;
- `odgovori(odg)` proverava odgovor, povecava poene ako je tacan i prelazi na sledece pitanje;
- `vratiBrojPoena()` vraca rezultat.

## Tok rada

1. Pokrene se `Server`.
2. Server kreira i registruje `KvizImpl`.
3. Klijent dobija remote referencu na `Kviz`.
4. Klijent poziva `pocetak()`.
5. Za svako pitanje poziva `vratiPitanje()`, prikazuje tekst i salje odgovor kroz `odgovori(...)`.
6. Na kraju poziva `vratiBrojPoena()`.

## Za ispit

U ispitnoj svesci ne treba pisati celu konzolnu aplikaciju sa svim ispisima, vec delove koji pokazuju RMI strukturu i logiku kviza.

Iz teksta zadatka treba da zakljucis sledece:

- `Kviz` je remote objekat zato sto klijent poziva njegove metode preko RMI-ja.
- `Pitanje` moze da bude `Serializable`, jer klijent samo dobija tekst pitanja i ponudjene odgovore kao kopiju.
- Server cuva pitanja, tacne odgovore, trenutni indeks i broj poena.
- Klijent ne proverava tacnost odgovora lokalno, nego odgovor salje serveru.

Obavezno treba napisati:

- remote interfejs `Kviz`;
- serializable interfejs ili klasu `Pitanje`;
- implementaciju `KvizImpl`;
- konstruktor koji formira 3 pitanja i 3 tacna odgovora;
- logiku metoda `pocetak`, `vratiPitanje`, `odgovori` i `vratiBrojPoena`;
- osnovni server `rebind`;
- osnovni klijentski `lookup` i redosled poziva metoda.

Ne mora detaljno da se pise:

- kompletan unos preko `Scanner`;
- kompletan lep ispis menija;
- sva hvatanja izuzetaka;
- validacija pogresnih odgovora van `a`, `b`, `c`;
- `System.in.read()`;
- detaljni komentari i formatiranje.

Minimalni remote interfejs:

```java
public interface Kviz extends Remote {
    public void pocetak() throws RemoteException;
    public Pitanje vratiPitanje() throws RemoteException;
    public void odgovori(String odg) throws RemoteException;
    public int vratiBrojPoena() throws RemoteException;
}
```

Minimalni `Pitanje` tip:

```java
public interface Pitanje extends Serializable {
    public String vratiTekst();
}
```

`PitanjeImpl` nije remote klasa. Ona samo implementira `Pitanje` i cuva podatke koji se serijalizuju:

```java
public class PitanjeImpl implements Pitanje {
    private String tekst;
    private String a;
    private String b;
    private String c;

    public String vratiTekst() {
        return tekst + " a) " + a + " b) " + b + " c) " + c;
    }
}
```

Pocetak `KvizImpl` treba da pokazuje da je to remote implementacija:

```java
public class KvizImpl extends UnicastRemoteObject implements Kviz {
    private Pitanje[] pitanja;
    private String[] tacniOdgovori;
    private int indeks;
    private int brojPoena;

    public KvizImpl() throws RemoteException {
        super();
    }
}
```

U interfejsu `Kviz` metode samo imaju povratni tip, naziv i `throws RemoteException`, npr. `void pocetak()` ili `int vratiBrojPoena()`.

U implementaciji `KvizImpl` te metode treba pisati kao `public synchronized`, npr:

```java
public synchronized void pocetak() throws RemoteException
public synchronized Pitanje vratiPitanje() throws RemoteException
public synchronized void odgovori(String odg) throws RemoteException
public synchronized int vratiBrojPoena() throws RemoteException
```

`public` je potrebno jer implementiras metode iz interfejsa, a `synchronized` je korisno zato sto remote objekat moze istovremeno da pozove vise klijenata, pa se cuva zajednicko stanje kviza.

Minimalna serverska logika:

```java
public synchronized void pocetak() throws RemoteException {
    brojPoena = 0;
    indeks = 0;
}

public synchronized Pitanje vratiPitanje() throws RemoteException {
    return pitanja[indeks];
}

public synchronized void odgovori(String odg) throws RemoteException {
    if (tacniOdgovori[indeks].equals(odg)) {
        brojPoena++;
    }
    indeks++;
}

public synchronized int vratiBrojPoena() throws RemoteException {
    return brojPoena;
}
```

Minimalni server:

```java
LocateRegistry.createRegistry(1099);
Naming.rebind("rmi://localhost:1099/Kviz", new KvizImpl());
```

Minimalni klijent:

```java
Kviz kviz = (Kviz) Naming.lookup("rmi://localhost:1099/Kviz");
kviz.pocetak();

for (int i = 0; i < 3; i++) {
    Pitanje p = kviz.vratiPitanje();
    System.out.println(p.vratiTekst());
    kviz.odgovori(odg);
}

System.out.println(kviz.vratiBrojPoena());
```

Najbitnija recenica za obrazlozenje: `Kviz` je remote zato sto cuva stanje kviza na serveru, dok je `Pitanje` serializable zato sto se klijentu salje samo kopija podataka za prikaz.

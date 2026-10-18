# Moji zakljucci za Java RMI

## Osnovna ideja

Na klijentskoj strani postoji klasa `Klijent`.

U njoj se nalazi `main` metoda i u toj metodi se klijent povezuje na server. Najbitniji deo je:

```java
Kviz kviz = (Kviz) Naming.lookup("rmi://localhost:1099/Kviz");
```

Ovim klijent ne dobija pravi objekat `KvizImpl`, nego dobija stub, odnosno proxy objekat preko kog moze da poziva udaljene metode.

`try-catch` postoji zato sto udaljeni poziv moze da pukne iz vise razloga:

- server nije pokrenut;
- registry nije pokrenut;
- ime objekta nije dobro;
- mreza nije dostupna;
- remote metoda baci `RemoteException`.

## Remote interfejs je ugovor

Interfejs remote objekta je ugovor izmedju servera i klijenta.

U nasem primeru taj ugovor je:

```java
public interface Kviz extends Remote
```

Taj interfejs mora da postoji i na serverskoj i na klijentskoj strani.

Serveru treba zato sto serverska implementacija mora da ga implementira:

```java
public class KvizImpl extends UnicastRemoteObject implements Kviz
```

Klijentu treba zato sto klijent preko tog interfejsa koristi stub:

```java
Kviz kviz = (Kviz) Naming.lookup(...);
```

Bitno: klasa koja implementira remote interfejs, kod nas `KvizImpl`, nalazi se samo na serverskoj strani. Klijent ne treba da zna kako je `KvizImpl` napisan, nego samo koje metode postoje u interfejsu `Kviz`.

## Sta je remote, a sta serializable

U ovom zadatku remote objekat je `Kviz`.

`Kviz` mora da bude remote zato sto cuva stanje kviza na serveru:

- trenutni indeks pitanja;
- tacne odgovore;
- broj poena.

`Pitanje` ne mora da bude remote.

`Pitanje` je `Serializable` zato sto server klijentu salje kopiju podataka o pitanju. Klijentu treba tekst pitanja i ponudjeni odgovori, ali ne treba udaljeni objekat nad kojim ce menjati stanje.

Preciznije: ne prenosi se samo tekst kao jedan string, nego se prenosi kopija objekta `PitanjeImpl`, odnosno njegovo stanje. Zbog toga klijent mora da zna i tip `PitanjeImpl`, da bi RMI mogao da deserijalizuje objekat koji stigne sa servera.

## Sta mora da postoji i kod servera i kod klijenta

Sve sto je deo ugovora mora da postoji na obe strane.

U nasem primeru to su:

- `Kviz.java`;
- `Pitanje.java`;
- `PitanjeImpl.java`.

`Kviz.java` postoji na obe strane zato sto ga server implementira, a klijent preko njega koristi stub.

`Pitanje.java` postoji na obe strane zato sto se pojavljuje kao povratna vrednost remote metode:

```java
Pitanje vratiPitanje()
```

`PitanjeImpl.java` postoji na obe strane zato sto server pravi objekat te klase, a klijent mora da ume da ga procita kada stigne preko mreze.

U ovim zadacima svi Java fajlovi stoje zajedno u istom folderu:

```text
Kviz.java
Pitanje.java
PitanjeImpl.java
KvizImpl.java
Server.java
Klijent.java
```

Njihove uloge su i dalje razlicite: interfejsi predstavljaju ugovor, `KvizImpl` i `Server` pripadaju serverskoj logici, a `Klijent` pokrece klijentsku aplikaciju.

## Sta postoji samo na serveru

Na serveru postoji implementacija remote objekta:

```java
KvizImpl.java
```

Tu se nalazi prava logika kviza.

Klijent ne treba da ima `KvizImpl`, zato sto klijent ne izvrsava logiku kviza lokalno. Klijent samo poziva metode preko remote interfejsa.

## Sta postoji samo na klijentu

Na klijentu postoji:

```java
Klijent.java
```

Tu se nalazi `main` metoda klijentske aplikacije.

Klijent:

- dobija stub preko `Naming.lookup`;
- poziva `pocetak()`;
- trazi pitanje preko `vratiPitanje()`;
- salje odgovor preko `odgovori(...)`;
- na kraju trazi broj poena preko `vratiBrojPoena()`.

Klijent ne proverava tacnost odgovora sam. To radi server, jer server cuva pravo stanje kviza.

## Sta radi Server

`Server` prvo pokrece RMI registry na portu `1099`:

```java
LocateRegistry.createRegistry(1099);
```

Zatim pravi remote objekat:

```java
Kviz kviz = new KvizImpl();
```

Zatim taj objekat registruje u registry:

```java
Naming.rebind("rmi://localhost:1099/Kviz", kviz);
```

`Kviz` u URL-u je simbolicko ime remote objekta u registry-ju. Ne mora obavezno da bude isto kao ime klase, ali je prakticno da bude jasno i slicno nazivu servisa.

Klijent kasnije mora da koristi isto ime:

```java
Naming.lookup("rmi://localhost:1099/Kviz");
```

## Najbitniji zakljucak

Server cuva pravu implementaciju i pravo stanje.

Klijent ima samo:

- svoj `main`;
- remote interfejse i serializable klase potrebne za kompajliranje;
- stub koji dobije preko `lookup`.

Remote interfejs govori sta sme da se poziva udaljeno.

Serializable klase sluze da se podaci prenesu kao kopija.

Implementacija remote interfejsa ostaje na serveru.

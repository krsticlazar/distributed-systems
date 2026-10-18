# Mobilni operater - objasnjenje

## Ideja zadatka

Klijent preko glavnog servisa `Operater` pronalazi jednog korisnika po broju telefona. Zatim nad tim korisnikom uplacuje minute, poruke ili internet, ili trazi trenutno stanje.

## Struktura

- `Operater` je glavni remote interfejs i ima metodu `vratiKorisnika`.
- `OperaterImpl` cuva korisnike u memoriji i vraca remote referencu na trazenog korisnika.
- `Korisnik` je remote interfejs jednog korisnika.
- `KorisnikImpl` cuva minute, poruke, internet, tarife i racun.
- `Stanje` je `Serializable`, jer klijent samo dobija snapshot stanja.
- `StanjeImpl` nosi podatke o stanju korisnika.
- `Server` registruje `OperaterImpl`.
- `Klijent` prikazuje meni i poziva metode nad udaljenim korisnikom.

## Sta ide na ispitu

Ovo je zadatak sa dva remote nivoa:

- `Operater` je ulaz u sistem;
- `Korisnik` je udaljeni objekat nad kojim se menjaju podaci.

`Stanje` nije remote zato sto klijent ne treba da menja stanje direktno. Server mu samo vraca kopiju trenutnog stanja.

Za ispit je najbitnije da ne vracas `KorisnikImpl`, nego `Korisnik`, jer se udaljeni objekti koriste preko remote interfejsa.

## Tok rada

1. Server kreira `OperaterImpl`.
2. `OperaterImpl` kreira vise korisnika.
3. Klijent preko `lookup` dobija `Operater`.
4. Klijent unosi broj telefona.
5. `Operater.vratiKorisnika(...)` vraca remote referencu na korisnika.
6. Klijent poziva `uplatiMinute`, `uplatiPoruke`, `uplatiInternet` ili `vratiStanje`.

## Za ispit

U ispitnoj svesci ne treba pisati ceo konzolni meni, vec RMI strukturu i metode koje tekst zadatka trazi.

Iz teksta zadatka treba da zakljucis sledece:

- `Operater` je glavni remote servis.
- `Korisnik` je remote objekat jer njegovo stanje ostaje na serveru i menja se pozivima klijenta.
- `Stanje` je `Serializable`, jer se klijentu vraca samo snapshot za ispis.
- `OperaterImpl` glumi bazu korisnika i vraca `Korisnik`, ne `KorisnikImpl`.

Obavezno treba napisati:

- remote interfejs `Operater` sa metodom `vratiKorisnika(String broj)`;
- remote interfejs `Korisnik` sa metodama za uplatu i proveru stanja;
- serializable tip `Stanje`;
- `OperaterImpl` koji u konstruktoru pravi korisnike;
- `KorisnikImpl` koji cuva minute, poruke, internet, tarife i racun;
- logiku uplate koja povecava resurs i racun;
- osnovni server `rebind`;
- osnovni klijentski `lookup` i poziv nad dobijenim korisnikom.

Ne mora detaljno da se pise:

- svih 10 korisnika sa realnim vrednostima;
- kompletan meni;
- kompletan `Scanner` unos;
- detaljna validacija pogresnog unosa;
- formatiranje ispisa;
- sva hvatanja izuzetaka.

Minimalni `Operater` interfejs:

```java
public interface Operater extends Remote {
    public Korisnik vratiKorisnika(String broj) throws RemoteException;
}
```

Minimalni `Korisnik` interfejs:

```java
public interface Korisnik extends Remote {
    public void uplatiMinute(int minuti) throws RemoteException;
    public void uplatiPoruke(int poruke) throws RemoteException;
    public void uplatiInternet(int internet) throws RemoteException;
    public Stanje vratiStanje() throws RemoteException;
}
```

Minimalni `Stanje` tip:

```java
public interface Stanje extends Serializable {
    public int vratiMinute();
    public int vratiPoruke();
    public int vratiInternet();
    public float vratiRacun();
}
```

Minimalna logika korisnika:

```java
public void uplatiMinute(int minuti) throws RemoteException {
    this.minuti += minuti;
    racun += minuti * minutiTarifa;
}

public void uplatiPoruke(int poruke) throws RemoteException {
    this.poruke += poruke;
    racun += poruke * porukeTarifa;
}

public void uplatiInternet(int internet) throws RemoteException {
    this.internet += internet;
    racun += internet * internetTarifa;
}

public Stanje vratiStanje() throws RemoteException {
    return new StanjeImpl(minuti, poruke, internet, racun);
}
```

Minimalna logika operatera:

```java
private HashMap<String, Korisnik> korisnici = new HashMap<String, Korisnik>();

public Korisnik vratiKorisnika(String broj) throws RemoteException {
    return korisnici.get(broj);
}
```

Minimalni server:

```java
LocateRegistry.createRegistry(1099);
Naming.rebind("rmi://localhost:1099/Operater", new OperaterImpl());
```

Minimalni klijent:

```java
Operater op = (Operater) Naming.lookup("rmi://localhost:1099/Operater");
Korisnik k = op.vratiKorisnika(broj);
k.uplatiMinute(100);
Stanje s = k.vratiStanje();
System.out.println(s.vratiRacun());
```

Najbitnija recenica za obrazlozenje: `Operater` sluzi samo da pronadje korisnika, `Korisnik` je remote jer cuva pravo serversko stanje, a `Stanje` je serializable jer se salje kao kopija za prikaz.

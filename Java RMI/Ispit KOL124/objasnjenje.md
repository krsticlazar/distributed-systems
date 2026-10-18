# KOL124 - FootballScore

## Sta zadatak trazi

Treba napraviti RMI sistem za pracenje rezultata fudbalskih utakmica. Server cuva utakmice, klijenti mogu da procitaju rezultate, preuzmu jednu utakmicu, dodaju gol i pretplate se na promenu rezultata.

Server se registruje na portu `4096`.

## Struktura

- `FootballScore` je glavni remote interfejs sistema.
- `FootballScoreImpl` cuva sve utakmice u mapi.
- `Match` je remote interfejs jedne utakmice.
- `MatchImpl` cuva rezultat jedne utakmice i listu pretplacenih callback klijenata.
- `Stadium` je `Serializable` jer se samo salje kao podatak.
- `Callback` je remote interfejs koji implementira klijent.
- `ClientUser` prikazuje utakmice, prikazuje stadion utakmice 2 i pretplacuje se na utakmicu 1.
- `ClientAdmin` dodaje gol domacem timu na utakmici 1.

## Zasto je Match Remote

`Match` nije obican serializable objekat jer klijent treba da poziva metode koje menjaju stvarno stanje utakmice na serveru:

```java
void addHomeGoal()
void addAwayGoal()
```

Da je `Match` samo `Serializable`, klijent bi dobio kopiju utakmice i menjao bi lokalni objekat. To nije ono sto zadatak trazi.

## Zasto postoji callback

Tekst kaze da korisnici treba da se pretplate na promenu rezultata i da callback interfejs ima metodu `resultChanged(int matchId)`.

Zato klijent implementira `Callback`, prosledi ga utakmici metodom `subscribe`, a server ga pozove kada se rezultat promeni.

## Tok rada

1. Server pokrece registry na portu `4096`.
2. Server pravi `FootballScoreImpl`.
3. Konstruktor `FootballScoreImpl` pravi dve utakmice sa id `1` i `2`.
4. Server registruje `FootballScore`.
5. `ClientUser` preuzima `FootballScore`.
6. `ClientUser` prikazuje sve rezultate.
7. `ClientUser` preuzima utakmicu `2` i ispisuje naziv stadiona.
8. `ClientUser` preuzima utakmicu `1` i pretplacuje callback.
9. `ClientAdmin` preuzima utakmicu `1` i dodaje gol domacem timu.
10. `MatchImpl` obavestava sve pretplacene klijente pozivom `resultChanged(1)`.

## Za ispit

U ispitnoj svesci treba napisati samo bitne RMI delove. Ne treba pisati kompletno formatiranje ispisa, nepotrebne menije i sve detalje oko unosa.

Iz teksta zadatka treba zakljuciti:

- `FootballScore` je glavni remote objekat.
- `Match` je poseban remote objekat jer klijent treba da poziva metode nad stvarnom utakmicom na serveru.
- `Stadium` je `Serializable` jer se samo prenosi kao podatak.
- `Callback` je remote interfejs koji implementira klijent.
- `FootballScoreImpl` u konstruktoru pravi dve utakmice i cuva ih u mapi.
- `Server` mora da koristi port `4096`.

Minimalni `FootballScore` interfejs:

```java
public interface FootballScore extends Remote {
    String getAllResults() throws RemoteException;
    Match getMatch(int id) throws RemoteException;
}
```

Minimalni `Match` interfejs:

```java
public interface Match extends Remote {
    void addHomeGoal() throws RemoteException;
    void addAwayGoal() throws RemoteException;
    Stadium getStadium() throws RemoteException;
    String getResult() throws RemoteException;
    void subscribe(Callback callback) throws RemoteException;
}
```

Minimalni callback interfejs:

```java
public interface Callback extends Remote {
    void resultChanged(int matchId) throws RemoteException;
}
```

Minimalni `Stadium`:

```java
public class Stadium implements Serializable {
    private String name;
    private String city;
}
```

Minimalni `FootballScoreImpl`:

```java
private Map<Integer, Match> utakmice = new LinkedHashMap<>();

public FootballScoreImpl() throws RemoteException {
    utakmice.put(1, new MatchImpl(1, "Tim 1", "Tim 2", new Stadium("Stadion 1", "Grad 1")));
    utakmice.put(2, new MatchImpl(2, "Tim 3", "Tim 4", new Stadium("Stadion 2", "Grad 2")));
}

public String getAllResults() throws RemoteException {
    StringBuilder sb = new StringBuilder();

    for (Match match : utakmice.values()) {
        sb.append(match.getResult()).append(System.lineSeparator());
    }

    return sb.toString();
}

public Match getMatch(int id) throws RemoteException {
    return utakmice.get(id);
}
```

Minimalna promena rezultata u `MatchImpl`:

```java
public void addHomeGoal() throws RemoteException {
    List<Callback> pretplaceni;

    synchronized (this) {
        homeGoals++;
        pretplaceni = new ArrayList<>(callbacks);
    }

    for (Callback callback : pretplaceni) {
        callback.resultChanged(id);
    }
}
```

Minimalno server pokretanje:

```java
LocateRegistry.createRegistry(4096);
Naming.rebind("rmi://localhost:4096/FootballScore", new FootballScoreImpl());
```

Minimalni `ClientUser`:

```java
FootballScore fs = (FootballScore) Naming.lookup("rmi://localhost:4096/FootballScore");
System.out.println(fs.getAllResults());
Match m2 = fs.getMatch(2);
System.out.println(m2.getStadium().getName());
Match m1 = fs.getMatch(1);
Callback cb = new CallbackImpl();
m1.subscribe(cb);
```

Minimalni `ClientAdmin`:

```java
FootballScore fs = (FootballScore) Naming.lookup("rmi://localhost:4096/FootballScore");
Match m1 = fs.getMatch(1);
m1.addHomeGoal();
```

Najbitnija recenica za obrazlozenje: `FootballScore` je ulazna tacka sistema, `Match` je udaljeni objekat koji cuva stvarni rezultat na serveru, a `Callback` sluzi da server obavesti pretplacene klijente kada se rezultat promeni.

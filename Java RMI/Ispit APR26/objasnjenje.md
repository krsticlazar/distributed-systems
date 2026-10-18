# APR26 - objasnjenje

## Sta zadatak trazi

Server prima dva prirodna broja `N` i `M` i generise sve proste brojeve izmedju njih. Bitan deo recenice je da se svaki element vraca klijentu cim se izgenerise.

Zbog toga je resenje uradjeno preko callback-a.

## Struktura

- `Generator` je remote interfejs servera.
- `GeneratorImpl` prolazi kroz interval i proverava koji brojevi su prosti.
- `Callback` je remote interfejs koji implementira klijent.
- `Klijent.CallbackImpl` je udaljeni objekat na strani klijenta.
- `Server` registruje `GeneratorImpl` u RMI registry.
- `Klijent` unosi `N` i `M`, pravi callback objekat i salje ga serveru.

## Sta ide na ispitu

Ovo nije obican zadatak gde metoda vrati `ArrayList<Integer>`, jer tekst kaze da se svaki element vraca cim se izgenerise. To je signal za callback.

Minimalna ispitna logika:

- server ima metodu `generisiProsteBrojeve(int n, int m, Callback cb)`;
- klijent implementira `Callback`;
- server za svaki prost broj poziva `cb.prostBrojGenerisan(i)`;
- klijent u callback metodi ispisuje primljeni broj.

## Tok rada

1. Server kreira i registruje `GeneratorImpl`.
2. Klijent preko `lookup` dobija `Generator`.
3. Klijent pravi `CallbackImpl`.
4. Klijent poziva `generisiProsteBrojeve(n, m, cb)`.
5. Server generise brojeve i za svaki prost broj poziva callback.
6. Klijent odmah ispisuje svaki broj koji server posalje.

## Za ispit

U ispitnoj svesci se ne ocekuje kompletna aplikacija kao u folderu, vec bitni RMI delovi koji pokazuju da razumes komunikaciju.

Iz teksta zadatka treba da zakljucis sledece:

- `N` i `M` su obicni `int` parametri i prenose se po vrednosti.
- Prosti brojevi ne treba da se vrate odjednom kao lista.
- Recenica "svaki od elemenata vraca klijentu cim se element izgenerise" znaci da treba callback.
- Server mora da ima remote metod koji prima interval i callback objekat.
- Klijent mora da ima remote callback objekat koji server moze da pozove.

Obavezno treba napisati:

- remote interfejs servera, npr. `Generator`;
- callback remote interfejs, npr. `Callback`;
- implementaciju servera `GeneratorImpl`;
- metodu za proveru da li je broj prost;
- deo klijenta koji pravi callback objekat i prosledjuje ga serveru.

Ne mora detaljno da se pise:

- kompletan meni;
- unos preko `Scanner`;
- sva hvatanja izuzetaka;
- `System.in.read()`;
- detaljna validacija za negativan unos;
- kompletno formatiranje ispisa.

Minimalni server interfejs:

```java
public interface Generator extends Remote {
    public void generisiProsteBrojeve(int n, int m, Callback cb) throws RemoteException;
}
```

Minimalni callback interfejs:

```java
public interface Callback extends Remote {
    public void prostBrojGenerisan(int broj) throws RemoteException;
}
```

Minimalna serverska logika:

```java
public void generisiProsteBrojeve(int n, int m, Callback cb) throws RemoteException {
    for (int i = n; i <= m; i++) {
        if (prost(i)) {
            cb.prostBrojGenerisan(i);
        }
    }
}
```

Provera prostog broja:

```java
private boolean prost(int broj) {
    if (broj < 2) return false;

    for (int i = 2; i * i <= broj; i++) {
        if (broj % i == 0) return false;
    }

    return true;
}
```

Minimalni callback na klijentu:

```java
public class CallbackImpl extends UnicastRemoteObject implements Callback {
    public CallbackImpl() throws RemoteException {
        super();
    }

    public void prostBrojGenerisan(int broj) throws RemoteException {
        System.out.println(broj);
    }
}
```

Minimalni klijentski poziv:

```java
Generator g = (Generator) Naming.lookup("rmi://localhost:1099/Generator");
Callback cb = new CallbackImpl();
g.generisiProsteBrojeve(n, m, cb);
```

Ako treba pomenuti server pokretanje, dovoljno je:

```java
LocateRegistry.createRegistry(1099);
Naming.rebind("rmi://localhost:1099/Generator", new GeneratorImpl());
```

Najbitnija recenica za obrazlozenje: callback se koristi zato sto server treba da salje svaki prost broj klijentu odmah kada ga nadje, umesto da klijent ceka celu listu kao povratnu vrednost metode.

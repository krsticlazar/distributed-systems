# Java RMI i RMI Callback - priprema za kviz

Ovaj fajl je namenjen kao dopuna uz `2.1 Java RMI.pdf` i `2.2 Java RMI Callback.pdf`.
Ideja nije da zameni slajdove, nego da ti skrene paznju na stvari koje se na slajdovima cesto vide samo "na brzinu", a bas one znaju da dodju na kvizu ili da naprave zabunu na vezbi.

## Kratke dopune uz slajdove

### 1. `rmiregistry` nije isto sto i remote objekat

Ovo je najcesca zabuna.

- `rmiregistry` je registar imena i referenci na udaljene objekte.
- Klijent prvo kontaktira registar da bi dobio referencu na trazeni objekat.
- Tek posle toga klijent preko te reference poziva metode udaljenog objekta.

Bitna posledica:

- URL tipa `rmi://localhost:1099/Kviz` govori gde je **registar** i pod kojim imenom je objekat prijavljen.
- To ne znaci da sam remote objekat radi bas na portu `1099`.
- Port `1099` je tipicno port registra, dok remote objekat dobije svoj port pri export-u.

### 2. Klijent obicno ne radi sa "pravim" serverskim objektom

Klijent preko `Naming.lookup(...)` ne dobija stvarnu instancu serverske implementacije, nego dobija **stub**.

- Stub je proxy objekat na strani klijenta.
- On implementira isti remote interfejs.
- Klijent zato radi preko interfejsa, a ne preko konkretne implementacije.

Zato je tipican obrazac:

```java
Kviz kviz = (Kviz) Naming.lookup("rmi://localhost/Kviz");
```

umesto:

```java
KvizImpl kviz = ...
```

### 3. Zasto remote interfejs mora da `extends Remote`

`Remote` je marker interfejs. On ne daje metode, ali RMI sistemu govori:

- "ovaj interfejs opisuje metode koje smeju da se pozivaju udaljeno".

Bez toga interfejs jeste obican Java interfejs, ali nije RMI remote interfejs.

### 4. Zasto metode bacaju `RemoteException`

Kod lokalnog poziva metoda obicno razmisljas samo o logici metode.
Kod udaljenog poziva postoji jos mnogo razloga za neuspeh:

- mreza nije dostupna,
- server nije pokrenut,
- registar ne radi,
- objekat nije registrovan,
- serijalizacija nije uspela,
- udaljeni proces je pao.

Zato remote metode moraju da deklarisu `throws RemoteException`.

### 5. Sta se prenosi po vrednosti, a sta po referenci

Na slajdovima je to kratko receno, ali je veoma bitno:

- primitivni tipovi se prenose po vrednosti,
- `Serializable` objekti se prenose po vrednosti,
- remote objekti se prenose po referenci, tacnije kao remote referenca/stub.

Prakticno:

- ako kao argument saljes `String`, `int`, `ArrayList<String>` ili neki objekat koji implementira `Serializable`, salje se kopija podataka;
- ako saljes objekat koji je remote, druga strana ne dobija "kopiju objekta", nego dobija referencu preko koje moze da ga zove.

### 6. `UnicastRemoteObject` i "export" objekta

Kada implementacija nasledi `UnicastRemoteObject`, RMI sistem moze da je izveze, odnosno da omoguci da prima udaljene pozive.

U lab zadacima se najcesce koristi upravo ovaj jednostavan obrazac:

```java
public class KvizImpl extends UnicastRemoteObject implements Kviz
```

Bez export-a objekat postoji samo lokalno u procesu i ne moze da prima RMI pozive sa druge JVM.

### 7. `bind` i `rebind`

Na slajdovima se uglavnom koristi `rebind`.

- `bind` ce prijaviti ime samo ako ono vec ne postoji u registru;
- `rebind` ce upisati objekat pod tim imenom i pregaziti staru vezu ako je postojala.

Za labske primere je `rebind` cesci jer je prakticniji pri ponovnom pokretanju servera.

### 8. Sta je danas sa stub/skeleton generisanjem

Na starijim primerima mozes videti `rmic`.

- nekada su se stub i skeleton klase eksplicitno generisale,
- u novijim Java verzijama to se radi dinamicki,
- zato se u tipicnim studentskim primerima `rmic` vise ne koristi.

Poenta za kviz: koncept stub/skeleton i dalje postoji, cak i ako ih ti ne generises rucno.

### 9. Sta je sustina callback-a

Bez callback-a klijent mora stalno da pita server:

- "da li ima promena?"
- "da li ima novih podataka?"

To je polling.

Sa callback-om:

- klijent napravi svoj remote callback objekat,
- prosledi serveru referencu na taj objekat,
- server cuva te reference,
- kada se dogadjaj desi, server pozove callback metodu na klijentu.

Dakle, callback je i dalje RMI poziv, samo u obrnutom smeru: **server poziva metod na klijentovom remote objektu**.

### 10. Callback nije "magija", nego jos jedan remote poziv

Ovo je vazna mentalna slika:

- u obicnom RMI scenariju klijent zove server;
- u callback scenariju klijent i dalje zove server, ali server posle toga moze da zove klijenta.

To radi zato sto i klijent hostuje remote objekat.

### 11. Zasto se kod callback-a cesto koriste `register` i `unregister`

Server mora da zna:

- koje klijente treba obavestiti,
- koje callback objekte treba izbaciti kada klijent vise nije zainteresovan.

Zato se cesto prave metode:

```java
register(Callback cb)
unregister(Callback cb)
```

### 12. Zasto se u callback primeru cesto koristi `synchronized`

Ako vise klijenata paralelno registruje/odjavljuje callback objekte ili server istovremeno iterira kroz listu callback-ova, lako moze doci do problema sa deljenim stanjem.

`synchronized` tu najcesce stiti:

- listu prijavljenih klijenata,
- konzistentnost pristupa zajednickim podacima.

### 13. Najcesce greske koje vrede za kviz i lab

- remote interfejs ne nasledi `Remote`,
- metoda u remote interfejsu ne baca `RemoteException`,
- klijent koristi implementacionu klasu umesto interfejsa,
- u `lookup` se omasi ime objekta,
- zaboravi se da objekat mora biti export-ovan,
- callback objekat na klijentu nije remote objekat,
- ocekuje se da se `Serializable` objekat deli kao zajednicka instanca, a zapravo se salje kopija,
- mesa se port registra sa portom samog udaljenog objekta.

## 30 kratkih pitanja za pripremu

### Pitanja

1. Sta je Java RMI?
2. Koja su tri glavna ucesnika u osnovnom RMI scenariju?
3. Cemu sluzi `rmiregistry`?
4. Sta predstavlja URL oblika `rmi://hostname:port/name`?
5. Da li port u tom URL-u mora da bude port samog remote objekta?
6. Zasto remote interfejs mora da `extends java.rmi.Remote`?
7. Zasto metode remote interfejsa deklarisu `throws RemoteException`?
8. Zasto implementaciona klasa cesto nasleduje `UnicastRemoteObject`?
9. Da li klijent treba da radi sa `KalkulatorImpl` ili sa `Kalkulator` interfejsom?
10. Sta vraca `Naming.lookup(...)`?
11. Ako server uradi `Naming.rebind("rmi://localhost:1099/Calc", obj)`, da li klijent moze da koristi `Naming.lookup("rmi://localhost/Calc")`?
12. Sta se desava ako remote interfejs ima metodu koja ne baca `RemoteException`?
13. Da li klijent moze direktno da instancira serverski remote objekat i tako "zaobidje" RMI?
14. Ako remote klasa ima i lokalne metode koje nisu u remote interfejsu, da li klijent moze udaljeno da ih poziva?
15. Kako se prenosi `int` kao parametar udaljene metode?
16. Kako se prenosi objekat koji implementira `Serializable`?
17. Kako se prenosi objekat koji je i sam remote objekat?
18. Sta je stub?
19. Sta je skeleton i zasto se danas redje vidi u kodu?
20. Koja je prakticna razlika izmedju `bind` i `rebind`?
21. Sta je polling, a sta callback?
22. Zasto callback resenje moze biti bolje od stalnog polling-a?
23. Ko pravi callback objekat: server ili klijent?
24. Gde se obicno cuva lista callback objekata?
25. Zasto callback klasa na klijentu mora da bude remote objekat?
26. U sledecem primeru, da li je klasa spremna za pravi callback poziv?

```java
public class MojCallback implements Callback {
    public void notifyChange() throws RemoteException {
        System.out.println("Promena");
    }
}
```

27. Sta ce se desiti ako server pri obavestavanju prodje kroz listu callback objekata, a jedan klijent je u medjuvremenu ugasen?
28. Sta je problem u sledecem interfejsu?

```java
public interface Servis extends Remote {
    Rezultat izracunaj(int x);
}
```

29. Ako server kao rezultat vrati `ArrayList<String>`, da li klijent dobija zajednicku listu sa serverom ili kopiju podataka?
30. U kom redosledu se najcesce pokrecu delovi RMI aplikacije za testiranje na vezbi?

## Kratki odgovori

1. Java RMI je mehanizam koji omogucava da Java program poziva metode objekta koji se nalazi u drugoj JVM, lokalno u drugom procesu ili na udaljenom racunaru.
2. Klijent, server i `rmiregistry`.
3. Sluzi da poveze simbolicko ime objekta sa remote referencom kako bi klijent mogao da nadje trazeni objekat.
4. Lokaciju registra i ime pod kojim je objekat prijavljen u registru.
5. Ne. To je tipicno port registra, ne obavezno port samog remote objekta.
6. Zato sto time interfejs postaje remote interfejs koji RMI sistem prepoznaje kao interfejs za udaljene pozive.
7. Zato sto udaljeni poziv moze da ne uspe zbog mreze, registra, serijalizacije ili pada procesa.
8. Zato sto se time objekat tipicno export-uje i osposobljava da prima udaljene pozive.
9. Sa remote interfejsom `Kalkulator`, ne sa konkretnom implementacijom.
10. Vraca stub, odnosno remote referencu koju obicno kastujemo na tip remote interfejsa.
11. Da, moze. Ako se port izostavi, podrazumeva se `1099`.
12. To je greska u definiciji remote interfejsa. Po pravilima RMI-a, remote metode treba da deklarisu `throws RemoteException`.
13. Ako instancira objekat lokalno, to vise nije udaljeni poziv nego lokalni rad u istoj JVM. Time se zaobilazi smisao RMI-a.
14. Ne. Klijent udaljeno moze da poziva samo metode koje su izlozene kroz remote interfejs.
15. Po vrednosti.
16. Po vrednosti, odnosno salje se kopija stanja objekta.
17. Po referenci, prakticno kao stub.
18. Proxy objekat na strani klijenta koji prima lokalni poziv, pakuje argumente i salje zahtev udaljenom objektu.
19. Skeleton je deo na serverskoj strani koji raspakuje poziv i prosledjuje ga pravom objektu; danas se uglavnom generise/obezbedjuje automatski.
20. `bind` ne dozvoljava da vec postojece ime bude pregazeno, dok `rebind` pregazuje staru registraciju.
21. Polling je stalno ispitivanje servera da li ima promene; callback je kada server sam obavesti klijenta.
22. Zato sto smanjuje nepotrebno stalno ispitivanje servera i omogucava da klijent bude obavesten bas kada se dogadjaj desi.
23. Klijent.
24. Na serveru.
25. Zato sto server mora udaljeno da pozove njegov metod, pa callback objekat mora biti dostupan kroz RMI.
26. Ne potpuno. Implementira interfejs, ali nije export-ovana kao remote objekat, npr. preko `UnicastRemoteObject`, pa server ne moze jednostavno da je poziva udaljeno.
27. Poziv callback-a za tog klijenta moze baciti `RemoteException`; server to treba da ocekuje i po potrebi ukloni neispravnu referencu iz liste.
28. Metoda `izracunaj` ne deklarise `throws RemoteException`. Takodje, `Rezultat` mora biti remote tip ili `Serializable`.
29. Kopiju podataka, pod uslovom da je lista serijalizabilna kao i njeni elementi.
30. Najcesce: pokrenuti `rmiregistry` ili napraviti registry, zatim server koji registruje objekat, pa onda klijenta.

## Mini podsetnik za kviz

Ako treba da zapamtis samo nekoliko stvari, neka to budu ove:

1. Klijent prvo trazi objekat u registru, pa tek onda poziva njegov metod.
2. Klijent radi preko remote interfejsa i stuba, ne preko implementacije.
3. `RemoteException` je obavezna mentalna oznaka da je poziv mrezi izlozen.
4. `Serializable` ide po vrednosti, remote objekat ide po referenci.
5. Callback znaci da i klijent moze da hostuje remote objekat koji server posle zove.

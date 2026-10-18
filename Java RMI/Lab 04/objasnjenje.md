# Prijava ispita - objasnjenje

## Sta zadatak trazi

Treba napraviti RMI sistem preko koga student moze da prijavi ispit i da proveri sve prijavljene ispite.

## Struktura

- `EStudSluzba` je glavni remote servis.
- `EStudSluzbaImpl` cuva mapu studenata i vraca trazenog studenta po broju indeksa.
- `Student` je remote interfejs jednog studenta.
- `StudentImpl` cuva prijavu jednog studenta i menja je na serveru.
- `Prijava` je serializable interfejs, jer se prijava salje klijentu kao kopija podataka.
- `PrijavaImpl` cuva listu ispita i formatira je za ispis.
- `Server` pokrece registry i registruje `EStudSluzba`.
- `Klijent` prikazuje meni i poziva metode udaljenih objekata.

## Sta ide na ispitu

Za ispit je dovoljno da prepoznas dva nivoa:

- glavni remote servis `EStudSluzba`, koji vraca `Student`;
- remote objekat `Student`, nad kojim se poziva `prijaviIspit` i `vratiPrijavu`.

`Prijava` ne mora da bude remote jer klijent samo cita trenutno stanje. Zato je `Serializable`.

## Tok rada

1. Server kreira `EStudSluzbaImpl`.
2. `EStudSluzbaImpl` kreira 10 studenata.
3. Klijent preko `lookup` dobija `EStudSluzba`.
4. Klijent unosi broj indeksa.
5. Servis vraca remote referencu na `Student`.
6. Klijent nad studentom prijavljuje ispit ili preuzima `Prijava` objekat.

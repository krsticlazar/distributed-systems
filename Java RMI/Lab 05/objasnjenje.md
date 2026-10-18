# Aukcija - objasnjenje

## Ideja zadatka

Klijent preko glavnog servisa aukcije dobija udaljeni objekat konkretnog eksponata. Nad eksponatom zatim cita naziv i cenu, povecava cenu, prijavljuje licitaciju ili odustaje.

## Struktura

- `EAukcija` je glavni remote interfejs.
- `EAukcijaImpl` cuva vise eksponata i vraca trazeni `Eksponat`.
- `Eksponat` je remote interfejs jednog eksponata.
- `EksponatImpl` cuva naziv, cenu i trenutno prijavljenog klijenta.
- `KlijentAukcije` je `Serializable`, jer se podaci o klijentu salju serveru kao kopija.
- `Server` registruje `EAukcijaImpl`.
- `Klijent` pronalazi eksponat i poziva metode nad njim.

## Sta ide na ispitu

Ovo je zadatak sa dva remote nivoa:

- `EAukcija` je ulaz u sistem;
- `Eksponat` je udaljeni objekat cije stanje ostaje na serveru.

`Eksponat` treba da bude remote zato sto vise klijenata treba da vidi istu cenu i istog trenutno prijavljenog klijenta. Da se eksponat salje kao `Serializable`, klijent bi radio nad kopijom i promene ne bi bile zajednicke.

`KlijentAukcije` treba da bude `Serializable` zato sto ne sadrzi udaljene metode. On samo prenosi podatke o klijentu na server.

## Tok rada

1. Server kreira `EAukcijaImpl`.
2. `EAukcijaImpl` kreira nekoliko `EksponatImpl` objekata.
3. Klijent preko `lookup` dobija `EAukcija`.
4. Klijent unosi id eksponata.
5. `EAukcija.vratiEksponat(...)` vraca remote referencu na eksponat.
6. Klijent nad eksponatom poziva licitaciju, povecanje cene ili odustajanje.

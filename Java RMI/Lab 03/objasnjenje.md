# eBanka - objasnjenje

## Ideja zadatka

Klijent preko glavnog servisa `EBanka` pronalazi korisnika po JBK broju. Zatim nad tim korisnikom prebacuje novac sa dinarskog na devizni racun ili obrnuto.

## Struktura

- `EBanka` je glavni remote interfejs i ima metodu `vratiKorisnika`.
- `EBankaImpl` cuva korisnike u memoriji.
- `Korisnik` je remote interfejs jednog bankarskog korisnika.
- `KorisnikImpl` cuva dinarsko i devizno stanje i izvrsava transfere.
- `Stanje` je `Serializable`, jer se stanje salje klijentu kao kopija.
- `StanjeImpl` nosi iznose na dinarskom i deviznom racunu.
- `Server` registruje `EBankaImpl`.
- `Klijent` prikazuje meni i poziva metode udaljenog korisnika.

## Sta ide na ispitu

Ovo je isti obrazac kao mobilni operater:

- glavni servis: `EBanka`;
- udaljeni entitet: `Korisnik`;
- snapshot podataka: `Stanje`.

Transferi se rade na serveru u `KorisnikImpl`, jer server cuva pravo stanje racuna. Klijent samo salje iznos i kurs.

Za ispit ne moras komplikovati validaciju. Bitno je da transfer menja odgovarajuca serverska polja:

- dinarski u devizni: `dinarski -= iznos`, `devizni += iznos / kurs`;
- devizni u dinarski: `devizni -= iznos`, `dinarski += iznos * kurs`.

## Tok rada

1. Server kreira `EBankaImpl`.
2. `EBankaImpl` kreira vise korisnika.
3. Klijent preko `lookup` dobija `EBanka`.
4. Klijent unosi JBK.
5. `EBanka.vratiKorisnika(...)` vraca remote referencu na korisnika.
6. Klijent poziva transfer ili `vratiStanje`.

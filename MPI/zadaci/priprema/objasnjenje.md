# Objasnjenje zadatka `priprema`

Ovaj zadatak je direktna priprema za laboratorijsku vezbu iz MPI I/O. Pokriva tri glavne stvari iz prezentacija:

1. pojedinacni pokazivac na fajl: `MPI_File_seek` + `MPI_File_write`
2. eksplicitni pomeraj: `MPI_File_read_at`
3. nekontinualni upis preko pogleda na fajl: `MPI_Type_vector` + `MPI_File_set_view` + `MPI_File_write_all`

Program je namerno pisan skolski, slicno primerima sa casa. Zato nema posebnih helper funkcija, nema `CHECK_MPI` macro-a i nema dodatne apstrakcije.

## Osnovna ideja zadatka

Pokrecemo 4 MPI procesa.

Svaki proces ima svoj `rank`:

```text
P0 -> rank = 0
P1 -> rank = 1
P2 -> rank = 2
P3 -> rank = 3
```

Svaki proces ima 10 integera:

```c
#define N 10
```

Bafer procesa se puni ovako:

```c
for (i = 0; i < N; i++) {
    buf[i] = rank * N + i;
}
```

Zato procesi imaju:

```text
P0: 0 1 2 3 4 5 6 7 8 9
P1: 10 11 12 13 14 15 16 17 18 19
P2: 20 21 22 23 24 25 26 27 28 29
P3: 30 31 32 33 34 35 36 37 38 39
```

`NW` je velicina bloka u trecem delu:

```c
#define NW 2
```

Znaci u trecem delu ne upisujemo svih 10 brojeva jednog procesa odjednom, nego blokove od po 2 broja.

## MPI_Init, rank i nprocs

Na pocetku:

```c
MPI_Init(&argc, &argv);
MPI_Comm_rank(MPI_COMM_WORLD, &rank);
MPI_Comm_size(MPI_COMM_WORLD, &nprocs);
```

Ovo je standardni pocetak MPI programa.

`MPI_Init` pokrece MPI okruzenje.

`MPI_Comm_rank` daje redni broj trenutnog procesa.

`MPI_Comm_size` daje ukupan broj procesa.

Zatim proveravamo:

```c
if (nprocs != 4) {
    ...
}
```

Zasto tacno 4? Zato sto slika u zadatku kaze "pretpostavljajuci da postoje 4 procesa". Treci deo je napisan za taj raspored.

## Deo I - upis u `file1.bin`

Prvi deo zadatka:

```text
Prvi proces upisuje 0-9 od pocetka datoteke,
drugi proces 10-19 sa pomerajem 10,
i tako dalje.
Koristiti pojedinacne pokazivace na datoteke.
```

Kod:

```c
MPI_File_open(MPI_COMM_WORLD, "file1.bin",
              MPI_MODE_CREATE | MPI_MODE_WRONLY,
              MPI_INFO_NULL, &fh);
```

Ovim svi procesi otvaraju isti fajl.

`MPI_MODE_CREATE` znaci: napravi fajl ako ne postoji.

`MPI_MODE_WRONLY` znaci: otvori za pisanje.

`MPI_INFO_NULL` znaci: ne koristimo dodatne MPI hints.

### Offset

```c
offset = (MPI_Offset)rank * N * sizeof(int);
```

Ovo je najbitniji red u prvom delu.

Offset je u bajtovima.

Svaki proces pre svog dela preskace `rank` grupa od po `N` integera.

Za `N = 10` i `sizeof(int) = 4`:

```text
P0: offset = 0 * 10 * 4 = 0
P1: offset = 1 * 10 * 4 = 40
P2: offset = 2 * 10 * 4 = 80
P3: offset = 3 * 10 * 4 = 120
```

Ako bi greskom stavio:

```c
offset = rank * N;
```

to bi bilo pogresno, jer bi to bilo 10 bajtova, a ne 10 integera.

### Pojedinacni pokazivac

```c
MPI_File_seek(fh, offset, MPI_SEEK_SET);
MPI_File_write(fh, buf, N, MPI_INT, &status);
```

Ovo je stil sa Termina 1.

`MPI_File_seek` pomera individualni pokazivac trenutnog procesa.

`MPI_SEEK_SET` znaci da se pomeraj meri od pocetka fajla.

`MPI_File_write` zatim upisuje `N` elemenata tipa `MPI_INT` iz `buf`.

Bitno:

```text
seek offset -> bajtovi
write count -> broj elemenata tipa MPI_INT
```

Na kraju:

```c
MPI_File_close(&fh);
```

Fajl se zatvara pre sledece faze.

## Deo II - citanje iz `file1.bin`

Drugi deo zadatka:

```text
Ponovo otvoriti datoteku.
Svaki proces cita podatke upravo napisane koristeci funkcije sa eksplicitnim pomerajem.
```

Zato opet otvaramo fajl:

```c
MPI_File_open(MPI_COMM_WORLD, "file1.bin",
              MPI_MODE_RDONLY,
              MPI_INFO_NULL, &fh);
```

Sada je mod:

```c
MPI_MODE_RDONLY
```

jer samo citamo.

### Eksplicitni pomeraj

```c
MPI_File_read_at(fh, offset, rbuf, N, MPI_INT, &status);
```

Ovo je razlika u odnosu na prvi deo.

Ovde nema:

```c
MPI_File_seek(...)
```

Funkcija `MPI_File_read_at` direktno dobija `offset`.

To znaci:

```text
citaj iz fajla od pozicije offset,
upisi u memorijski bafer rbuf,
procitaj N elemenata tipa MPI_INT
```

Tok podataka:

```text
file1.bin -> rbuf
```

U prvom delu tok je bio:

```text
buf -> file1.bin
```

### Provera citanja

```c
ok = 1;
for (i = 0; i < N; i++) {
    if (rbuf[i] != rank * N + i) {
        ok = 0;
    }
}
```

Svaki proces proverava da li je procitao bas svoje podatke.

Proces 2, na primer, ocekuje:

```text
20 21 22 23 24 25 26 27 28 29
```

Ako je sve dobro:

```c
printf("Proces %d: citanje OK [%d ... %d]\n",
       rank, rbuf[0], rbuf[N - 1]);
```

Redosled ispisa procesa nije bitan. Procesi rade paralelno, pa se linije mogu pojaviti razlicitim redosledom.

## Deo III - upis u `file2.bin`

Treci deo zadatka:

```text
Svaki proces upisuje upravo procitane podatke u novu datoteku
na raspored sa slike.
Poslednje upisivanje uciniti sto efikasnijim.
```

Slika trazi raspored:

```text
0 1 | 10 11 | 20 21 | 30 31 | 2 3 | 12 13 | 22 23 | 32 33 | ...
```

To znaci:

```text
uzmi 2 broja od P0,
pa 2 broja od P1,
pa 2 broja od P2,
pa 2 broja od P3,
onda opet 2 broja od P0,
i tako dalje.
```

To je round-robin pristup po blokovima od `NW = 2`.

Ovo nije kontinualan upis za jedan proces.

Proces 0 u fajlu treba da upise:

```text
0 1        pa kasnije 2 3        pa kasnije 4 5 ...
```

Izmedju njegovih blokova nalaze se blokovi drugih procesa.

Zato koristimo:

```text
izvedeni tip + file view + grupni upis
```

### MPI_Type_vector

```c
MPI_Type_vector(N / NW, NW, NW * nprocs, MPI_INT, &filetype);
MPI_Type_commit(&filetype);
```

Opsti oblik:

```c
MPI_Type_vector(count, blocklength, stride, oldtype, &newtype);
```

Kod nas:

```text
count       = N / NW
blocklength = NW
stride      = NW * nprocs
oldtype     = MPI_INT
```

Za `N = 10`, `NW = 2`, `nprocs = 4`:

```text
count       = 10 / 2 = 5
blocklength = 2
stride      = 2 * 4 = 8
```

Znacenje:

```text
filetype ima 5 blokova,
svaki blok ima 2 integera,
pocetak sledeceg bloka istog procesa udaljen je 8 integera.
```

Zasto 8? Zato sto jedan krug ima:

```text
2 integera P0 + 2 integera P1 + 2 integera P2 + 2 integera P3 = 8 integera
```

`MPI_Type_commit` je obavezan pre koriscenja izvedenog tipa.

### disp za file view

```c
disp = (MPI_Offset)rank * NW * sizeof(int);
```

`disp` je pocetak pogleda za svaki proces.

Za `NW = 2`:

```text
P0: disp = 0 * 2 * sizeof(int)
P1: disp = 1 * 2 * sizeof(int)
P2: disp = 2 * 2 * sizeof(int)
P3: disp = 3 * 2 * sizeof(int)
```

To znaci:

```text
P0 pocinje od mesta za 0 1
P1 pocinje od mesta za 10 11
P2 pocinje od mesta za 20 21
P3 pocinje od mesta za 30 31
```

### MPI_File_set_view

```c
MPI_File_set_view(fh, disp, MPI_INT, filetype,
                  "native", MPI_INFO_NULL);
```

Ova funkcija kaze procesu:

```text
od pozicije disp u fajlu,
vidis samo delove opisane tipom filetype,
sve ostalo preskaci.
```

Parametri:

- `fh` je otvoren fajl
- `disp` je bajtovski pomeraj od pocetka fajla
- `MPI_INT` je `etype`, osnovna jedinica pogleda
- `filetype` opisuje rupe i blokove
- `"native"` znaci standardna reprezentacija podataka na ovoj masini
- `MPI_INFO_NULL` znaci bez dodatnih hints

Najbitnija mentalna slika:

```text
stvarni fajl: P0 P1 P2 P3 P0 P1 P2 P3 ...
proces P0 vidi: P0          P0          ...
```

### MPI_File_write_all

```c
MPI_File_write_all(fh, rbuf, N, MPI_INT, &status);
```

Ovo je grupni upis.

Svi procesi moraju da pozovu ovu funkciju.

Zasto `_all`? Zato sto zadatak kaze "sto efikasnije". U Terminu 2 pravilo je:

```text
file view + grupna operacija = efikasniji pristup
```

Svaki proces upisuje svojih `N` integera iz `rbuf`, ali zbog `file view`-a ti integeri zavrsavaju u odvojenim blokovima u stvarnom fajlu.

Bez `file view`-a, proces bi upisao svih 10 integera kontinualno. Sa `file view`-om, MPI ih rasporedi u blokove od po 2.

## MPI_Barrier pre prikaza

```c
MPI_Barrier(MPI_COMM_WORLD);
```

Ovo je tu samo pre provere da proces 0 ne krene da cita `file2.bin` za prikaz pre nego sto su svi zavrsili treci deo.

Nije sustina MPI I/O resenja, ali je korisno za uredan ispis.

## Provera sa obicnim C fajlom

Na kraju rank 0 radi:

```c
FILE *fp = fopen("file2.bin", "rb");
fread(result, sizeof(int), total, fp);
```

Ovo nije MPI I/O deo zadatka. To je samo brz nacin da se prikaze sta pise u binarnom fajlu.

Na ispitu/labu, glavni deo su MPI funkcije pre toga.

## Funkcije koje ovaj zadatak pokriva

```c
MPI_File_open
MPI_File_seek
MPI_File_write
MPI_File_read_at
MPI_Type_vector
MPI_Type_commit
MPI_File_set_view
MPI_File_write_all
MPI_File_close
MPI_Type_free
```

## Najcesce greske

Offset i `disp` su u bajtovima:

```c
rank * N * sizeof(int)
rank * NW * sizeof(int)
```

`count` u `read/write` je broj elemenata, ne broj bajtova:

```c
MPI_File_write(fh, buf, N, MPI_INT, &status);
```

`stride` u `MPI_Type_vector` je u elementima tipa `oldtype`, ovde u integerima:

```c
NW * nprocs
```

`MPI_File_write_all` je grupna funkcija. Ako jedan proces ne udje u nju, ostali mogu da cekaju.


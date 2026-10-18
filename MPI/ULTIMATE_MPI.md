# Ultimativni MPI - priprema za ispit

Ova skripta je napravljena prema:

- Markdown blanketima iz foldera `Blanketi`;
- `Materijal/beleske.txt` i `Materijal/beleske_2.txt`;
- prezentacijama `MPI Parallel IO Termin 1.md` i `MPI Parallel IO Termin 2.md`;
- dozvoljenom podsetniku `Materijal/MPI_DS_funkcije.md`;
- postojecim primerima sa vezbi.

U `DS Kol I 2026.md` nema MPI zadatka, pa je taj blanket preskocen. U ostala tri blanketa postoji po jedan MPI zadatak. Naknadno su dodati i zadaci sa rokova APR25, Oktobar 2 2025 i JUN26, prema dostavljenom tekstu i slikama zadataka.

Konvertovani Markdown zadrzava pozivanje na slike, ali ne i same slike. Za zadatak iz juna 2024. sema sa slike je: P0, P1 i P2 prvo upisuju po 1 element, zatim po 2, pa po 3 i tako dalje do 14. Za zadatak iz aprila 2026. prvo se redom upisuju prve polovine svih procesa, a zatim druge polovine svih procesa.

## Teorijski uvod - funkcije koje te cekaju

### Osnovni MPI pojmovi

MPI program se pokrece kao vise procesa. Svaki proces izvrsava isti program, ali ima svoj rang:

```c
MPI_Comm_rank(MPI_COMM_WORLD, &rank);
MPI_Comm_size(MPI_COMM_WORLD, &nprocs);
```

- `MPI_COMM_WORLD` je komunikator koji sadrzi sve pokrenute procese.
- `rank` je identifikator procesa, od `0` do `nprocs - 1`.
- `nprocs` je ukupan broj procesa.
- Procesi nemaju zajednicke lokalne promenljive. Svaki proces ima svoj `buf`, `rank`, pokazivac i adresni prostor.
- Komunikator odredjuje grupu procesa koja ucestvuje u komunikaciji ili kolektivnoj I/O operaciji.

Standardni pocetak i kraj programa su `MPI_Init` i `MPI_Finalize`, ali se oni na ispitu obicno podrazumevaju. Sustina zadatka su podela posla, komunikacija, offseti i pogled na fajl.

### Point-to-point i kolektivna komunikacija

Point-to-point operacije komuniciraju izmedju konkretnog posiljaoca i primaoca:

```c
MPI_Send(buf, count, datatype, dest, tag, comm);
MPI_Recv(buf, count, datatype, source, tag, comm, &status);
```

- `dest` i `source` su rankovi unutar prosledjenog komunikatora.
- `tag` razlikuje vrste poruka.
- `count` je broj elemenata, a ne broj bajtova, osim kada je `datatype = MPI_BYTE`.
- Kod blokirajucih operacija treba proveriti redosled da svi procesi ne ostanu zaglavljeni u `MPI_Send`.
- `MPI_Sendrecv` bezbedno objedinjuje jedno slanje i jedno primanje i zato je koristan za prsten i razmenu sa susedima.

Neblokirajuce funkcije odmah vracaju `MPI_Request`:

```c
MPI_Irecv(recvbuf, count, datatype, source, tag, comm, &req1);
MPI_Isend(sendbuf, count, datatype, dest, tag, comm, &req2);

/* koristan rad */

MPI_Wait(&req1, &status);
MPI_Wait(&req2, &status);
```

`MPI_Wait` garantuje da je operacija zavrsena. `MPI_Test` samo proverava stanje i ne blokira. Bafer koji ucestvuje u nezavrsenoj neblokirajucoj operaciji ne treba menjati.

Kolektivne funkcije pozivaju svi procesi komunikatora:

- `MPI_Bcast` salje iste podatke od jednog root procesa svima;
- `MPI_Scatter` deli niz sa root procesa na jednake delove;
- `MPI_Gather` sakuplja jednake lokalne delove na root procesu;
- `MPI_Reduce` kombinuje lokalne rezultate, na primer pomocu `MPI_SUM` ili `MPI_MAX`;
- `MPI_Scan` racuna prefiksnu redukciju;
- `MPI_Barrier` zadrzava svaki proces dok svi ne stignu do iste tacke.

Najvaznije pravilo: ako je funkcija kolektivna, svi clanovi tog komunikatora moraju da je pozovu istim redosledom. U suprotnom program moze zauvek cekati.

### Zasto MPI-IO

Los tradicionalni pristup je da svi procesi posalju podatke procesu 0, a da samo proces 0 pise fajl. Time proces 0 postaje usko grlo. Druga losa krajnost je da svaki proces pravi poseban fajl, jer se dobija veliki broj fajlova koje kasnije treba spajati.

MPI-IO omogucava da svi procesi pristupaju istom binarnom fajlu. Svaki proces opisuje deo fajla koji mu pripada, a MPI implementacija organizuje paralelno citanje ili pisanje.

Binarni fajl nema tekstualne redove. On je niz bajtova. Ako se integer u datom sistemu cuva u 4 bajta, vrednost `10` se u little-endian hex prikazu vidi kao:

```text
0A 00 00 00
```

Red od 16 bajtova koji prikazuje hex editor samo je nacin prikaza: to nije logicki red u fajlu.

### Otvaranje i zatvaranje fajla

```c
MPI_File_open(MPI_COMM_WORLD, "podaci.bin",
              MPI_MODE_CREATE | MPI_MODE_WRONLY,
              MPI_INFO_NULL, &fh);

MPI_File_close(&fh);
```

- `MPI_File_open` otvara jedan zajednicki fajl za procese komunikatora.
- `MPI_MODE_RDONLY`, `MPI_MODE_WRONLY` i `MPI_MODE_RDWR` odredjuju pristup.
- `MPI_MODE_CREATE` kreira fajl ako ne postoji.
- `MPI_INFO_NULL` znaci da ne prosledjujemo dodatne optimizacione savete.
- `fh` je MPI file handle.
- `open` i `close` su kolektivni nad komunikatorom prosledjenim u `open`.

### Tri nacina odredjivanja pozicije u fajlu

#### 1. Pojedinacni pokazivac

Svaki proces ima svoj pokazivac. Promena pokazivaca jednog procesa ne menja pokazivace ostalih:

```c
MPI_File_seek(fh, offset, MPI_SEEK_SET);
MPI_File_read(fh, buf, count, MPI_INT, &status);
MPI_File_write(fh, buf, count, MPI_INT, &status);
```

Ovo koristis kada zadatak kaze **pojedinacni pokazivac** ili **implicitni pomeraj**.

`MPI_SEEK_SET` racuna pomeraj od pocetka fajla, `MPI_SEEK_CUR` od trenutne pozicije, a `MPI_SEEK_END` od kraja.

Mali primer: tri procesa upisuju po 4 integera u odvojene delove fajla.

```c
int N = 4;
MPI_Offset offset =
    (MPI_Offset)rank * N * sizeof(int);

MPI_File_seek(fh, offset, MPI_SEEK_SET);
MPI_File_write(fh, buf, N, MPI_INT, &status);
```

Raspored je:

```text
P0 pise pozicije 0-3,  offset = 0 bajtova
P1 pise pozicije 4-7,  offset = 16 bajtova
P2 pise pozicije 8-11, offset = 32 bajta
```

Ovde je pretpostavljeno da `sizeof(int) = 4`. Svaki proces prvo pomera samo svoj pokazivac, a `MPI_File_write` zatim upisuje 4 integera i pomera taj pokazivac iza njih.

#### 2. Eksplicitni pomeraj

Pozicija se prosledjuje direktno I/O funkciji i ne menja pojedinacni pokazivac:

```c
MPI_File_read_at(fh, offset, buf, count, MPI_INT, &status);
MPI_File_write_at(fh, offset, buf, count, MPI_INT, &status);
```

Ovo koristis kada zadatak izricito trazi **funkcije sa eksplicitnim pomerajem**. Tada ne pises `MPI_File_seek`.

Mali primer: svaki od tri procesa cita ista 4 integera koja je u prethodnom primeru upisao:

```c
int N = 4;
MPI_Offset offset =
    (MPI_Offset)rank * N * sizeof(int);

MPI_File_read_at(fh, offset, read_buf,
                 N, MPI_INT, &status);
```

Proces P1 direktno cita od bajta 16, bez prethodnog `seek`. Vrednost `offset` vazi samo za ovaj poziv i ne menja pojedinacni pokazivac procesa.

Najkraca razlika:

```text
pojedinacni pokazivac: seek(offset), pa read/write
eksplicitni pomeraj:  read_at/write_at odmah dobija offset
```

#### 3. Deljeni pokazivac

Svi procesi dele jednu poziciju:

```c
MPI_File_read_shared(fh, buf, count, MPI_INT, &status);
MPI_File_write_shared(fh, buf, count, MPI_INT, &status);
```

Posle svakog poziva pokazivac se pomera. Ne moze se predvideti koji ce rank prvi pristupiti fajlu. To je dobro kada svaki proces treba da uzme sledeci slobodan blok, a nije vazno koji rank dobija koji blok.

Mali primer: fajl ima 20 integera, a cetiri procesa citaju po 5:

```c
MPI_File_read_shared(fh, read_buf,
                     5, MPI_INT, &status);
```

Deljeni pokazivac na pocetku pokazuje na poziciju 0. Proces koji prvi izvrsi poziv dobija pozicije 0-4 i pomera zajednicki pokazivac na poziciju 5. Sledeci proces dobija pozicije 5-9, zatim 10-14 i poslednji 15-19.

Moguci raspored je:

```text
P2 dobije 0-4
P0 dobije 5-9
P3 dobije 10-14
P1 dobije 15-19
```

Moguc je i bilo koji drugi redosled rankova. Bitno je da svaki poziv uzima sledeci blok sa zajednicke pozicije. Zato se shared pokazivac koristi kada raspodela konkretnih blokova po rankovima nije vazna.

Ordered varijante koriste deljeni pokazivac, ali rade po rastucem ranku:

```c
MPI_File_read_ordered(fh, buf, count, MPI_INT, &status);
MPI_File_write_ordered(fh, buf, count, MPI_INT, &status);
```

`shared` znaci nepredvidiv redosled procesa, a `ordered` predvidiv redosled po ranku.

### Najvaznija razlika: offset i count

Za `N` integera po procesu:

```c
offset = (MPI_Offset)rank * N * sizeof(int);
MPI_File_read_at(fh, offset, buf, N, MPI_INT, &status);
```

- `offset` je u bajtovima, zato se mnozi sa `sizeof(int)`;
- `N` je broj elemenata tipa `MPI_INT`, zato se ne mnozi sa `sizeof(int)`.

Ako se radi sa sirovim bajtovima:

```c
offset = (MPI_Offset)rank * bytes_per_process;
MPI_File_read_at(fh, offset, buf,
                 bytes_per_process, MPI_BYTE, &status);
```

Tada su i offset i count brojcano izrazeni u bajtovima, jer jedan `MPI_BYTE` predstavlja jedan bajt.

### Izvedeni tipovi

Izvedeni tip opisuje raspored elemenata, a ne njihove vrednosti.

Kontinualni tip:

```c
MPI_Type_contiguous(N, MPI_INT, &newtype);
MPI_Type_commit(&newtype);
```

Vektorski tip:

```c
MPI_Type_vector(num_blocks, blocklength, stride,
                MPI_INT, &filetype);
MPI_Type_commit(&filetype);
```

- `num_blocks` je broj blokova;
- `blocklength` je broj elemenata u jednom bloku;
- `stride` je rastojanje od pocetka jednog do pocetka sledeceg bloka, u elementima tipa `oldtype`.

`MPI_Type_vector` se koristi kada svi blokovi imaju istu duzinu i pravilan razmak.

Indeksirani tip:

```c
MPI_Type_indexed(num_blocks, blocklengths, displs,
                 MPI_INT, &filetype);
MPI_Type_commit(&filetype);
```

Koristi se kada blokovi imaju razlicite duzine ili nepravilne polozaje. `blocklengths[i]` i `displs[i]` posebno opisuju svaki blok. `displs` je u elementima tipa `oldtype`.

`MPI_Type_create_subarray` opisuje jednu podmatricu unutar globalne matrice. `MPI_Type_create_darray` automatski opisuje deo distribuiranog vise-dimenzionalnog niza za dati rank.

`MPI_Type_commit` je obavezan pre upotrebe izvedenog tipa.

### Razlika izmedju contiguous, vector i indexed tipa

Sva tri izvedena tipa opisuju **gde se nalaze elementi**, ali opisuju razlicite rasporede.

| Tip | Raspored | Kada se koristi |
|---|---|---|
| `MPI_Type_contiguous` | Jedan neprekinut niz elemenata, bez rupa | Svi elementi su jedan do drugog |
| `MPI_Type_vector` | Vise blokova iste duzine, sa istim razmakom | Pravilan, periodican raspored |
| `MPI_Type_indexed` | Blokovi mogu imati razlicite duzine i polozaje | Nepravilan raspored |

#### Contiguous

```c
MPI_Type_contiguous(10, MPI_INT, &contig);
MPI_Type_commit(&contig);
```

Jedna instanca tipa `contig` sadrzi 10 susednih integera:

```text
[0 1 2 3 4 5 6 7 8 9]
```

Nema praznina i nema potrebe za nizom pomeraja. Parametar `10` je broj elemenata tipa `MPI_INT`, a ne broj bajtova.

#### Vector

```c
MPI_Type_vector(count, blocklength, stride,
                MPI_INT, &vector_type);
MPI_Type_commit(&vector_type);
```

Vector opisuje `count` blokova. Svaki blok ima `blocklength` integera, a od pocetka jednog do pocetka sledeceg bloka ima `stride` integera.

Primer:

```c
MPI_Type_vector(3, 2, 5, MPI_INT, &vector_type);
```

Opisani elementi su:

```text
[X X . . . X X . . . X X]
```

- postoje 3 bloka;
- svaki blok ima 2 integera;
- poceci blokova su udaljeni 5 integera;
- izmedju blokova ostaju po 3 nevidljiva integera.

`stride` ukljucuje i sam blok i rupu posle njega. Ne predstavlja samo velicinu rupe.

#### Indexed

```c
int blocklengths[3] = { 1, 3, 2 };
int displs[3] = { 0, 5, 11 };

MPI_Type_indexed(3, blocklengths, displs,
                 MPI_INT, &indexed_type);
MPI_Type_commit(&indexed_type);
```

Ovaj tip opisuje:

- 1 integer od pozicije 0;
- 3 integera od pozicije 5;
- 2 integera od pozicije 11.

Sema je:

```text
[X . . . . X X X . . . X X]
```

Kod `indexed` tipa ne postoji jedan zajednicki `blocklength` ni jedan zajednicki `stride`. Svaki blok ima svoju duzinu i pocetak.

Posto je `oldtype = MPI_INT`, vrednosti u `blocklengths` i `displs` predstavljaju broj integera. Kada bi `oldtype` bio `MPI_BYTE`, predstavljale bi bajtove.

### Da li su 4 procesa isto sto i 4 bloka tipa

Nisu. Ovo je veoma vazna razlika:

```text
broj procesa != count u MPI_Type_vector
```

`count` u `MPI_Type_vector` je broj blokova koje **jedan proces vidi u svom izvedenom tipu**. Ne govori koliko ukupno procesa postoji.

Takodje postoje dva razlicita parametra koja se zovu `count` u razlicitim funkcijama:

```c
MPI_Type_vector(count, blocklength, stride, oldtype, &newtype);
MPI_File_read(fh, buf, count, datatype, &status);
```

- `count` u `MPI_Type_vector` je broj blokova u izvedenom tipu;
- `count` u `MPI_File_read` je broj elemenata tipa `datatype` koji se prenose u memorijski bafer.

### Primer: fajl ima 40 integera, postoje 4 procesa

Pretpostavimo:

```text
40 integera ukupno
4 procesa
10 integera po procesu
sizeof(int) = 4 bajta
velicina fajla = 40 * 4 = 160 bajtova
```

#### Slucaj 1 - svaki proces cita jedan kontinualni blok od 10

Raspored je:

```text
P0: pozicije  0-9
P1: pozicije 10-19
P2: pozicije 20-29
P3: pozicije 30-39
```

Ovde izvedeni tip uopste nije potreban. Svaki proces ima samo jedan kontinualni blok:

```c
int local_n = 40 / nprocs;  /* 10 */

MPI_Offset offset =
    (MPI_Offset)rank * local_n * sizeof(int);

MPI_File_read_at(fh, offset, buf,
                 local_n, MPI_INT, &status);
```

Za procese su offseti:

| Proces | Preskoceni integeri | Offset u bajtovima |
|---:|---:|---:|
| P0 | 0 | 0 |
| P1 | 10 | 40 |
| P2 | 20 | 80 |
| P3 | 30 | 120 |

U pozivu `MPI_File_read_at` pise se:

```c
count = 10;
datatype = MPI_INT;
```

Ne pise se:

```c
count = 10 * sizeof(int);
```

Razlog je sto `count` predstavlja broj elemenata tipa `MPI_INT`. Samo je `offset` u bajtovima.

Ako bi se bas pravio kontinualni tip, mogao bi da bude:

```c
MPI_Type_contiguous(10, MPI_INT, &ten_ints);
MPI_Type_commit(&ten_ints);
```

Ali to je nepotrebno za obicno citanje jednog kontinualnog bloka, jer `MPI_File_read_at(..., 10, MPI_INT, ...)` vec potpuno opisuje zahtev.

Pogresno bi bilo:

```c
MPI_Type_vector(4, 10, 10, MPI_INT, &filetype);
```

Ovo ne znaci "4 procesa po 10". To znaci da **svaki proces koji koristi taj tip** vidi 4 bloka po 10 integera, odnosno ukupno 40 integera.

#### Slucaj 2 - svaki proces cita svaki cetvrti integer

Raspored je:

```text
P0: 0, 4, 8, 12, ..., 36
P1: 1, 5, 9, 13, ..., 37
P2: 2, 6, 10, 14, ..., 38
P3: 3, 7, 11, 15, ..., 39
```

Svaki proces vidi 10 blokova. Svaki blok ima jedan integer, a poceci blokova su udaljeni 4 integera:

```c
MPI_Type_vector(10, 1, 4, MPI_INT, &filetype);
MPI_Type_commit(&filetype);

disp = (MPI_Offset)rank * sizeof(int);

MPI_File_set_view(fh, disp, MPI_INT, filetype,
                  "native", MPI_INFO_NULL);

MPI_File_read_all(fh, buf, 10, MPI_INT, &status);
```

Ovde je:

```text
count u vectoru = 10 blokova po procesu
blocklength = 1 integer po bloku
stride = 4 integera
```

Broj blokova je 10, a ne 4. Cetiri je broj procesa i istovremeno stride samo zato sto se uzima svaki cetvrti element.

#### Slucaj 3 - svaki proces cita dva bloka od po 5

Pretpostavimo round-robin raspored:

```text
P0(5) P1(5) P2(5) P3(5) P0(5) P1(5) P2(5) P3(5)
```

Svaki proces ukupno cita 10 integera, ali kroz 2 odvojena bloka:

```c
MPI_Type_vector(2, 5, 5 * nprocs,
                MPI_INT, &filetype);
MPI_Type_commit(&filetype);

disp = (MPI_Offset)rank * 5 * sizeof(int);

MPI_File_set_view(fh, disp, MPI_INT, filetype,
                  "native", MPI_INFO_NULL);

MPI_File_read_all(fh, buf, 10, MPI_INT, &status);
```

Za cetiri procesa parametri su:

```text
count u vectoru = 2 bloka po procesu
blocklength = 5 integera
stride = 5 * 4 = 20 integera
disp = rank * 5 * sizeof(int) bajtova
```

Ponovo, `blocklength` je `5`, a ne `5 * sizeof(int)`. Mnozenje sa `sizeof(int)` koristi se samo za `disp` zato sto je `disp` u bajtovima.

### Pravilo za brzo odredjivanje tipa

Postavi sebi tri pitanja:

1. Da li proces ima samo jedan neprekinut blok? Koristi obican offset i `read/write`; `contiguous` najcesce nije ni potreban.
2. Da li proces ima vise blokova jednake duzine i jednakog razmaka? Koristi `MPI_Type_vector`.
3. Da li se duzine blokova ili razmaci razlikuju? Koristi `MPI_Type_indexed`.

Za `MPI_Type_vector` racunaj:

```text
count       = koliko blokova pripada jednom procesu
blocklength = koliko oldtype elemenata ima jedan blok
stride      = koliko oldtype elemenata ima od pocetka mog bloka
              do pocetka sledeceg mog bloka
```

Tek nakon toga racunaj:

```text
disp = pocetak prvog bloka tog ranka, u bajtovima
```

### File view - pogled procesa na fajl

```c
MPI_File_set_view(fh, disp, MPI_INT, filetype,
                  "native", MPI_INFO_NULL);
```

- `disp` je pocetak pogleda u bajtovima;
- `etype` je osnovna jedinica fajla, ovde `MPI_INT`;
- `filetype` opisuje vidljive blokove i rupe izmedju njih;
- `"native"` koristi prirodnu binarnu reprezentaciju sistema;
- svaki proces moze imati drugaciji `disp` i `filetype`.

Posle postavljanja pogleda proces vidi samo svoje blokove kao da su kontinualni. Zato jedan kontinualan lokalni bafer moze jednim pozivom biti rasporedjen na vise nekontinualnih mesta u fajlu.

### Nezavisne i kolektivne I/O operacije

Nezavisne operacije ne cekaju da drugi procesi pozovu istu I/O funkciju:

```c
MPI_File_read(fh, buf, count, datatype, &status);
MPI_File_write(fh, buf, count, datatype, &status);
```

Kolektivne I/O operacije pozivaju svi procesi komunikatora:

```c
MPI_File_read_all(fh, buf, count, datatype, &status);
MPI_File_write_all(fh, buf, count, datatype, &status);
```

Kolektivne operacije omogucavaju MPI implementaciji da spoji mnogo sitnih i isprepletanih zahteva u manji broj vecih I/O pristupa. Kada zadatak kaze **sto efikasnije**, u ovoj oblasti se najcesce ocekuje:

```text
izvedeni tip + MPI_File_set_view + MPI_File_read_all/write_all
```

Ipak, tekst zadatka ima prednost. Ako pise pojedinacni pokazivac, koristi se `seek + read/write`, cak iako bi kolektivna funkcija mogla biti brza.

### Neblokirajuci MPI-IO

```c
MPI_File_iwrite_at(fh, offset, buf, count,
                   MPI_INT, &request);

/* racunanje koje ne menja buf */

MPI_Wait(&request, &status);
```

`MPI_File_iwrite_at` pokrece operaciju i odmah vraca zahtev. Proces moze da racuna dok traje I/O. `MPI_Wait` je potreban pre ponovne upotrebe bafera, zatvaranja fajla ili oslanjanja na rezultat operacije.

### Konzistentnost

Problem nastaje kada vise procesa pristupa istom fajlu i bar jedan proces pise, posebno kada jedan proces treba da procita ono sto je drugi upravo upisao.

Najjednostavniji ispitni obrazac iz beleski je:

```c
/* faza upisa */
MPI_File_close(&fh);

MPI_Barrier(MPI_COMM_WORLD);

MPI_File_open(MPI_COMM_WORLD, "podaci.bin",
              MPI_MODE_RDONLY, MPI_INFO_NULL, &fh);
/* faza citanja */
```

Time su upis i citanje razdvojeni u dve sekvence. Druge opcije su:

- `MPI_File_set_atomicity(fh, 1)` uz odgovarajucu barijeru;
- `MPI_File_sync(fh)` za sinhronizovanje fajla.

`MPI_File_sync` je kolektivan. Ako ga pozove jedan proces komunikatora, moraju ga pozvati i ostali. Atomicnost moze smanjiti performanse, pa je `close -> barrier -> open` najlaksi odgovor kada zadatak ne zahteva naprednije resenje.

## Povezan realan primer - dva binarna fajla

### Postavka

Program se pokrece sa cetiri procesa. Svaki proces ima cetiri integera:

```text
P0:  0  1  2  3
P1: 10 11 12 13
P2: 20 21 22 23
P3: 30 31 32 33
```

Potrebno je:

1. pojedinacnim pokazivacem upisati procesne blokove u `ulaz.bin`;
2. ponovo otvoriti fajl i eksplicitnim pomerajem procitati svoj blok;
3. efikasno upisati `izlaz.bin` ciklicno, po dva integera svakog procesa.

### Korak 1 - kontinualni upis u ulaz.bin

Svaki proces ima `N = 4` integera. Njegov pocetni pomeraj je:

```text
offset = rank * N * sizeof(int)
```

Ako je `sizeof(int) = 4`, pocetni bajtovi procesa su:

| Proces | Pocetni integer | Pocetni bajt |
|---:|---:|---:|
| P0 | 0 | 0 |
| P1 | 4 | 16 |
| P2 | 8 | 32 |
| P3 | 12 | 48 |

Kljucni kod:

```c
#define N 4
#define BLOCK 2

offset = (MPI_Offset)rank * N * sizeof(int);

MPI_File_open(MPI_COMM_WORLD, "ulaz.bin",
              MPI_MODE_CREATE | MPI_MODE_WRONLY,
              MPI_INFO_NULL, &fh);

MPI_File_seek(fh, offset, MPI_SEEK_SET);
MPI_File_write(fh, buf, N, MPI_INT, &status);

MPI_File_close(&fh);
```

Logicki sadrzaj `ulaz.bin` je:

```text
0 1 2 3 | 10 11 12 13 | 20 21 22 23 | 30 31 32 33
```

Fajl ima `4 procesa * 4 integera * 4 bajta = 64 bajta`, ako je integer velicine 4 bajta.

### Korak 2 - citanje svog bloka eksplicitnim pomerajem

Koristi se isti offset jer svaki proces cita tacno ono sto je prethodno upisao:

```c
MPI_Barrier(MPI_COMM_WORLD);

MPI_File_open(MPI_COMM_WORLD, "ulaz.bin",
              MPI_MODE_RDONLY, MPI_INFO_NULL, &fh);

MPI_File_read_at(fh, offset, read_buf,
                 N, MPI_INT, &status);

MPI_File_close(&fh);
```

Ovde nema `MPI_File_seek`. To je glavna stvar koju treba pokazati kada tekst trazi eksplicitni pomeraj.

Posle citanja:

```text
P0 read_buf:  0  1  2  3
P1 read_buf: 10 11 12 13
P2 read_buf: 20 21 22 23
P3 read_buf: 30 31 32 33
```

### Korak 3 - nekontinualni kolektivni upis u izlaz.bin

Zeljeni raspored je:

```text
0 1 | 10 11 | 20 21 | 30 31 |
2 3 | 12 13 | 22 23 | 32 33
```

Svaki proces ima:

- `N / BLOCK = 4 / 2 = 2` bloka;
- `BLOCK = 2` integera po bloku;
- `BLOCK * nprocs = 2 * 4 = 8` integera od pocetka jednog do pocetka sledeceg svog bloka.

Zato je filetype:

```c
MPI_Type_vector(N / BLOCK, BLOCK, BLOCK * nprocs,
                MPI_INT, &filetype);
MPI_Type_commit(&filetype);
```

Pocetak pogleda zavisi od ranka:

```c
disp = (MPI_Offset)rank * BLOCK * sizeof(int);
```

Za integer od 4 bajta:

| Proces | `disp` | Vidljivi blokovi integer pozicija |
|---:|---:|---|
| P0 | 0 bajtova | 0-1 i 8-9 |
| P1 | 8 bajtova | 2-3 i 10-11 |
| P2 | 16 bajtova | 4-5 i 12-13 |
| P3 | 24 bajta | 6-7 i 14-15 |

Kljucni upis:

```c
MPI_File_open(MPI_COMM_WORLD, "izlaz.bin",
              MPI_MODE_CREATE | MPI_MODE_WRONLY,
              MPI_INFO_NULL, &fh);

MPI_File_set_view(fh, disp, MPI_INT, filetype,
                  "native", MPI_INFO_NULL);

MPI_File_write_all(fh, read_buf, N,
                   MPI_INT, &status);

MPI_File_close(&fh);
```

`read_buf` je u memoriji kontinualan, na primer `10 11 12 13`. Pogled procesa P1 govori MPI-ju da:

- `10 11` upise na integer pozicije 2-3;
- preskoci pozicije koje pripadaju drugim procesima;
- `12 13` upise na pozicije 10-11.

Svi procesi pozivaju `MPI_File_write_all`. MPI vidi sve njihove zahteve i moze efikasnije da organizuje pristup disku.

### Sta ovaj primer povezuje

- `rank` odredjuje koji deo fajla pripada procesu;
- `offset` je u bajtovima;
- `count = N` predstavlja broj integera;
- `seek + write` predstavlja pojedinacni pokazivac;
- `read_at` predstavlja eksplicitni pomeraj;
- `MPI_Type_vector` opisuje pravilno ponavljanje blokova;
- `disp` pomera prvi vidljivi blok procesa;
- `MPI_File_set_view` daje svakom procesu drugaciji pogled;
- `MPI_File_write_all` daje kolektivni i efikasniji upis;
- blokovi se ne preklapaju, pa nema dva procesa koja pisu iste bajtove.

## Kako pisati MPI resenje u ispitnoj svesci

Ne treba prepisivati ceo C program ako zadatak to izricito ne trazi. Podrazumevaju se:

- zaglavlja, `main`, `MPI_Init` i `MPI_Finalize`;
- provera povratnih vrednosti i alokacija standardnih bafera;
- `MPI_Comm_rank(MPI_COMM_WORLD, &rank)` i `MPI_Comm_size(MPI_COMM_WORLD, &nprocs)`, ali u resenju mora biti jasno sta su `rank` i `nprocs`;
- generisanje proizvoljnih ili slucajnih podataka, osim ako je ono sustina zadatka.

U svesci treba napisati deo koji dokazuje da je raspored podataka dobro shvacen:

1. formule za broj elemenata i pomeraj;
2. trazeni nacin pristupa: pojedinacni pokazivac, eksplicitni pomeraj ili deljeni pokazivac;
3. izvedeni tip i pogled na fajl kada je raspored nekontinualan;
4. kolektivnu I/O operaciju kada zadatak trazi efikasnost;
5. sinhronizaciju kada se prelazi iz faze upisa u fazu citanja.

Najvaznije jedinice:

- `MPI_Offset offset` u `MPI_File_seek` i `_at` funkcijama je u bajtovima;
- `disp` u `MPI_File_set_view` je u bajtovima;
- `count` u `MPI_File_read/write` je broj elemenata tipa `datatype`;
- `stride` i pomeraji u `MPI_Type_vector/indexed` izrazeni su u elementima tipa `oldtype`.

---

## Stvarni zadatak 1 - DS Kol I 2024, zadatak 3

### 1. Originalni tekst zadatka

> Napisati MPI program koji vrsi paralelni upis i citanje binarne datoteke, prema sledecim zahtevima:
>
> 1) Svaki proces upisuje N slucajno generisanih celih brojeva u datoteku dat.dat. Upis se vrsi upotrebom pojedinacnih pokazivaca, dok redosled podataka u fajlu ide od podataka poslednjeg do podataka prvog procesa.
>
> 2) Ponovo otvoriti datoteku. Svi procesi vrse citanje N podataka iz datoteke dat.dat, tako da se ne moze predvideti koji ce proces procitati koji deo datoteke. Obratiti paznju na konzistentnost prilikom citanja.

### 2. Analiza podele podataka i posla

Svaki proces ima lokalni bafer `buf` sa `N` integera. Ako postoji `nprocs` procesa, fajl sadrzi ukupno:

```text
nprocs * N integera
```

Podaci se upisuju obrnutim redosledom procesa:

```text
podaci P(nprocs-1), podaci P(nprocs-2), ..., podaci P1, podaci P0
```

Proces `rank` zato preskace:

```text
(nprocs - 1 - rank) * N integera
```

Posto `MPI_File_seek` ocekuje bajtove:

```text
offset = (nprocs - 1 - rank) * N * sizeof(int)
```

U drugom delu proces ne sme unapred da zna koji blok dobija. To je direktna primena deljenog pokazivaca. Svaki `MPI_File_read_shared` cita sledecih `N` integera i pomera isti pokazivac, ali redosled pristupanja procesa nije garantovan.

### 3. Kljucni MPI kod za ispit

Pretpostavlja se da su poznati `rank`, `nprocs`, `N`, `buf`, `read_buf`, `fh` i `status`.

```c
MPI_Offset offset;

offset = (MPI_Offset)(nprocs - 1 - rank) * N * sizeof(int);

MPI_File_open(MPI_COMM_WORLD, "dat.dat",
              MPI_MODE_CREATE | MPI_MODE_WRONLY,
              MPI_INFO_NULL, &fh);

MPI_File_seek(fh, offset, MPI_SEEK_SET);
MPI_File_write(fh, buf, N, MPI_INT, &status);
MPI_File_close(&fh);

MPI_Barrier(MPI_COMM_WORLD);

MPI_File_open(MPI_COMM_WORLD, "dat.dat",
              MPI_MODE_RDONLY, MPI_INFO_NULL, &fh);

MPI_File_read_shared(fh, read_buf, N, MPI_INT, &status);

MPI_File_close(&fh);
```

### 4. Objasnjenje promenljivih, formula i redosleda

- `rank` je rang procesa od `0` do `nprocs - 1`.
- `nprocs - 1 - rank` obrce redosled. Poslednji proces dobija pomeraj `0`, a proces 0 dobija najveci pomeraj.
- `N * sizeof(int)` je velicina jednog procesnog bloka u bajtovima.
- `MPI_File_seek + MPI_File_write` koristi se zato sto je izricito trazen pojedinacni pokazivac.
- Svaki proces ima svoj pojedinacni pokazivac, pa pomeranje jednog ne menja pokazivac drugog.
- `MPI_File_close`, barijera i novo otvaranje razdvajaju fazu upisa od faze citanja. Ovo je najjednostavniji pristup konzistentnosti iz beleski.
- Posle ponovnog otvaranja deljeni pokazivac je na pocetku fajla, pa dodatni `seek` nije potreban.
- `MPI_File_read_shared` pomera zajednicki pokazivac za `N` elemenata. Blokovi se ne preklapaju, ali nije poznato koji rank prvi stize do pokazivaca.
- Fajl je otvoren preko `MPI_COMM_WORLD`, pa svi procesi moraju pozvati kolektivne operacije `open`, `close` i barijeru istim redosledom.

Primer za cetiri procesa:

```text
fajl posle upisa: P3 | P2 | P1 | P0
citanje: svaki proces dobija jedan blok, ali dodela blok -> rank nije predvidiva
```

### 5. Zasto bas ove operacije

- `MPI_File_seek` i `MPI_File_write` postuju zahtev o pojedinacnim pokazivacima.
- `MPI_File_write_at` ne treba koristiti u prvom delu jer bi to bio eksplicitni pomeraj, a zadatak trazi pojedinacni pokazivac.
- `MPI_File_read_shared` je odgovarajuci izbor kada deo fajla mora biti dodeljen procesu dinamicki.
- `MPI_File_read_ordered` nije dobar izbor, jer bi tada raspored bio predvidiv: citanje bi islo po ranku.
- Zatvaranje, barijera i ponovno otvaranje daju jasno odvojene I/O faze i uklanjaju trku izmedju upisa i citanja.

### 6. Sta obavezno napisati i naglasiti

- Obrnutu formulu: `(nprocs - 1 - rank) * N * sizeof(int)`.
- Da je `offset` u bajtovima, a `N` u pozivu `write/read` broj integera.
- `seek + write`, jer se trazi pojedinacni pokazivac.
- `read_shared`, jer se ne zna koji proces cita koji blok.
- `close -> barrier -> open` zbog konzistentnosti.
- Da se `read_ordered` ne koristi jer bi redosled tada bio poznat.

---

## Stvarni zadatak 2 - DS Jun 2024 NP, zadatak 3

### 1. Originalni tekst zadatka

> Napisati MPI program koji vrsi paralelni upis i citanje binarne datoteke, prema sledecim zahtevima:
>
> a) Svaki proces upisuje po 105 proizvoljnih celih brojeva u datoteku file1.dat. Upis se vrsi upotrebom pojedinacnih pokazivaca, dok redosled podataka u fajlu ide od podataka poslednjeg do podataka prvog procesa.
>
> b) Ponovo otvoriti datoteku. Svaki proces vrsi citanje upravo upisanih podataka upotrebom funkcija sa eksplicitnim pomerajem.
>
> c) Upravo procitane podatke upisati u novu datoteku, na nacin prikazan na slici (za slucaj od 3 procesa). U poslednjem zahtevu posebno obratiti paznju na efikasnost paralelizacije upisa.

### 2. Analiza podele podataka i posla

Prva dva dela su obrnuti kontinualni blokovi, kao u prethodnom zadatku:

```text
LOCAL_N = 105
offset = (nprocs - 1 - rank) * LOCAL_N * sizeof(int)
```

Sema sa slike u trecem delu, za tacno tri procesa, glasi:

```text
P0 upisuje 1, P1 upisuje 1, P2 upisuje 1,
P0 upisuje 2, P1 upisuje 2, P2 upisuje 2,
P0 upisuje 3, P1 upisuje 3, P2 upisuje 3,
...
P0 upisuje 14, P1 upisuje 14, P2 upisuje 14.
```

Dakle, 105 elemenata svakog procesa deli se na blokove duzine:

```text
1, 2, 3, ..., 14
```

To je moguce zato sto:

```text
1 + 2 + ... + 14 = 14 * 15 / 2 = 105
```

Za tri procesa izlaz izgleda:

```text
P0(1), P1(1), P2(1),
P0(2), P1(2), P2(2),
P0(3), P1(3), P2(3), ...
P0(14), P1(14), P2(14)
```

Blokovi istog procesa nemaju jednaku duzinu, pa `MPI_Type_vector` nije dovoljan. Potreban je `MPI_Type_indexed`, koji za svaki blok prima posebnu duzinu i pocetni pomeraj.

Za indeks `k = 0..13`:

```text
duzina trenutnog bloka = k + 1
broj elemenata u svim prethodnim krugovima = nprocs * k * (k + 1) / 2
broj elemenata prethodnih procesa u trenutnom krugu = rank * (k + 1)

displ[k] = nprocs * k * (k + 1) / 2 + rank * (k + 1)
```

Pomeraji `displ` su u jedinicama `MPI_INT`, jer je `oldtype` u `MPI_Type_indexed` jednak `MPI_INT`.

### 3. Kljucni MPI kod za ispit

```c
#define LOCAL_N 105
#define NBLOCKS 14

MPI_Offset offset;
MPI_Datatype filetype;
int blocklengths[NBLOCKS], displs[NBLOCKS];
int k;

offset = (MPI_Offset)(nprocs - 1 - rank)
         * LOCAL_N * sizeof(int);

/* a) Pojedinacni pokazivac, obrnut redosled */
MPI_File_open(MPI_COMM_WORLD, "file1.dat",
              MPI_MODE_CREATE | MPI_MODE_WRONLY,
              MPI_INFO_NULL, &fh);

MPI_File_seek(fh, offset, MPI_SEEK_SET);
MPI_File_write(fh, buf, LOCAL_N, MPI_INT, &status);
MPI_File_close(&fh);

MPI_Barrier(MPI_COMM_WORLD);

/* b) Eksplicitni pomeraj */
MPI_File_open(MPI_COMM_WORLD, "file1.dat",
              MPI_MODE_RDONLY, MPI_INFO_NULL, &fh);

MPI_File_read_at(fh, offset, read_buf,
                 LOCAL_N, MPI_INT, &status);
MPI_File_close(&fh);

/* c) Promenljive duzine blokova: 1, 2, ..., 14 */
for (k = 0; k < NBLOCKS; k++) {
    blocklengths[k] = k + 1;
    displs[k] = nprocs * k * (k + 1) / 2
                + rank * (k + 1);
}

MPI_Type_indexed(NBLOCKS, blocklengths, displs,
                 MPI_INT, &filetype);
MPI_Type_commit(&filetype);

MPI_File_open(MPI_COMM_WORLD, "file2.dat",
              MPI_MODE_CREATE | MPI_MODE_WRONLY,
              MPI_INFO_NULL, &fh);

MPI_File_set_view(fh, 0, MPI_INT, filetype,
                  "native", MPI_INFO_NULL);

MPI_File_write_all(fh, read_buf,
                   LOCAL_N, MPI_INT, &status);

MPI_File_close(&fh);
```

### 4. Objasnjenje promenljivih, formula i redosleda

- `LOCAL_N = 105` je broj integera po procesu.
- `NBLOCKS = 14` sledi iz trougaonog zbira `1 + ... + 14 = 105`.
- `offset` u prva dva dela je isti, jer proces mora da procita upravo blok koji je prethodno upisao.
- U delu a) koriste se `seek` i `write`, odnosno pojedinacni pokazivac.
- U delu b) nema `seek`; pomeraj se direktno prosledjuje funkciji `MPI_File_read_at`.
- `blocklengths[k] = k + 1` opisuje rastucu duzinu bloka.
- Prvi sabirak u `displs[k]` preskace sve prethodne krugove svih procesa.
- Drugi sabirak preskace blokove procesa sa manjim rankom u tekucem krugu.
- `MPI_Type_commit` aktivira izvedeni tip pre nego sto se upotrebi u pogledu na fajl.
- `disp` u `MPI_File_set_view` je nula zato sto su svi rank-zavisni pomeraji vec ugradjeni u `displs`.
- `etype = MPI_INT` odredjuje osnovnu jedinicu pogleda, a `filetype` mesta koja su procesu vidljiva.
- `MPI_File_write_all` svi procesi moraju pozvati. MPI tada moze objediniti njihove isprepletane zahteve.
- `MPI_File_open`, `MPI_File_close` i `MPI_File_write_all` pozivaju svi clanovi `MPI_COMM_WORLD` istim redosledom.

Provera formule za tri procesa:

| `k` | duzina | `P0 displ` | `P1 displ` | `P2 displ` |
|---:|---:|---:|---:|---:|
| 0 | 1 | 0 | 1 | 2 |
| 1 | 2 | 3 | 5 | 7 |
| 2 | 3 | 9 | 12 | 15 |

Dakle:

- P0 u prvom krugu pise poziciju 0;
- P1 pise poziciju 1;
- P2 pise poziciju 2;
- zatim P0 pise dve pozicije od 3, P1 dve od 5, P2 dve od 7;
- sledeci krug pocinje na poziciji 9.

Preklapanja nema.

### 5. Zasto bas ove operacije

- `MPI_File_seek + MPI_File_write` je izricito trazeni pojedinacni pokazivac.
- `MPI_File_read_at` je funkcija sa eksplicitnim pomerajem.
- `MPI_Type_indexed` je potreban zato sto blokovi imaju razlicite duzine. `MPI_Type_vector` podrzava samo jednaku duzinu svih blokova.
- `MPI_File_set_view` pretvara nekontinualan raspored u logicki kontinualan pogled svakog procesa.
- `MPI_File_write_all` je kolektivni upis i ispunjava zahtev da poslednji upis bude efikasan.

### 6. Sta obavezno napisati i naglasiti

- Obrnuti offset u delovima a) i b).
- Razliku: a) `seek + write`, b) `read_at`.
- Jednacinu `105 = 1 + 2 + ... + 14`.
- `MPI_Type_indexed`, ne `MPI_Type_vector`.
- Tacnu formulu za `displs[k]`.
- Da su `displs` u integerima, a `disp` i I/O offseti u bajtovima.
- `MPI_Type_commit`, `MPI_File_set_view` i `MPI_File_write_all`.

---

## Stvarni zadatak 3 - DS April 2026 NP, zadatak 16

### 1. Originalni tekst zadatka

> Datoteka input.dat sadrzi ukupno 1MB podataka. Napisati MPI program koji vrsi obradu ovih podataka i priprema ih za vizualizaciju, ujedno vrseci paralelni upis i citanje datoteke. Na pocetku, svi procesi vrse citanje iste kolicine podataka tako da prvi proces cita poslednji skup podataka, drugi proces pretposlednji skup, itd. koriscenjem funkcija sa pojedinacnim pokazivacem. Procitane podatke procesi upisuju u datoteku april.dat, tako sto ih dele na 2 jednaka dela i upisuju po semi prikazanoj na slici (za slucaj od 4 procesa), pri cemu je potrebno obratiti paznju na paralelizaciju tog upisa.

### 2. Analiza podele podataka i posla

Zadatak govori o bajtovima, a ne o integerima. Zato se koriste `MPI_BYTE` i velicine u bajtovima:

```text
FILE_SIZE = 1024 * 1024
local_bytes = FILE_SIZE / nprocs
half = local_bytes / 2
```

Potrebno je da `FILE_SIZE` bude deljiv sa `nprocs`, a `local_bytes` sa 2.

Citanje ide obrnutim redosledom blokova:

```text
offset = (nprocs - 1 - rank) * local_bytes
```

Svaki proces zatim deli lokalni bafer na dve jednake polovine:

- prva polovina je na slici oznacena kosim linijama;
- druga polovina je na slici oznacena tackama.

Donji deo slike pokazuje da se u `april.dat` prvo zajedno smestaju sve prve polovine, po ranku, a zatim sve druge polovine, po ranku. Oznaka `P4` na slici predstavlja cetvrti prikazani proces; u MPI programu sa cetiri procesa njegov rank je `3`.

Za cetiri procesa raspored u `april.dat` je:

```text
[P0-prva] [P1-prva] [P2-prva] [P3-prva]
[P0-druga] [P1-druga] [P2-druga] [P3-druga]
```

Jedan proces zato ima dva bloka:

- `count = 2`;
- `blocklength = half`;
- `stride = half * nprocs`;
- `disp = rank * half`.

`stride = half * nprocs` vodi od pocetka prve polovine nekog procesa do pocetka njegove druge polovine. Izmedju njih se nalaze prve polovine ostalih procesa.

### 3. Kljucni MPI kod za ispit

Pretpostavlja se da `buf` ima `local_bytes` bajtova.

```c
#define FILE_SIZE (1024 * 1024)

int local_bytes = FILE_SIZE / nprocs;
int half = local_bytes / 2;
MPI_Offset offset, disp;
MPI_Datatype filetype;

offset = (MPI_Offset)(nprocs - 1 - rank) * local_bytes;

/* Citanje obrnutih blokova pojedinacnim pokazivacem */
MPI_File_open(MPI_COMM_WORLD, "input.dat",
              MPI_MODE_RDONLY, MPI_INFO_NULL, &fh);

MPI_File_seek(fh, offset, MPI_SEEK_SET);
MPI_File_read(fh, buf, local_bytes, MPI_BYTE, &status);
MPI_File_close(&fh);

/* Dve jednake polovine u nekontinualnom rasporedu */
MPI_Type_vector(2, half, half * nprocs,
                MPI_BYTE, &filetype);
MPI_Type_commit(&filetype);

disp = (MPI_Offset)rank * half;

MPI_File_open(MPI_COMM_WORLD, "april.dat",
              MPI_MODE_CREATE | MPI_MODE_WRONLY,
              MPI_INFO_NULL, &fh);

MPI_File_set_view(fh, disp, MPI_BYTE, filetype,
                  "native", MPI_INFO_NULL);

MPI_File_write_all(fh, buf, local_bytes,
                   MPI_BYTE, &status);

MPI_File_close(&fh);
```

### 4. Objasnjenje promenljivih, formula i redosleda

- `FILE_SIZE` je tacno 1 MiB, odnosno `1024 * 1024` bajtova.
- `local_bytes` je jednaka kolicina koju cita svaki proces.
- `offset` obrce blokove. Proces 0 cita poslednji, proces 1 pretposlednji, a poslednji proces cita prvi blok.
- `MPI_File_read` ima `count = local_bytes` i `datatype = MPI_BYTE`; zato je count broj bajtova.
- `MPI_Type_vector(2, half, half*nprocs, MPI_BYTE, ...)` opisuje dve polovine istog procesa u izlaznom fajlu.
- `MPI_Type_commit` mora prethoditi upotrebi izvedenog tipa u `MPI_File_set_view`.
- `stride` se meri u elementima `oldtype`. Posto je `oldtype = MPI_BYTE`, ovde je stride direktno broj bajtova.
- `disp = rank * half` pomera prvi blok procesa iza prvih polovina svih procesa manjeg ranka.
- I `etype` i osnovni tip `filetype` su `MPI_BYTE`, pa se ceo pogled tumaci u bajtovima.
- Posle pogleda, lokalni `buf` je i dalje kontinualan. MPI ga prema `filetype` rasporedjuje na dva mesta u fajlu.
- `MPI_File_write_all` je kolektivna operacija; svi procesi je pozivaju istim redosledom.
- Fajlovi su otvoreni preko `MPI_COMM_WORLD`, pa svaki proces mora ucestvovati u `open`, `close` i kolektivnom upisu.

Za cetiri procesa:

```text
P0: disp=0,      blokovi na 0 i 4*half
P1: disp=half,   blokovi na half i 5*half
P2: disp=2*half, blokovi na 2*half i 6*half
P3: disp=3*half, blokovi na 3*half i 7*half
```

### 5. Zasto bas ove operacije

- `seek + read` je obavezno jer zadatak trazi pojedinacni pokazivac.
- Koristi se `MPI_BYTE` jer tekst ne tvrdi da su podaci integeri.
- `MPI_Type_vector` prirodno opisuje dva bloka jednake duzine i pravilnog razmaka.
- `MPI_File_set_view` uklanja potrebu za dva posebna upisa po procesu.
- `MPI_File_write_all` je efikasniji za isprepletan nekontinualni raspored i direktno ispunjava zahtev o paralelizaciji.

### 6. Sta obavezno napisati i naglasiti

- `1MB = 1024 * 1024` bajtova.
- `local_bytes = FILE_SIZE / nprocs`.
- Obrnuti offset bez `sizeof`, jer se vec radi u bajtovima.
- `half = local_bytes / 2`.
- `MPI_Type_vector(2, half, half*nprocs, MPI_BYTE, ...)`.
- `disp = rank * half`.
- `set_view + write_all` za efikasan upis.
- Pretpostavke deljivosti.

---

## Stvarni zadatak 4 - APR25

### 1. Originalni tekst zadatka

> Napisati MPI program koji realizuje mnozenje matrice A dimenzija `n x k` i vektora `b` duzine `k`, cime se dobija i prikazuje rezultujuci vektor `c`. Takodje, program pronalazi i prikazuje maksimalnu vrednost elemenata u matrici A, kao i sumu elemenata svake vrste matrice A.
>
> Izracunavanje se obavlja tako sto master proces salje svakom procesu po `s` kolona matrice A (`s` je zadata konstanta) i po `s` elemenata vektora b. Svi elementi kolone matrice A se salju odjednom. Svi procesi ucestvuju u izracunavanjima potrebnim za generisanje rezultata programa.
>
> Svi rezultati programa se prikazuju u procesu koji sadrzi maksimalnu vrednost elemenata u matrici A, nakon raspodele kolona po procesima. Zadatak resiti primenom grupnih operacija, osim za slanje kolona matrice A za koje se koristi Point-to-Point operacija.

### 2. Analiza podele podataka i posla

Pretpostavljamo da su elementi tipa `int`, da proces 0 predstavlja master i da vazi:

```text
k = s * nprocs
```

Svaki proces `rank` dobija kolone:

```text
rank*s, rank*s + 1, ..., rank*s + s - 1
```

i odgovarajuce elemente vektora:

```text
b[rank*s], ..., b[rank*s + s - 1]
```

Matrica je u C-u zapisana po vrstama. Elementi jedne kolone zato nisu kontinualni u memoriji:

```text
A[0][j], A[1][j], A[2][j], ..., A[n-1][j]
```

Izmedju dva susedna elementa iste kolone nalazi se `k` integera, odnosno duzina jedne vrste. Cela kolona se zato opisuje vektorskim tipom:

```c
MPI_Type_vector(n, 1, k, MPI_INT, &column_type);
```

Znacenje parametara:

- `n` blokova, jer kolona ima `n` elemenata;
- svaki blok sadrzi jedan integer;
- `stride = k`, jer je od `A[i][j]` do `A[i+1][j]` udaljenost `k` integera.

Master svaku kolonu salje jednim `MPI_Send`. Prijemni bafer cuva lokalne kolone kontinualno kao `local_A[s][n]`, pa primalac jednu kolonu prima pomocu:

```c
MPI_Recv(local_A[j], n, MPI_INT, ...);
```

Tipovi posiljaoca i primaoca ne moraju imati isti raspored u memoriji; dovoljno je da oba opisuju isti niz od `n` elemenata tipa `MPI_INT`.

Odgovarajuci deo vektora `b` je kontinualan, pa se deli kolektivnom funkcijom `MPI_Scatter`.

Svaki proces racuna:

```text
local_c[i]       = suma A[i][global_j] * b[global_j]
local_row_sum[i] = suma A[i][global_j]
local_max        = maksimum u njegovih s kolona
```

To su parcijalni rezultati samo za lokalnih `s` kolona. Konacan rezultat dobija se sabiranjem po procesima:

```text
c[i]       = suma svih local_c[i]
row_sum[i] = suma svih local_row_sum[i]
```

Proces za prikaz ne zna se unapred. Zato:

1. `MPI_Gather` sakuplja lokalne maksimume na master procesu;
2. master pronalazi globalni maksimum i rank procesa koji ga sadrzi;
3. `MPI_Bcast` saopstava svim procesima taj rank;
4. `MPI_Reduce` salje konacni `c` i sume vrsta upravo tom procesu.

Ako se ista maksimalna vrednost nalazi u vise procesa, bira se proces najmanjeg ranka.

### 3. Kljucni MPI kod za ispit

Pretpostavljaju se matrica `A[n][k]` i vektor `b[k]` na master procesu, kao i lokalni baferi `local_A[s][n]`, `local_b[s]`, `local_c[n]` i `local_row_sum[n]`. Baferi `c[n]` i `row_sum[n]` postoje u svim procesima, jer se proces koji ce postati root redukcija saznaje tek tokom izvrsavanja.

```c
#define MASTER 0

MPI_Datatype column_type;
int global_col, p, i, j;
int local_max, global_max, result_rank;
int max_by_rank[nprocs];

/* Mora da vazi k == s * nprocs. */
MPI_Type_vector(n, 1, k, MPI_INT, &column_type);
MPI_Type_commit(&column_type);

/* Jedino se kolone matrice salju point-to-point operacijama. */
if (rank == MASTER) {
    for (p = 0; p < nprocs; p++) {
        for (j = 0; j < s; j++) {
            global_col = p * s + j;

            if (p == MASTER) {
                for (i = 0; i < n; i++)
                    local_A[j][i] = A[i][global_col];
            } else {
                MPI_Send(&A[0][global_col], 1, column_type,
                         p, j, MPI_COMM_WORLD);
            }
        }
    }
} else {
    for (j = 0; j < s; j++) {
        MPI_Recv(local_A[j], n, MPI_INT,
                 MASTER, j, MPI_COMM_WORLD, &status);
    }
}

/* Svaki proces dobija s odgovarajucih elemenata vektora b. */
MPI_Scatter(b, s, MPI_INT,
            local_b, s, MPI_INT,
            MASTER, MPI_COMM_WORLD);

/* Svi procesi racunaju nad svojim kolonama. */
local_max = local_A[0][0];

for (i = 0; i < n; i++) {
    local_c[i] = 0;
    local_row_sum[i] = 0;

    for (j = 0; j < s; j++) {
        local_c[i] += local_A[j][i] * local_b[j];
        local_row_sum[i] += local_A[j][i];

        if (local_A[j][i] > local_max)
            local_max = local_A[j][i];
    }
}

/* Master odredjuje koji proces sadrzi globalni maksimum. */
MPI_Gather(&local_max, 1, MPI_INT,
           max_by_rank, 1, MPI_INT,
           MASTER, MPI_COMM_WORLD);

if (rank == MASTER) {
    global_max = max_by_rank[0];
    result_rank = 0;

    for (p = 1; p < nprocs; p++) {
        if (max_by_rank[p] > global_max) {
            global_max = max_by_rank[p];
            result_rank = p;
        }
    }
}

MPI_Bcast(&result_rank, 1, MPI_INT,
          MASTER, MPI_COMM_WORLD);
MPI_Bcast(&global_max, 1, MPI_INT,
          MASTER, MPI_COMM_WORLD);

/* Rezultati se redukuju na proces koji sadrzi maksimum. */
MPI_Reduce(local_c, c, n, MPI_INT, MPI_SUM,
           result_rank, MPI_COMM_WORLD);

MPI_Reduce(local_row_sum, row_sum, n, MPI_INT, MPI_SUM,
           result_rank, MPI_COMM_WORLD);

if (rank == result_rank) {
    printf("Maksimum matrice A: %d\n", global_max);

    for (i = 0; i < n; i++)
        printf("c[%d] = %d, suma vrste = %d\n",
               i, c[i], row_sum[i]);
}
```

### 4. Objasnjenje promenljivih, formula i redosleda

- `MASTER = 0` je proces koji na pocetku poseduje celu matricu i vektor.
- `column_type` opisuje nekontinualnu kolonu u matrici zapisanoj po vrstama.
- `global_col = p*s + j` je globalni indeks `j`-te kolone koja pripada procesu `p`.
- Master ne salje poruku samom sebi, vec svoje prve `s` kolona lokalno kopira. Tako i master ucestvuje u racunanju.
- Svaka kolona drugom procesu salje se jednim `MPI_Send` pozivom sa `count = 1` i `datatype = column_type`.
- Primalac prima `n` kontinualnih integera. `local_A[j][i]` predstavlja element vrste `i` u lokalnoj koloni `j`.
- Tag `j` razlikuje lokalne kolone. Svaki primalac poziva `Recv` istim redom kojim master njemu salje kolone.
- `MPI_Scatter` radi zato sto su delovi vektora `b` kontinualni i svaki proces dobija tacno `s` elemenata.
- `local_c[i]` je parcijalni skalarni proizvod za vrstu `i`.
- `local_row_sum[i]` je parcijalna suma vrste `i`, samo preko lokalnih kolona.
- `MPI_Gather` sakuplja po jedan lokalni maksimum sa svakog procesa.
- Dva `MPI_Bcast` poziva obezbedjuju da svi znaju `result_rank` i `global_max`.
- Root funkcije `MPI_Reduce` ne mora biti master; moze biti dinamicki izabrani `result_rank`, jer ga pre redukcije znaju svi procesi.
- Obe redukcije koriste `MPI_SUM`, jer se parcijalni rezultati po kolonama sabiraju.

Redosled je vazan:

```text
raspodela kolona -> Scatter vektora b -> lokalni racun
-> Gather maksimuma -> izbor i Bcast result_rank-a
-> Reduce rezultata na result_rank -> prikaz
```

Deadlock ne nastaje zato sto svaki proces koji nije master unapred poziva odgovarajuce `MPI_Recv` operacije, dok master salje kolone u istom redosledu. Sve kolektivne operacije svi procesi pozivaju istim redom.

### 5. Zasto bas ove operacije

- `MPI_Type_vector + MPI_Send` ispunjava zahtev da se svi elementi jedne nekontinualne kolone posalju odjednom point-to-point operacijom.
- `MPI_Scatter` je prirodan izbor za jednake, kontinualne grupe od po `s` elemenata vektora `b`.
- `MPI_Gather` omogucava masteru da odredi u kom procesu se nalazi maksimum.
- `MPI_Bcast` je potreban jer root narednih redukcija nije unapred poznat svim procesima.
- `MPI_Reduce` sa `MPI_SUM` formira ceo vektor `c` i pune sume vrsta od parcijalnih vrednosti.
- Ne koristi se `MPI_Gather` za `local_c`, jer elemente sa istim indeksom treba sabrati, a ne samo spojiti.

### 6. Sta obavezno napisati i naglasiti

- Uslov `k = s * nprocs`.
- Podelu kolona procesa `rank`: od `rank*s` do `rank*s+s-1`.
- `MPI_Type_vector(n, 1, k, MPI_INT, &column_type)`.
- Da se cela kolona salje jednim `MPI_Send`, dok primalac prima `n` elemenata `MPI_INT`.
- `MPI_Scatter` za vektor `b`.
- Formule za `local_c`, parcijalnu sumu vrste i lokalni maksimum.
- `Gather -> izbor result_rank -> Bcast`.
- Dve `Reduce` operacije sa `root = result_rank`.
- Da i master obradjuje svojih `s` kolona.
- Da svi kolektivni pozivi moraju biti izvrseni istim redosledom.

---

## Stvarni zadatak 5 - Oktobar 2 2025, zadatak 17

### 1. Originalni tekst zadatka

> Datoteka `input.dat` sadrzi ukupno 10MB podataka. Napisati MPI program koji vrsi obradu ovih podataka i priprema ih za obradu, ujedno vrseci paralelni upis i citanje datoteke.
>
> Na pocetku, svi procesi vrse citanje iste kolicine podataka tako da poslednji proces cita prvi skup podataka, pretposlednji drugi skup, itd. koriscenjem funkcija sa pojedinacnim pokazivacem.
>
> Procitane podatke procesi upisuju u datoteku `output.dat`, tako sto ih dele na cetiri jednaka dela i upisuju po semi prikazanoj na slici (za slucaj od 3 procesa), pri cemu je potrebno obratiti paznju na paralelizaciju tog upisa.
>
> Napomena: Zadatak resiti koriscenjem osmobitnog tipa podataka.

### 2. Analiza podele podataka i posla

Koristi se osmobitni lokalni bafer, na primer `unsigned char *buf`, i MPI tip `MPI_BYTE`.

```text
FILE_SIZE   = 10 * 1024 * 1024 bajtova
local_bytes = FILE_SIZE / nprocs
part        = local_bytes / 4
```

Poslednji proces cita prvi blok, pa je pomeraj procesa `rank`:

```text
offset = (nprocs - 1 - rank) * local_bytes
```

Posto se radi sa bajtovima, nema mnozenja sa `sizeof`.

Svaki lokalni bafer deli se na cetiri cetvrtine:

```text
P0: P0-deo1 | P0-deo2 | P0-deo3 | P0-deo4
P1: P1-deo1 | P1-deo2 | P1-deo3 | P1-deo4
P2: P2-deo1 | P2-deo2 | P2-deo3 | P2-deo4
```

Sema sa slike zahteva sledeci izlaz:

```text
P0-deo1 | P1-deo1 | P2-deo1 |
P0-deo2 | P1-deo2 | P2-deo2 |
P0-deo3 | P1-deo3 | P2-deo3 |
P0-deo4 | P1-deo4 | P2-deo4
```

Jedan proces zato vidi cetiri jednaka bloka:

```text
count       = 4
blocklength = part
stride      = part * nprocs
disp        = rank * part
```

Ovo je pravilan raspored, pa se koristi `MPI_Type_vector`, a ne `MPI_Type_indexed`.

Vazan uslov zadatka je:

```text
FILE_SIZE % (4 * nprocs) == 0
```

Tacnih `10 * 1024 * 1024` bajtova nije deljivo sa 3. Zato slika sa tri procesa predstavlja semu rasporeda, dok potpuno jednaka podela zahteva broj procesa za koji gornji uslov vazi. Ako se na ispitu izricito zahteva bas tri procesa i tacno 10 MiB, uslovi zadatka ostavljaju jedan bajt viska i nisu istovremeno ostvarivi bez posebnog pravila za ostatak.

### 3. Kljucni MPI kod za ispit

Pretpostavlja se da je osmobitni bafer `buf` alociran sa `local_bytes` elemenata.

```c
#define FILE_SIZE (10 * 1024 * 1024)

int local_bytes = FILE_SIZE / nprocs;
int part = local_bytes / 4;
MPI_Offset offset, disp;
MPI_Datatype filetype;

/* Poslednji proces cita prvi blok. */
offset = (MPI_Offset)(nprocs - 1 - rank) * local_bytes;

MPI_File_open(MPI_COMM_WORLD, "input.dat",
              MPI_MODE_RDONLY, MPI_INFO_NULL, &fh);

MPI_File_seek(fh, offset, MPI_SEEK_SET);
MPI_File_read(fh, buf, local_bytes, MPI_BYTE, &status);

MPI_File_close(&fh);

/* Cetiri cetvrtine istog procesa u pravilnom rasporedu. */
MPI_Type_vector(4, part, part * nprocs,
                MPI_BYTE, &filetype);
MPI_Type_commit(&filetype);

disp = (MPI_Offset)rank * part;

MPI_File_open(MPI_COMM_WORLD, "output.dat",
              MPI_MODE_CREATE | MPI_MODE_WRONLY,
              MPI_INFO_NULL, &fh);

MPI_File_set_view(fh, disp, MPI_BYTE, filetype,
                  "native", MPI_INFO_NULL);

MPI_File_write_all(fh, buf, local_bytes,
                   MPI_BYTE, &status);

MPI_File_close(&fh);
```

### 4. Objasnjenje promenljivih, formula i redosleda

- `local_bytes` je broj bajtova koji cita jedan proces.
- `offset` je vec izrazen u bajtovima jer je lokalna kolicina izrazena u bajtovima.
- `nprocs - 1 - rank` obrce redosled blokova pri citanju.
- `MPI_File_seek + MPI_File_read` koristi se zato sto zadatak trazi pojedinacni pokazivac.
- `part` je jedna cetvrtina lokalnog bafera.
- `MPI_Type_vector(4, part, part*nprocs, MPI_BYTE, ...)` opisuje cetiri bloka istog procesa.
- `stride` vodi od pocetka jedne cetvrtine procesa do pocetka njegove sledece cetvrtine u izlaznom fajlu.
- `disp = rank*part` postavlja prvu cetvrtinu procesa iza prvih cetvrtina svih procesa manjeg ranka.
- `filetype` u `MPI_File_set_view` opisuje raspored u fajlu.
- `MPI_BYTE` u `MPI_File_write_all` opisuje kontinualni osmobitni bafer u memoriji.
- `MPI_File_write_all` je kolektivna operacija i svi procesi moraju da je pozovu.

Za tri procesa, pogled svakog procesa je:

```text
P0: 0,             3*part, 6*part, 9*part
P1: part,          4*part, 7*part, 10*part
P2: 2*part,        5*part, 8*part, 11*part
```

Svaka navedena pozicija je pocetak bloka duzine `part`.

### 5. Zasto bas ove operacije

- `seek + read` postuje zahtev o pojedinacnom pokazivacu.
- `MPI_BYTE` postuje napomenu o osmobitnom tipu podataka.
- `MPI_Type_vector` se koristi jer su sva cetiri bloka jednaka i razmak je konstantan.
- `MPI_File_set_view` omogucava da kontinualni lokalni bafer bude rasporedjen na cetiri mesta u fajlu.
- `MPI_File_write_all` ispunjava zahtev o efikasnoj paralelizaciji upisa.
- `MPI_Type_indexed` nije potreban, jer nema razlicitih duzina blokova ni nepravilnih pomeraja.

### 6. Sta obavezno napisati i naglasiti

- Obrnuti offset `(nprocs - 1 - rank) * local_bytes`.
- Da nema `sizeof`, jer su vrednosti vec u bajtovima.
- `part = local_bytes / 4`.
- `MPI_Type_vector(4, part, part*nprocs, MPI_BYTE, &filetype)`.
- `disp = rank * part`.
- Redosled `commit -> open -> set_view -> write_all`.
- Razliku: `filetype` opisuje fajl, a `MPI_BYTE` u `write_all` memorijski bafer.
- Uslov `FILE_SIZE % (4*nprocs) == 0`.

---

## Stvarni zadatak 6 - JUN26, zadatak 18

### 1. Originalni tekst zadatka

> Datoteka `input.dat` sadrzi ukupno 1kB podataka. Napisati MPI program koji vrsi obradu ovih podataka i priprema ih za vizualizaciju, ujedno vrseci paralelni upis i citanje datoteke.
>
> a) Na pocetku, svi procesi vrse citanje iste kolicine podataka tako da prvi proces cita poslednji skup podataka, drugi proces pretposlednji skup, itd. koriscenjem funkcija sa eksplicitnim pomerajem.
>
> b) Procitane podatke procesi upisuju u datoteku `output.dat`, tako sto ih dele na 2 jednaka dela i upisuju po semi prikazanoj na slici (za slucaj od 4 procesa), pri cemu je potrebno obratiti paznju na paralelizaciju tog upisa.

### 2. Analiza podele podataka i posla

Podaci su zadati u bajtovima, pa koristimo osmobitni bafer i `MPI_BYTE`:

```text
FILE_SIZE   = 1024 bajta
local_bytes = FILE_SIZE / nprocs
half        = local_bytes / 2
```

Za cetiri procesa:

```text
local_bytes = 1024 / 4 = 256 bajtova
half        = 256 / 2 = 128 bajtova
```

Proces 0 cita poslednji blok, proces 1 pretposlednji itd:

```text
offset = (nprocs - 1 - rank) * local_bytes
```

Za cetiri procesa:

| Proces | Blok iz `input.dat` | Offset |
|---:|---:|---:|
| P0 | poslednji, blok 3 | 768 B |
| P1 | blok 2 | 512 B |
| P2 | blok 1 | 256 B |
| P3 | prvi, blok 0 | 0 B |

Zadatak izricito trazi funkcije sa eksplicitnim pomerajem. Zato se koristi:

```c
MPI_File_read_at(...)
```

bez prethodnog `MPI_File_seek`.

Svaki proces svoj procitani bafer deli na prvu i drugu polovinu. Sema sa slike zahteva:

```text
P0-prva | P1-prva | P2-prva | P3-prva |
P0-druga | P1-druga | P2-druga | P3-druga
```

Oznaka `P4` na slici predstavlja cetvrti prikazani proces; u programu sa cetiri procesa njegov rank je 3.

Jedan proces vidi dva jednaka bloka sa pravilnim razmakom:

```text
count       = 2 bloka
blocklength = half
stride      = half * nprocs
disp        = rank * half
```

Zato se koristi `MPI_Type_vector`.

### 3. Kljucni MPI kod za ispit

Pretpostavlja se da osmobitni bafer `buf` ima `local_bytes` elemenata.

```c
#define FILE_SIZE 1024

int local_bytes = FILE_SIZE / nprocs;
int half = local_bytes / 2;
MPI_Offset offset, disp;
MPI_Datatype filetype;

/* a) Obrnuti blokovi, eksplicitni pomeraj */
offset = (MPI_Offset)(nprocs - 1 - rank) * local_bytes;

MPI_File_open(MPI_COMM_WORLD, "input.dat",
              MPI_MODE_RDONLY, MPI_INFO_NULL, &fh);

MPI_File_read_at(fh, offset, buf,
                 local_bytes, MPI_BYTE, &status);

MPI_File_close(&fh);

/* b) Dve jednake polovine u pravilnom rasporedu */
MPI_Type_vector(2, half, half * nprocs,
                MPI_BYTE, &filetype);
MPI_Type_commit(&filetype);

disp = (MPI_Offset)rank * half;

MPI_File_open(MPI_COMM_WORLD, "output.dat",
              MPI_MODE_CREATE | MPI_MODE_WRONLY,
              MPI_INFO_NULL, &fh);

MPI_File_set_view(fh, disp, MPI_BYTE, filetype,
                  "native", MPI_INFO_NULL);

MPI_File_write_all(fh, buf, local_bytes,
                   MPI_BYTE, &status);

MPI_File_close(&fh);
```

### 4. Objasnjenje promenljivih, formula i redosleda

- `local_bytes` je broj bajtova koji pripada jednom procesu.
- `offset` je vec u bajtovima, pa se ne mnozi sa `sizeof`.
- Izraz `nprocs - 1 - rank` obrce redosled blokova.
- `MPI_File_read_at` direktno dobija offset i ne koristi niti menja pojedinacni pokazivac.
- `count = local_bytes` u `read_at` znaci broj elemenata tipa `MPI_BYTE`, odnosno broj bajtova.
- `MPI_Type_vector(2, half, half*nprocs, ...)` opisuje dve polovine jednog procesa u izlaznom fajlu.
- Prvi argument `2` znaci dva bloka jednog procesa, a ne dva procesa.
- `half` je broj bajtova u jednom bloku.
- `half*nprocs` vodi od pocetka prve polovine procesa do pocetka njegove druge polovine.
- `disp = rank*half` postavlja pocetak prve polovine tog procesa.
- `filetype` u `set_view` opisuje raspored u fajlu.
- `MPI_BYTE` u `write_all` opisuje kontinualni bafer u memoriji.
- `MPI_File_write_all` svi procesi pozivaju kolektivno.

Za cetiri procesa pogledi su:

```text
P0: blokovi pocinju na 0 i 4*half
P1: blokovi pocinju na half i 5*half
P2: blokovi pocinju na 2*half i 6*half
P3: blokovi pocinju na 3*half i 7*half
```

### 5. Zasto bas ove operacije

- `MPI_File_read_at` se koristi jer zadatak trazi eksplicitni pomeraj.
- `MPI_BYTE` se koristi zato sto je velicina fajla i podela izrazena u bajtovima.
- `MPI_Type_vector` odgovara dvama blokovima iste duzine sa jednakim razmakom.
- `MPI_File_set_view` omogucava da svaki proces svoja dva mesta u fajlu vidi kao kontinualan prostor.
- `MPI_File_write_all` omogucava efikasan kolektivni upis svih isprepletanih zahteva.
- `MPI_Type_indexed` nije potreban jer blokovi nisu nepravilni.

### 6. Sta obavezno napisati i naglasiti

- `FILE_SIZE = 1024`.
- `local_bytes = FILE_SIZE/nprocs` i `half = local_bytes/2`.
- Obrnuti offset `(nprocs - 1 - rank)*local_bytes`.
- `MPI_File_read_at`, bez `MPI_File_seek`.
- `MPI_Type_vector(2, half, half*nprocs, MPI_BYTE, &filetype)`.
- `disp = rank*half`.
- Redosled `commit -> open -> set_view -> write_all`.
- Da `filetype` opisuje fajl, a `MPI_BYTE` u `write_all` memorijski bafer.
- Uslov da je `FILE_SIZE` deljiv sa `2*nprocs`.

---

## Dodatni ispitni zadatak 1 - Scatter i Reduce

### Tekst zadatka za samostalni rad

Proces 0 poseduje niz od `N` celih brojeva, pri cemu je `N` deljivo brojem procesa. Ravnomerno raspodeliti niz svim procesima. Svaki proces racuna sumu svog dela, a proces 0 na kraju prikazuje zbir svih elemenata. Koristiti grupne MPI operacije.

### Ispitno resenje

```c
int local_n = N / nprocs;
int local_sum = 0, global_sum;

MPI_Scatter(a, local_n, MPI_INT,
            local_a, local_n, MPI_INT,
            0, MPI_COMM_WORLD);

for (i = 0; i < local_n; i++)
    local_sum += local_a[i];

MPI_Reduce(&local_sum, &global_sum, 1, MPI_INT,
           MPI_SUM, 0, MPI_COMM_WORLD);
```

### Analiza i objasnjenje

- Proces 0 je korenski proces, odnosno `root = 0`.
- Svaki proces dobija `local_n = N / nprocs` uzastopnih elemenata.
- `MPI_Scatter` na procesu 0 koristi `a` kao izvor. Na ostalim procesima send bafer nije znacajan.
- Svaki proces lokalno racuna parcijalnu sumu bez komunikacije.
- `MPI_Reduce` kombinuje po jedan integer sa svakog procesa operacijom `MPI_SUM`.
- Samo proces 0 dobija validan `global_sum`.
- Obe funkcije su kolektivne: svi procesi iz `MPI_COMM_WORLD` moraju ih pozvati istim redosledom.
- Deadlock-a nema jer nema neuskladjenih parova `Send/Recv`.

### Zasto ove operacije

`Scatter` odgovara ravnomernoj podeli jednog niza, a `Reduce` svodi po jednu lokalnu vrednost na jedan globalni rezultat. Rucne petlje sa `MPI_Send` i `MPI_Recv` bile bi duze i ne bi pratile zahtev o grupnim operacijama.

### Sta napisati na ispitu

- Pretpostavku `N % nprocs == 0`.
- Formulu `local_n = N / nprocs`.
- Isti `root = 0` u obe kolektivne operacije.
- Lokalni racun izmedju `Scatter` i `Reduce`.
- Da samo root koristi rezultat redukcije.

---

## Dodatni ispitni zadatak 2 - prsten i Sendrecv

### Tekst zadatka za samostalni rad

Organizovati procese u jednodimenzionalnu periodicnu Dekartovu topologiju. Svaki proces salje svoj rang desnom susedu i prima rang levog suseda. Razmenu realizovati bez mogucnosti deadlock-a.

### Ispitno resenje

```c
MPI_Comm ring_comm;
int dims[1] = { nprocs };
int periods[1] = { 1 };
int left, right;
int send_value, recv_value;

MPI_Cart_create(MPI_COMM_WORLD, 1, dims, periods,
                0, &ring_comm);

MPI_Comm_rank(ring_comm, &rank);
MPI_Cart_shift(ring_comm, 0, 1, &left, &right);

send_value = rank;

MPI_Sendrecv(&send_value, 1, MPI_INT, right, 0,
             &recv_value, 1, MPI_INT, left, 0,
             ring_comm, &status);
```

### Analiza i objasnjenje

- Topologija ima jednu dimenziju, pa su `dims` i `periods` nizovi duzine 1.
- `dims[0] = nprocs` smesta sve procese u isti prsten.
- `periods[0] = 1` spaja poslednji proces sa prvim.
- `reorder = 0` cuva postojece rangove.
- `MPI_Cart_shift(..., direction=0, disp=1, ...)` vraca rang levog izvora i desnog odredista.
- Svaki proces salje desno i prima levo u istom pozivu.
- Tagovi slanja i prijema su isti.
- Za `rank = 0`, levi sused je `nprocs - 1`; za poslednji rank, desni sused je 0.

### Zasto ove operacije

`MPI_Cart_shift` izracunava susede bez rucnih modulo formula. `MPI_Sendrecv` istovremeno opisuje slanje i prijem i MPI bezbedno rasporedjuje operacije, pa nema deadlock-a koji bi mogao nastati kada bi svi procesi prvo pozvali blokirajuci `MPI_Send`.

### Sta napisati na ispitu

- `periods[0] = 1` zbog prstena.
- Da je smer `0`, jer postoji samo jedna dimenzija.
- Odakle dolaze `left` i `right`.
- Isti komunikator `ring_comm` u svim topoloskim i komunikacionim funkcijama.
- `MPI_Sendrecv` kao zastitu od deadlock-a.

---

## Dodatni ispitni zadatak 3 - kolektivno citanje podmatrica

### Tekst zadatka za samostalni rad

Binarna datoteka `matrix.dat` sadrzi matricu od `M x N` elemenata tipa `float`, zapisanu po vrstama. Sest procesa je rasporedjeno u mrezu `2 x 3`. Svaki proces treba kolektivno da procita svoju podmatricu dimenzija `(M/2) x (N/3)`. Pretpostaviti da je `M` deljivo sa 2, a `N` sa 3. Koristiti pogled na fajl.

### Ispitno resenje

```c
MPI_Comm cart_comm;
MPI_Datatype filetype;
int dims[2] = { 2, 3 };
int periods[2] = { 0, 0 };
int coords[2];
int gsizes[2] = { M, N };
int lsizes[2] = { M / 2, N / 3 };
int starts[2];
int local_count = lsizes[0] * lsizes[1];

MPI_Cart_create(MPI_COMM_WORLD, 2, dims, periods,
                0, &cart_comm);
MPI_Comm_rank(cart_comm, &rank);
MPI_Cart_coords(cart_comm, rank, 2, coords);

starts[0] = coords[0] * lsizes[0];
starts[1] = coords[1] * lsizes[1];

MPI_Type_create_subarray(2, gsizes, lsizes, starts,
                         MPI_ORDER_C, MPI_FLOAT, &filetype);
MPI_Type_commit(&filetype);

MPI_File_open(cart_comm, "matrix.dat",
              MPI_MODE_RDONLY, MPI_INFO_NULL, &fh);

MPI_File_set_view(fh, 0, MPI_FLOAT, filetype,
                  "native", MPI_INFO_NULL);

MPI_File_read_all(fh, local_matrix, local_count,
                  MPI_FLOAT, &status);

MPI_File_close(&fh);
```

### Analiza i objasnjenje

- `gsizes = {M, N}` opisuje celu matricu.
- `lsizes = {M/2, N/3}` opisuje lokalnu podmatricu jednog procesa.
- `dims = {2, 3}` znaci dva procesna reda i tri procesne kolone.
- Topologija nije periodicna jer podmatrice na krajevima nisu susedi preko granice.
- `coords[0]` je procesni red, a `coords[1]` procesna kolona.
- `starts` daje pocetnu koordinatu lokalne podmatrice u globalnoj matrici.
- `MPI_ORDER_C` znaci da je poslednji indeks, odnosno kolona, kontinualan u memoriji.
- `MPI_Type_create_subarray` opisuje nekontinualne redove lokalne podmatrice u globalnom fajlu.
- `disp = 0`, jer je pocetak podmatrice vec opisan nizom `starts`.
- U memoriji je lokalna matrica kontinualna, pa se cita `local_count` elemenata tipa `MPI_FLOAT`.
- `MPI_File_read_all` svi procesi komunikatora pozivaju kolektivno.

### Zasto ove operacije

Rucno citanje svakog reda zahtevalo bi vise `seek/read` poziva. `subarray + set_view + read_all` je nivo 3 pristupa iz prezentacije: jedan izvedeni tip opisuje celu podmatricu, a kolektivna operacija daje MPI implementaciji mogucnost da objedini zahteve.

### Sta napisati na ispitu

- Uslove `M % 2 == 0`, `N % 3 == 0` i tacno sest procesa.
- Ispravne lokalne dimenzije: druga je `N/3`, ne `M/3`.
- Formule za `starts[0]` i `starts[1]`.
- `MPI_ORDER_C` i `MPI_FLOAT`.
- `MPI_Type_commit`, `set_view` i `read_all`.
- Da `local_count = (M/2) * (N/3)`.

---

## Puskica

```c
/* rank govori KOJI sam proces; nprocs govori KOLIKO nas ima */
MPI_Comm_rank(MPI_COMM_WORLD, &rank);
MPI_Comm_size(MPI_COMM_WORLD, &nprocs);

/* COUNT - GLAVNO PRAVILO */
/* Kod prenosa count = broj jedinica prosledjenog datatype-a */
/* count=10 uz MPI_INT znaci 10 integera; uz MPI_BYTE znaci 10 bajtova */
/* count=1 uz izvedeni column_type znaci 1 cela kolona koju taj tip opisuje */
/* Kod vector/indexed konstruktora count znaci broj blokova novog tipa */

/* Jednaka kontinualna podela: svaki rank dobija local_n elemenata */
local_n = N / nprocs;
first = rank * local_n;
offset = (MPI_Offset)rank * local_n * sizeof(int);
/* Obrnuta podela: poslednji rank pocinje od pocetka fajla */
reverse_offset = (MPI_Offset)(nprocs - 1 - rank) * local_n * sizeof(int);

/* N ovde znaci N elemenata MPI_INT iz kontinualnog memorijskog bafera */
/* Bafer se puni petljom, ali se ceo upisuje jednim I/O pozivom */
int buf[N];
for (i = 0; i < N; i++)
    buf[i] = rand() % 100;
MPI_File_write(fh, buf, N, MPI_INT, &status);

/* Send count = koliko datatype elemenata posiljalac salje */
/* Recv count = kapacitet prijemnog bafera u recv datatype elementima */
/* Send/Recv: poruka ide izmedju tacno odredjena dva procesa */
/* source/dest i tag moraju da se poklope; los redosled moze dati deadlock */
MPI_Send(buf, count, MPI_INT, dest, tag, comm);
MPI_Recv(buf, count, MPI_INT, source, tag, comm, &status);

/* Isend/Irecv count ima isto znacenje kao kod Send/Recv */
/* Isend/Irecv samo pokrecu prenos; Wait potvrdi da je zaista zavrsen */
/* Bafer ne menjati pre Wait-a */
MPI_Irecv(recvbuf, count, MPI_INT, source, tag, comm, &req[0]);
MPI_Isend(sendbuf, count, MPI_INT, dest, tag, comm, &req[1]);
MPI_Wait(&req[0], &status);
MPI_Wait(&req[1], &status);

/* Sendrecv ima poseban send count i recv count; ne moraju biti jednaki */
/* Sendrecv: bezbedna istovremena razmena sa susedima bez deadlock-a */
MPI_Sendrecv(sendbuf, count, MPI_INT, dest, tag,
             recvbuf, count, MPI_INT, source, tag,
             comm, &status);

/* KOLEKTIVNE: svi procesi komunikatora pozivaju istim redosledom */
/* Barrier ne prenosi podatke; samo ceka da svi stignu */
MPI_Barrier(MPI_COMM_WORLD);

/* Bcast count = koliko elemenata istog bafera root salje svakom procesu */
/* Root ima podatak pre poziva, posle poziva ga imaju svi */
MPI_Bcast(buf, count, MPI_INT, root, MPI_COMM_WORLD);

/* Scatter send count = elemenata ZA JEDAN proces, ne ukupan broj */
/* Scatter recv count = koliko elemenata staje u lokalni recvbuf */
/* Root deli svoj niz na jednake kontinualne delove */
MPI_Scatter(sendbuf, local_n, MPI_INT,
            recvbuf, local_n, MPI_INT, root, MPI_COMM_WORLD);

/* Gather send count = koliko salje SVAKI proces */
/* Gather recv count = koliko root prima OD SVAKOG procesa */
/* Root zato mora imati prostor za recv count * nprocs elemenata */
/* Gather sakuplja po rankovima, ali ne sabira */
MPI_Gather(sendbuf, local_n, MPI_INT,
           recvbuf, local_n, MPI_INT, root, MPI_COMM_WORLD);

/* Reduce count = broj pozicija koje se kombinuju element-po-element */
/* count=1: po jedna vrednost svih procesa daje jedan rezultat */
/* count=n: n lokalnih elemenata daje n redukovanih elemenata */
/* Samo root dobija konacan rezultat */
MPI_Reduce(&local, &global, 1, MPI_INT,
           MPI_SUM, root, MPI_COMM_WORLD);

/* Scan count = broj pozicija koje se prefiksno kombinuju */
/* Svaki rank dobija rezultat od ranka 0 do svog ranka */
MPI_Scan(&local, &prefix, 1, MPI_INT,
         MPI_SUM, MPI_COMM_WORLD);

/* IZVEDENI TIP opisuje raspored elemenata, ne njihove vrednosti */

/* contiguous count = koliko oldtype elemenata cini JEDNU instancu novog tipa */
/* Ovde jedna instanca contig = n susednih MPI_INT bez rupa */
MPI_Type_contiguous(n, MPI_INT, &contig);

/* vector: jedan proces vidi vise jednakih blokova u pravilnom ritmu */
/* vector count/num_blocks = koliko blokova vidi TAJ proces */
/* To nije broj procesa niti ukupan broj elemenata u fajlu */
/* block_len = koliko uzastopnih elemenata uzima u jednom bloku */
/* stride = od pocetka njegovog bloka do pocetka sledeceg */
/* stride ukljucuje uzeti blok i preskocene elemente */
MPI_Type_vector(num_blocks, block_len, stride,
                MPI_INT, &vector_type);

/* indexed: blokovi jednog procesa nemaju istu duzinu ili pravilan razmak */
/* indexed count/num_blocks = broj blokova i duzina oba ulazna niza */
/* blocklengths[i] govori koliko uzima u i-tom bloku */
/* displs[i] govori gde i-ti blok pocinje, u oldtype elementima */
MPI_Type_indexed(num_blocks, blocklengths, displs,
                 MPI_INT, &indexed_type);

/* count=1 ovde znaci jedan blok; blok ima 1 MPI_INT na poziciji 5 */
int one_blocklength[1] = { 1 };
int one_displ[1] = { 5 };
MPI_Type_indexed(1, one_blocklength, one_displ,
                 MPI_INT, &one_int_at_5);
MPI_Type_commit(&one_int_at_5);

/* resized menja razmak do sledece instance tipa, ne menja njegove podatke */
MPI_Type_create_resized(oldtype, 0, extent, &resized_type);

/* commit aktivira izvedeni tip; mora biti pre Send/Recv ili file view-a */
MPI_Type_commit(&filetype);

/* Primer: proces uzima 5 integera, preskace blokove ostalih procesa */
/* Zatim uzima svojih sledecih 5; ukupno vidi 2 svoja bloka */
num_blocks = 2;
block_len = 5;
stride = 5 * nprocs;
disp = (MPI_Offset)rank * 5 * sizeof(int);
MPI_Type_vector(num_blocks, block_len, stride,
                MPI_INT, &filetype);

/* Opsti round-robin: lokalni podaci se dele na jednake blokove */
num_blocks = local_n / block_len;
stride = block_len * nprocs;
disp = (MPI_Offset)rank * block_len * sizeof(int);
MPI_Type_vector(num_blocks, block_len, stride,
                MPI_INT, &filetype);

/* APR25: kolona je rasuta po vrstama; vector je spaja u jednu poruku */
/* n elemenata kolone, uzima se po 1, sledeci je k mesta dalje */
MPI_Type_vector(n, 1, k, MPI_INT, &column_type);
MPI_Type_commit(&column_type);
global_col = dest * s + local_col;
/* Send count=1 column_type znaci jedna cela kolona, odnosno n integera */
MPI_Send(&A[0][global_col], 1, column_type,
         dest, local_col, MPI_COMM_WORLD);
/* Recv count=n uz MPI_INT znaci n kontinualnih integera lokalne kolone */
MPI_Recv(local_A[local_col], n, MPI_INT,
         MASTER, local_col, MPI_COMM_WORLD, &status);

/* Delovi b su kontinualni, zato Scatter; nema potrebe za izvedenim tipom */
/* s je send count za jedan proces i ujedno lokalni recv count */
MPI_Scatter(b, s, MPI_INT, local_b, s, MPI_INT,
            MASTER, MPI_COMM_WORLD);

/* Gather maksimuma otkriva KOJI proces treba da prikaze rezultate */
/* count=1: svaki rank salje samo svoj jedan local_max */
MPI_Gather(&local_max, 1, MPI_INT,
           max_by_rank, 1, MPI_INT,
           MASTER, MPI_COMM_WORLD);

/* Svi moraju saznati dinamicki izabran root pre Reduce operacija */
/* count=1: emituje se jedan integer result_rank */
MPI_Bcast(&result_rank, 1, MPI_INT,
          MASTER, MPI_COMM_WORLD);

/* Parcijalni proizvodi i parcijalne sume vrsta sabiraju se na result_rank */
/* count=n: zasebno se sabira svih n elemenata rezultujuceg vektora */
MPI_Reduce(local_c, c, n, MPI_INT, MPI_SUM,
           result_rank, MPI_COMM_WORLD);
/* count=n: zasebno se sabira parcijalna suma svake od n vrsta */
MPI_Reduce(local_row_sum, row_sum, n, MPI_INT, MPI_SUM,
           result_rank, MPI_COMM_WORLD);

/* Jun 2024: indexed jer blokovi rastu 1, 2, ..., 14 */
/* displs = prethodni krugovi svih procesa + raniji rankovi ovog kruga */
int p0_displs[14] =
    {0, 3, 9, 18, 30, 45, 63, 84, 108, 135, 165, 198, 234, 273};
int p1_displs[14] =
    {1, 5, 12, 22, 35, 51, 70, 92, 117, 145, 176, 210, 247, 287};
int p2_displs[14] =
    {2, 7, 15, 26, 40, 57, 77, 100, 126, 155, 187, 222, 260, 301};

for (k = 0; k < 14; k++) {
    blocklengths[k] = k + 1;
    displs[k] = nprocs * k * (k + 1) / 2
                + rank * (k + 1);
}
/* indexed count=14 jer jedan proces ima 14 rastucih blokova */
MPI_Type_indexed(14, blocklengths, displs,
                 MPI_INT, &filetype);

/* April 2026: svaki proces ima 2 polovine istog obima */
/* U fajlu prvo idu prve polovine svih rankova, pa druge polovine */
local_bytes = FILE_SIZE / nprocs;
half = local_bytes / 2;
/* vector count=2 jer jedan proces vidi tacno svoja 2 bloka */
MPI_Type_vector(2, half, half * nprocs,
                MPI_BYTE, &filetype);
MPI_Type_commit(&filetype);
disp = (MPI_Offset)rank * half;
MPI_File_open(MPI_COMM_WORLD, "april.dat",
              MPI_MODE_CREATE | MPI_MODE_WRONLY,
              MPI_INFO_NULL, &fh);
/* view govori gde su 2 bloka ovog ranka u zajednickom fajlu */
MPI_File_set_view(fh, disp, MPI_BYTE, filetype,
                  "native", MPI_INFO_NULL);
/* write_all count=local_bytes uz MPI_BYTE znaci toliko bajtova iz buf-a */
/* filetype opisuje raspored U FAJLU; MPI_BYTE opisuje buf U MEMORIJI */
MPI_File_write_all(fh, buf, local_bytes,
                   MPI_BYTE, &status);
MPI_File_close(&fh);

/* JUN26: 1kB, obrnuti blokovi, citanje sa eksplicitnim pomerajem */
file_size = 1024;
local_bytes = file_size / nprocs;
half = local_bytes / 2;
offset = (MPI_Offset)(nprocs - 1 - rank) * local_bytes;
MPI_File_open(MPI_COMM_WORLD, "input.dat",
              MPI_MODE_RDONLY, MPI_INFO_NULL, &fh);
/* count=local_bytes uz MPI_BYTE znaci toliko bajtova; nema seek-a */
MPI_File_read_at(fh, offset, buf,
                 local_bytes, MPI_BYTE, &status);
MPI_File_close(&fh);

/* Dva jednaka bloka ranka: sve prve, zatim sve druge polovine */
MPI_Type_vector(2, half, half * nprocs,
                MPI_BYTE, &filetype);
MPI_Type_commit(&filetype);
disp = (MPI_Offset)rank * half;
MPI_File_open(MPI_COMM_WORLD, "output.dat",
              MPI_MODE_CREATE | MPI_MODE_WRONLY,
              MPI_INFO_NULL, &fh);
MPI_File_set_view(fh, disp, MPI_BYTE, filetype,
                  "native", MPI_INFO_NULL);
/* count=local_bytes opisuje buf; filetype iz view-a opisuje fajl */
MPI_File_write_all(fh, buf, local_bytes,
                   MPI_BYTE, &status);
MPI_File_close(&fh);

/* Oktobar 2 2025: obrnut redosled citanja osmobitnih blokova */
file_size = 10 * 1024 * 1024;
local_bytes = file_size / nprocs;
part = local_bytes / 4;
offset = (MPI_Offset)(nprocs - 1 - rank) * local_bytes;
MPI_File_open(MPI_COMM_WORLD, "input.dat",
              MPI_MODE_RDONLY, MPI_INFO_NULL, &fh);
MPI_File_seek(fh, offset, MPI_SEEK_SET);
/* read count=local_bytes uz MPI_BYTE znaci tacno toliko bajtova */
MPI_File_read(fh, buf, local_bytes, MPI_BYTE, &status);
MPI_File_close(&fh);

/* Svaki rank ima 4 jednaka dela; fajl grupise isti deo svih rankova */
/* vector count=4 jer jedan proces vidi svoja 4 bloka */
MPI_Type_vector(4, part, part * nprocs,
                MPI_BYTE, &filetype);
MPI_Type_commit(&filetype);
disp = (MPI_Offset)rank * part;
MPI_File_open(MPI_COMM_WORLD, "output.dat",
              MPI_MODE_CREATE | MPI_MODE_WRONLY,
              MPI_INFO_NULL, &fh);
MPI_File_set_view(fh, disp, MPI_BYTE, filetype,
                  "native", MPI_INFO_NULL);
/* write_all count je broj bajtova u buf-u; vector vec opisuje 4 mesta */
MPI_File_write_all(fh, buf, local_bytes,
                   MPI_BYTE, &status);
MPI_File_close(&fh);

/* subarray: rucno opisujem velicinu i pocetak svoje podmatrice */
/* Globalna matrica ostaje jedna, proces vidi samo svoj pravougaonik */
gsizes[0] = M;
gsizes[1] = N;
lsizes[0] = M / proc_rows;
lsizes[1] = N / proc_cols;
starts[0] = coords[0] * lsizes[0];
starts[1] = coords[1] * lsizes[1];
MPI_Type_create_subarray(2, gsizes, lsizes, starts,
                         MPI_ORDER_C, MPI_FLOAT, &filetype);

/* darray: MPI sam odredjuje deo globalnog niza iz ranka i procesne mreze */
distribs[0] = MPI_DISTRIBUTE_BLOCK;
distribs[1] = MPI_DISTRIBUTE_BLOCK;
dargs[0] = MPI_DISTRIBUTE_DFLT_DARG;
dargs[1] = MPI_DISTRIBUTE_DFLT_DARG;
psizes[0] = proc_rows;
psizes[1] = proc_cols;
MPI_Type_create_darray(nprocs, rank, 2, gsizes,
                       distribs, dargs, psizes,
                       MPI_ORDER_C, MPI_FLOAT, &filetype);

/* Dekartova topologija daje procesu koordinate i susede u procesnoj mrezi */
MPI_Cart_create(MPI_COMM_WORLD, ndims, dims, periods,
                0, &cart_comm);
MPI_Cart_coords(cart_comm, rank, ndims, coords);
MPI_Cart_shift(cart_comm, direction, 1, &source, &dest);

/* File_open otvara isti fajl za komunikator; open/close su kolektivni */
MPI_File_open(MPI_COMM_WORLD, "file.dat",
              MPI_MODE_CREATE | MPI_MODE_RDWR,
              MPI_INFO_NULL, &fh);
MPI_File_close(&fh);

/* TRI NACINA ODREDJIVANJA POZICIJE U FAJLU */
offset = (MPI_Offset)rank * N * sizeof(int);

/* 1. Pojedinacni: svaki rank pamti SVOJU trenutnu poziciju */
/* seek je promeni, a read/write je zatim automatski pomeri */
/* read/write count = elemenata memorijskog datatype-a za ovaj poziv */
MPI_File_seek(fh, offset, MPI_SEEK_SET);
MPI_File_read(fh, buf, count, MPI_INT, &status);
MPI_File_write(fh, buf, count, MPI_INT, &status);

/* 2. Eksplicitni: ovaj poziv ide direktno na offset i ne menja pointer */
/* Koristi _at bez prethodnog seek-a */
/* _at count = elemenata memorijskog datatype-a, offset je u bajtovima */
MPI_File_read_at(fh, offset, buf, count, MPI_INT, &status);
MPI_File_write_at(fh, offset, buf, count, MPI_INT, &status);

/* 3. Deljeni: svi rankovi pomeraju ISTI pointer */
/* Sledeci slobodan blok dobija rank koji prvi stigne; rank redosled nije poznat */
/* shared count = koliko datatype elemenata taj rank uzima iz sledeceg bloka */
MPI_File_seek_shared(fh, 0, MPI_SEEK_SET);
MPI_File_read_shared(fh, buf, count, MPI_INT, &status);
MPI_File_write_shared(fh, buf, count, MPI_INT, &status);

/* IZBOR PREMA RASPOREDU PODATAKA */
/* Jedan kontinualni opseg: dovoljan je direktan offset, nema filetype-a */
/* local_n je I/O count: broj MPI_INT elemenata koje ovaj rank upisuje */
offset = (MPI_Offset)rank * local_n * sizeof(int);
MPI_File_write_at(fh, offset, buf, local_n,
                  MPI_INT, &status);

/* Vektor: vise jednakih blokova sa istim razmakom */
/* vector num_blocks opisuje FAJL; write_all count opisuje memorijski buf */
/* local_n u write_all je ukupan broj MPI_INT iz kontinualnog buf-a */
/* Redosled: type -> commit -> open -> set_view -> write_all */
MPI_Type_vector(num_blocks, block_len, stride,
                MPI_INT, &filetype);
MPI_Type_commit(&filetype);
MPI_File_open(MPI_COMM_WORLD, "file.dat",
              MPI_MODE_CREATE | MPI_MODE_WRONLY,
              MPI_INFO_NULL, &fh);
MPI_File_set_view(fh, disp, MPI_INT, filetype,
                  "native", MPI_INFO_NULL);
MPI_File_write_all(fh, buf, local_n, MPI_INT, &status);
MPI_File_close(&fh);

/* Indexed: vise blokova razlicitih duzina ili nepravilnih pocetaka */
/* indexed num_blocks broji blokove u fajlu */
/* write_all local_n broji sve MPI_INT elemente u memorijskom buf-u */
/* Redosled: type -> commit -> open -> set_view -> write_all */
MPI_Type_indexed(num_blocks, blocklengths, displs,
                 MPI_INT, &filetype);
MPI_Type_commit(&filetype);
MPI_File_open(MPI_COMM_WORLD, "file.dat",
              MPI_MODE_CREATE | MPI_MODE_WRONLY,
              MPI_INFO_NULL, &fh);
MPI_File_set_view(fh, disp, MPI_INT, filetype,
                  "native", MPI_INFO_NULL);
MPI_File_write_all(fh, buf, local_n, MPI_INT, &status);
MPI_File_close(&fh);

/* File view: proces sakriva tudje delove i svoje blokove vidi kontinualno */
/* disp je stvarni pocetak prvog vidljivog bloka tog ranka */
MPI_File_set_view(fh, disp, MPI_INT, filetype,
                  "native", MPI_INFO_NULL);
/* Posle view-a count i dalje broji elemente u buf-u, ne blokove filetype-a */
MPI_File_read(fh, buf, count, MPI_INT, &status);
MPI_File_write(fh, buf, count, MPI_INT, &status);

/* read_all/write_all: svi ucestvuju, MPI spaja njihove I/O zahteve */
/* Koristi kada se trazi efikasnost, posebno uz nekontinualan file view */
/* count = koliko datatype elemenata svaki proces prenosi iz svog buf-a */
MPI_File_read_all(fh, buf, count, MPI_INT, &status);
MPI_File_write_all(fh, buf, count, MPI_INT, &status);

/* Neblokirajuci I/O: pokreni disk, radi nesto drugo, pa Wait */
/* Bafer nije bezbedan za izmenu dok zahtev nije zavrsen */
/* count ima isto znacenje kao kod odgovarajuce blokirajuce I/O funkcije */
MPI_File_iread_at(fh, offset, buf, count,
                  MPI_INT, &request);
MPI_File_iwrite_at(fh, offset, buf, count,
                   MPI_INT, &request);
MPI_Wait(&request, &status);

/* Ordered koristi deljeni pointer, ali garantuje redosled P0, P1, P2... */
/* count = koliko datatype elemenata upisuje/cita SVAKI rank */
MPI_File_read_ordered(fh, buf, count, MPI_INT, &status);
MPI_File_write_ordered(fh, buf, count, MPI_INT, &status);

/* KONZISTENTNOST: close -> barrier -> open -> read */
/* close zavrsava upis; barrier ceka da svi zavrse tu fazu */
/* barrier sam ne upisuje podatke na disk */
MPI_File_close(&fh);
MPI_Barrier(MPI_COMM_WORLD);
MPI_File_open(MPI_COMM_WORLD, "file.dat",
              MPI_MODE_RDONLY, MPI_INFO_NULL, &fh);
/* count = koliko MPI_INT elemenata se cita nakon sinhronizovane faze */
MPI_File_read(fh, buf, count, MPI_INT, &status);

/* Atomicity stiti preklopljene pristupe; sync forsira vidljivost fajla */
/* Obe opcije mogu biti skuplje od najlakse close/barrier/open varijante */
MPI_File_set_atomicity(fh, 1);
MPI_Barrier(MPI_COMM_WORLD);
MPI_File_sync(fh);

/* JEDINICE: offset/disp su bajtovi; I/O count je broj datatype elemenata */
/* vector stride/displs su u oldtype elementima, ne u bajtovima */
offset = (MPI_Offset)element_index * sizeof(int);
disp = (MPI_Offset)first_visible_int * sizeof(int);
count = number_of_elements;
stride = distance_in_oldtype_elements;
```

# Objasnjenje zadatka `shared_neblokirajuci`

Ovaj zadatak pokriva MPI I/O funkcije iz Termina 2 koje nisu koriscene u `priprema` zadatku.

Glavne teme:

1. ordered upis sa deljenim pokazivacem
2. shared upis sa deljenim pokazivacem
3. neblokirajuci upis sa eksplicitnim pomerajem

Glavne funkcije:

```c
MPI_File_write_ordered
MPI_File_write_shared
MPI_File_seek_shared
MPI_File_iwrite_at
MPI_Wait
```

## Osnovna ideja

Svaki proces ima dva bafera:

```c
int buf[N];
int async_buf[N];
```

`N` je:

```c
#define N 4
```

Prvi bafer:

```c
buf[i] = rank * 10 + i;
```

Za 4 procesa:

```text
P0: 0 1 2 3
P1: 10 11 12 13
P2: 20 21 22 23
P3: 30 31 32 33
```

Drugi bafer:

```c
async_buf[i] = rank * 100 + i;
```

Za 4 procesa:

```text
P0: 0 1 2 3
P1: 100 101 102 103
P2: 200 201 202 203
P3: 300 301 302 303
```

Tako se jasno vidi koji proces je sta upisao.

## Pomocna funkcija za prikaz

Na vrhu postoji:

```c
static void print_file_as_ints(const char *filename, int count)
```

Ona nije MPI I/O deo zadatka.

Sluzi samo da rank 0 procita binarni fajl obicnim C funkcijama:

```c
fopen
fread
fclose
```

i prikaze integer vrednosti.

To radimo da odmah vidimo raspored u fajlu.

## Deo I - MPI_File_write_ordered

Kod:

```c
MPI_File_open(MPI_COMM_WORLD, "ordered.bin",
              MPI_MODE_CREATE | MPI_MODE_WRONLY,
              MPI_INFO_NULL, &fh);

MPI_File_set_size(fh, 0);
MPI_File_write_ordered(fh, buf, N, MPI_INT, &status);
MPI_File_close(&fh);
```

### Sta je ordered upis

`MPI_File_write_ordered` je grupna operacija sa deljenim pokazivacem.

To znaci:

- svi procesi moraju da je pozovu
- koristi se jedan logicki deljeni pokazivac na fajl
- redosled upisa ide po ranku procesa

Za 4 procesa rezultat je deterministican:

```text
P0 pa P1 pa P2 pa P3
```

Zato je `ordered.bin`:

```text
0 1 2 3 10 11 12 13 20 21 22 23 30 31 32 33
```

### Zasto je ovo vazno

Ovo se koristi kada hoces da svi procesi upisuju u isti fajl, ali hoces redosled po ranku.

To je drugacije od obicnog `MPI_File_write_shared`, gde redosled nije garantovan.

### MPI_File_set_size

```c
MPI_File_set_size(fh, 0);
```

Ovim praznimo fajl na pocetku.

Ako fajl postoji od ranije i bio je duzi, bez ovoga bi stari podaci mogli ostati na kraju fajla.

## Deo II - MPI_File_write_shared

Kod:

```c
MPI_File_open(MPI_COMM_WORLD, "shared.bin",
              MPI_MODE_CREATE | MPI_MODE_WRONLY,
              MPI_INFO_NULL, &fh);

MPI_File_set_size(fh, 0);
MPI_File_seek_shared(fh, 0, MPI_SEEK_SET);
MPI_File_write_shared(fh, buf, N, MPI_INT, &status);
MPI_File_close(&fh);
```

### Sta je shared pointer

Do sada smo najcesce imali:

```text
svaki proces ima svoj individualni pokazivac
```

Kod shared varijante:

```text
svi procesi dele jedan pokazivac na fajl
```

Kada jedan proces upise `N` integera, deljeni pokazivac se pomeri za `N` integera. Sledeci proces koji pise nastavlja od nove pozicije.

### MPI_File_seek_shared

```c
MPI_File_seek_shared(fh, 0, MPI_SEEK_SET);
```

Ovo postavlja deljeni pokazivac na pocetak fajla.

Slicno je kao `MPI_File_seek`, ali za shared pointer.

### MPI_File_write_shared

```c
MPI_File_write_shared(fh, buf, N, MPI_INT, &status);
```

Svaki proces upisuje svoj `buf`.

Bitno:

```text
redosled nije garantovan po ranku
```

Zato `shared.bin` moze biti:

```text
0 1 2 3 10 11 12 13 20 21 22 23 30 31 32 33
```

ali moze biti i:

```text
20 21 22 23 0 1 2 3 30 31 32 33 10 11 12 13
```

Oba mogu biti ispravna, jer `write_shared` ne obecava rank redosled.

### Kada se koristi

Koristi se kada ti nije bitno koji proces upisuje na koju poziciju.

Tipican primer iz prezentacije:

```text
log fajl
```

Ako ti je redosled bitan, koristi `MPI_File_write_ordered`.

## Razlika ordered vs shared

`MPI_File_write_ordered`:

```text
grupna funkcija
svi procesi ucestvuju
upis ide po ranku
redosled deterministican
```

`MPI_File_write_shared`:

```text
koristi deljeni pokazivac
redosled nije garantovan po ranku
dobro kada redosled nije bitan
```

Kratko:

```text
ordered = znam redosled
shared  = ne znam redosled
```

## Deo III - MPI_File_iwrite_at

Kod:

```c
MPI_File_open(MPI_COMM_WORLD, "async.bin",
              MPI_MODE_CREATE | MPI_MODE_WRONLY,
              MPI_INFO_NULL, &fh);

MPI_File_set_size(fh, nprocs * N * sizeof(int));

offset = (MPI_Offset)rank * N * sizeof(int);
MPI_File_iwrite_at(fh, offset, async_buf, N, MPI_INT, &request);

suma = 0;
for (i = 0; i < 100000; i++) {
    suma += i % 7;
}

MPI_Wait(&request, &status);
MPI_File_close(&fh);
```

### Sta je neblokirajuci I/O

Obican upis:

```c
MPI_File_write_at(...)
```

blokira proces dok se operacija ne zavrsi.

Neblokirajuci upis:

```c
MPI_File_iwrite_at(...)
```

pokrene upis i odmah vrati `MPI_Request`.

Proces onda moze da radi nesto drugo.

U primeru:

```c
for (i = 0; i < 100000; i++) {
    suma += i % 7;
}
```

Ovo predstavlja "koristan rad" iz prezentacije.

### MPI_Request

```c
MPI_Request request;
```

`request` identifikuje zapocetu neblokirajucu operaciju.

Bez njega kasnije ne bismo znali koju operaciju cekamo.

### MPI_Wait

```c
MPI_Wait(&request, &status);
```

Ovo ceka da se neblokirajuci upis zavrsi.

Bitno pravilo:

```text
posle iwrite_at moras imati Wait ili Test
```

Ako nemas `MPI_Wait`, program moze da nastavi kao da je upis gotov, a on mozda jos nije zavrsen.

Takodje ne treba menjati `async_buf` dok se operacija ne zavrsi.

### Zasto koristimo iwrite_at

Funkcija:

```c
MPI_File_iwrite_at(fh, offset, async_buf, N, MPI_INT, &request);
```

ima eksplicitni offset.

Offset:

```c
offset = (MPI_Offset)rank * N * sizeof(int);
```

Zato svaki proces pise na tacno svoje mesto.

Za `N = 4`:

```text
P0 offset = 0
P1 offset = 4 * sizeof(int)
P2 offset = 8 * sizeof(int)
P3 offset = 12 * sizeof(int)
```

Zato je `async.bin` deterministican:

```text
0 1 2 3 100 101 102 103 200 201 202 203 300 301 302 303
```

## Zasto u ovom zadatku nema file view

Ovaj zadatak nije o nekontinualnom rasporedu po procesu.

Prvi deo pokazuje ordered shared pointer.

Drugi deo pokazuje obican shared pointer.

Treci deo pokazuje neblokirajuci `_at`.

Zato nam ne treba:

```c
MPI_File_set_view
MPI_Type_vector
```

To je vec pokriveno u `priprema` i `subarray_matrica`.

## Funkcije koje ovaj zadatak pokriva

```c
MPI_File_open
MPI_File_set_size
MPI_File_write_ordered
MPI_File_seek_shared
MPI_File_write_shared
MPI_File_iwrite_at
MPI_Wait
MPI_File_close
```

## Najcesce greske

Ne ocekuj da `MPI_File_write_shared` upisuje po ranku. Ako ti treba rank redosled, koristi:

```c
MPI_File_write_ordered
```

Posle neblokirajuceg I/O mora:

```c
MPI_Wait
```

ili:

```c
MPI_Test
```

Offset je u bajtovima:

```c
rank * N * sizeof(int)
```

`count` je broj elemenata:

```c
N, MPI_INT
```

`MPI_File_set_size(fh, 0)` je tu da ocisti fajl pre novog upisa.


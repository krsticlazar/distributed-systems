# Primer: shared pointer, ordered i neblokirajuci I/O

Ovaj primer pokriva funkcije iz Termina 2 koje nisu u `priprema` zadatku:

```c
MPI_File_write_ordered
MPI_File_write_shared
MPI_File_seek_shared
MPI_File_iwrite_at
MPI_Wait
```

Program pravi 3 fajla:

```text
ordered.bin
shared.bin
async.bin
```

## Kompajliranje

```bash
cd ~/distribuirani-mpi/zadaci/shared_neblokirajuci
mpicc shared_neblokirajuci.c -o shared_neblokirajuci
```

## Pokretanje

```bash
mpirun --oversubscribe -np 4 ./shared_neblokirajuci
```

## Deo I - MPI_File_write_ordered

Kod:

```c
MPI_File_write_ordered(fh, buf, N, MPI_INT, &status);
```

Ovo je grupna operacija sa deljenim pokazivacem.

Upis ide po ranku procesa:

```text
P0 pa P1 pa P2 pa P3
```

Ako je `N = 4`, ocekivano:

```text
ordered.bin:
0 1 2 3 10 11 12 13 20 21 22 23 30 31 32 33
```

## Deo II - MPI_File_write_shared

Kod:

```c
MPI_File_seek_shared(fh, 0, MPI_SEEK_SET);
MPI_File_write_shared(fh, buf, N, MPI_INT, &status);
```

Svi procesi dele jedan file pointer.

Bitno: redosled nije garantovan po ranku.

Zato `shared.bin` uvek ima sve blokove, ali redosled moze biti razlicit:

```text
0 1 2 3 20 21 22 23 10 11 12 13 30 31 32 33
```

ili neki drugi redosled.

Ovo je korisno za log fajlove kada nije bitno ko prvi upisuje.

## Deo III - MPI_File_iwrite_at + MPI_Wait

Kod:

```c
MPI_File_iwrite_at(fh, offset, async_buf, N, MPI_INT, &request);

/* neki koristan rad */

MPI_Wait(&request, &status);
```

`MPI_File_iwrite_at` je neblokirajuca funkcija. Ona pokrene upis i vrati `MPI_Request`.

`MPI_Wait` se poziva kasnije i ceka da se upis zavrsi.

U primeru svaki proces pise na eksplicitni offset:

```c
offset = rank * N * sizeof(int);
```

Zato je `async.bin` deterministican:

```text
async.bin:
0 1 2 3 100 101 102 103 200 201 202 203 300 301 302 303
```

## Rucna provera fajlova

```bash
od -An -t d4 -w64 ordered.bin
od -An -t d4 -w64 shared.bin
od -An -t d4 -w64 async.bin
```

## Ciscenje

```bash
rm -f shared_neblokirajuci ordered.bin shared.bin async.bin
```

## Najbitnije za ispit

`write_ordered`:

```text
grupno + deljeni pokazivac + redosled po ranku
```

`write_shared`:

```text
deljeni pokazivac + redosled nije garantovan
```

`iwrite_at`:

```text
neblokirajuci upis + eksplicitni offset + obavezan MPI_Wait
```

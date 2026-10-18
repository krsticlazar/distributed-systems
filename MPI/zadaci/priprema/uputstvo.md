# Priprema lab - MPI I/O

Ovo je skolska verzija resenja, namerno pisana slicno primerima sa casa.

Nema `CHECK_MPI` macro-a, nema pomocnih funkcija, nema dodatne apstrakcije. Kod prati redosled iz prezentacija:

```text
MPI_File_open
MPI_File_seek / MPI_File_read_at / MPI_File_set_view
MPI_File_write / MPI_File_write_all
MPI_File_close
```

## Fajlovi

```text
priprema.c
```

Program pravi:

```text
file1.bin
file2.bin
```

`file1.bin` je prvi fajl:

```text
P0: 0 1 2 3 4 5 6 7 8 9
P1: 10 11 12 13 14 15 16 17 18 19
P2: 20 21 22 23 24 25 26 27 28 29
P3: 30 31 32 33 34 35 36 37 38 39
```

`file2.bin` je drugi fajl, upisan efikasno preko `file view`-a:

```text
0 1 10 11 20 21 30 31 2 3 12 13 22 23 32 33 4 5 14 15 24 25 34 35 6 7 16 17 26 27 36 37 8 9 18 19 28 29 38 39
```

## Kompajliranje u WSL Ubuntu

Udji u folder gde je `priprema.c`:

```bash
cd ~/distribuirani-mpi/priprema
```

ili ako drzis strukturu sa folderom `zadaci`:

```bash
cd ~/distribuirani-mpi/zadaci/priprema
```

Kompajliraj:

```bash
mpicc priprema.c -o priprema
```

## Pokretanje

Program je za tacno 4 procesa:

```bash
mpirun --oversubscribe -np 4 ./priprema
```

Ako kod tebe `mpirun -np 4 ./priprema` radi bez `--oversubscribe`, mozes i tako. Kod WSL-a je normalno da Open MPI nekad trazi `--oversubscribe`.

## Ocekivani izlaz

Redosled linija za procese moze da se razlikuje, jer procesi stampaju paralelno.

Bitno je da svi procesi imaju `citanje OK`, i da `file2.bin` ima ovaj raspored:

```text
0 1 10 11 20 21 30 31 2 3 12 13 22 23 32 33 4 5 14 15 24 25 34 35 6 7 16 17 26 27 36 37 8 9 18 19 28 29 38 39
```

Primer:

```text
Proces 0: citanje OK [0 ... 9]
Proces 1: citanje OK [10 ... 19]
Proces 2: citanje OK [20 ... 29]
Proces 3: citanje OK [30 ... 39]

Sadrzaj file2.bin:
0 1 10 11 20 21 30 31 2 3 12 13 22 23 32 33 4 5 14 15 24 25 34 35 6 7 16 17 26 27 36 37 8 9 18 19 28 29 38 39
```

## Kako zadatak odgovara prezentacijama

Prvi deo koristi pojedinacni pokazivac:

```c
offset = (MPI_Offset)rank * N * sizeof(int);
MPI_File_seek(fh, offset, MPI_SEEK_SET);
MPI_File_write(fh, buf, N, MPI_INT, &status);
```

To je stil iz Termina 1: `seek`, pa `write`.

Drugi deo koristi eksplicitni pomeraj:

```c
MPI_File_read_at(fh, offset, rbuf, N, MPI_INT, &status);
```

To je stil iz Termina 1: `_at` funkcija, bez `seek`.

Treci deo koristi `file view` i grupni upis:

```c
MPI_Type_vector(N / NW, NW, NW * nprocs, MPI_INT, &filetype);
MPI_Type_commit(&filetype);

disp = (MPI_Offset)rank * NW * sizeof(int);

MPI_File_set_view(fh, disp, MPI_INT, filetype,
                  "native", MPI_INFO_NULL);

MPI_File_write_all(fh, rbuf, N, MPI_INT, &status);
```

To je stil iz Termina 2:

```text
izvedeni tip + file view + grupna I/O operacija
```

## Zasto su `N` i `NW`

```c
#define N 10
#define NW 2
```

`N` je broj integera po procesu.

`NW` je velicina jednog bloka u trecem delu. Sa slike iz zadatka vidi se da se uzimaju po 2 broja od svakog procesa:

```text
0 1 | 10 11 | 20 21 | 30 31 | 2 3 | ...
```

Zato je:

```c
MPI_Type_vector(N / NW, NW, NW * nprocs, MPI_INT, &filetype);
```

Za 4 procesa:

```text
N / NW       = 10 / 2 = 5 blokova
NW           = 2 integera po bloku
NW * nprocs  = 2 * 4 = 8 integera razmaka
```

`disp` je pocetak prvog bloka za dati proces:

```c
disp = rank * NW * sizeof(int);
```

## Rucna provera binarnih fajlova

Binarni fajl ne otvaraj kao tekst.

Prikaz kao integeri:

```bash
od -An -t d4 -w160 file1.bin
od -An -t d4 -w160 file2.bin
```

## Ciscenje

```bash
rm -f priprema file1.bin file2.bin
```

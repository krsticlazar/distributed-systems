# Primer: subarray matrica

Ovaj primer pokriva funkcije koje nisu u `priprema` zadatku:

```c
MPI_Cart_create
MPI_Cart_coords
MPI_Type_create_subarray
MPI_File_set_view
MPI_File_write_all
```

Ideja iz Termina 2: globalna matrica je sacuvana u fajlu, a svaki proces ima samo svoju podmatricu.

## Sta program radi

Globalna matrica:

```text
4 x 8
```

Procesi:

```text
2 x 2
```

Svaki proces dobija lokalnu podmatricu:

```text
2 x 4
```

Raspored:

```text
P0 P1
P2 P3
```

Svaki proces puni svoj lokalni bafer vrednostima globalnih indeksa. Kada se sve upise u `matrix.bin`, rezultat treba da bude:

```text
0  1  2  3  4  5  6  7
8  9 10 11 12 13 14 15
16 17 18 19 20 21 22 23
24 25 26 27 28 29 30 31
```

## Kompajliranje

```bash
cd ~/distribuirani-mpi/zadaci/subarray_matrica
mpicc subarray_matrica.c -o subarray_matrica
```

## Pokretanje

```bash
mpirun --oversubscribe -np 4 ./subarray_matrica
```

## Ocekivani izlaz

```text
Sadrzaj matrix.bin kao matrica 4 x 8:
 0  1  2  3  4  5  6  7
 8  9 10 11 12 13 14 15
16 17 18 19 20 21 22 23
24 25 26 27 28 29 30 31
```

## Najbitniji deo

Cartesian topologija daje koordinate procesa:

```c
MPI_Cart_create(MPI_COMM_WORLD, 2, dims, periods, 0, &comm2d);
MPI_Cart_coords(comm2d, rank, 2, coords);
```

Pocetak podmatrice:

```c
starts[0] = coords[0] * LOCAL_ROWS;
starts[1] = coords[1] * LOCAL_COLS;
```

Subarray opisuje gde je lokalna podmatrica u globalnoj matrici:

```c
MPI_Type_create_subarray(2, sizes, subsizes, starts,
                         MPI_ORDER_C, MPI_INT, &filetype);
MPI_Type_commit(&filetype);
```

File view koristi taj tip:

```c
MPI_File_set_view(fh, 0, MPI_INT, filetype,
                  "native", MPI_INFO_NULL);
MPI_File_write_all(fh, local, LOCAL_ROWS * LOCAL_COLS,
                   MPI_INT, &status);
```

## Rucna provera fajla

```bash
od -An -t d4 -w32 matrix.bin
```

## Ciscenje

```bash
rm -f subarray_matrica matrix.bin
```

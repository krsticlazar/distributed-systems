# Objasnjenje zadatka `subarray_matrica`

Ovaj zadatak pokriva deo iz Termina 2 koji se odnosi na pristup matricama, podmatricama i raspodeljenim nizovima u fajlu.

Glavne funkcije:

```c
MPI_Cart_create
MPI_Cart_coords
MPI_Type_create_subarray
MPI_File_set_view
MPI_File_write_all
```

Ideja: imamo jednu globalnu matricu u fajlu, ali svaki MPI proces u memoriji ima samo svoju lokalnu podmatricu.

## Sta program pravi

Globalna matrica ima dimenzije:

```c
#define ROWS 4
#define COLS 8
```

Znaci globalno imamo:

```text
4 reda x 8 kolona = 32 integera
```

Procesi su rasporedjeni kao procesorska mreza:

```c
#define PROC_ROWS 2
#define PROC_COLS 2
```

To znaci:

```text
2 x 2 procesa = 4 procesa
```

Lokalna podmatrica svakog procesa:

```c
#define LOCAL_ROWS (ROWS / PROC_ROWS)
#define LOCAL_COLS (COLS / PROC_COLS)
```

Konkretno:

```text
LOCAL_ROWS = 4 / 2 = 2
LOCAL_COLS = 8 / 2 = 4
```

Svaki proces ima podmatricu `2 x 4`.

## Raspored procesa

Procesi se posmatraju kao mreza:

```text
P0 P1
P2 P3
```

Svaki proces treba da upise svoj deo globalne matrice.

Globalna matrica treba da izgleda:

```text
 0  1  2  3  4  5  6  7
 8  9 10 11 12 13 14 15
16 17 18 19 20 21 22 23
24 25 26 27 28 29 30 31
```

Podmatrice:

```text
P0 dobija redove 0-1, kolone 0-3
P1 dobija redove 0-1, kolone 4-7
P2 dobija redove 2-3, kolone 0-3
P3 dobija redove 2-3, kolone 4-7
```

## Zasto koristimo Cartesian topologiju

U kodu:

```c
int dims[2], periods[2], coords[2];
MPI_Comm comm2d;
```

`dims` opisuje dimenzije procesorske mreze.

```c
dims[0] = PROC_ROWS;
dims[1] = PROC_COLS;
```

Kod nas:

```text
dims[0] = 2
dims[1] = 2
```

`periods` kaze da li je mreza periodicna. Ovde nije:

```c
periods[0] = 0;
periods[1] = 0;
```

Kreira se 2D komunikator:

```c
MPI_Cart_create(MPI_COMM_WORLD, 2, dims, periods, 0, &comm2d);
```

Zasto? Zato sto procesi tada mogu da dobiju koordinate u mrezi.

```c
MPI_Cart_coords(comm2d, rank, 2, coords);
```

Primer:

```text
rank 0 -> coords = (0, 0)
rank 1 -> coords = (0, 1)
rank 2 -> coords = (1, 0)
rank 3 -> coords = (1, 1)
```

Napomena: zbog `reorder = 0` u `MPI_Cart_create`, MPI ne menja redosled rankova.

## Pocetak podmatrice

Kada znamo koordinate procesa, racunamo gde njegova podmatrica pocinje u globalnoj matrici:

```c
starts[0] = coords[0] * LOCAL_ROWS;
starts[1] = coords[1] * LOCAL_COLS;
```

`starts[0]` je pocetni red.

`starts[1]` je pocetna kolona.

Primer:

```text
P0 coords = (0,0) -> starts = (0,0)
P1 coords = (0,1) -> starts = (0,4)
P2 coords = (1,0) -> starts = (2,0)
P3 coords = (1,1) -> starts = (2,4)
```

Ovo je tacno ono sto treba za podmatrice.

## Popunjavanje lokalnog bafera

Svaki proces ima:

```c
int local[LOCAL_ROWS * LOCAL_COLS];
```

To je lokalna podmatrica u memoriji, ali zapisana kao 1D niz.

Kod:

```c
for (i = 0; i < LOCAL_ROWS; i++) {
    for (j = 0; j < LOCAL_COLS; j++) {
        int global_i = starts[0] + i;
        int global_j = starts[1] + j;
        local[i * LOCAL_COLS + j] = global_i * COLS + global_j;
    }
}
```

Ovde se lokalni elementi pune vrednostima koje odgovaraju globalnim indeksima.

Formula:

```c
global_i * COLS + global_j
```

To je standardni C row-major indeks za matricu.

Primer:

```text
element na globalnoj poziciji (2, 5) ima vrednost 2 * 8 + 5 = 21
```

Zato je lako proveriti da li je fajl dobar.

## Parametri za MPI_Type_create_subarray

Prvo se postavljaju globalne dimenzije:

```c
sizes[0] = ROWS;
sizes[1] = COLS;
```

Kod nas:

```text
sizes = {4, 8}
```

Zatim lokalne dimenzije:

```c
subsizes[0] = LOCAL_ROWS;
subsizes[1] = LOCAL_COLS;
```

Kod nas:

```text
subsizes = {2, 4}
```

`starts` smo vec izracunali preko Cartesian koordinata.

Zatim:

```c
MPI_Type_create_subarray(2, sizes, subsizes, starts,
                         MPI_ORDER_C, MPI_INT, &filetype);
MPI_Type_commit(&filetype);
```

Znacenje:

```text
napravi izvedeni tip koji opisuje podmatricu
dimenzije subsizes
koja pocinje na starts
unutar globalne matrice sizes
```

`MPI_ORDER_C` znaci C raspored u memoriji, red po red.

`MPI_INT` znaci da su elementi matrice integeri.

`filetype` je tip koji opisuje mesto podmatrice u fajlu.

## Zasto je `disp = 0`

Kod:

```c
MPI_File_set_view(fh, 0, MPI_INT, filetype,
                  "native", MPI_INFO_NULL);
```

Ovde je `disp = 0`.

Zasto ne koristimo:

```c
rank * nesto * sizeof(int)
```

Zato sto `filetype` koji je napravljen preko `MPI_Type_create_subarray` vec sadrzi informaciju gde podmatrica pocinje.

Kod `vector` primera iz pripreme morali smo da racunamo `disp`, jer je `filetype` opisivao isti obrazac za sve procese, a pocetak je bio razlicit.

Kod `subarray` primera:

```text
starts je vec razlicit za svaki proces
```

Zato je `disp = 0`.

## Upis preko MPI_File_write_all

Kod:

```c
MPI_File_write_all(fh, local, LOCAL_ROWS * LOCAL_COLS,
                   MPI_INT, &status);
```

Svaki proces upisuje svoju lokalnu podmatricu.

`count` je:

```c
LOCAL_ROWS * LOCAL_COLS
```

Kod nas:

```text
2 * 4 = 8 integera po procesu
```

Zbog `file view`-a, tih 8 integera ne ide bilo gde u fajl, nego tacno na mesto podmatrice tog procesa.

`write_all` je grupna operacija, pa svi procesi moraju da je pozovu.

## Zasto koristimo MPI_File_set_size

Kod:

```c
MPI_File_set_size(fh, ROWS * COLS * sizeof(int));
```

Ovo postavlja velicinu fajla na tacnu velicinu globalne matrice.

Nije glavna poenta zadatka, ali je korisno:

- ako fajl postoji od ranije i bio je veci, odsece se visak
- rezultat bude cist

## Provera na kraju

Rank 0 cita fajl preko obicnog C `fread`:

```c
FILE *fp = fopen("matrix.bin", "rb");
fread(matrix, sizeof(int), ROWS * COLS, fp);
```

Ovo nije deo MPI I/O resenja. Ovo je samo prikaz rezultata da vidis matricu.

Glavni MPI deo je pre toga:

```text
MPI_Type_create_subarray
MPI_File_set_view
MPI_File_write_all
```

## Darray vs subarray

U prezentaciji postoji i:

```c
MPI_Type_create_darray
```

`darray` se koristi kada MPI-ju prepustis raspodelu globalnog niza po procesima.

`subarray` se koristi kada ti sam znas:

```text
globalne dimenzije
lokalne dimenzije
pocetne koordinate podmatrice
```

U ovom primeru koristimo `subarray` jer je lakse za razumevanje i lepo se vidi kako koordinate procesa daju pocetak podmatrice.

## Funkcije koje ovaj zadatak pokriva

```c
MPI_Cart_create
MPI_Cart_coords
MPI_Type_create_subarray
MPI_Type_commit
MPI_File_open
MPI_File_set_size
MPI_File_set_view
MPI_File_write_all
MPI_File_close
MPI_Type_free
MPI_Comm_free
```

## Najcesce greske

`sizes` su globalne dimenzije:

```text
4 x 8
```

`subsizes` su lokalne dimenzije:

```text
2 x 4
```

`starts` nisu u bajtovima. To su koordinate podmatrice u elementima matrice.

Kod `MPI_File_set_view`, `disp = 0` jer `subarray` vec opisuje pocetak.

`MPI_Type_commit` mora pre `MPI_File_set_view`.

`MPI_File_write_all` moraju pozvati svi procesi.


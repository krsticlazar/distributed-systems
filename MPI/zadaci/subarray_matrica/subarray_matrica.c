/*
 * Primer: upis globalne matrice pomocu MPI_Type_create_subarray.
 *
 * 4 procesa su rasporedjena kao 2 x 2 procesorska mreza.
 * Globalna matrica je 4 x 8.
 * Svaki proces upisuje svoju lokalnu podmatricu 2 x 4.
 *
 * Pokretanje:
 *   mpirun --oversubscribe -np 4 ./subarray_matrica
 */

#include <stdio.h>
#include <stdlib.h>
#include "mpi.h"

#define ROWS 4
#define COLS 8

#define PROC_ROWS 2
#define PROC_COLS 2

#define LOCAL_ROWS (ROWS / PROC_ROWS)
#define LOCAL_COLS (COLS / PROC_COLS)

int main(int argc, char **argv)
{
    int rank, nprocs;
    int i, j;
    int local[LOCAL_ROWS * LOCAL_COLS];
    int sizes[2], subsizes[2], starts[2];
    int dims[2], periods[2], coords[2];
    MPI_Comm comm2d;
    MPI_File fh;
    MPI_Status status;
    MPI_Datatype filetype;

    MPI_Init(&argc, &argv);
    MPI_Comm_rank(MPI_COMM_WORLD, &rank);
    MPI_Comm_size(MPI_COMM_WORLD, &nprocs);

    if (nprocs != PROC_ROWS * PROC_COLS) {
        if (rank == 0) {
            printf("Program pokrenuti sa tacno 4 procesa.\n");
            printf("Primer: mpirun --oversubscribe -np 4 ./subarray_matrica\n");
        }

        MPI_Finalize();
        return 0;
    }

    /*
     * Cartesian topologija sluzi da proces dobije koordinate u mrezi.
     * Primer za 4 procesa:
     *   coords (0,0) -> gornji levi blok
     *   coords (0,1) -> gornji desni blok
     *   coords (1,0) -> donji levi blok
     *   coords (1,1) -> donji desni blok
     */
    dims[0] = PROC_ROWS;
    dims[1] = PROC_COLS;
    periods[0] = 0;
    periods[1] = 0;

    MPI_Cart_create(MPI_COMM_WORLD, 2, dims, periods, 0, &comm2d);
    MPI_Cart_coords(comm2d, rank, 2, coords);

    starts[0] = coords[0] * LOCAL_ROWS;
    starts[1] = coords[1] * LOCAL_COLS;

    /*
     * Lokalni bafer punimo vrednostima koje odgovaraju globalnim indeksima.
     * Ako je sve dobro upisano, fajl predstavlja matricu:
     * 0  1  2  ... 31
     */
    for (i = 0; i < LOCAL_ROWS; i++) {
        for (j = 0; j < LOCAL_COLS; j++) {
            int global_i = starts[0] + i;
            int global_j = starts[1] + j;
            local[i * LOCAL_COLS + j] = global_i * COLS + global_j;
        }
    }

    sizes[0] = ROWS;
    sizes[1] = COLS;

    subsizes[0] = LOCAL_ROWS;
    subsizes[1] = LOCAL_COLS;

    /*
     * filetype opisuje podmatricu procesa unutar globalne matrice.
     * disp je 0 jer subarray vec nosi informaciju o pocetku podmatrice.
     */
    MPI_Type_create_subarray(2, sizes, subsizes, starts,
                             MPI_ORDER_C, MPI_INT, &filetype);
    MPI_Type_commit(&filetype);

    MPI_File_open(MPI_COMM_WORLD, "matrix.bin",
                  MPI_MODE_CREATE | MPI_MODE_WRONLY,
                  MPI_INFO_NULL, &fh);

    MPI_File_set_size(fh, ROWS * COLS * sizeof(int));

    MPI_File_set_view(fh, 0, MPI_INT, filetype,
                      "native", MPI_INFO_NULL);

    MPI_File_write_all(fh, local, LOCAL_ROWS * LOCAL_COLS,
                       MPI_INT, &status);

    MPI_File_close(&fh);
    MPI_Type_free(&filetype);
    MPI_Comm_free(&comm2d);

    /*
     * Provera iz procesa 0.
     * Ovo nije MPI I/O deo zadatka, samo lak prikaz rezultata.
     */
    if (rank == 0) {
        int matrix[ROWS * COLS];
        FILE *fp = fopen("matrix.bin", "rb");

        if (fp == NULL) {
            printf("Greska pri otvaranju matrix.bin.\n");
            MPI_Finalize();
            return 0;
        }

        fread(matrix, sizeof(int), ROWS * COLS, fp);
        fclose(fp);

        printf("Sadrzaj matrix.bin kao matrica %d x %d:\n", ROWS, COLS);
        for (i = 0; i < ROWS; i++) {
            for (j = 0; j < COLS; j++) {
                printf("%2d ", matrix[i * COLS + j]);
            }
            printf("\n");
        }
    }

    MPI_Finalize();
    return 0;
}

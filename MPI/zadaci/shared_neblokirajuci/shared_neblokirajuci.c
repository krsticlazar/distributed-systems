/*
 * Primer: deljeni pokazivac, ordered upis i neblokirajuci I/O.
 *
 * Pokriva:
 *   MPI_File_write_ordered
 *   MPI_File_write_shared
 *   MPI_File_seek_shared
 *   MPI_File_iwrite_at
 *   MPI_Wait
 *
 * Pokretanje:
 *   mpirun --oversubscribe -np 4 ./shared_neblokirajuci
 */

#include <stdio.h>
#include <stdlib.h>
#include "mpi.h"

#define N 4

static void print_file_as_ints(const char *filename, int count)
{
    int i;
    int *data = (int *)malloc(count * sizeof(int));
    FILE *fp = fopen(filename, "rb");

    if (fp == NULL) {
        printf("Ne mogu da otvorim %s.\n", filename);
        free(data);
        return;
    }

    fread(data, sizeof(int), count, fp);
    fclose(fp);

    printf("%s:\n", filename);
    for (i = 0; i < count; i++) {
        printf("%d ", data[i]);
    }
    printf("\n\n");

    free(data);
}

int main(int argc, char **argv)
{
    int rank, nprocs, i;
    int buf[N];
    int async_buf[N];
    int suma;
    MPI_File fh;
    MPI_Status status;
    MPI_Request request;
    MPI_Offset offset;

    MPI_Init(&argc, &argv);
    MPI_Comm_rank(MPI_COMM_WORLD, &rank);
    MPI_Comm_size(MPI_COMM_WORLD, &nprocs);

    for (i = 0; i < N; i++) {
        buf[i] = rank * 10 + i;
        async_buf[i] = rank * 100 + i;
    }

    /*
     * I) Ordered upis sa deljenim pokazivacem.
     *
     * Svi procesi pozivaju kolektivnu funkciju.
     * Upis ide redom po ranku: P0, P1, P2, ...
     */
    MPI_File_open(MPI_COMM_WORLD, "ordered.bin",
                  MPI_MODE_CREATE | MPI_MODE_WRONLY,
                  MPI_INFO_NULL, &fh);

    MPI_File_set_size(fh, 0);
    MPI_File_write_ordered(fh, buf, N, MPI_INT, &status);
    MPI_File_close(&fh);

    if (rank == 0) {
        printf("I) MPI_File_write_ordered - redosled po ranku\n");
        print_file_as_ints("ordered.bin", nprocs * N);
    }

    /*
     * II) Shared upis.
     *
     * Svi procesi dele jedan file pointer.
     * Redosled upisa nije garantovan po ranku.
     */
    MPI_File_open(MPI_COMM_WORLD, "shared.bin",
                  MPI_MODE_CREATE | MPI_MODE_WRONLY,
                  MPI_INFO_NULL, &fh);

    MPI_File_set_size(fh, 0);
    MPI_File_seek_shared(fh, 0, MPI_SEEK_SET);
    MPI_File_write_shared(fh, buf, N, MPI_INT, &status);
    MPI_File_close(&fh);

    if (rank == 0) {
        printf("II) MPI_File_write_shared - redosled nije garantovan\n");
        print_file_as_ints("shared.bin", nprocs * N);
    }

    /*
     * III) Neblokirajuci upis sa eksplicitnim pomerajem.
     *
     * iwrite_at pokrene upis i odmah vrati request.
     * Proces moze da radi neki koristan posao.
     * MPI_Wait ceka da se upis stvarno zavrsi.
     */
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

    if (rank == 0) {
        printf("III) MPI_File_iwrite_at + MPI_Wait - neblokirajuci upis\n");
        printf("Pomocna suma da postoji neki rad izmedju iwrite_at i Wait: %d\n",
               suma);
        print_file_as_ints("async.bin", nprocs * N);
    }

    MPI_Finalize();
    return 0;
}

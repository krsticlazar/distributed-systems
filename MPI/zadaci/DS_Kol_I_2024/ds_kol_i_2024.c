#include <stdio.h>
#include <stdlib.h>
#include <time.h>
#include "mpi.h"

#define N 10

int main(int argc, char **argv)
{
    int rank, nprocs, i;
    int buf[N], read_buf[N];
    MPI_File fh;
    MPI_Status status;
    MPI_Offset offset;

    MPI_Init(&argc, &argv);
    MPI_Comm_rank(MPI_COMM_WORLD, &rank);
    MPI_Comm_size(MPI_COMM_WORLD, &nprocs);

    srand((unsigned int)time(NULL) + (unsigned int)(rank * 1009));
    for (i = 0; i < N; i++) {
        buf[i] = rand() % 1000;
    }

    /*
     * Svaki proces koristi svoj pokazivac.
     * Poslednji proces pise od pocetka fajla.
     */
    offset = (MPI_Offset)(nprocs - 1 - rank) * N * sizeof(int);

    MPI_File_open(MPI_COMM_WORLD, "dat.dat",
                  MPI_MODE_CREATE | MPI_MODE_WRONLY,
                  MPI_INFO_NULL, &fh);

    MPI_File_seek(fh, offset, MPI_SEEK_SET);
    MPI_File_write(fh, buf, N, MPI_INT, &status);

    MPI_File_close(&fh);

    /*
     * Zatvaranje, barijera i ponovno otvaranje razdvajaju
     * fazu upisa od faze citanja.
     */
    MPI_Barrier(MPI_COMM_WORLD);

    MPI_File_open(MPI_COMM_WORLD, "dat.dat",
                  MPI_MODE_RDONLY, MPI_INFO_NULL, &fh);

    /*
     * Svi procesi dele isti pokazivac. Svaki cita sledecih N
     * integera, ali se ne zna koji rank dobija koji blok.
     */
    MPI_File_read_shared(fh, read_buf, N, MPI_INT, &status);

    MPI_File_close(&fh);

    printf("Proces %d procitao blok: ", rank);
    for (i = 0; i < N; i++) {
        printf("%d ", read_buf[i]);
    }
    printf("\n");

    MPI_Finalize();
    return 0;
}

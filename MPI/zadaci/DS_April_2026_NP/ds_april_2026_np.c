#include <stdio.h>
#include "mpi.h"

#define FILE_SIZE (1024 * 1024)
#define LOCAL_BYTES (FILE_SIZE / 4)

int main(int argc, char **argv)
{
    int rank, nprocs;
    int local_bytes, half;
    unsigned char buf[LOCAL_BYTES];
    MPI_File fh;
    MPI_Status status;
    MPI_Offset offset, disp;
    MPI_Datatype filetype;

    MPI_Init(&argc, &argv);
    MPI_Comm_rank(MPI_COMM_WORLD, &rank);
    MPI_Comm_size(MPI_COMM_WORLD, &nprocs);

    if (nprocs != 4) {
        if (rank == 0) {
            printf("Program pokrenuti sa tacno 4 procesa.\n");
        }
        MPI_Finalize();
        return 1;
    }

    local_bytes = FILE_SIZE / nprocs;
    half = local_bytes / 2;

    /*
     * Proces 0 cita poslednji blok, proces 1 pretposlednji itd.
     * Koristi se pojedinacni pokazivac.
     */
    offset = (MPI_Offset)(nprocs - 1 - rank) * local_bytes;

    MPI_File_open(MPI_COMM_WORLD, "input.dat",
                  MPI_MODE_RDONLY, MPI_INFO_NULL, &fh);

    MPI_File_seek(fh, offset, MPI_SEEK_SET);
    MPI_File_read(fh, buf, local_bytes, MPI_BYTE, &status);

    MPI_File_close(&fh);

    /*
     * Sema sa slike:
     * [P0-prva][P1-prva][P2-prva][P3-prva]
     * [P0-druga][P1-druga][P2-druga][P3-druga]
     *
     * Jedan proces zato vidi 2 bloka od po half bajtova.
     * Od pocetka prve do pocetka njegove druge polovine
     * ima half * nprocs bajtova.
     */
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
    MPI_Type_free(&filetype);

    if (rank == 0) {
        printf("Kreiran je april.dat.\n");
    }

    MPI_Finalize();
    return 0;
}

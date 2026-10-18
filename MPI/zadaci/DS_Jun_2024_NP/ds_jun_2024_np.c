#include <stdio.h>
#include "mpi.h"

#define LOCAL_N 105
#define NBLOCKS 14

int main(int argc, char **argv)
{
    int rank, nprocs, i, k;
    int buf[LOCAL_N], read_buf[LOCAL_N];
    int blocklengths[NBLOCKS];
    int displs[NBLOCKS];
    MPI_File fh;
    MPI_Status status;
    MPI_Offset offset;
    MPI_Datatype filetype;

    MPI_Init(&argc, &argv);
    MPI_Comm_rank(MPI_COMM_WORLD, &rank);
    MPI_Comm_size(MPI_COMM_WORLD, &nprocs);

    if (nprocs != 3) {
        if (rank == 0) {
            printf("Program pokrenuti sa tacno 3 procesa.\n");
        }
        MPI_Finalize();
        return 1;
    }

    for (i = 0; i < LOCAL_N; i++) {
        buf[i] = rank * 1000 + i;
    }

    /*
     * a) Pojedinacni pokazivac i obrnut redosled procesnih blokova.
     */
    offset = (MPI_Offset)(nprocs - 1 - rank)
             * LOCAL_N * sizeof(int);

    MPI_File_open(MPI_COMM_WORLD, "file1.dat",
                  MPI_MODE_CREATE | MPI_MODE_WRONLY,
                  MPI_INFO_NULL, &fh);

    MPI_File_seek(fh, offset, MPI_SEEK_SET);
    MPI_File_write(fh, buf, LOCAL_N, MPI_INT, &status);

    MPI_File_close(&fh);
    MPI_Barrier(MPI_COMM_WORLD);

    /*
     * b) Citanje istog bloka funkcijom sa eksplicitnim pomerajem.
     */
    MPI_File_open(MPI_COMM_WORLD, "file1.dat",
                  MPI_MODE_RDONLY, MPI_INFO_NULL, &fh);

    MPI_File_read_at(fh, offset, read_buf,
                     LOCAL_N, MPI_INT, &status);

    MPI_File_close(&fh);

    /*
     * c) P0, P1 i P2 prvo upisuju po 1 element, zatim po 2,
     * pa po 3 i tako dalje do 14.
     * Zbir 1 + 2 + ... + 14 je 105, pa se koristi indexed tip.
     * write_all omogucava efikasan kolektivni upis.
     */
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
    MPI_Type_free(&filetype);

    if (rank == 0) {
        printf("Kreirani su file1.dat i file2.dat.\n");
    }

    MPI_Finalize();
    return 0;
}

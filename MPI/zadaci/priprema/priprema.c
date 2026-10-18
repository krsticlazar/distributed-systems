/*
 * Distribuirani sistemi - priprema za MPI I/O lab
 *
 * I)   Svaki proces upisuje 10 integera u file1.bin.
 *      Koristi se pojedinacni pokazivac: MPI_File_seek + MPI_File_write.
 *
 * II)  Svaki proces cita svoje podatke iz file1.bin.
 *      Koristi se eksplicitni pomeraj: MPI_File_read_at.
 *
 * III) Svaki proces upisuje procitane podatke u file2.bin po blokovima od
 *      po 2 integera. Koristi se file view + grupni upis MPI_File_write_all.
 *
 * Pokretanje:
 *   mpirun --oversubscribe -np 4 ./priprema
 */

#include <stdio.h>
#include <stdlib.h>
#include "mpi.h"

#define N 10
#define NW 2

int main(int argc, char **argv)
{
    int rank, nprocs, i;
    int buf[N], rbuf[N];
    int ok;
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
            printf("Primer: mpirun --oversubscribe -np 4 ./priprema\n");
        }

        MPI_Finalize();
        return 0;
    }

    for (i = 0; i < N; i++) {
        buf[i] = rank * N + i;
    }

    /*
     * I) Upis u prvi fajl.
     *
     * Proces 0 upisuje 0-9 od pocetka fajla.
     * Proces 1 upisuje 10-19 sa pomerajem 10 integera.
     * Proces 2 upisuje 20-29 sa pomerajem 20 integera itd.
     */
    MPI_File_open(MPI_COMM_WORLD, "file1.bin",
                  MPI_MODE_CREATE | MPI_MODE_WRONLY,
                  MPI_INFO_NULL, &fh);

    offset = (MPI_Offset)rank * N * sizeof(int);

    MPI_File_seek(fh, offset, MPI_SEEK_SET);
    MPI_File_write(fh, buf, N, MPI_INT, &status);

    MPI_File_close(&fh);

    /*
     * II) Citanje iz prvog fajla.
     *
     * Sada nema MPI_File_seek. Pomeraj se direktno prosledjuje
     * funkciji MPI_File_read_at.
     */
    MPI_File_open(MPI_COMM_WORLD, "file1.bin",
                  MPI_MODE_RDONLY,
                  MPI_INFO_NULL, &fh);

    MPI_File_read_at(fh, offset, rbuf, N, MPI_INT, &status);

    MPI_File_close(&fh);

    ok = 1;
    for (i = 0; i < N; i++) {
        if (rbuf[i] != rank * N + i) {
            ok = 0;
        }
    }

    if (ok) {
        printf("Proces %d: citanje OK [%d ... %d]\n",
               rank, rbuf[0], rbuf[N - 1]);
    } else {
        printf("Proces %d: greska pri citanju\n", rank);
    }

    /*
     * III) Efikasan upis u drugi fajl.
     *
     * Za 4 procesa i NW = 2, zeljeni raspored je:
     * 0 1 | 10 11 | 20 21 | 30 31 | 2 3 | 12 13 | ...
     *
     * Ovo je round-robin pristup po blokovima od po NW integera.
     * Zato filetype opisuje:
     * - N/NW blokova
     * - svaki blok ima NW integera
     * - razmak do sledeceg bloka istog procesa je NW*nprocs
     */
    MPI_Type_vector(N / NW, NW, NW * nprocs, MPI_INT, &filetype);
    MPI_Type_commit(&filetype);

    disp = (MPI_Offset)rank * NW * sizeof(int);

    MPI_File_open(MPI_COMM_WORLD, "file2.bin",
                  MPI_MODE_CREATE | MPI_MODE_WRONLY,
                  MPI_INFO_NULL, &fh);

    MPI_File_set_view(fh, disp, MPI_INT, filetype,
                      "native", MPI_INFO_NULL);

    MPI_File_write_all(fh, rbuf, N, MPI_INT, &status);

    MPI_File_close(&fh);
    MPI_Type_free(&filetype);

    /*
     * Dodatna provera izlaznog fajla.
     * Nije sustina zadatka, samo da odmah vidimo sta je upisano.
     */
    MPI_Barrier(MPI_COMM_WORLD);

    if (rank == 0) {
        int total = N * nprocs;
        int *result = (int *)malloc(total * sizeof(int));
        FILE *fp = fopen("file2.bin", "rb");

        if (fp == NULL) {
            printf("Greska pri otvaranju file2.bin za proveru.\n");
            free(result);
            MPI_Finalize();
            return 0;
        }

        fread(result, sizeof(int), total, fp);
        fclose(fp);

        printf("\nSadrzaj file2.bin:\n");
        for (i = 0; i < total; i++) {
            printf("%d ", result[i]);
        }
        printf("\n");

        free(result);
    }

    MPI_Finalize();
    return 0;
}

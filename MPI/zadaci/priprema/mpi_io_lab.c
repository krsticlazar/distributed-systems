/*
 * Distribuirani sistemi — Lab 3: MPI Parallel I/O
 *
 * I)   Svaki proces upisuje N integera u file1.bin (seek + write)
 * II)  Svaki proces cita upravo upisane podatke iz file1.bin (read_at)
 * III) Podaci se upisuju u file2.bin po interleaved sablonu
 *      — koristeci file view + MPI_File_write_all (najefikasnije)
 *
 * Pokretanje: mpirun -np 4 ./mpi_io_lab
 */

#include <stdio.h>
#include <stdlib.h>
#include "mpi.h"

#define N   10    /* broj integera po procesu            */
#define NW   2    /* velicina bloka u Part III (window)  */

int main(int argc, char** argv)
{
    int rank, nprocs, i;
    MPI_File   fh;
    MPI_Status status;
    MPI_Offset offset;

    MPI_Init(&argc, &argv);
    MPI_Comm_rank(MPI_COMM_WORLD, &rank);
    MPI_Comm_size(MPI_COMM_WORLD, &nprocs);

    /* Inicijalni bafer procesa rank:  {rank*N, rank*N+1, ..., rank*N+N-1} */
    int wbuf[N];
    for (i = 0; i < N; i++)
        wbuf[i] = rank * N + i;

    /* =============================================================
     * I)  Upis u file1.bin — individualni pokazivaci (seek + write)
     *
     *     Raspored u fajlu:
     *       [P0: 0..9] [P1: 10..19] [P2: 20..29] [P3: 30..39]
     * ============================================================= */

    MPI_File_open(MPI_COMM_WORLD, "file1.bin",
                  MPI_MODE_CREATE | MPI_MODE_WRONLY,
                  MPI_INFO_NULL, &fh);

    offset = (MPI_Offset) rank * N * sizeof(int);
    MPI_File_seek(fh, offset, MPI_SEEK_SET);
    MPI_File_write(fh, wbuf, N, MPI_INT, &status);

    MPI_File_close(&fh);

    if (rank == 0)
        printf("[I]  file1.bin napisan  (%d procesa x %d int-a)\n",
               nprocs, N);

    /* =============================================================
     * II) Citanje iz file1.bin — eksplicitni pomeraj (read_at)
     *     Svaki proces cita tacno onaj deo koji je upisao.
     *     Verifikacija: rbuf[i] mora biti == rank*N + i
     * ============================================================= */

    int rbuf[N];

    MPI_File_open(MPI_COMM_WORLD, "file1.bin",
                  MPI_MODE_RDONLY,
                  MPI_INFO_NULL, &fh);

    offset = (MPI_Offset) rank * N * sizeof(int);
    MPI_File_read_at(fh, offset, rbuf, N, MPI_INT, &status);

    MPI_File_close(&fh);

    /* Verifikacija */
    int ok = 1;
    for (i = 0; i < N; i++) {
        if (rbuf[i] != rank * N + i) {
            printf("[II] Proces %d GRESKA  [idx %d]  dobijeno=%d  ocekivano=%d\n",
                   rank, i, rbuf[i], rank * N + i);
            ok = 0;
        }
    }
    if (ok)
        printf("[II] Proces %d: OK  procitano [%d .. %d]\n",
               rank, rbuf[0], rbuf[N - 1]);

    /* =============================================================
     * III) Upis u file2.bin — interleaved sablon, najefikasnije
     *
     *      Ocekivani sadrzaj (NW=2, nprocs=4, N=10):
     *      [P0:0,1][P1:10,11][P2:20,21][P3:30,31]
     *      [P0:2,3][P1:12,13]...
     *      [P0:8,9][P1:18,19][P2:28,29][P3:38,39]
     *
     *      Resenje:
     *        filetype = vector(N/NW blokova, duz NW, stride NW*nprocs)
     *        disp     = rank * NW * sizeof(int)
     *        -> MPI_File_set_view + MPI_File_write_all  (Level 3)
     * ============================================================= */

    MPI_Datatype filetype;
    MPI_Type_vector(
        N / NW,          /* count     — koliko blokova                  */
        NW,              /* blocklen  — duzina bloka u int-ovima         */
        NW * nprocs,     /* stride    — rastojanje izmedju blok-pocetaka */
        MPI_INT,
        &filetype
    );
    MPI_Type_commit(&filetype);

    MPI_Offset disp = (MPI_Offset) rank * NW * sizeof(int);

    MPI_File_open(MPI_COMM_WORLD, "file2.bin",
                  MPI_MODE_CREATE | MPI_MODE_WRONLY,
                  MPI_INFO_NULL, &fh);

    MPI_File_set_view(fh, disp, MPI_INT, filetype, "native", MPI_INFO_NULL);
    MPI_File_write_all(fh, rbuf, N, MPI_INT, &status);

    MPI_File_close(&fh);
    MPI_Type_free(&filetype);

    /* Verifikacija iz procesa 0 — obican fread jer je fajl vec zatvoren */
    if (rank == 0) {
        int total = N * nprocs;
        int* result = (int*) malloc(total * sizeof(int));

        FILE* fp = fopen("file2.bin", "rb");
        if (!fp) {
            fprintf(stderr, "[III] Greska pri otvaranju file2.bin!\n");
            free(result);
            MPI_Abort(MPI_COMM_WORLD, 1);
        }
        size_t n_read = fread(result, sizeof(int), total, fp);
        if ((int)n_read != total)
            fprintf(stderr, "[III] Upozorenje: procitano %zu od %d int-a\n",
                    n_read, total);
        fclose(fp);

        printf("\n[III] file2.bin  (%d int-a):\n", total);
        for (i = 0; i < total; i++)
            printf("%d ", result[i]);
        printf("\n");

        free(result);
    }

    MPI_Finalize();
    return 0;
}

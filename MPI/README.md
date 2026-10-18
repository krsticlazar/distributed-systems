# Distributed Systems - MPI Practice Projects

This directory contains Message Passing Interface (MPI) lab tasks, parallel I/O implementations, and exam problem solutions for the **Distributed Systems** course at the **Faculty of Electronic Engineering, University of Niš** (Elektronski fakultet u Nišu).

The programs are written in **C** using standard MPI-1 and MPI-2 (Parallel I/O) specifications, compatible with MS-MPI on Windows and OpenMPI/MPICH on Linux.

---

## 🚀 Key Concepts & Demonstrations

- **Point-to-Point Communication**:
  - Blocking: `MPI_Send`, `MPI_Recv`, `MPI_Sendrecv`.
  - Non-blocking: `MPI_Isend`, `MPI_Irecv`, `MPI_Wait`, `MPI_Test`.
- **Collective Operations**:
  - Broadcast: `MPI_Bcast`.
  - Scattering and Gathering: `MPI_Scatter`, `MPI_Gather`, `MPI_Scatterv`, `MPI_Gatherv`.
  - Reductions: `MPI_Reduce`, `MPI_Allreduce`.
- **Derived Datatypes**:
  - Contiguous: `MPI_Type_contiguous`.
  - Vector / Strided: `MPI_Type_vector`.
  - Indexed: `MPI_Type_indexed`.
  - Subarray (multi-dimensional grids): `MPI_Type_create_subarray`.
  - Structures: `MPI_Type_create_struct`.
- **MPI-2 Parallel I/O**:
  - File management: `MPI_File_open`, `MPI_File_close`, `MPI_File_set_size`.
  - File Views: `MPI_File_set_view` using displacements, elementary types (`etype`), and file types (`filetype`).
  - Collective & Individual Access: `MPI_File_read_all`, `MPI_File_write_all`, `MPI_File_write_at_all`, `MPI_File_read_ordered`.
  - Shared and individual file pointers.

---

## 📁 Tasks Overview (`zadaci/`)

| Folder | Description | Focus Area |
|---|---|---|
| [`DS_April_2026_NP/`](file:///D:/ELFAK/IV%20godina/Distribuirani%20sistemi/Racunske/MPI/zadaci/DS_April_2026_NP) | Solved April 2026 exam problem: partitioned parallel array writing | Parallel I/O, file view, multi-phase offsets |
| [`DS_Jun_2024_NP/`](file:///D:/ELFAK/IV%20godina/Distribuirani%20sistemi/Racunske/MPI/zadaci/DS_Jun_2024_NP) | Solved June 2024 exam problem: triangular / step-wise data distribution | Custom file types, round-robin chunk writing |
| [`DS_Kol_I_2024/`](file:///D:/ELFAK/IV%20godina/Distribuirani%20sistemi/Racunske/MPI/zadaci/DS_Kol_I_2024) | Solved Colloquium 1 2024 exam problem | Process coordination, collective routines |
| [`priprema/`](file:///D:/ELFAK/IV%20godina/Distribuirani%20sistemi/Racunske/MPI/zadaci/priprema) | Introductory lab exercises and fundamental file operations | Basic file creation, contiguous views |
| [`shared_neblokirajuci/`](file:///D:/ELFAK/IV%20godina/Distribuirani%20sistemi/Racunske/MPI/zadaci/shared_neblokirajuci) | Asynchronous parallel I/O and shared file pointer mechanics | Non-blocking file reads/writes, concurrency |
| [`subarray_matrica/`](file:///D:/ELFAK/IV%20godina/Distribuirani%20sistemi/Racunske/MPI/zadaci/subarray_matrica) | 2D matrix file decomposition among processes | `MPI_Type_create_subarray`, submatrix writing |

---

## 📚 Study Materials & Scripts

- [`ULTIMATE_MPI.md`](file:///D:/ELFAK/IV%20godina/Distribuirani%20sistemi/Racunske/MPI/ULTIMATE_MPI.md): Exhaustive exam preparation guide in Serbian covering all MPI functions, offset calculations, file view mechanics, and fully explained solutions to exam tasks.
- [`Materijal/`](file:///D:/ELFAK/IV%20godina/Distribuirani%20sistemi/Racunske/MPI/Materijal): Lecture slides, notes (`beleske_full.md`), and quick function syntax references (`MPI_DS_funkcije.md`).

---

## 🛠️ How to Compile and Run

### On Windows (MS-MPI with Visual Studio or GCC/Clang)
```powershell
# Compile with MS-MPI headers and libraries
cl.exe /MD /I "%MSMPI_INC%" main.c /link /LIBPATH:"%MSMPI_LIB64%" msmpi.lib

# Run with mpiexec (e.g., across 4 processes)
mpiexec -n 4 main.exe
```

### On Linux (OpenMPI / MPICH)
```bash
# Compile
mpicc -O2 -o main main.c

# Run
mpirun -np 4 ./main
```

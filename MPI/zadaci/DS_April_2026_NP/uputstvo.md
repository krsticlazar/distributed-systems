# DS April 2026 NP

Otvori Windows Terminal i izaberi Ubuntu profil. Ako si u PowerShell profilu, prvo pokreni:

```powershell
wsl -d Ubuntu
```

U Ubuntu terminalu predji u folder zadatka, napravi ulaz od tacno 1 MiB, kompajliraj i pokreni:

```bash
cd ~/distribuirani-mpi/zadaci/DS_April_2026_NP
dd if=/dev/urandom of=input.dat bs=1M count=1
mpicc ds_april_2026_np.c -o ds_april_2026_np
mpirun --oversubscribe -np 4 ./ds_april_2026_np
```

Ako si folder smestio na drugo mesto, promeni putanju u `cd` komandi. Program pravi `april.dat` i pokrece se sa tacno 4 procesa.

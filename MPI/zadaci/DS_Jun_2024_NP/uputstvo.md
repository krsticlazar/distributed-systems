# DS Jun 2024 NP

Otvori Windows Terminal i izaberi Ubuntu profil. Ako si u PowerShell profilu, prvo pokreni:

```powershell
wsl -d Ubuntu
```

U Ubuntu terminalu predji u folder zadatka, kompajliraj i pokreni:

```bash
cd ~/distribuirani-mpi/zadaci/DS_Jun_2024_NP
mpicc ds_jun_2024_np.c -o ds_jun_2024_np
mpirun --oversubscribe -np 3 ./ds_jun_2024_np
```

Ako si folder smestio na drugo mesto, promeni putanju u `cd` komandi. Program pravi `file1.dat` i `file2.dat` i pokrece se sa tacno 3 procesa.

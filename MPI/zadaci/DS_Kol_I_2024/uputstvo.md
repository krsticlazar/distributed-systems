# DS Kol I 2024

Otvori Windows Terminal i izaberi Ubuntu profil. Ako si u PowerShell profilu, prvo pokreni:

```powershell
wsl -d Ubuntu
```

U Ubuntu terminalu predji u folder zadatka, kompajliraj i pokreni:

```bash
cd ~/distribuirani-mpi/zadaci/DS_Kol_I_2024
mpicc ds_kol_i_2024.c -o ds_kol_i_2024
mpirun --oversubscribe -np 4 ./ds_kol_i_2024
```

Ako si folder smestio na drugo mesto, promeni putanju u `cd` komandi. Program pravi `dat.dat`. Redosled kojim procesi stampaju procitane blokove nije garantovan.

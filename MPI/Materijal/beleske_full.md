# MPI Parallel I/O - beleske za Termin 1

Ove beleske prate prezentaciju `MPI Parallel IO Termin 1.md` redom po slajdovima. U tekstu su spojeni: ono sto pise na slajdovima, tvoje beleske sa termina i dodatno objasnjenje predznanja koje je potrebno da bi drugi termin i lab bili laksi.

## Brzi preduslovi pre slajdova

### 1. Sta je MPI proces

U MPI programu obicno pokrenes isti program vise puta, kao vise procesa. Svaki proces izvrsava isti kod, ali ima svoj `rank`.

```c
MPI_Comm_rank(MPI_COMM_WORLD, &rank);
MPI_Comm_size(MPI_COMM_WORLD, &nprocs);
```

`rank` je redni broj procesa u komunikatoru. Ako ima `nprocs` procesa, rangovi su `0, 1, 2, ..., nprocs - 1`. `MPI_COMM_WORLD` je komunikator koji obuhvata sve procese pokrenute u programu.

Kada kazemo "proces 0 cita prvi deo fajla, proces 1 drugi deo", to se u kodu najcesce pretvara u formulu koja koristi `rank`, npr:

```c
offset = rank * velicina_dela;
```

### 2. Binarni fajl i offset

MPI I/O u ovoj prezentaciji radi sa binarnim, neformatiranim fajlovima. To znaci da se podaci ne cuvaju kao tekst `"123"`, nego kao bajtovi koji predstavljaju vrednost broja u memoriji.

Vazno: u fajlu se pozicije mere bajtovima, osim kada se koristi `file view`, gde neki pomeraji mogu biti u jedinicama `etype`. U primerima pre `MPI_File_set_view`, prakticno racunas u bajtovima.

Najcesca greska:

```c
MPI_File_seek(fh, rank * nints, MPI_SEEK_SET); // pogresno ako je nints broj intova
```

Treba:

```c
MPI_File_seek(fh, rank * bufsize, MPI_SEEK_SET); // bufsize je broj bajtova
```

Ako hoces da preskocis `k` integera, pomeraj je:

```c
k * sizeof(int)
```

### 3. Razlika izmedju offset-a i count-a

Ovo je centralno za ispit:

```c
MPI_File_read(fh, buf, count, datatype, &status);
```

`count` nije broj bajtova, nego broj elemenata tipa `datatype`.

Primer:

```c
MPI_File_read(fh, buf, 10, MPI_INT, &status);
```

Ovo cita 10 integera, tj. uobicajeno 40 bajtova ako je `sizeof(int) == 4`.

Sa druge strane, `MPI_File_seek` u osnovnim primerima trazi pomeraj u bajtovima:

```c
MPI_File_seek(fh, 10 * sizeof(int), MPI_SEEK_SET);
```

Ovo pozicionira pokazivac posle prvih 10 integera.

### 4. Izvedeni tipovi koji se koriste u prezentaciji

Izvedeni MPI tip opisuje raspored podataka. Ne opisuje samo "koji je tip", nego i "koliko elemenata, na kojim razmacima".

Najbitnije funkcije:

```c
MPI_Type_contiguous(count, oldtype, &newtype);
```

Pravi novi tip od `count` uzastopnih elemenata.

```c
MPI_Type_vector(count, blocklength, stride, oldtype, &newtype);
```

Pravi tip od `count` blokova. Svaki blok ima `blocklength` elemenata, a pocetak sledeceg bloka je udaljen `stride` elemenata od pocetka prethodnog bloka.

```c
MPI_Type_create_resized(oldtype, lb, extent, &newtype);
```

Menja "ukupnu sirinu" tipa, tj. `extent`. Ovo je posebno bitno kod `file view`, jer se `filetype` ponavlja kroz fajl. Ako zelis obrazac "2 integera vidljiva, zatim 4 integera praznina", ukupna sirina obrasca mora biti 6 integera.

Posle kreiranja izvedenog tipa mora:

```c
MPI_Type_commit(&newtype);
```

Bez `commit`, MPI ne sme da koristi taj tip u komunikaciji ili I/O operaciji.

---

## Slajd 1 - MPI PARALLEL I/O

Tema je paralelni ulaz/izlaz u MPI-ju, tj. kako vise MPI procesa cita i pise fajlove. Ovo je prirodan nastavak distribuiranih sistema, jer imamo vise procesa koji rade zajedno, a fajl je zajednicki resurs.

Veza sa tvojim beleskama: ova oblast je spoj operativnih sistema i paralelnih/distribuiranih sistema. Iz OS-a znas ideje kao sto su procesi, fajlovi, pokazivaci u fajlu, `fork`, `pipe`; ovde te ideje koristis u programu gde vise MPI procesa radi nad istim fajlom.

Glavna ideja termina: umesto da jedan proces radi sav I/O ili da svaki proces pravi svoj fajl, MPI omogucava koordinisan pristup jednom zajednickom fajlu.

Za lab i ispit najvaznije ce biti:

- osnovne MPI I/O funkcije: `open`, `seek`, `read`, `write`, `close`
- funkcije sa eksplicitnim pomerajem: `read_at`, `write_at`
- `MPI_File_set_view`
- izvedeni tipovi za nekontinualni pristup fajlu

---

## Slajd 2 - Nekad i sad

Prvi tradicionalni pristup: svi procesi salju podatke jednom procesu, a taj jedan proces sakuplja sve podatke i upisuje ih u fajl.

To je logicki slicno operaciji `gather`: podaci se skupljaju na jednom mestu. Proces 0, na primer, primi podatke od procesa 1, 2, 3, itd, pa onda on sam pise u fajl.

Prednost ovog pristupa je jednostavnost. Lako je programirati i lako je razumeti: samo jedan proces dira fajl, pa nema problema oko toga ko gde pise.

Mana je sto nema pravog I/O paralelizma. Iako racunanje mozda rade svi procesi, upis u fajl radi samo jedan. Time jedan proces postaje usko grlo.

Tvoja beleska o latenciji je bitna: ako mnogo procesa brzo zavrsi, a jedan proces kasni, centralni proces i dalje mora da ceka kompletne podatke. U praksi vreme zavisi od najsporijeg dela sistema, plus od dodatnog vremena za slanje svih podataka centralnom procesu.

Zasto nije skalabilno: ako povecas broj procesa, povecavas i kolicinu podataka koju jedan proces mora da primi i upise. Umesto da se opterecenje raspodeli, ono se gomila na jednom procesu.

Za ispit: kada vidis "svi salju jednom procesu, pa jedan pise", odgovor je da je jednostavno, ali nema paralelizma i centralni proces je bottleneck.

---

## Slajd 3 - Nekad i sad 2

Drugi tradicionalni pristup: svaki proces pise u svoj poseban fajl.

Primer:

- proces 0 pise `out0.bin`
- proces 1 pise `out1.bin`
- proces 2 pise `out2.bin`
- proces 3 pise `out3.bin`

Prednost: postoji paralelan upis. Svaki proces moze da pise nezavisno, bez cekanja da jedan centralni proces sve sakupi.

Mana: dobija se mnogo manjih fajlova. To je neprakticno za kasniju obradu, citanje, arhiviranje i proveru rezultata. Ako imas hiljade procesa, imaces hiljade fajlova.

Ovaj pristup takodje ne zahteva MPI I/O. Svaki proces moze obicnim C funkcijama (`fopen`, `fwrite`, `fclose`) da otvori svoj fajl i pise u njega.

Za lab: ovaj pristup se obicno ne trazi kao konacno resenje, jer cilj MPI I/O teme jeste da vise procesa radi nad istim fajlom.

---

## Slajd 4 - Nekad i sad 3

Moderan, kooperativni pristup: vise procesa pristupa istom fajlu.

To je ono sto MPI I/O uvodi kao glavni alat. Procesi ne moraju da salju sve jednom procesu i ne moraju da prave odvojene fajlove. Umesto toga, svaki proces moze da pise svoj deo zajednickog fajla.

Ključno pitanje je koordinacija. Ako dva procesa pisu na isto mesto u fajlu, rezultat je neodredjen ili pogresan. Zato svaki proces mora da zna gde je njegov deo fajla.

U osnovnim primerima to se radi preko formule:

```c
offset = rank * velicina_bloka_u_bajtovima;
```

Tvoja beleska kaze da "svaki proces ima svoj offset i ne zavisi od drugih procesa". To je sustina za osnovne primere. Svaki proces racuna svoju poziciju iz `rank`-a i velicine bloka.

Preciznije: MPI fajl moze imati individualne file pointere za procese, a postoje i funkcije sa zajednickim file pointerom. U ovom terminu uglavnom radis sa individualnim pokazivacem ili eksplicitnim offset-om, sto je lakse za kontrolu.

Za ispit: moderan pristup je skalabilniji jer se I/O moze rasporediti po procesima, ali zahteva pravilno racunanje pozicija i/ili `file view`.

---

## Slajd 5 - Podsetnik

Slajd podseca na standardni File I/O u C-u.

Otvaranje i zatvaranje fajla:

```c
FILE *fp = fopen("file_name", "mode");
fclose(fp);
```

Tekstualni fajlovi koriste funkcije kao:

```c
fprintf(fp, "%d", x);
fscanf(fp, "%d", &x);
fgets(str, n, fp);
fputs(str, fp);
```

Kod tekstualnih fajlova broj `123` se cuva kao karakteri `'1'`, `'2'`, `'3'`. To je citljivo coveku, ali nije najefikasnije za masivni I/O.

Binarni fajlovi koriste:

```c
fread(ptr, size, nmemb, fp);
fwrite(ptr, size, nmemb, fp);
fseek(fp, offset, whence);
```

Kod binarnih fajlova vrednost se cuva u obliku koji je blizi memorijskoj reprezentaciji. Racunaru je to brze i prirodnije za velike kolicine podataka.

MPI I/O funkcije iz ove prezentacije odnose se na neformatirane binarne fajlove. Zato je bitno da razumes `fread`, `fwrite` i `fseek`, jer MPI funkcije imaju slicnu logiku, samo rade u paralelnom okruzenju.

Za lab: kada zadatak kaze "upisuje cele brojeve u datoteku", u ovoj temi se uglavnom misli na binarni upis `MPI_INT`, ne na tekstualni `fprintf`.

---

## Slajd 6 - Osnovne MPI I/O funkcije

Osnovne funkcije lice na C funkcije za fajlove:

```c
MPI_File_open(...)
MPI_File_seek(...)
MPI_File_read(...)
MPI_File_write(...)
MPI_File_close(...)
```

Razlike u odnosu na C:

- fajl se predstavlja tipom `MPI_File`, ne `FILE *`
- otvaranje ukljucuje komunikator, npr. `MPI_COMM_WORLD`
- tip podataka je `MPI_Datatype`, npr. `MPI_INT`
- funkcije vracaju `int` error code
- status je `MPI_Status`, slicno kao kod `MPI_Recv`

Povratna vrednost nije "rezultat citanja", nego status greske. Ako je sve u redu, povratna vrednost je `MPI_SUCCESS`.

`MPI_Info` sluzi za dodatne savete implementaciji, tzv. hints. U nasim zadacima se koristi:

```c
MPI_INFO_NULL
```

To znaci da ne prosledjujemo posebne optimizacione parametre.

Najbitnije za pamcenje:

```c
MPI_File_open(MPI_COMM_WORLD, "filename", mode, MPI_INFO_NULL, &fh);
MPI_File_seek(fh, offset, MPI_SEEK_SET);
MPI_File_read(fh, buf, count, MPI_INT, &status);
MPI_File_write(fh, buf, count, MPI_INT, &status);
MPI_File_close(&fh);
```

---

## Slajd 7 - Otvaranje i zatvaranje fajla

Funkcija:

```c
int MPI_File_open(
    MPI_Comm comm,
    const char *filename,
    int amode,
    MPI_Info info,
    MPI_File *fh
);
```

`comm` govori koja grupa procesa kolektivno otvara fajl. Ako stavis `MPI_COMM_WORLD`, svi procesi iz programa ucestvuju u otvaranju.

`filename` je ime fajla.

`amode` je mod otvaranja:

- `MPI_MODE_RDONLY` - samo citanje
- `MPI_MODE_WRONLY` - samo pisanje
- `MPI_MODE_RDWR` - citanje i pisanje
- `MPI_MODE_CREATE` - napravi fajl ako ne postoji

Modovi se kombinuju bitovskim OR operatorom:

```c
MPI_MODE_CREATE | MPI_MODE_WRONLY
```

To znaci: ako fajl ne postoji, kreiraj ga, i otvori ga za pisanje.

`info` je za hints, kod nas skoro uvek:

```c
MPI_INFO_NULL
```

`fh` je file handle, tj. MPI pokazivac na otvoreni fajl.

Zatvaranje:

```c
MPI_File_close(&fh);
```

Bitna razlika u odnosu na obicni C je sto je `MPI_File_open` kolektivna funkcija nad komunikatorom. Ako svi procesi iz `MPI_COMM_WORLD` treba da ucestvuju, svi moraju da dodju do tog poziva.

Za ispit: ako zadatak trazi upis u novi fajl, obicno ti treba:

```c
MPI_MODE_CREATE | MPI_MODE_WRONLY
```

Ako treba i citanje i pisanje:

```c
MPI_MODE_CREATE | MPI_MODE_RDWR
```

---

## Slajd 8 - Upis i citanje

Citanje:

```c
MPI_File_read(fh, buf, count, datatype, &status);
```

Upis:

```c
MPI_File_write(fh, buf, count, datatype, &status);
```

Parametri:

- `fh` je otvoreni fajl
- `buf` je memorijski bafer
- `count` je broj elemenata
- `datatype` je tip jednog elementa, npr. `MPI_INT`
- `status` cuva informacije o operaciji

Ako pises:

```c
MPI_File_write(fh, buf, 10, MPI_INT, &status);
```

to znaci: iz memorije uzmi 10 integera iz `buf` i upisi ih u fajl na trenutnoj poziciji file pointera.

Ako citas:

```c
MPI_File_read(fh, buf, 10, MPI_INT, &status);
```

to znaci: procitaj 10 integera iz fajla sa trenutne pozicije i upisi ih u memoriju u `buf`.

Ove funkcije koriste individualni file pointer procesa. Zato se pre njih cesto poziva:

```c
MPI_File_seek(...)
```

Bez pravilnog pozicioniranja, vise procesa moze pokusati da cita ili pise pogresan deo fajla.

---

## Slajd 9 - Pozicioniranje

Funkcija:

```c
MPI_File_seek(MPI_File fh, MPI_Offset offset, int whence);
```

`offset` je pomeraj. U osnovnim primerima, pre `MPI_File_set_view`, racunas ga u bajtovima jer je podrazumevani pogled na fajl bajtovski.

`whence` govori odakle se meri pomeraj:

- `MPI_SEEK_SET` - od pocetka fajla
- `MPI_SEEK_CUR` - od trenutne pozicije pokazivaca
- `MPI_SEEK_END` - od kraja fajla

Primer:

```c
MPI_File_seek(fh, 0, MPI_SEEK_SET);
```

Postavlja pokazivac na pocetak fajla.

Primer:

```c
MPI_File_seek(fh, rank * bufsize, MPI_SEEK_SET);
```

Svaki proces se pozicionira na svoj deo fajla. Ako je `bufsize` velicina dela koji jedan proces obradjuje u bajtovima:

- proces 0 ide na `0 * bufsize`
- proces 1 ide na `1 * bufsize`
- proces 2 ide na `2 * bufsize`

Najvaznija ispitna greska: ne mesati broj elemenata i broj bajtova.

Ako jedan proces obradjuje 100 integera, pomeraj procesa `rank` je:

```c
rank * 100 * sizeof(int)
```

a ne:

```c
rank * 100
```

---

## Slajd 10 - Primer 1

Zadatak: postoji jedan zajednicki fajl velicine `FILESIZE`. Ukupno ima `N` procesa. Svaki proces cita svoj `1/N` deo fajla.

Raspored je kontinualan:

- proces 0 cita prvi blok
- proces 1 cita drugi blok
- proces 2 cita treci blok
- proces `nprocs - 1` cita poslednji blok

Ako je fajl velicine 1 MB i imamo 4 procesa:

- proces 0 cita bajtove od 0 do 262143
- proces 1 cita sledecih 262144 bajta
- proces 2 sledeci blok
- proces 3 poslednji blok

Formula:

```c
bufsize = FILESIZE / nprocs;
offset = rank * bufsize;
```

`bufsize` je velicina dela u bajtovima.

Ako se cita kao `MPI_INT`, broj integera koji stane u taj blok je:

```c
nints = bufsize / sizeof(int);
```

Za ispit: ovaj primer je osnova za sve zadatke gde procesi obradjuju jednake, kontinualne delove fajla.

---

## Slajd 11 - Primer 1 - resenje

Kod:

```c
#include "mpi.h"
#define FILESIZE (1024*1024)

int main(int argc, char** argv)
{
    int* buf, rank, nprocs, nints, bufsize;
    MPI_File fh;
    MPI_Status status;

    MPI_Init(&argc, &argv);
    MPI_Comm_rank(MPI_COMM_WORLD, &rank);
    MPI_Comm_size(MPI_COMM_WORLD, &nprocs);

    bufsize = FILESIZE / nprocs;
    buf = (int*) malloc(bufsize);
    nints = bufsize / sizeof(int);

    MPI_File_open(MPI_COMM_WORLD, "filename", MPI_MODE_RDONLY, MPI_INFO_NULL, &fh);
    MPI_File_seek(fh, rank * bufsize, MPI_SEEK_SET);
    MPI_File_read(fh, buf, nints, MPI_INT, &status);
    MPI_File_close(&fh);

    MPI_Finalize();
    return 0;
}
```

Objasnjenje redom:

`MPI_Init` pokrece MPI okruzenje. Pre toga ne treba koristiti MPI funkcije.

`MPI_Comm_rank` daje koji je trenutni proces.

`MPI_Comm_size` daje koliko ukupno procesa ucestvuje.

```c
bufsize = FILESIZE / nprocs;
```

racuna koliko bajtova pripada jednom procesu.

```c
buf = (int*) malloc(bufsize);
```

alocira toliko bajtova memorije. Iako je `buf` tipa `int*`, `malloc` prima broj bajtova, pa je ovde `bufsize` ispravan.

```c
nints = bufsize / sizeof(int);
```

racuna koliko integera citamo iz tog bajtovskog bloka.

```c
MPI_File_seek(fh, rank * bufsize, MPI_SEEK_SET);
```

postavlja fajl pokazivac na pocetak bloka koji pripada ovom procesu.

```c
MPI_File_read(fh, buf, nints, MPI_INT, &status);
```

cita `nints` integera u bafer.

Prakticna napomena: kod sa slajda pretpostavlja da je `FILESIZE` deljiv sa `nprocs` i da je `bufsize` deljiv sa `sizeof(int)`. U ispitnim zadacima se obicno podrazumeva da je podela uredna.

---

## Slajd 12 - Primer 1 + Upis u fajl

Isti obrazac moze da se koristi za upis. Razlika je:

- umesto `MPI_File_read` koristi se `MPI_File_write`
- fajl se otvara za pisanje
- po potrebi se dodaje `MPI_MODE_CREATE`

Primer otvaranja:

```c
MPI_File_open(
    MPI_COMM_WORLD,
    "/pfs/datafile",
    MPI_MODE_CREATE | MPI_MODE_WRONLY,
    MPI_INFO_NULL,
    &fh
);
```

Zatim:

```c
MPI_File_seek(fh, rank * bufsize, MPI_SEEK_SET);
MPI_File_write(fh, buf, nints, MPI_INT, &status);
```

Logika je ista kao kod citanja: svaki proces prvo ode na svoj deo fajla, a zatim upise svoj deo memorijskog bafera.

Za lab: prvi korak iz `priprema lab.md` jako lici na ovo. Ako svaki proces upisuje 10 celih brojeva, pomeraj procesa `rank` je:

```c
rank * 10 * sizeof(int)
```

a upis je:

```c
MPI_File_write(fh, buf, 10, MPI_INT, &status);
```

---

## Slajd 13 - Primer 1 + upis u fajl - resenje

Kod:

```c
#include "mpi.h"
#define FILESIZE (1024*1024)

int main(int argc, char** argv)
{
    int* buf, rank, nprocs, nints, bufsize;
    MPI_File fh;
    MPI_Status status;

    MPI_Init(&argc, &argv);
    MPI_Comm_rank(MPI_COMM_WORLD, &rank);
    MPI_Comm_size(MPI_COMM_WORLD, &nprocs);

    bufsize = FILESIZE / nprocs;
    buf = (int*) malloc(bufsize);
    nints = bufsize / sizeof(int);

    MPI_File_open(MPI_COMM_WORLD, "filename",
                  MPI_MODE_CREATE | MPI_MODE_WRONLY,
                  MPI_INFO_NULL, &fh);

    MPI_File_seek(fh, rank * bufsize, MPI_SEEK_SET);
    MPI_File_write(fh, buf, nints, MPI_INT, &status);
    MPI_File_close(&fh);

    MPI_Finalize();
    return 0;
}
```

Najvazniji deo:

```c
MPI_File_seek(fh, rank * bufsize, MPI_SEEK_SET);
MPI_File_write(fh, buf, nints, MPI_INT, &status);
```

`seek` koristi bajtovski pomeraj, a `write` koristi broj integera.

Ako bi greskom napisao:

```c
MPI_File_seek(fh, rank * nints, MPI_SEEK_SET);
```

onda bi procesi bili previse blizu jedan drugom u fajlu, jer `nints` nije broj bajtova. To bi dovelo do preklapanja upisa.

Za proveru razumevanja:

Ako svaki proces pise 10 integera i `sizeof(int) = 4`, onda svaki proces pise 40 bajtova.

- rank 0 pocinje od bajta 0
- rank 1 pocinje od bajta 40
- rank 2 pocinje od bajta 80
- rank 3 pocinje od bajta 120

---

## Slajd 14 - MPI I/O funkcije - Nastavak

Pet osnovnih funkcija mogu da rese mnogo zadataka:

- otvori fajl
- pozicioniraj se
- citaj ili pisi
- zatvori fajl

Ali nisu uvek najefikasnije i nisu uvek najlepse za slozene rasporede podataka.

Problem nastaje kada podaci u fajlu nisu jedan kontinualan blok po procesu. Na primer:

- proces 0 treba da cita blokove 0, 4, 8, 12
- proces 1 treba da cita blokove 1, 5, 9, 13
- proces 2 treba da cita blokove 2, 6, 10, 14

To se moze resiti sa mnogo `seek + read` poziva, ali je sporo i ruzno.

Zato MPI uvodi naprednije funkcije:

- funkcije sa eksplicitnim pomerajem: `MPI_File_read_at`, `MPI_File_write_at`
- kolektivne I/O operacije: npr. `MPI_File_read_all`, `MPI_File_write_all`
- nekontinualni pristup preko `file view`

Za drugi termin ocekuj da se nastavi upravo na ove ideje: efikasniji upis/citanje i grupne operacije.

---

## Slajd 15 - Upotreba eksplicitnog pomeraja

Do sada je obrazac bio:

```c
MPI_File_seek(fh, offset, MPI_SEEK_SET);
MPI_File_read(fh, buf, count, MPI_INT, &status);
```

ili:

```c
MPI_File_seek(fh, offset, MPI_SEEK_SET);
MPI_File_write(fh, buf, count, MPI_INT, &status);
```

To koristi individualni file pointer procesa. `seek` pomera taj pokazivac, a `read/write` radi od trenutne pozicije.

Funkcije sa eksplicitnim pomerajem spajaju te dve ideje:

```c
MPI_File_read_at(fh, offset, buf, count, datatype, &status);
MPI_File_write_at(fh, offset, buf, count, datatype, &status);
```

Ovde direktno kazes odakle se cita ili gde se pise. Ne treba prethodni `MPI_File_seek`.

Veza sa tvojom beleskom: na ispitu treba razlikovati:

- funkcije sa pojedinacnim/individualnim pokazivacem: bez `_at`
- funkcije sa eksplicitnim pomerajem: sa `_at`

Bez `_at`:

```c
MPI_File_seek(...);
MPI_File_write(...);
```

Sa `_at`:

```c
MPI_File_write_at(...);
```

Bitno: eksplicitni offset ne zavisi od trenutne pozicije individualnog pokazivaca. To smanjuje sansu da pogresis redosled `seek` poziva.

---

## Slajd 16 - Upotreba eksplicitnog pomeraja - primer

Primer:

```c
offset = 12;
MPI_File_write_at(fh, offset, buf, 2, MPI_INT, &status);
```

U osnovnom podrazumevanom pogledu na fajl ovo znaci: preskoci prvih 12 bajtova i odatle upisi 2 integera iz `buf`.

Ako je `sizeof(int) = 4`, 2 integera zauzimaju 8 bajtova. Upis ce zahvatiti bajtove od 12 do 19.

Ako bi zadatak rekao "preskoci prvih 12 integera", onda offset nije `12`, nego:

```c
12 * sizeof(int)
```

Ovo je veoma cesta zamka, jer tekst zadatka moze da govori u elementima, dok funkcija trazi pomeraj kroz fajl.

Kada vidis:

```c
MPI_File_write_at(fh, offset, buf, count, MPI_INT, &status);
```

razdvoji u glavi:

- `offset` govori gde u fajlu pocinje operacija
- `count` govori koliko `MPI_INT` elemenata se upisuje
- `buf` je memorija iz koje se pise

---

## Slajd 17 - Primer 1 - resenje + explicit-offset

Kod iz prvog primera moze se pojednostaviti:

```c
MPI_File_open(MPI_COMM_WORLD, "filename", MPI_MODE_RDONLY,
              MPI_INFO_NULL, &fh);

MPI_File_read_at(fh, rank * bufsize, buf, nints, MPI_INT, &status);

MPI_File_close(&fh);
```

Vise nema:

```c
MPI_File_seek(...)
```

jer je offset direktno u `MPI_File_read_at`.

Uporedno:

```c
MPI_File_seek(fh, rank * bufsize, MPI_SEEK_SET);
MPI_File_read(fh, buf, nints, MPI_INT, &status);
```

je ekvivalentno sa:

```c
MPI_File_read_at(fh, rank * bufsize, buf, nints, MPI_INT, &status);
```

u ovom jednostavnom primeru.

Za lab: drugi korak iz pripreme kaze da se ponovo otvori datoteka i da svaki proces cita svoje podatke koristeci funkcije sa eksplicitnim pomerajem. To znaci da treba nesto ovog oblika:

```c
MPI_File_read_at(fh, rank * 10 * sizeof(int), buf, 10, MPI_INT, &status);
```

Ako nakon citanja proveravas ispravnost, proces `rank` treba da ocekuje vrednosti:

```c
rank * 10, rank * 10 + 1, ..., rank * 10 + 9
```

ako je prvi proces pisao 0-9, drugi 10-19 itd.

---

## Slajd 18 - Nekontinualni pristup i grupne operacije

Ovo je prelazni slajd ka glavnoj temi za ispit: `file view`.

Do sada su procesi citali ili pisali kontinualne blokove:

```text
P0 P0 P0 | P1 P1 P1 | P2 P2 P2 | P3 P3 P3
```

Ali mnoge aplikacije imaju raspored tipa:

```text
P0 P1 P2 P0 P1 P2
```

Proces 0 ne zeli sve podatke od pocetka do nekog kraja, nego zeli vise manjih delova koji su razbacani kroz fajl.

Nekontinualni pristup znaci: podaci koji pripadaju jednom procesu nisu spojeni u fajlu.

Grupne operacije znace: vise procesa zajedno poziva I/O operaciju, pa MPI moze efikasnije da organizuje citanje/upis.

Za drugi termin: ocekuj funkcije kao `MPI_File_read_all` i `MPI_File_write_all`, koje su kolektivne varijante citanja/upisa.

---

## Slajd 19 - Nekontinualni pristup i grupne operacije - objasnjenje

Slajd kaze da bi jedno resenje bilo da se za svaki mali deo fajla pozove posebno:

```c
MPI_File_seek(...);
MPI_File_read(...);
```

ili:

```c
MPI_File_seek(...);
MPI_File_write(...);
```

To radi, ali je neefikasno.

Zamisli da proces treba da procita 1000 malih blokova. Ako za svaki blok radi poseban `seek` i `read`, program ce imati mnogo I/O poziva. To povecava overhead.

Bolja ideja je: opisi MPI-ju obrazac podataka i onda jednim pozivom citaj ili pisi kao da su podaci kontinualni.

Tvoja beleska lepo kaze: nekontinualni pristup zelimo da svedemo na kontinualni pristup iz ugla procesa.

Primer:

```text
fajl stvarno:    0 1 2 0 1 2
proces 0 vidi:   0     0
```

Proces 0 ne mora da zna za delove procesa 1 i 2. Ako mu postavis dobar `file view`, on "vidi" samo svoje delove i moze da cita/pise sekvencijalno kroz svoj pogled.

To je najbitnija mentalna slika za `MPI_File_set_view`.

---

## Slajd 20 - Nekontinualni pristup

`File view`, tj. pogled na fajl, definise koji deo fajla je vidljiv procesu.

Podrazumevano, kada proces otvori fajl, vidi ceo fajl kao niz bajtova. To je kao da je:

```c
disp = 0;
etype = MPI_BYTE;
filetype = MPI_BYTE;
```

Kada promenis pogled, proces vise ne mora da vidi ceo fajl. Moze da vidi samo:

- svaki treci blok
- dva bloka pa praznina
- deo fajla koji pocinje od njegovog `rank`-a
- blokove rasporedjene po matrici ili mrezi

Funkcije za citanje/upis tada preskacu delove koji nisu u pogledu.

Zasto se to radi:

1. Da proces radi sa prirodnim tipom, npr. `MPI_INT`, a ne sa bajtovima.
2. Da se preskoce delovi fajla koji pripadaju drugim procesima.
3. Da se nekontinualan raspored u fajlu predstavi kao kontinualan tok podataka iz ugla procesa.

Za ispit: ako zadatak ima sliku gde procesi upisuju u "rupe", "kolone", "svaki n-ti blok" ili "dva odvojena bloka", skoro sigurno treba `MPI_File_set_view` i izvedeni tip.

---

## Slajd 21 - File view

Funkcija:

```c
MPI_File_set_view(
    MPI_File fh,
    MPI_Offset disp,
    MPI_Datatype etype,
    MPI_Datatype filetype,
    const char *datarep,
    MPI_Info info
);
```

Parametri:

`fh` je otvoreni fajl.

`disp` je displacement, tj. broj bajtova koji se preskace od pocetka fajla. Ovo je uvek u bajtovima.

`etype` je elementarni tip. To je osnovna jedinica pristupa u fajlu. Ako je `etype = MPI_INT`, onda se pozicije posle postavljanja pogleda posmatraju u jedinicama integera kroz taj pogled.

`filetype` opisuje obrazac vidljivih i nevidljivih delova fajla. Moze biti osnovni tip ili izvedeni tip.

`datarep` je reprezentacija podataka. U nasim zadacima:

```c
"native"
```

To znaci da se podaci u fajlu cuvaju u istoj reprezentaciji kao u memoriji na toj masini.

`info` je:

```c
MPI_INFO_NULL
```

Najvaznija recenica sa slajda: pogled na fajl pocinje od `disp` i sadrzi vise kontinualnih kopija `filetype`-a.

To znaci da se `filetype` ponavlja kroz fajl:

```text
disp | filetype | filetype | filetype | ...
```

Zato je `extent` izvedenog tipa bitan. Ako je `filetype` "2 integera vidljiva u obrascu duzine 6 integera", onda njegov `extent` mora biti 6 integera.

---

## Slajd 22 - File view - Primer

Zadatak: napraviti pogled koji:

- preskace prvih 5 integera od pocetka fajla
- koristi `etype = MPI_INT`
- u svakom obrascu vidi 2 integera
- zatim preskace 4 integera
- obrazac ukupno traje 6 integera

Mentalna slika:

```text
prvih 5 intova:  preskoci
zatim obrazac:  vidi vidi preskoci preskoci preskoci preskoci
ponavljanje:    vidi vidi preskoci preskoci preskoci preskoci
```

Ako oznacimo vidljive delove sa `X`, a preskocene sa `_`:

```text
_ _ _ _ _ | X X _ _ _ _ | X X _ _ _ _ | X X _ _ _ _
```

`disp` je:

```c
5 * sizeof(int)
```

jer se preskace 5 integera od pocetka fajla, a `disp` se zadaje u bajtovima.

`etype` je:

```c
MPI_INT
```

`filetype` treba da opisuje "2 integera vidljiva, ukupna sirina 6 integera".

Ovo je tipican primer gde se koristi izvedeni tip i `MPI_Type_create_resized`.

---

## Slajd 23 - File view - Primer - Resenje

Resenje sa `contiguous + resized`:

```c
MPI_Type_contiguous(2, MPI_INT, &contig);
MPI_Type_create_resized(contig, 0, 6 * sizeof(int), &filetype);

MPI_Type_commit(&filetype);

MPI_File_set_view(fh, 5 * sizeof(int), etype, filetype,
                  "native", MPI_INFO_NULL);
```

Prvi red:

```c
MPI_Type_contiguous(2, MPI_INT, &contig);
```

pravi tip koji sadrzi 2 uzastopna integera. To su vidljivi podaci.

Drugi red:

```c
MPI_Type_create_resized(contig, 0, 6 * sizeof(int), &filetype);
```

kaze: stvarni korisni deo su 2 integera, ali ukupna sirina obrasca je 6 integera. Tako se pri ponavljanju `filetype`-a napravi praznina od 4 integera.

`MPI_Type_commit` priprema tip za upotrebu.

`MPI_File_set_view` zatim postavlja pogled:

- pocetak pogleda je posle prvih 5 integera
- elementarni tip je `MPI_INT`
- obrazac je `filetype`
- reprezentacija je `native`
- hints nema

Ovo resenje je vrlo vazno jer jasno pokazuje zasto postoji `resize`: bez njega bi MPI mogao da ponavlja tip odmah posle 2 integera, a nama treba ponavljanje na svakih 6 integera.

---

## Slajd 24 - File view - Primer - Resenje 2

Slajd daje alternativu preko vektorskog tipa:

```c
MPI_Type_vector(1, 2, 6, MPI_INT, &filetype);

MPI_Type_commit(&filetype);

MPI_File_set_view(fh, 5 * sizeof(int), etype, filetype,
                  "native", MPI_INFO_NULL);
```

Znacenje parametara za `MPI_Type_vector`:

```c
MPI_Type_vector(count, blocklength, stride, oldtype, &newtype);
```

Ovde je:

- `count = 1` - jedan blok
- `blocklength = 2` - blok ima 2 integera
- `stride = 6` - razmak je 6 integera
- `oldtype = MPI_INT`

Ideja slajda: filetype predstavlja dva integera u obrascu koji se ponavlja na rastojanju 6 integera.

Prakticna napomena za razumevanje: za ispit prati oblik koji je dat na slajdu, ali najjasnije i najbezbednije resenje za obrazac "2 vidljiva + 4 prazna" je prethodni slajd sa `MPI_Type_create_resized`, jer eksplicitno postavlja ukupnu duzinu obrasca na 6 integera.

Za pamcenje:

- `vector` je prirodan kada imas vise blokova sa istim razmakom
- `resized` je prirodan kada treba da kontrolises koliko se obrazac pomera pri ponavljanju

---

## Slajd 25 - File view - Primer - Celo resenje

U Markdown konverziji slajd nema dodatni tekst/kod, ali po naslovu pripada prethodnom primeru. Celo resenje bi imalo sledeci redosled:

```c
MPI_File fh;
MPI_Datatype contig, filetype;
MPI_Datatype etype = MPI_INT;

MPI_File_open(MPI_COMM_WORLD, "filename",
              MPI_MODE_RDONLY,
              MPI_INFO_NULL, &fh);

MPI_Type_contiguous(2, MPI_INT, &contig);
MPI_Type_create_resized(contig, 0, 6 * sizeof(int), &filetype);
MPI_Type_commit(&filetype);

MPI_File_set_view(fh, 5 * sizeof(int), etype, filetype,
                  "native", MPI_INFO_NULL);

/* ovde ide MPI_File_read ili MPI_File_write */

MPI_Type_free(&filetype);
MPI_Type_free(&contig);
MPI_File_close(&fh);
```

Sta je bitno u celom resenju:

1. Otvoris fajl.
2. Napravis izvedeni tip.
3. Commit-ujes izvedeni tip.
4. Postavis pogled na fajl.
5. Tek onda citas ili pises.
6. Na kraju zatvoris fajl i oslobodis tipove.

U kodovima sa slajdova se cesto izostavi `MPI_Type_free`, ali u urednom kodu treba osloboditi izvedene tipove kada vise nisu potrebni.

---

## Slajd 26 - File view primer 2

Zadatak: svaki proces upisuje jedan kontinualni blok fajla. Svaki proces ima drugaciji pocetak.

Ovo je slicno osnovnom primeru sa `seek`, ali se sada radi preko `file view`.

Ako svaki proces upisuje `N` integera:

- proces 0 pocinje od `0 * N * sizeof(int)`
- proces 1 pocinje od `1 * N * sizeof(int)`
- proces 2 pocinje od `2 * N * sizeof(int)`
- proces 3 pocinje od `3 * N * sizeof(int)`

Razlika je u mentalnom modelu:

- sa `seek`: svi vide ceo fajl, ali se svako pomeri na svoj offset
- sa `set_view`: svaki proces dobije pogled koji pocinje od njegovog dela fajla

Za proces 1, pogled pocinje posle prvih `N` integera. Ako onda pozove:

```c
MPI_File_write(fh, buf, N, MPI_INT, MPI_STATUS_IGNORE);
```

on pise od pocetka svog pogleda, ne mora posebno da radi `seek`.

---

## Slajd 27 - File view primer 2 - resenje

Kod:

```c
#define N 100

MPI_Datatype arraytype;
MPI_Offset disp;

disp = rank * sizeof(int) * N;
etype = MPI_INT;

MPI_Type_contiguous(N, MPI_INT, &arraytype);
MPI_Type_commit(&arraytype);

MPI_File_open(MPI_COMM_WORLD, "/pfs/datafile",
              MPI_MODE_CREATE | MPI_MODE_RDWR,
              MPI_INFO_NULL, &fh);

MPI_File_set_view(fh, disp, etype, arraytype,
                  "native", MPI_INFO_NULL);

MPI_File_write(fh, buf, N, etype, MPI_STATUS_IGNORE);
```

Objasnjenje:

```c
disp = rank * sizeof(int) * N;
```

racuna gde pocinje deo fajla za trenutni proces.

```c
MPI_Type_contiguous(N, MPI_INT, &arraytype);
```

pravi `filetype` koji predstavlja `N` uzastopnih integera.

```c
MPI_File_set_view(fh, disp, MPI_INT, arraytype, "native", MPI_INFO_NULL);
```

postavlja pogled tako da proces vidi svoj blok.

```c
MPI_File_write(fh, buf, N, etype, MPI_STATUS_IGNORE);
```

upisuje `N` integera.

Ovaj primer je jednostavan, jer je filetype kontinualan. Ipak je koristan jer pokazuje ulogu `disp`: svaki proces ima drugaciji pocetak pogleda.

Tvoja beleska: `disp` nije isto za svaki proces. Tacno. To je ono sto razdvaja delove procesa u fajlu.

---

## Slajd 28 - File view primer 3

Zadatak: svaki proces upisuje svoje kontinualne podatke u dva razlicita bloka u fajlu.

To znaci da proces u memoriji mozda ima podatke spojene:

```text
buf:  [blok A][blok B]
```

ali u fajlu ti blokovi nisu spojeni. Izmedju njih se nalaze podaci drugih procesa.

Ako ima 4 procesa i svaki blok ima `NW` integera, raspored u fajlu moze mentalno da izgleda ovako:

```text
P0 P1 P2 P3 | P0 P1 P2 P3
```

Svaki `P` ovde predstavlja blok od `NW` integera.

Proces 0 treba da vidi:

```text
P0          | P0
```

Proces 1 treba da vidi:

```text
   P1       |    P1
```

Zato svaki proces ima drugaciji `disp`, ali isti oblik `filetype`-a.

Ovo je pravi primer za `MPI_Type_vector`, jer se vidi vise blokova sa pravilnim razmakom.

---

## Slajd 29 - File view primer 3 - Resenje

Kod:

```c
int buf[NW * 2];

MPI_File_open(MPI_COMM_WORLD, "/data2",
              MPI_MODE_RDWR,
              MPI_INFO_NULL, &fh);

/* definisemo pogled na 2 bloka od po NW integera,
   na NW*nprocs udaljenosti */
MPI_Type_vector(2, NW, NW * nprocs, MPI_INT, &fileblk);
MPI_Type_commit(&fileblk);

disp = (MPI_Offset) rank * NW * sizeof(int);

MPI_File_set_view(fh, disp, MPI_INT, fileblk,
                  "native", MPI_INFO_NULL);

/* Upis 2 'ablk' tipa, gde svaki ima po NW integera */
MPI_Type_contiguous(NW, MPI_INT, &ablk);
MPI_Type_commit(&ablk);

MPI_File_write(fh, (void *)buf, 2, ablk, &status);
```

Najbitniji red:

```c
MPI_Type_vector(2, NW, NW * nprocs, MPI_INT, &fileblk);
```

Parametri znace:

- `2` - postoje dva bloka koja proces vidi u fajlu
- `NW` - svaki blok ima `NW` integera
- `NW * nprocs` - pocetak drugog bloka je udaljen za celu grupu blokova svih procesa
- `MPI_INT` - osnovni tip je integer

Ako ima 4 procesa, `stride = NW * 4`. To znaci: od pocetka prvog bloka procesa preskaces blokove za P0, P1, P2, P3 da bi dosao do sledeceg bloka istog procesa.

Pomeraj:

```c
disp = rank * NW * sizeof(int);
```

Proces 0 pocinje od prvog bloka. Proces 1 preskace jedan blok od `NW` integera. Proces 2 preskace dva bloka itd.

Postavljanje pogleda:

```c
MPI_File_set_view(fh, disp, MPI_INT, fileblk, "native", MPI_INFO_NULL);
```

Od tog trenutka proces vidi samo svoja dva bloka u fajlu.

Zatim se pravi memorijski tip:

```c
MPI_Type_contiguous(NW, MPI_INT, &ablk);
```

`ablk` predstavlja jedan blok od `NW` integera u memoriji.

Upis:

```c
MPI_File_write(fh, buf, 2, ablk, &status);
```

znaci: iz memorije upisi 2 bloka tipa `ablk`. Ukupno se upisuje `2 * NW` integera. Zbog `file view`-a, MPI ih nece upisati spojeno u fajl, nego u dva vidljiva bloka procesa.

Tvoja beleska "P0 vidi fajl kao prvi i peti blok spojeni" je tacna za 4 procesa: ako se blokovi broje od 0, proces 0 vidi blok 0 i blok 4. Iz ugla procesa oni izgledaju kao spojeni, iako u stvarnom fajlu izmedju njih stoje blokovi drugih procesa.

---

## Najvaznije za ispit iz Termina 1

1. Razlikuj bajtove i broj elemenata.

`seek`/`disp` koriste pomeraj kroz fajl. Ako preskaces integera, mnozis sa `sizeof(int)`.

```c
rank * N * sizeof(int)
```

`read/write` koriste `count` i `datatype`.

```c
MPI_File_write(fh, buf, N, MPI_INT, &status);
```

2. Razlikuj implicitni i eksplicitni pomeraj.

Implicitni, preko individualnog pokazivaca:

```c
MPI_File_seek(fh, offset, MPI_SEEK_SET);
MPI_File_write(fh, buf, count, MPI_INT, &status);
```

Eksplicitni, bez `seek`:

```c
MPI_File_write_at(fh, offset, buf, count, MPI_INT, &status);
```

3. `MPI_File_set_view` je kljuc za nekontinualni pristup.

Ako proces treba da radi sa delovima fajla koji nisu spojeni, ne zelis mnogo `seek/read` poziva. Definises pogled na fajl.

4. `disp` je pocetak pogleda u bajtovima.

```c
disp = rank * NW * sizeof(int);
```

5. `etype` je elementarna jedinica pogleda.

Najcesce:

```c
MPI_INT
```

6. `filetype` opisuje obrazac u fajlu.

Za kontinualan blok:

```c
MPI_Type_contiguous(N, MPI_INT, &arraytype);
```

Za vise blokova sa pravilnim razmakom:

```c
MPI_Type_vector(count, blocklength, stride, MPI_INT, &fileblk);
```

Za obrazac koji mora da se ponavlja sa posebnom ukupnom sirinom:

```c
MPI_Type_create_resized(...)
```

7. Posle kreiranja izvedenog tipa obavezno:

```c
MPI_Type_commit(&type);
```

8. Mentalna slika za `file view`:

Proces ne menja stvarni fajl. On menja svoj pogled na fajl. Delovi koji nisu u pogledu se preskacu.

---

## Veza sa laboratorijskom vezbom

Prvi deo lab pripreme:

Svaki proces upisuje 10 integera. Proces 0 pise 0-9, proces 1 pise 10-19 itd. Sa pojedinacnim pokazivacem:

```c
int buf[10];

for (int i = 0; i < 10; i++) {
    buf[i] = rank * 10 + i;
}

MPI_File_open(MPI_COMM_WORLD, "data.bin",
              MPI_MODE_CREATE | MPI_MODE_WRONLY,
              MPI_INFO_NULL, &fh);

MPI_File_seek(fh, rank * 10 * sizeof(int), MPI_SEEK_SET);
MPI_File_write(fh, buf, 10, MPI_INT, &status);
MPI_File_close(&fh);
```

Drugi deo:

Ponovo se otvara fajl i cita se sa eksplicitnim pomerajem:

```c
MPI_File_open(MPI_COMM_WORLD, "data.bin",
              MPI_MODE_RDONLY,
              MPI_INFO_NULL, &fh);

MPI_File_read_at(fh, rank * 10 * sizeof(int), buf, 10, MPI_INT, &status);
MPI_File_close(&fh);
```

Provera:

```c
for (int i = 0; i < 10; i++) {
    if (buf[i] != rank * 10 + i) {
        /* greska */
    }
}
```

Treci deo lab vezbe ce verovatno traziti efikasniji upis u novi fajl. Ako raspored nije kontinualan, trazi se `MPI_File_set_view` i izvedeni tip. Ako pise "poslednje upisivanje uciniti sto efikasnijim", to je signal da ne treba mnogo pojedinacnih `seek/write` poziva, nego opisati raspored preko `file view`-a, a u drugom terminu moguce i preko kolektivnog `MPI_File_write_all`.

---

# MPI Parallel I/O - beleske za Termin 2

Drugi termin nastavlja tacno tamo gde je prvi stao: prvi termin ti je dao osnovne I/O funkcije, eksplicitni pomeraj i `file view`; drugi termin objasnjava kako da to uradis efikasnije, kako da citas/pises raspodeljene matrice, kako rade neblokirajuce I/O funkcije, deljeni pokazivac i konzistentnost.

Glavna ideja Termina 2:

- ako svaki proces samostalno radi mnogo malih `read/write` operacija, performanse su lose
- bolji pristup je da MPI-ju opises obrazac pristupa preko izvedenog tipa i `file view`
- jos bolji pristup je da svi procesi koriste grupne funkcije, npr. `MPI_File_read_all` i `MPI_File_write_all`
- najbolji stil za zadatke je najcesce: izvedeni tip + `MPI_File_set_view` + grupna I/O funkcija

## Slajd 1 - MPI PARALLEL I/O, Termin 2

Ovaj termin prosiruje prethodni:

- nezavisne operacije iz Termina 1: `MPI_File_read`, `MPI_File_write`, `MPI_File_read_at`, `MPI_File_write_at`
- grupne operacije: `MPI_File_read_all`, `MPI_File_write_all`
- raspodeljeni nizovi/matrice: `MPI_Type_create_darray`, `MPI_Type_create_subarray`
- neblokirajuce I/O funkcije: `MPI_File_iread`, `MPI_File_iwrite`, `_at` varijante
- split collective I/O: `MPI_File_write_all_begin/end`
- deljeni pokazivac: `MPI_File_write_shared`, `MPI_File_read_shared`
- konzistentnost: kada vise procesa pise/cita isti fajl

Za zadatke se najvise ocekuje da znas da prepoznas obrazac podataka i izaberes pravu kombinaciju funkcija.

---

## Slajd 2 - Grupne I/O operacije

U Terminu 1 smo uglavnom koristili nezavisan pristup fajlu. To znaci:

```c
MPI_File_write(fh, buf, count, MPI_INT, &status);
```

pozove proces 0 kada on dodje do te linije, proces 1 kada on dodje do te linije itd. Nije obavezno da cekaju jedan drugog bas na toj I/O operaciji.

Grupna I/O operacija znaci da svi procesi iz komunikatora koji je otvorio fajl moraju da pozovu istu kolektivnu I/O funkciju.

Primer:

```c
MPI_File_read_all(fh, buf, count, MPI_INT, &status);
```

Ako je fajl otvoren sa:

```c
MPI_File_open(MPI_COMM_WORLD, ...);
```

onda svi procesi iz `MPI_COMM_WORLD` moraju da pozovu `MPI_File_read_all`.

Pravilo je isto kao kod `MPI_Barrier`, `MPI_Bcast`, `MPI_Reduce`: kolektivna funkcija se stvarno zavrsava tek kada svi procesi ucestvuju.

Tvoja beleska: kod nezavisnih operacija proces sa rankom 0 ne ceka ostale. Kod grupnih operacija ceka se grupa, ali MPI dobija sansu da optimizuje ukupan zahtev.

---

## Slajd 3 - Grupne I/O operacije - nastavak

Prednost grupnog I/O je bolja performansa.

Zasto? Disk/fajl sistem ima veliku latenciju. CPU operacije su brze, pristup RAM-u je sporiji, a pristup disku/fajl sistemu je jos mnogo sporiji. Ako svaki proces salje mnogo malih I/O zahteva, dobijas mnogo latencija i mnogo overhead-a.

MPI implementacija moze da pogleda zahteve svih procesa zajedno i da ih spoji u vece zahteve.

Los stil:

```text
mnogo malih nezavisnih pristupa
```

Bolji stil:

```text
manje velikih kolektivnih pristupa
```

Posebno je korisno kada su pristupi procesa nekontinualni i isprepleteni. Tada MPI moze da uradi optimizaciju koju ti rucno tesko i ruzno pises.

Za ispit: ako se trazi "efikasnije" ili "sto efikasnije", razmisljaj o `file view` + grupna operacija.

---

## Slajd 4 - Grupne I/O operacije - nastavak

Slajd objasnjava pozadinsku optimizaciju.

Svaki proces ima svoj deo fajla, ali delovi mogu biti rasporedjeni ovako:

```text
P0 P1 P2 P0 P1 P2 P0 P1 P2
```

Ako svaki proces radi samostalno, dobija se puno malih zahteva:

- P0 cita blok 0, pa blok 3, pa blok 6
- P1 cita blok 1, pa blok 4, pa blok 7
- P2 cita blok 2, pa blok 5, pa blok 8

Kolektivni I/O omogucava MPI-ju da vidi sve zahteve zajedno i da cita vece kontinualne regione fajla, pa da podatke interno raspodeli procesima.

Ti u kodu ne moras da radis tu optimizaciju rucno. Tvoj posao je:

1. opisati obrazac podataka izvedenim tipom
2. postaviti `file view`
3. pozvati `MPI_File_read_all` ili `MPI_File_write_all`

---

## Slajd 5 - Grupne I/O operacije za citanje i upis

Funkcije:

```c
int MPI_File_read_all(
    MPI_File fh,
    void *buf,
    int count,
    MPI_Datatype datatype,
    MPI_Status *status
);
```

```c
int MPI_File_write_all(
    MPI_File fh,
    const void *buf,
    int count,
    MPI_Datatype datatype,
    MPI_Status *status
);
```

Parametri su isti kao kod obicnih:

```c
MPI_File_read(...)
MPI_File_write(...)
```

Razlika je u tome sto su `_all` funkcije kolektivne. Svi procesi iz komunikatora moraju da ih pozovu.

Ne menja se nacin racunanja `count`:

```c
MPI_File_write_all(fh, buf, 10, MPI_INT, &status);
```

znaci da svaki proces upisuje 10 integera iz svog bafera, ne 10 bajtova.

Tvoja beleska: "jedino menjamo funkciju koju pozivamo, argumenti su isti". Tacno.

---

## Slajd 6 - Grupne I/O operacije - primer

Zadatak: ima ukupno `N` procesa. Svaki proces cita manje delove fajla po round-robin principu.

Round-robin znaci ciklicno:

```text
P0 P1 P2 ... Pn-1 P0 P1 P2 ... Pn-1 ...
```

Ako svaki mali blok ima `INTS_PER_BLK` integera, onda raspored izgleda:

```text
blok 0: P0
blok 1: P1
blok 2: P2
...
blok nprocs-1: Pn-1
blok nprocs: P0
```

Proces 0 ne cita jedan kontinualan veliki deo, nego vise malih blokova rasutih kroz fajl. To je nekontinualan pristup.

Efikasno resenje:

- `MPI_Type_vector` opisuje blokove koje proces vidi
- `MPI_File_set_view` sakrije tudje blokove
- `MPI_File_read_all` omogucava grupno citanje

Mentalna slika za jedan proces:

```text
stvarni fajl: P0 P1 P2 P3 P0 P1 P2 P3
P0 vidi:      P0          P0
```

Iz ugla P0, njegova dva bloka izgledaju kao jedan tok podataka.

---

## Slajd 7 - Grupne I/O operacije - primer - resenje

Korak 1: otvoriti fajl za citanje.

```c
MPI_File_open(MPI_COMM_WORLD, "/pfs/datafile",
              MPI_MODE_RDONLY, MPI_INFO_NULL, &fh);
```

Korak 2: napraviti izvedeni tip koji opisuje sta svaki proces vidi u fajlu.

```c
MPI_Type_vector(nints / INTS_PER_BLK,
                INTS_PER_BLK,
                INTS_PER_BLK * nprocs,
                MPI_INT,
                &filetype);

MPI_Type_commit(&filetype);
```

Objasnjenje `MPI_Type_vector(count, blocklength, stride, oldtype, &newtype)`:

- `count = nints / INTS_PER_BLK`
- `blocklength = INTS_PER_BLK`
- `stride = INTS_PER_BLK * nprocs`
- `oldtype = MPI_INT`

`nints` je ukupan broj integera koji jedan proces treba da procita.

`INTS_PER_BLK` je broj integera u jednom malom bloku.

`nints / INTS_PER_BLK` je broj takvih blokova za jedan proces.

`INTS_PER_BLK * nprocs` je rastojanje od pocetka jednog bloka istog procesa do pocetka sledeceg bloka istog procesa.

Primer sa 4 procesa i `INTS_PER_BLK = 2`:

```text
P0 P1 P2 P3 P0 P1 P2 P3
```

Od pocetka P0 bloka do sledeceg P0 bloka ima:

```c
2 * nprocs = 2 * 4 = 8
```

integera.

---

## Slajd 8 - MPI_Type_vector primer

Podaci sa slajda:

```text
FILESIZE = 800 B
nprocs = 10
bufsize = 80 B
nints = 80 / 4 = 20
INTS_PER_BLK = 5
```

`FILESIZE = 800 B` znaci da ce svih 10 procesa zajedno procitati 800 bajtova.

```c
bufsize = FILESIZE / nprocs = 800 / 10 = 80 B
```

Jedan proces cita 80 bajtova.

Ako je jedan `int` 4 bajta:

```c
nints = bufsize / sizeof(int) = 80 / 4 = 20
```

Jedan proces cita 20 integera ukupno.

`INTS_PER_BLK = 5` nije izracunato iz 20. To je zadato velicinom malog bloka na slici/zadatku. Znaci: svaki mali blok ima 5 integera.

Broj blokova za jedan proces:

```c
nints / INTS_PER_BLK = 20 / 5 = 4
```

Zato vector ima `count = 4`.

Kod:

```c
MPI_Type_vector(nints / INTS_PER_BLK,
                INTS_PER_BLK,
                INTS_PER_BLK * nprocs,
                MPI_INT,
                &filetype);
```

Konkretno:

```c
MPI_Type_vector(4, 5, 50, MPI_INT, &filetype);
```

Znacenje:

- 4 bloka
- svaki blok ima 5 integera
- pocetak sledeceg bloka je 50 integera posle pocetka prethodnog

Zasto je `stride = 50`?

```c
INTS_PER_BLK * nprocs = 5 * 10 = 50
```

U bajtovima:

```c
50 * sizeof(int) = 50 * 4 = 200 B
```

To je jedan ceo round: 5 integera za P0, 5 za P1, ..., 5 za P9.

---

## Slajd 9 - Grupne I/O operacije - nastavak resenja

Korak 3: postavljanje pogleda na fajl.

```c
MPI_File_set_view(fh,
                  INTS_PER_BLK * sizeof(int) * rank,
                  MPI_INT,
                  filetype,
                  "native",
                  MPI_INFO_NULL);
```

`disp` je:

```c
INTS_PER_BLK * sizeof(int) * rank
```

Zasto?

Procesi ne pocinju na istom mestu:

- P0 pocinje od 0
- P1 pocinje posle prvog bloka od `INTS_PER_BLK` integera
- P2 pocinje posle dva takva bloka

Za `INTS_PER_BLK = 5` i `sizeof(int) = 4`:

- P0: `5 * 4 * 0 = 0 B`
- P1: `5 * 4 * 1 = 20 B`
- P2: `5 * 4 * 2 = 40 B`

`disp` je uvek bajtovski pomeraj od pocetka fajla.

`etype = MPI_INT`, pa operacije posle pogleda rade u jedinicama integera.

`filetype` je vektorski tip koji kaze: vidi 5 integera, preskoci blokove drugih procesa, pa opet vidi 5 integera.

---

## Slajd 10 - Grupne I/O operacije - read_all i zatvaranje

Korak 4:

```c
MPI_File_read_all(fh, buf, nints, MPI_INT, MPI_STATUS_IGNORE);
```

Ovo mora da pozove svaki proces iz komunikatora koji je koriscen u `MPI_File_open`.

Svaki proces cita `nints` integera u svoj `buf`. Zbog `file view`-a, ti integeri nisu nuzno kontinualni u stvarnom fajlu, ali jesu kontinualni iz ugla procesa.

Korak 5:

```c
MPI_File_close(&fh);
```

Bitno: `MPI_File_read_all` je kolektivna operacija. Ako jedan proces ne dodje do tog poziva, ostali mogu da cekaju zauvek.

Za zadatak: kada god koristis `read_all/write_all`, pazi da svi procesi prodju kroz isti kolektivni poziv.

---

## Slajd 11 - Grupne I/O operacije - celo resenje

Celo resenje sa slajda:

```c
#include "mpi.h"

#define FILESIZE 1048576
#define INTS_PER_BLK 16

int main(int argc, char **argv)
{
    int *buf, rank, nprocs, nints, bufsize;
    MPI_File fh;
    MPI_Datatype filetype;

    MPI_Init(&argc, &argv);
    MPI_Comm_rank(MPI_COMM_WORLD, &rank);
    MPI_Comm_size(MPI_COMM_WORLD, &nprocs);

    bufsize = FILESIZE / nprocs;
    buf = (int *) malloc(bufsize);
    nints = bufsize / sizeof(int);

    MPI_File_open(MPI_COMM_WORLD, "/pfs/datafile",
                  MPI_MODE_RDONLY, MPI_INFO_NULL, &fh);

    MPI_Type_vector(nints / INTS_PER_BLK,
                    INTS_PER_BLK,
                    INTS_PER_BLK * nprocs,
                    MPI_INT,
                    &filetype);

    MPI_Type_commit(&filetype);

    MPI_File_set_view(fh,
                      INTS_PER_BLK * sizeof(int) * rank,
                      MPI_INT,
                      filetype,
                      "native",
                      MPI_INFO_NULL);

    MPI_File_read_all(fh, buf, nints, MPI_INT, MPI_STATUS_IGNORE);
    MPI_File_close(&fh);

    MPI_Type_free(&filetype);
    free(buf);
    MPI_Finalize();
    return 0;
}
```

Ovaj kod je sablon za round-robin pristup:

1. izracunas koliko bajtova i integera cita svaki proces
2. napravis vektorski `filetype`
3. `disp` pomeris za `rank * velicina_bloka`
4. postavis pogled
5. citas grupno

Ako zadatak trazi upis umesto citanja:

```c
MPI_File_write_all(fh, buf, nints, MPI_INT, MPI_STATUS_IGNORE);
```

i fajl otvoris sa `MPI_MODE_CREATE | MPI_MODE_WRONLY` ili `MPI_MODE_CREATE | MPI_MODE_RDWR`.

---

## Slajd 12 - Pristup nizovima u datotekama

Ovo je uvod u rad sa nizovima i matricama u fajlovima.

U paralelnim programima cesto postoji globalna matrica, ali svaki proces u memoriji drzi samo jedan deo te matrice.

Primer globalne matrice:

```text
12 x 12 matrica
```

Ako ima 4 procesa, svaki moze drzati podmatricu:

```text
P0 P1
P2 P3
```

Problem: u memoriji procesa lokalna podmatrica je kontinualna, ali u fajlu njen raspored moze biti nekontinualan zbog redova globalne matrice.

Za to MPI ima posebne izvedene tipove:

- `MPI_Type_create_darray`
- `MPI_Type_create_subarray`

---

## Slajd 13 - Pristup nizovima u datotekama

MPI olaksava pristup multidimenzionalnim nizovima.

Dve funkcije:

```c
MPI_Type_create_darray(...)
```

Koristi se kada hoces da MPI sam odredi koji deo globalnog niza pripada kom procesu na osnovu broja procesa, rank-a, dimenzija i nacina distribucije.

```c
MPI_Type_create_subarray(...)
```

Koristi se kada ti rucno znas: globalna velicina, lokalna velicina i pocetne koordinate podpolja.

Kratka razlika:

```text
darray   = MPI sam raspodeljuje globalno polje procesima
subarray = ti kazes tacno koje podpolje pripada procesu
```

Za zadatke je `subarray` cesto laksi za razumevanje, jer direktno racunas:

```c
start_row = coords[0] * local_rows;
start_col = coords[1] * local_cols;
```

---

## Slajd 14 - MPI_Type_create_darray

Prototip:

```c
int MPI_Type_create_darray(
    int size,
    int rank,
    int ndims,
    int gsizes[],
    int distribs[],
    int dargs[],
    int psizes[],
    int order,
    MPI_Datatype oldtype,
    MPI_Datatype *newtype
);
```

Funkcija pravi izvedeni tip koji opisuje deo N-dimenzionalnog globalnog polja koji pripada jednom procesu.

Parametri:

`size` je ukupan broj procesa medju kojima se niz distribuira. Ako koristis `MPI_COMM_WORLD`, to je najcesce `nprocs`.

`rank` je rank trenutnog procesa. Na osnovu njega MPI zna koji deo polja opisuje.

`ndims` je broj dimenzija globalnog polja. Za matricu je `2`.

`gsizes` su globalne dimenzije. Za matricu `m x n`:

```c
gsizes[0] = m; // broj redova
gsizes[1] = n; // broj kolona
```

`distribs` govori kako se podaci dele po svakoj dimenziji. U prezentaciji se koristi:

```c
MPI_DISTRIBUTE_BLOCK
```

To znaci blokovska podela. Proces dobija blok redova/kolona, ne svaki drugi element.

`dargs` su dodatni argumenti distribucije. Najcesce:

```c
MPI_DISTRIBUTE_DFLT_DARG
```

To znaci: prepusti MPI-ju podrazumevanu velicinu bloka.

`psizes` je procesorska mreza. Za 6 procesa rasporedjenih kao 2 x 3:

```c
psizes[0] = 2;
psizes[1] = 3;
```

To znaci: globalna matrica se deli po redovima na 2 dela i po kolonama na 3 dela. Ukupno `2 * 3 = 6` procesa.

`order` je raspored u memoriji:

- `MPI_ORDER_C` - C stil, redovi jedan za drugim, row-major
- `MPI_ORDER_FORTRAN` - Fortran stil, kolone jedna za drugom, column-major

`oldtype` je tip jednog elementa globalnog polja, npr. `MPI_FLOAT`.

`newtype` je izvedeni tip koji dobijes.

Najkrace: `darray` ti napravi `filetype` za deo globalne matrice koji pripada procesu `rank`.

---

## Slajd 15 - Primer 1 sa darray

Zadatak: 2D polje dimenzija `m x n`, 6 procesa rasporedjenih `2 x 3`.

Kod:

```c
gsizes[0] = m;
gsizes[1] = n;

distribs[0] = MPI_DISTRIBUTE_BLOCK;
distribs[1] = MPI_DISTRIBUTE_BLOCK;

dargs[0] = MPI_DISTRIBUTE_DFLT_DARG;
dargs[1] = MPI_DISTRIBUTE_DFLT_DARG;

psizes[0] = 2;
psizes[1] = 3;

MPI_Comm_rank(MPI_COMM_WORLD, &rank);

MPI_Type_create_darray(6, rank, 2,
                       gsizes, distribs, dargs, psizes,
                       MPI_ORDER_C, MPI_FLOAT,
                       &filetype);

MPI_Type_commit(&filetype);
```

Sta ovo znaci:

- globalna matrica ima `m` redova i `n` kolona
- procesorska mreza ima 2 podele po vertikali/redovima i 3 podele po horizontali/kolonama
- svaki proces dobija jedan blok matrice
- `filetype` opisuje gde se taj blok nalazi u fajlu kao deo globalne matrice

Zatim:

```c
MPI_File_open(MPI_COMM_WORLD, "filename",
              MPI_MODE_CREATE | MPI_MODE_WRONLY,
              MPI_INFO_NULL, &fh);

MPI_File_set_view(fh, 0, MPI_FLOAT, filetype,
                  "native", MPI_INFO_NULL);

local_array_size = num_local_rows * num_local_cols;

MPI_File_write_all(fh, buf_local_array,
                   local_array_size, MPI_FLOAT, &status);

MPI_File_close(&fh);
```

`disp = 0` zato sto `filetype` vec opisuje tacno gde je podpolje u globalnom fajlu. Ne pomeras svaki proces rucno preko `disp`; to radi `darray` kroz tip.

`buf_local_array` je lokalna memorija procesa, kontinualan niz lokalnih elemenata.

`MPI_File_write_all` upisuje sve lokalne podmatrice u jedan globalni fajl.

---

## Slajd 16 - MPI_Type_create_subarray

Prototip:

```c
int MPI_Type_create_subarray(
    int ndims,
    int sizes[],
    int subsizes[],
    int offsets[],
    int order,
    MPI_Datatype oldtype,
    MPI_Datatype *newtype
);
```

Ova funkcija pravi izvedeni tip za jedno podpolje unutar veceg polja.

Parametri:

`ndims` je broj dimenzija. Za matricu:

```c
ndims = 2;
```

`sizes` su globalne dimenzije:

```c
sizes[0] = m;
sizes[1] = n;
```

`subsizes` su dimenzije lokalnog podpolja:

```c
subsizes[0] = local_rows;
subsizes[1] = local_cols;
```

`offsets` su pocetne koordinate lokalnog podpolja u globalnoj matrici:

```c
offsets[0] = start_row;
offsets[1] = start_col;
```

`order` je `MPI_ORDER_C` za C matrice.

`oldtype` je tip elementa, npr. `MPI_FLOAT`.

Najvaznija mentalna slika:

```text
globalna matrica:

red 0:  x x x x x x
red 1:  x A A x x x
red 2:  x A A x x x
red 3:  x x x x x x

subarray:
start = (1, 1)
subsizes = (2, 2)
```

`subarray` opisuje blok `A A / A A` u globalnoj matrici.

---

## Slajd 17 - Primer 1 pomocu subarray funkcije

Slajd daje istu ideju kao `darray`, ali sada sami racunamo pocetak podmatrice.

Globalna matrica:

```c
gsizes[0] = m; // broj redova
gsizes[1] = n; // broj kolona
```

Procesorska mreza:

```c
psizes[0] = 2; // broj procesa po redovima
psizes[1] = 3; // broj procesa po kolonama
```

Lokalne dimenzije:

```c
lsizes[0] = m / psizes[0]; // lokalni broj redova
lsizes[1] = n / psizes[1]; // lokalni broj kolona
```

Napomena: na slajdu je verovatno greska gde pise `lsizes[1] = m / psizes[1]`; za kolone treba `n / psizes[1]`.

Kreira se Cartesian topologija:

```c
dims[0] = 2;
dims[1] = 3;
periods[0] = periods[1] = 1;

MPI_Cart_create(MPI_COMM_WORLD, 2, dims, periods, 0, &comm);
MPI_Comm_rank(comm, &rank);
MPI_Cart_coords(comm, rank, 2, coords);
```

Zasto Cartesian topologija? Da svaki proces lako dobije koordinate u mrezi procesa.

Ako je proces u koordinatama:

```c
coords[0] = red u procesorskoj mrezi
coords[1] = kolona u procesorskoj mrezi
```

onda pocetak njegovog podpolja u globalnoj matrici:

```c
start_indices[0] = coords[0] * lsizes[0];
start_indices[1] = coords[1] * lsizes[1];
```

Zatim:

```c
MPI_Type_create_subarray(2,
                         gsizes,
                         lsizes,
                         start_indices,
                         MPI_ORDER_C,
                         MPI_FLOAT,
                         &filetype);

MPI_Type_commit(&filetype);
```

I upis:

```c
MPI_File_open(MPI_COMM_WORLD, "filename",
              MPI_MODE_CREATE | MPI_MODE_WRONLY,
              MPI_INFO_NULL, &fh);

MPI_File_set_view(fh, 0, MPI_FLOAT, filetype,
                  "native", MPI_INFO_NULL);

local_array_size = lsizes[0] * lsizes[1];

MPI_File_write_all(fh, buf_local_array,
                   local_array_size, MPI_FLOAT, &status);

MPI_File_close(&fh);
```

Razlika `darray` vs `subarray`:

- `darray`: prosledis rank i pravila distribucije, MPI odredi deo
- `subarray`: ti izracunas `start_indices`, pa MPI napravi tip za taj tacan deo

Za zadatke: ako ti daju sliku i mozes lako izracunati `start_indices`, `subarray` je razumljiviji.

---

## Slajd 18 - Neblokirajuci I/O i Split Collective I/O

Ovo je uvod u I/O funkcije koje ne blokiraju odmah proces.

Normalna blokirajuca funkcija:

```c
MPI_File_write_at(fh, offset, buf, count, MPI_INT, &status);
```

Proces se vraca iz funkcije kada je operacija zavrsena.

Neblokirajuca funkcija:

```c
MPI_File_iwrite_at(fh, offset, buf, count, MPI_INT, &request);
```

Proces pokrene operaciju i moze da nastavi sa drugim radom. Kasnije mora da proveri/zavrsi operaciju preko `MPI_Wait` ili `MPI_Test`.

---

## Slajd 19 - Neblokirajuce I/O funkcije

Motivacija je ista kao kod `MPI_Isend` i `MPI_Irecv`:

- izbegavanje cekanja kada proces moze da radi nesto korisno
- preklapanje I/O operacije i racunanja
- smanjenje bespotrebne sinhronizacije

Funkcije:

```c
MPI_File_iread(fh, buf, count, datatype, &request);
MPI_File_iwrite(fh, buf, count, datatype, &request);
MPI_File_iread_at(fh, offset, buf, count, datatype, &request);
MPI_File_iwrite_at(fh, offset, buf, count, datatype, &request);
```

`i` znaci immediate/nonblocking.

Bitno pravilo: ne smes menjati ili osloboditi `buf` dok operacija nije zavrsena.

Obavezno na kraju:

```c
MPI_Wait(&request, &status);
```

ili provera:

```c
MPI_Test(&request, &flag, &status);
```

Za ispit: ako koristis neblokirajuci I/O, mora da postoji `MPI_Request` i zavrsavanje operacije.

---

## Slajd 20 - Nacin rada neblokirajucih funkcija

Funkcija vraca `MPI_Request`.

```c
MPI_Request request;
MPI_File_iread_at(fh, offset, buf, count, MPI_INT, &request);
```

Kasnije:

```c
MPI_Wait(&request, &status);
```

`MPI_Wait` je blokirajuca. Proces tu ceka dok se operacija ne zavrsi.

Ili:

```c
int flag = 0;
MPI_Test(&request, &flag, &status);
```

`MPI_Test` nije blokirajuca. Samo proveri da li je operacija zavrsena:

- `flag = true` ako jeste
- `flag = false` ako nije

Tvoja beleska: za `MPI_Wait` ne treba posebna `while` petlja ako ti je cilj samo da sacekas kraj. `MPI_Wait` sam blokira dok se operacija ne zavrsi.

---

## Slajd 21 - Primer neblokirajuceg I/O

Kod:

```c
MPI_Request request;

MPI_File_iwrite_at(fh, offset, buf, count, datatype, &request);

for (i = 0; i < 1000; i++) {
    /* neko izracunavanje */
}

MPI_Wait(&request, &status);
```

Ideja:

1. pokrenes upis
2. dok I/O sistem radi upis, CPU radi neko racunanje
3. pre nego sto nastavis dalje i pre nego sto diras rezultat/bafer/fajl kao da je zavrseno, pozoves `MPI_Wait`

Kod citanja je ovo posebno logicno kada mozes da unapred pokrenes citanje podataka koji ce ti trebati kasnije. Dok se citaju, radis obradu prethodnog dela.

Za zadatke: ako nema zahteva za neblokirajuci I/O, ne komplikuj. Koristi blokirajuce funkcije.

---

## Slajd 22 - Split collective I/O

Split collective je neblokirajuca forma grupnih I/O operacija.

Umesto jednog poziva:

```c
MPI_File_write_all(fh, buf, count, datatype, &status);
```

imas pocetak i kraj:

```c
MPI_File_write_all_begin(fh, buf, count, datatype);

/* neki koristan rad */

MPI_File_write_all_end(fh, buf, &status);
```

Svi procesi moraju da pozovu `begin`, i svi moraju da pozovu `end`.

Ogranicenje sa slajda: nad jednim fajlom u jednom trenutku moze biti aktivna samo jedna split collective I/O operacija.

Tvoja beleska: do sada nije padalo na ispitu. Zato ovo pamti kao koncept, ali za prakticne zadatke prvo resavaj sa `MPI_File_write_all` ili `MPI_File_read_all`.

---

## Slajd 23 - Deljeni pokazivac na fajl

Do sada su postojala dva nacina:

1. individualni file pointer

```c
MPI_File_seek(fh, offset, MPI_SEEK_SET);
MPI_File_write(fh, buf, count, MPI_INT, &status);
```

2. eksplicitni pomeraj

```c
MPI_File_write_at(fh, offset, buf, count, MPI_INT, &status);
```

Treca mogucnost je deljeni pokazivac na fajl. Svi procesi dele jednu trenutnu poziciju pokazivaca.

Funkcije:

```c
MPI_File_write_shared(fh, buf, count, datatype, &status);
MPI_File_read_shared(fh, buf, count, datatype, &status);
MPI_File_seek_shared(fh, offset, whence);
```

Ako proces P0 upise 100 integera preko shared pointera, shared pointer se pomeri za tih 100 integera. Sledeci proces koji pise shared funkcijom nastavlja od nove pozicije.

Kada se koristi? Kada ti nije bitno koji proces dobija tacno koji deo fajla, nego samo da se podaci nadovezuju.

Tipican primer: log fajl.

---

## Slajd 24 - Deljeni pokazivac - nastavak

Nakon svakog shared citanja/upisa, pozicija deljenog pokazivaca se azurira.

Ako tri procesa pozovu:

```c
MPI_File_write_shared(fh, buf, 100, MPI_INT, &status);
```

svaki ce upisati 100 integera, a shared pointer ce se pomeriti posle svakog upisa.

Ali redosled nije odredjen, osim ako koristis ordered funkcije.

Svi procesi moraju imati isti `file view`. To je logicno: ako delimo jedan pokazivac, svi moraju da imaju isto razumevanje sta znaci trenutna pozicija.

`MPI_File_seek_shared` pomera deljeni pokazivac:

```c
MPI_File_seek_shared(fh, 0, MPI_SEEK_SET);
```

---

## Slajd 25 - Deljeni pokazivac - primer

Primer:

```c
#include "mpi.h"

int main(int argc, char *argv[])
{
    int buf[1000];
    MPI_File fh;

    MPI_Init(&argc, &argv);

    MPI_File_open(MPI_COMM_WORLD, "filename",
                  MPI_MODE_CREATE | MPI_MODE_WRONLY,
                  MPI_INFO_NULL, &fh);

    MPI_File_write_shared(fh, buf, 1000, MPI_INT,
                          MPI_STATUS_IGNORE);

    MPI_File_close(&fh);
    MPI_Finalize();
}
```

Svi procesi upisuju log podatke. Redosled upisa nije vazan.

Ako redosled jeste vazan, nemoj koristiti obican `write_shared`, nego gledaj `write_ordered`.

Za ispit:

- `write_shared` = zajednicki pokazivac, redosled nije garantovan po ranku
- `write_ordered` = kolektivno, upis ide po ranku procesa

---

## Slajd 26 - Deljeni pokazivac - nastavak

Neblokirajuce varijante shared funkcija:

```c
MPI_File_iwrite_shared(fh, buf, count, datatype, &request);
MPI_File_iread_shared(fh, buf, count, datatype, &request);
```

Ovo su shared + nonblocking. Koriste `MPI_Request` i zavrsavaju se preko `MPI_Wait` ili `MPI_Test`.

Grupne/ordered operacije sa deljenim pokazivacem:

```c
MPI_File_read_ordered(fh, buf, count, datatype, &status);
MPI_File_write_ordered(fh, buf, count, datatype, &status);
```

Kod ordered funkcija citanje/upis ide redom po ranku.

Primer:

```text
rank 0 upise svoj blok
rank 1 upise svoj blok
rank 2 upise svoj blok
...
```

`read_ordered/write_ordered` su kolektivne funkcije. Svi procesi u grupi moraju da ih pozovu.

Bitna ispravka za razumevanje: `iwrite_shared/iread_shared` nisu isto sto i `write_ordered/read_ordered`. Prve su neblokirajuce shared operacije, druge su kolektivne ordered operacije.

---

## Slajd 27 - Konzistentnost

Konzistentnost nije problem u dva laka slucaja:

1. svi procesi samo citaju iz fajla
2. svaki proces pristupa posebnom fajlu

Problem nastaje kada vise procesa pristupa istom fajlu i bar jedan proces pise.

Ako procesi pristupaju razlicitim delovima fajla bez preklapanja, MPI garantuje da proces moze procitati ono sto je sam upisao bez dodatne sinhronizacije.

Primer bez preklapanja:

```text
P0 pise bajtove 0-99
P1 pise bajtove 100-199
```

Ovo je bezbedno jer ne diraju isti deo fajla.

Problematicno:

```text
P0 pise bajtove 0-99
P1 cita bajtove 0-99 odmah nakon toga
```

Ako nije jasno zavrseno i vidljivo, P1 mozda ne procita ono sto ocekujes.

---

## Slajd 28 - Konzistentnost - atomicity

Drugi primer: svaki proces mora da procita podatke koje je upravo upisao drugi proces. Pristupi se preklapaju.

U tom slucaju MPI ne garantuje konzistentnost automatski. Imas tri opcije.

Opcija 1: atomicity.

```c
MPI_File_set_atomicity(fh, 1);
```

Poziv pre upisa trazi atomicni rezim. To pomaze da se preklopljeni upisi/citanja vide konzistentnije.

Ali sama atomicnost nije isto sto i "svi su zavrsili". Zato se koristi i:

```c
MPI_Barrier(MPI_COMM_WORLD);
```

Barijera osigurava da svi procesi dodju do tacke posle upisa pre nego sto neko krene da cita.

Vazna ispravka iz beleski: `MPI_File_set_atomicity` nije isto sto i `flush` u C-u. Za eksplicitno sinhronizovanje fajla postoji `MPI_File_sync`. Atomicity je vise o pravilima vidljivosti i nedeljivosti pristupa kada se pristupi preklapaju. Moze usporiti program.

Sablon:

```c
MPI_File_set_atomicity(fh, 1);

/* upisi */

MPI_Barrier(MPI_COMM_WORLD);

/* citanja */
```

Za ispit: navedi i posledicu - atomicity moze da smanji performanse.

---

## Slajd 29 - Konzistentnost - zatvori/otvori i sekvence

Opcija 2: zatvoriti, pa ponovo otvoriti fajl.

Sablon:

```c
/* upis */
MPI_File_close(&fh);

MPI_Barrier(MPI_COMM_WORLD);

MPI_File_open(MPI_COMM_WORLD, "file",
              MPI_MODE_RDONLY, MPI_INFO_NULL, &fh);

/* citanje */
```

Ovo je najlaksi pristup za razumevanje i cesto najlaksi za studentske zadatke.

Zasto barijera? Da ne bi jedan proces otvorio fajl za citanje dok drugi jos nije zavrsio upis i zatvaranje.

Opcija 3: obezbediti da nijedna sekvenca upisa nije konkurentna sa sekvencom upisa/citanja drugog procesa.

Sekvenca je grupa operacija izmedju:

- `MPI_File_open`
- `MPI_File_close`
- `MPI_File_sync`

Ideja: razdvoji faze programa tako da se opasni upisi i citanja ne desavaju konkurentno.

Za zadatke, najlaksi nacin:

```text
faza upisa
close
barrier
open
faza citanja
```

Ako profesor eksplicitno trazi performanse i ne zeli zatvaranje/otvaranje, onda gledas `MPI_File_sync` ili atomicity.

---

## Slajd 30 - Treca opcija konzistentnosti - primer

Slajd prikazuje dva procesa i `MPI_File_sync`.

`MPI_File_sync(fh)` sinhronizuje fajl, ali je grupna operacija. To znaci: ako je fajl otvoren preko `MPI_COMM_WORLD`, svi procesi iz tog komunikatora moraju pozvati `MPI_File_sync`.

Primer sa slajda, pojednostavljeno:

Proces 0:

```c
MPI_File_open(MPI_COMM_WORLD, "file", ..., &fh1);
MPI_File_write_at(fh1, 0, buf, 100, MPI_BYTE, ...);
MPI_File_sync(fh1);

MPI_Barrier(MPI_COMM_WORLD);

MPI_File_sync(fh1); /* potrebno jer je sync kolektivan */
MPI_File_sync(fh1); /* potrebno jer drugi proces kasnije sinhronizuje */

MPI_Barrier(MPI_COMM_WORLD);

MPI_File_sync(fh1);
MPI_File_read_at(fh1, 100, buf, 100, MPI_BYTE, ...);
MPI_File_close(&fh1);
```

Proces 1:

```c
MPI_File_open(MPI_COMM_WORLD, "file", ..., &fh2);
MPI_File_sync(fh2); /* potrebno jer proces 0 sinhronizuje */

MPI_Barrier(MPI_COMM_WORLD);

MPI_File_sync(fh2);
MPI_File_write_at(fh2, 100, buf, 100, MPI_BYTE, ...);
MPI_File_sync(fh2);

MPI_Barrier(MPI_COMM_WORLD);

MPI_File_sync(fh2); /* potrebno jer je sync kolektivan */
MPI_File_read_at(fh2, 0, buf, 100, MPI_BYTE, ...);
MPI_File_close(&fh2);
```

Poenta nije da ucis ovaj kod napamet. Poenta:

- `MPI_File_sync` je kolektivna operacija
- ako jedan proces mora da je pozove, moraju i ostali iz komunikatora
- sync se koristi da razdvoji I/O sekvence
- ovaj pristup nije moguc kod grupnih I/O operacija iz slajda, jer se kolektivne I/O operacije same moraju organizovati kao grupa

Za zadatak: ako nisi siguran i nije naglasena optimizacija konzistentnosti, lakse je zatvori/otvori + barijera.

---

## Slajd 31 - Postizanje visokih MPI I/O performansi

Ovo je uvod u poslednji deo: isti podaci se mogu procitati/upisati na vise nacina, ali performanse nisu iste.

MPI I/O performanse zavise od toga:

- koliko I/O poziva pravis
- da li su pozivi mali ili veliki
- da li su nezavisni ili kolektivni
- da li koristis `file view`
- da li MPI moze da optimizuje zahteve svih procesa zajedno

---

## Slajd 32 - Kako uticati na MPI I/O performanse

Primer je 2D polje distribuirano na 16 procesa.

Svaki proces ima svoje lokalno podpolje. U memoriji procesa to lokalno podpolje moze biti lepo kontinualno, ali u globalnom fajlu njegovi redovi mogu biti razdvojeni zbog redova drugih procesa.

Zato isti pristup mozes napisati na vise nacina:

- puno pojedinacnih citanja po redovima
- kolektivna citanja po redovima
- jedan izvedeni tip koji opisuje celu podmatricu
- izvedeni tip + kolektivna operacija

To vodi do 4 nivoa pristupa.

---

## Slajd 33 - Cetiri nivoa pristupa

Nivo 0:

Procesi rade Unix stil pristupa. Za svaki red lokalne matrice rade poseban `seek + read`.

```text
mnogo malih nezavisnih operacija
```

Nivo 1:

Isto kao nivo 0, ali koriste grupne operacije.

```text
mnogo malih kolektivnih operacija
```

Bolje od nivoa 0, ali i dalje ima puno poziva.

Nivo 2:

Svaki proces kreira izvedeni tip i `file view`, pa radi jednu nezavisnu I/O operaciju.

```text
file view + jedan read/write
```

Nivo 3:

Isto kao nivo 2, ali koristi grupnu I/O operaciju.

```text
file view + read_all/write_all
```

Ovo je najbolji stil iz perspektive MPI I/O.

---

## Slajd 34 - Cetiri nivoa pristupa - kod

Nivo 0:

```c
MPI_File_open(..., "filename", ..., &fh);

for (i = 0; i < n_local_rows; i++) {
    MPI_File_seek(fh, ...);
    MPI_File_read(fh, row[i], ...);
}

MPI_File_close(&fh);
```

Problem: mnogo `seek/read` poziva.

Nivo 1:

```c
MPI_File_open(MPI_COMM_WORLD, "filename", ..., &fh);

for (i = 0; i < n_local_rows; i++) {
    MPI_File_seek(fh, ...);
    MPI_File_read_all(fh, row[i], ...);
}

MPI_File_close(&fh);
```

Bolje jer je kolektivno, ali i dalje ima puno poziva po redovima.

Nivo 2:

```c
MPI_Type_create_subarray(..., &subarray, ...);
MPI_Type_commit(&subarray);

MPI_File_open(..., "filename", ..., &fh);
MPI_File_set_view(fh, ..., subarray, ...);
MPI_File_read(fh, local_array, ...);
MPI_File_close(&fh);
```

Bolje jer jedan tip opisuje celu lokalnu podmatricu.

Nivo 3:

```c
MPI_Type_create_subarray(..., &subarray, ...);
MPI_Type_commit(&subarray);

MPI_File_open(MPI_COMM_WORLD, "filename", ..., &fh);
MPI_File_set_view(fh, ..., subarray, ...);
MPI_File_read_all(fh, local_array, ...);
MPI_File_close(&fh);
```

Najbolje: `subarray` opisuje raspored, a `read_all` daje MPI-ju sansu za kolektivnu optimizaciju.

Za ispit: ako pitaju "koji nivo treba koristiti", odgovor je nivo 3.

---

## Slajd 35 - Kako postici visoke performanse

Od nivoa 0 do nivoa 3 raste kolicina podataka po zahtevu.

Veci zahtevi su uglavnom efikasniji od mnogo malih zahteva, jer se manje puta placa I/O overhead.

Zakljucak:

```text
tezi nivou 3:
izvedeni tip + file view + kolektivna operacija
```

Ali u zadatku ne forsiraj nivo 3 ako se izricito trazi nesto drugo. Ako zadatak kaze "koristiti pojedinacne pokazivace", onda koristis `seek + write`. Ako kaze "eksplicitni pomeraj", koristis `_at`.

Prvo citaj tekst zadatka, pa optimizuj.

---

## Slajd 36 - Zakljucna razmatranja

MPI I/O resava nekoliko problema:

- visoko kasnjenje I/O sistema
- kooperativni pristup jednom fajlu
- mali i cesti pristupi fajl sistemu
- nekontinualni raspored podataka
- potreba da se I/O preklopi sa racunanjem

Alati:

- neblokirajuce I/O operacije
- grupne operacije
- izvedeni tipovi
- `file view`

Za zadatke:

1. Iz teksta zadatka izvuci raspored podataka.
2. Odredi da li je pristup kontinualan ili nekontinualan.
3. Ako je kontinualan, dovoljan je offset.
4. Ako je nekontinualan, treba izvedeni tip + `file view`.
5. Ako se trazi efikasnost, koristi `_all`.
6. Ako se trazi eksplicitni pomeraj, koristi `_at`.
7. Ako se trazi pojedinacni pokazivac, koristi `seek + read/write`.

---

# Ultimativni sablon za resavanje MPI I/O zadataka

## Korak 1 - prepoznaj sta zadatak trazi

Ako pise "koristiti pojedinacne pokazivace":

```c
MPI_File_seek(fh, offset, MPI_SEEK_SET);
MPI_File_write(fh, buf, count, MPI_INT, &status);
```

Ako pise "eksplicitni pomeraj":

```c
MPI_File_write_at(fh, offset, buf, count, MPI_INT, &status);
```

Ako pise "sto efikasnije" ili ima nekontinualan raspored:

```c
MPI_Type_vector(..., &filetype);
MPI_Type_commit(&filetype);
MPI_File_set_view(fh, disp, MPI_INT, filetype, "native", MPI_INFO_NULL);
MPI_File_write_all(fh, buf, count, MPI_INT, &status);
```

Ako je matrica/podmatrica:

```c
MPI_Type_create_subarray(..., &filetype);
MPI_File_set_view(fh, 0, etype, filetype, "native", MPI_INFO_NULL);
MPI_File_write_all(...);
```

## Korak 2 - odredi jedinice

Najcesca greska:

- offset/disp: bajtovi
- count u read/write: broj elemenata tipa `datatype`
- stride u `MPI_Type_vector`: broj elemenata tipa `oldtype`

Primer:

```c
offset = rank * 10 * sizeof(int); // bajtovi
count = 10;                       // 10 komada MPI_INT
datatype = MPI_INT;
```

## Korak 3 - kada pravis vector

Koristi:

```c
MPI_Type_vector(count, blocklength, stride, oldtype, &newtype);
```

Pitanja:

1. Koliko blokova vidi jedan proces? To je `count`.
2. Koliko elemenata ima jedan blok? To je `blocklength`.
3. Koliko elemenata ima od pocetka jednog mog bloka do pocetka sledeceg mog bloka? To je `stride`.

Za round-robin:

```c
count = nints / INTS_PER_BLK;
blocklength = INTS_PER_BLK;
stride = INTS_PER_BLK * nprocs;
```

Za lab treci korak, kada se pise po 2 broja iz svakog procesa:

```c
BLOCK = 2;
count = 10 / BLOCK;
blocklength = BLOCK;
stride = BLOCK * nprocs;
```

## Korak 4 - kada postavljas view

```c
MPI_File_set_view(fh, disp, etype, filetype, "native", MPI_INFO_NULL);
```

`disp` je gde pocinje prvi vidljivi blok tog procesa.

Ako proces `rank` pocinje posle `rank` blokova, a svaki blok ima `BLOCK` integera:

```c
disp = rank * BLOCK * sizeof(int);
```

`etype` je najcesce:

```c
MPI_INT
```

`filetype` je izvedeni tip koji opisuje ponavljanje i preskakanje.

## Korak 5 - citanje/upis posle view-a

Posle `MPI_File_set_view`, proces pise kao da je njegov pogled kontinualan.

Ako ima 10 integera u `buf`:

```c
MPI_File_write_all(fh, buf, 10, MPI_INT, &status);
```

MPI ce ih rasporediti kroz stvarni fajl prema `filetype`.

---

# Laboratorijska vezba - kompletan obrazac

Zadatak iz `priprema lab.md`:

1. Svaki proces upisuje 10 integera u prvi fajl.
2. Svaki proces ponovo cita svojih 10 integera, ali funkcijama sa eksplicitnim pomerajem.
3. Svaki proces upisuje procitane podatke u novi fajl, efikasno, prema slici.

## Deo I - upis pojedinacnim pokazivacem

Procesi:

```text
P0: 0 1 2 3 4 5 6 7 8 9
P1: 10 11 12 13 14 15 16 17 18 19
P2: 20 21 22 23 24 25 26 27 28 29
P3: 30 31 32 33 34 35 36 37 38 39
```

Kod:

```c
int buf[10];

for (int i = 0; i < 10; i++) {
    buf[i] = rank * 10 + i;
}

MPI_File_open(MPI_COMM_WORLD, "ulaz.bin",
              MPI_MODE_CREATE | MPI_MODE_WRONLY,
              MPI_INFO_NULL, &fh);

MPI_File_seek(fh, rank * 10 * sizeof(int), MPI_SEEK_SET);
MPI_File_write(fh, buf, 10, MPI_INT, &status);

MPI_File_close(&fh);
```

Zasto offset:

```c
rank * 10 * sizeof(int)
```

jer svaki proces pre svog dela preskace `rank` grupa od po 10 integera.

## Deo II - citanje eksplicitnim pomerajem

```c
int read_buf[10];

MPI_File_open(MPI_COMM_WORLD, "ulaz.bin",
              MPI_MODE_RDONLY,
              MPI_INFO_NULL, &fh);

MPI_File_read_at(fh,
                 rank * 10 * sizeof(int),
                 read_buf,
                 10,
                 MPI_INT,
                 &status);

MPI_File_close(&fh);
```

Provera:

```c
int ok = 1;

for (int i = 0; i < 10; i++) {
    if (read_buf[i] != rank * 10 + i) {
        ok = 0;
    }
}
```

Ako hoces zajednicku proveru:

```c
int all_ok;
MPI_Allreduce(&ok, &all_ok, 1, MPI_INT, MPI_MIN, MPI_COMM_WORLD);

if (rank == 0 && all_ok) {
    printf("Citanje je pravilno izvrseno.\n");
}
```

## Deo III - efikasan upis u novi fajl

Slika trazi ovakav raspored za 4 procesa:

```text
0 1 | 10 11 | 20 21 | 30 31 | 2 3 | 12 13 | 22 23 | 32 33 | ... | 8 9 | 18 19 | 28 29 | 38 39
```

Znaci svaki proces svoje lokalne podatke deli na blokove od po 2 integera.

Za 4 procesa:

- P0 pise `0 1`, zatim kasnije `2 3`, zatim `4 5`, ...
- P1 pise `10 11`, zatim `12 13`, ...
- P2 pise `20 21`, zatim `22 23`, ...
- P3 pise `30 31`, zatim `32 33`, ...

Ovo je round-robin po blokovima od 2 integera.

Sablon:

```c
#define LOCAL_COUNT 10
#define BLOCK 2

MPI_Datatype filetype;
MPI_Offset disp;

MPI_Type_vector(LOCAL_COUNT / BLOCK,
                BLOCK,
                BLOCK * nprocs,
                MPI_INT,
                &filetype);

MPI_Type_commit(&filetype);

disp = rank * BLOCK * sizeof(int);

MPI_File_open(MPI_COMM_WORLD, "izlaz.bin",
              MPI_MODE_CREATE | MPI_MODE_WRONLY,
              MPI_INFO_NULL, &fh);

MPI_File_set_view(fh, disp, MPI_INT, filetype,
                  "native", MPI_INFO_NULL);

MPI_File_write_all(fh, read_buf, LOCAL_COUNT, MPI_INT, &status);

MPI_File_close(&fh);
MPI_Type_free(&filetype);
```

Objasnjenje:

```c
MPI_Type_vector(5, 2, 8, MPI_INT, &filetype);
```

ako je `nprocs = 4`.

Znaci:

- 5 blokova po procesu, jer `10 / 2 = 5`
- svaki blok ima 2 integera
- od pocetka jednog bloka istog procesa do pocetka sledeceg ima `2 * 4 = 8` integera

`disp`:

```c
rank * 2 * sizeof(int)
```

jer:

- P0 pocinje od pozicije 0
- P1 pocinje posle 2 integera
- P2 pocinje posle 4 integera
- P3 pocinje posle 6 integera

Zasto `write_all`?

Zato sto zadatak kaze da poslednje upisivanje treba uciniti sto efikasnijim. Efikasno u ovoj temi znaci:

```text
file view + izvedeni tip + kolektivni upis
```

## Cela struktura lab programa

```c
#include <stdio.h>
#include <stdlib.h>
#include "mpi.h"

#define LOCAL_COUNT 10
#define BLOCK 2

int main(int argc, char **argv)
{
    int rank, nprocs;
    int buf[LOCAL_COUNT];
    int read_buf[LOCAL_COUNT];
    int ok = 1, all_ok;
    MPI_File fh;
    MPI_Status status;
    MPI_Datatype filetype;
    MPI_Offset offset, disp;

    MPI_Init(&argc, &argv);
    MPI_Comm_rank(MPI_COMM_WORLD, &rank);
    MPI_Comm_size(MPI_COMM_WORLD, &nprocs);

    for (int i = 0; i < LOCAL_COUNT; i++) {
        buf[i] = rank * LOCAL_COUNT + i;
    }

    /* I) Upis pojedinacnim pokazivacem */
    MPI_File_open(MPI_COMM_WORLD, "ulaz.bin",
                  MPI_MODE_CREATE | MPI_MODE_WRONLY,
                  MPI_INFO_NULL, &fh);

    offset = rank * LOCAL_COUNT * sizeof(int);
    MPI_File_seek(fh, offset, MPI_SEEK_SET);
    MPI_File_write(fh, buf, LOCAL_COUNT, MPI_INT, &status);
    MPI_File_close(&fh);

    MPI_Barrier(MPI_COMM_WORLD);

    /* II) Citanje eksplicitnim pomerajem */
    MPI_File_open(MPI_COMM_WORLD, "ulaz.bin",
                  MPI_MODE_RDONLY,
                  MPI_INFO_NULL, &fh);

    MPI_File_read_at(fh, offset, read_buf,
                     LOCAL_COUNT, MPI_INT, &status);

    MPI_File_close(&fh);

    for (int i = 0; i < LOCAL_COUNT; i++) {
        if (read_buf[i] != rank * LOCAL_COUNT + i) {
            ok = 0;
        }
    }

    MPI_Allreduce(&ok, &all_ok, 1, MPI_INT, MPI_MIN, MPI_COMM_WORLD);

    if (rank == 0) {
        printf("Provera citanja: %s\n", all_ok ? "OK" : "GRESKA");
    }

    /* III) Efikasan round-robin upis po blokovima od 2 integera */
    MPI_Type_vector(LOCAL_COUNT / BLOCK,
                    BLOCK,
                    BLOCK * nprocs,
                    MPI_INT,
                    &filetype);

    MPI_Type_commit(&filetype);

    disp = rank * BLOCK * sizeof(int);

    MPI_File_open(MPI_COMM_WORLD, "izlaz.bin",
                  MPI_MODE_CREATE | MPI_MODE_WRONLY,
                  MPI_INFO_NULL, &fh);

    MPI_File_set_view(fh, disp, MPI_INT, filetype,
                      "native", MPI_INFO_NULL);

    MPI_File_write_all(fh, read_buf,
                       LOCAL_COUNT, MPI_INT, &status);

    MPI_File_close(&fh);
    MPI_Type_free(&filetype);

    MPI_Finalize();
    return 0;
}
```

Napomene:

- `MPI_Barrier` izmedju I i II je prakticna zastita da svi zavrse upis pre citanja.
- `offset` u I i II je isti jer citamo tacno ono sto smo upisali.
- U III se ne koristi `seek`; `file view` preuzima raspored.
- Ako zadatak kaze tacno 4 procesa, ovaj kod radi za 4. Ako zelis strogo proveru, mozes dodati:

```c
if (nprocs != 4) {
    if (rank == 0) printf("Pokrenuti sa 4 procesa.\n");
    MPI_Finalize();
    return 1;
}
```

---

# Mini-podsetnik funkcija za zadatke

Otvaranje:

```c
MPI_File_open(MPI_COMM_WORLD, "file.bin",
              MPI_MODE_CREATE | MPI_MODE_WRONLY,
              MPI_INFO_NULL, &fh);
```

Pojedinacni pokazivac:

```c
MPI_File_seek(fh, offset, MPI_SEEK_SET);
MPI_File_write(fh, buf, count, MPI_INT, &status);
```

Eksplicitni pomeraj:

```c
MPI_File_write_at(fh, offset, buf, count, MPI_INT, &status);
```

File view:

```c
MPI_File_set_view(fh, disp, MPI_INT, filetype,
                  "native", MPI_INFO_NULL);
```

Grupni upis:

```c
MPI_File_write_all(fh, buf, count, MPI_INT, &status);
```

Vector:

```c
MPI_Type_vector(num_blocks, block_len, stride,
                MPI_INT, &filetype);
MPI_Type_commit(&filetype);
```

Subarray:

```c
MPI_Type_create_subarray(2, gsizes, lsizes, starts,
                         MPI_ORDER_C, MPI_FLOAT, &filetype);
MPI_Type_commit(&filetype);
```

Neblokirajuci I/O:

```c
MPI_Request request;
MPI_File_iwrite_at(fh, offset, buf, count, MPI_INT, &request);
/* koristan rad */
MPI_Wait(&request, &status);
```

Shared pointer:

```c
MPI_File_write_shared(fh, buf, count, MPI_INT, &status);
```

Ordered shared:

```c
MPI_File_write_ordered(fh, buf, count, MPI_INT, &status);
```

Konzistentnost, najlaksi studentski pristup:

```c
/* write phase */
MPI_File_close(&fh);
MPI_Barrier(MPI_COMM_WORLD);
MPI_File_open(MPI_COMM_WORLD, "file.bin",
              MPI_MODE_RDONLY, MPI_INFO_NULL, &fh);
/* read phase */
```

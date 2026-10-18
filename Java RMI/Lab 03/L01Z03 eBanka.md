DISTRIBUIRANI SISTEMI – LABORATORIJSKE VEŽBE – JAVA RMI

Zadatak 3

eBanka

Opis:

Korišćenjem  Java  RMI  tehnologije  implementirati  sistem  koji  omogućava  korisnicima
prebacivanje novca sa dinarskog na devizni račun  i obrnuto po trenutno važećem  kursu
putem eBanka korisničkog servisa.

Sistem treba da sadrži sledeće klase sa odgovarajućim metodama:

•  Server - klasa koja sadrži logiku izvršavanja serverskog dela sistema.
•  Klijent - klasa koja sadrži logiku izvršavanja klijentskog dela sistema.
•  Korisnik - interfejs klasa koja sadrži podatke jednog korisnika u banci.
o  Stanje  vratiStanje () - metoda koja vraća stanje datog korisnika.
o  void  transferDinarskiNaDevizni(float  iznos,  float  kurs)  -  metoda  koja

prebacuje zadati iznos sa dinarskog na devizni račun.

o  void  transferDevizniNaDinasrski(float  iznos,  float  kurs)  -  metoda  koja

prebacuje zadati iznos sa deviznog na dinarski račun.

•  KorisnikImpl - klasa koja implementira interfejs klase Korisnik
float

o  KorisnikImpl(String

iznosDevizni)  -
konstruktor  koji  uzima  jedinstveni  broj  korisnika,  inicijalni  iznos  na
dinarskom racunu i inicijalni iznos na deviznom racunu

iznosDinarski,

float

jbk,

•  Stanje  -  interfejs  klase  koja  treba  da  sadrći  podatke  sa  iznosima  na  dinarskom  i

deviznom računu.

o

o

float    vratiDinarskiIznos()  -  metoda  koja  vraća  iznos  na  dinarskom
računu.
float  vratiDevizniIznos() - metoda koja vraća iznos na deviznom računu.

•  StanjeImpl - klasa koja implementira interfejs klase Stanje

o  StanjeImpl(float  iznosDinarski,  float  iznosDevizni)  -  konstruktor  koji

uzima iznos na dinarskom racunu i iznos na deviznom racunu

•  EBanka - interfejs klasa koja sadrži (bazu) niz korisnika.

o  Korisnik    vratiKorisnika(String  jbk)  -  metoda  koja  vraća  referencu  na

korisnika za zadatim identifikatorom.

•  EBankaImpl - klasa koja implementira interfejs klase Banka

Sistem treba da prati sledeći scenario izvršavanja:

1.  Vrši  se  startovanje  servera  na  kome  se  kreira  instanca  klase  EBankaImpl.  U  okviru
klase EBankaImpl se inicijalizuje niz od deset objekata tipa  Korisnik sa  odgovarajucim
jedinstvenim brojevima korisnika, inicijalnim iznosima na dinarskom i deviznom računu.

DISTRIBUIRANI SISTEMI – LABORATORIJSKE VEŽBE – JAVA RMI

2. Vrši se startovanje klijenta u okviru koga se vrši konktovanje na server i referenciranje
objekta tipa EBanka.

3. Na klijentskoj strani se u okviru komandne linije ispisuje meni sa opcijama:
a) Transfer sa dinarskog na devizni račun
b) Transfer sa deviznog na dinarski račun
c) Provera stanja
d) Kraj

4. Na klijentskoj strani se u okviru komandne linije unosi odgovarajuca opcija iz menija
(npr. a) nakon čega se u okviru komandne linije zahteva unos broja korisnika kao i iznosa
za  transfer  praćeno  porukama  "Unesite  jedinstveni  broj  korisnika:  "  i  "Unesite  iznos:".
Korišćenjem  metode  <<EBanka>>::vratiKorisnika()  se  prvo  preuzima  referenca  na
odgovarajućeg
<<Korisnik>>::
se
transferDinarskiNaDevizni() vrši odgovarajući transfer novčanih sredstava.                .

pozivom  metoda

korisnika

zatim

a

5. Ukoliko se iz menija odabere opcija c) u okviru komandne linije se zahteva unos broja
korisnika koji se prosleđuje pozivu metode << Korisnik >>:: vratiStanje() nakon čega se
na podaci o stanju korisnika štampaju u komandnoj liniji.

6. Postupak se ponavlja sve dok korinik ne izabere opciju Kraj iz menija.

Primer izlaza na klijentskoj strani:

Dobrodosli u eBank korisnicki servis. Za nastavak izaberite opciju:
a) Transfer sa dinarskog na devizni račun
b) Transfer sa deviznog na dinarski račun
c) Provera stanja
d) Kraj

>a
Izbrali ste opciju za Transfer sa dinarskog na devizni račun:
Unesite jedinstveni broj korisnika:
>JBK123456
Unesite iznos:
>100

Dobrodosli u eBank korisnicki servis. Za nastavak izaberite opciju:
a) Transfer sa dinarskog na devizni račun
b) Transfer sa deviznog na dinarski račun
c) Provera stanja
d) Kraj

>c

Izbrali ste opciju za proveru stanja:

DISTRIBUIRANI SISTEMI – LABORATORIJSKE VEŽBE – JAVA RMI

Unesite jedinstveni broj korisnika:

>JBK123456
Vase stanje je:
Iznos na dinarskom računu: 120
Iznos na deviznom računu: 5000
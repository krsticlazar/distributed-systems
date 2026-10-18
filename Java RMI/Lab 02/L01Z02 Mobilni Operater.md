DISTRIBUIRANI SISTEMI – LABORATORIJSKE VEŽBE – JAVA RMI

Zadatak 2

Mobilni Operater

Opis:

Korišćenjem  Java  RMI  tehnologije  implementirati  sistem  koji  omogućava  korisnicima
kupovinu  dodatnih  minuta,  poruka  i  interneta,  kao  i  pregled  stanja  kod  mobilnog
operatera.

Sistem treba da sadrži sledeće klase sa odgovarajućim metodama:

•  Server - klasa koja sadrži logiku izvršavanja serverskog dela sistema.
•  Klijent - klasa koja sadrži logiku izvršavanja klijentskog dela sistema.
•  Korisnik - interfejs klasa koja sadrži informacije relevantne za jednog korisnika.

o  void  uplatiMinute(int  minuti)  -  metoda  koja  dodaje  minute  korisniku  sa

zadatim brojem i povecava racun u skladu sa tarifom.

o  void uplatiPoruke (int poruke) - metoda koja dodaje poruke korisniku sa

zadatim brojem i povecava racun u skladu sa tarifom.

o  void  uplatiInternet  (int  internet)  -  metoda  koja  dodaje  internet  korisniku

sa yadatim brojem i povecava racun u skladu sa tarifom.

o  Stanje vratiStanje () - metoda koja vraća trenutno stanje korisnika.

•  KorisnikImpl - klasa koja implementira interfejs klase Korisnik

o  KorisnikImpl(String  broj,  int  minuti,  int  poruke,  int  internet,  int
minutiTarifa, int porukeTarifa, int internetTarifa) - konstruktor koji uzima
broj telefona korisnika, pocetni broj minuta, pocetni broj poruka, pocetni
broj megabajta interneta, tarifu dinara po  minuti, tarifu dinara po  poruci,
tarifu dinara po megabajtu interneta

•  Stanje - interfejs klasa koja sadrži jedno pitanje i ponuđene odgovore.
int  vratiMinute() - metoda koja vraća broj preostalih minuta.
int  vratiPoruke() - metoda koja vraća broj preostalih poruka.
int vratiInternet() - metoda koja vraća broj preostalih megabajta interneta.
float vratiRačun () - metoda koja vraća iznos treuntnog računa.

o
o
o
o

•  StanjeImpl - klasa koja implementira interfejs klase Korisnik

o  StanjeImpl(String  broj, int  minuti,  int  poruke,  int  internet,  float  račun)  -
konstruktor  koji  uzima  broj  telefona  korisnika,  pocetni  broj  minuta,
pocetni broj poruka i trenutni iznos računa,

•  Operater - interfejs klasa koja sadrži (bazu) niz korisnika.

o  Korisnik  vratiKorisnika  (String  broj)  -  metoda  koja  vraća  referencu  na

objekat tipa Korisnik za zadati broj telefona.
•  OperaterImpl - klasa koja implementira interfejs klase Operater

DISTRIBUIRANI SISTEMI – LABORATORIJSKE VEŽBE – JAVA RMI

Sistem treba da prati sledeći scenario izvršavanja:

1.  Vrši  se  startovanje  servera  na  kome  se  kreira  instanca  klase  OperaterImpl.  U  okviru
klase OperaterImpl se inicijalizuje niz od deset objekata tipa Korisnik sa odgovarajucim
brojevima telefona, minuta, poruka, interneta kao i tarifama.

2. Vrši se startovanje klijenta u okviru koga se vrši konktovanje na server i referenciranje
objekta tipa Operater.

3. Na klijentskoj strani se u okviru komandne linije ispisuje meni sa opcijama:
a) Uplata Minuta
b) Uplata Poruka
c) Uplata Interneta
d) Provera stanja
d) Kraj

4. Na klijentskoj strani se u okviru komandne linije unosi odgovarajuca opcija iz menija
(npr. a) nakon čega se u okviru komandne linije zahteva unos broja korisnika kao i broja
minuta praćeno porukama "Unesite broj telefona: " i "Unesite broj minuta:". Korišćenjem
metode  <<Operater>>::vratiKorisnika()  se  prvo  preuzima  referenca  na  odgovarajućeg
korisnika  a  zatim  se  pozivom  metoda  <<Korisnik>>::uplatiMinute()  vrši  dodavanje
minuta.               .

5. Na klijentskoj strani se poziva metod <<Korisnik>>::vratiStanje (), koja vraća Objekat
Stanje na osnovu koga se štampaju informacije o trenutnom stanju zadatog korisnika.

Primer izlaza na klijentskoj strani:

Dobrodosli u korisnicki servis mobilnog operatera. Za nastavak izaberite opciju:
a) Uplata Minuta
b) Uplata Poruka
c) Uplata Interneta
d) Provera stanja
e) Kraj

/>a
Izbrali ste opciju za uplatu dodatnih minuta:
Unesite broj telefona korisnika:
/>060123456
Unesite broj dodatnih minuta:
/>100

Dobrodosli u korisnicki servis mobilnog operatera. Za nastavak izaberite opciju:
a) Uplata Minuta
b) Uplata Poruka
c) Uplata Interneta
d) Provera stanja

DISTRIBUIRANI SISTEMI – LABORATORIJSKE VEŽBE – JAVA RMI

e) Kraj

/>d
Izbrali ste opciju za proveru stanja:
Unesite broj telefona korisnika:
>060123456
Vas racun iznosi:
500 din:
DISTRIBUIRANI SISTEMI – LABORATORIJSKE VEŽBE – JAVA RMI

Zadatak 1

Matematički Kviz

Opis:

Korišćenjem  Java  RMI  tehnologije  implementirati  sistem  koji  omogućava  korisnicima
učestvovanje u matematičkom Kvizu.

Sistem treba da sadrži sledeće klase sa odgovarajućim metodama:

•  Server - klasa koja sadrži logiku izvršavanja serverskog dela sistema.
•  Klijent - klasa koja sadrži logiku izvršavanja klijentskog dela sistema.
•  Pitanje - interfejs klasa koja sadrži jedno pitanje i ponuđene odgovore.

o  String  vratiTekst()  -  metoda  koja  vraća  trekst  pitanja  sa  ponuđenim

odgovorima

•  PitanjeImpl - klasa koja implementira interfejs klase Pitanje

o  PitanjeImpl(String  tekst,  String  a,  String  b,  String  c)  -  konstruktor  koji

uzima tekst pitanja i tri ponuđena odgovora a,b i c

•  Kviz - interfejs klasa koja sadrži niz pitanja.

o  void pocetak() - metoda koja inicijalizuje kviz i postavlja broj poena na 0 i

indeks trenutnog pitanja tako da ukazuje na prvo pitanje u nizu.

o  Pitanje  vratiPitanje()  -  metoda  koja  vraća  objekat  Pitanje  na  osnovu

internog indeksa trenutnog pitanja.

o  void odgovori(String odg) - metoda koja uzima ogovor (u formatu: a,b ili
c)  i  vrši  internu  proveru  da  li  je  odgovor  tačan  i  ukoliko  jeste  povećava
broj poena za 1.
int vratiBrojPoena() - metoda koja vraća broj poena.

o

•  KvizImpl - klasa koja implementira interfejs klase Kviz

Sistem treba da prati sledeći scenario izvršavanja:

1. Vrši se startovanje servera na kome se kreira instanca klase KvizImpl. U okviru klase
KvizImpl  se  inicijalizuje  niz  od  tri  objekata  tipa  Pitanje,  kao  i  niz  od  tri  objekata  tipa
String koji sadrže tačne odgovore.

2. Vrši se startovanje klijenta u okviru koga se vrši konktovanje na server i referenciranje
objekta tipa Kviz.

3. Na klijentskoj strani se poziva metoda <<Kviz>>::pocetak().

4. Na klijentskoj strani se inicijalizuje petlja koja se treba da se izvrši tri puta (Za svako
pitanje po jedanput).

DISTRIBUIRANI SISTEMI – LABORATORIJSKE VEŽBE – JAVA RMI

5.  Na  klijentskoj  strani  se  poziva  metod  <<Kviz>>::vratiPitanje(),  koja  vraća  Objekat
Pitanje.  Pozivom  metoda  <<Pitanje>>::vratiTekst()  vrši  se  preuzimanje  teksta  pitanja  i
ponuđenih odgovora i štampa u okviru komandne linije.

6. U okviru komandne linije se zahteva od korisnika da unese tačan odgovor nakon čega
se poziva metod <<Kviz>>::odgovori ().

7.  Nakon  odgovora  na  sva  tri  pitanja  poziva  se  metod  <<Kviz>>::vratiBrojPoena()  i
dobijena vrednost se štampa u okviru komandne linije.

Primer izlaza na klijentskoj strani:

Pitanje 1
1+1= ?
a) 1 b) 2 c) 3

Unesite odgovor:
b

Pitanje 2
2*3= ?
a) 6 b) 2 c) 1

Unesite odgovor:
a

Pitanje 3
10/2= ?
a) 1 b) 2 c) 5

Unesite odgovor:
c

Broj Poena:
3
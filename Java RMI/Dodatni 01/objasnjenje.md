# Dodatni 01 - elektronsko glasanje

## Struktura

- `Glasanje` je jedini remote objekat.
- `GlasanjeImpl` cuva glasove i birace koji su vec glasali.
- `Rezultat` je `Serializable` snapshot rezultata.
- Callback nije potreban jer klijent sam trazi rezultat.

## Kolekcije

- `Map<Integer, Integer>` povezuje kandidata i broj glasova.
- `Set<String>` cuva jedinstvene brojeve biraca koji su vec glasali.

## Za ispit

Obavezno napisati:

- `Glasanje extends Remote`;
- `Rezultat implements Serializable`;
- potpise metoda `glasaj` i `vratiRezultat`;
- tri kandidata u konstruktoru;
- proveru `Set` kolekcije pre glasanja;
- povecavanje broja glasova u `Map` kolekciji;
- racunanje mesta kandidata;
- osnovni `rebind` i `lookup`.

Ne treba pisati importe, detaljnu validaciju, kompletne `try-catch` blokove ili meni.

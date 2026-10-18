# Ispit KOL126 - pracenje cena akcija

## Struktura

- `StockService` je glavni remote servis.
- `StockServiceImpl` cuva akcije i callback reference.
- `Stock` je `Serializable`, jer klijent dobija kopije akcija za prikaz.
- `Callback` je remote interfejs koji implementira `Client`.
- `AdminClient` menja cenu kroz `StockService`.

## Tok rada

1. Server registruje servis na portu `5099`.
2. `Client` radi `lookup`, prikazuje akcije i salje callback serveru.
3. `AdminClient` poziva `changePrice`.
4. Server menja cenu i poziva `priceChanged` svih pretplacenih klijenata.

## Za ispit

Obavezno napisati:

- `StockService extends Remote`;
- `Callback extends Remote` sa metodom `priceChanged(int id, double newPrice)`;
- `Stock implements Serializable`;
- dve akcije u konstruktoru `StockServiceImpl`;
- listu akcija i listu callback objekata;
- logiku `changePrice` i callback poziv;
- registry i `rebind` na portu `5099`;
- `Client` koji prikazuje akcije i registruje callback;
- `AdminClient` koji menja cenu.

Ne treba pisati importe, validaciju, kompletne `try-catch` blokove, unos ili kod za cekanje klijenta.

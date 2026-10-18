# Dodatni 02 - digitalna biblioteka

## Struktura

- `Library` je glavni remote servis.
- `Book` je remote objekat jer klijent menja stvarno stanje knjige na serveru.
- `BookInfo` je `Serializable` snapshot namenjen prikazu.
- `LibraryImpl` cuva remote knjige u mapi.

## Tok rada

1. Server registruje `Library` na portu `2099`.
2. Klijent dobija `Library` stub.
3. `getAllBooks` vraca kopije `BookInfo` objekata.
4. `getBook` vraca remote `Book` referencu.
5. `borrow` menja serversko polje `available`.

## Za ispit

Obavezno napisati:

- `Library extends Remote`;
- `Book extends Remote`;
- `BookInfo implements Serializable`;
- `BookImpl extends UnicastRemoteObject implements Book`;
- `borrow` kao `public synchronized` metodu;
- `Library.getBook` vraca `Book`, ne `BookImpl`;
- `getInfo` pravi novi `BookInfo`;
- osnovni `rebind`, `lookup` i poziv `borrow`.

Ne treba pisati importe, validaciju, kompletne `try-catch` blokove ili meni.

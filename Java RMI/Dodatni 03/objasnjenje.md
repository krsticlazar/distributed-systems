# Dodatni 03 - sistem redova cekanja

## Struktura

- `QueueService` je glavni remote servis.
- `Ticket` je `Serializable`, jer klijent dobija kopiju broja i imena.
- `QueueCallback` je remote interfejs koji implementira korisnicki klijent.
- `QueueServiceImpl` cuva FIFO red i callback za svaki broj tiketa.

## Kolekcije

- `Queue<Ticket>` koristi `offer` za dodavanje i `poll` za uzimanje prvog tiketa.
- `Map<Integer, QueueCallback>` povezuje broj tiketa i klijentski callback.

## Tok rada

1. Klijent dobija servis i pravi callback objekat.
2. `takeTicket` dodaje tiket u red i pamti callback.
3. `AdminClient` poziva `callNext`.
4. Server uzima prvi tiket i poziva callback tog klijenta.

## Za ispit

Obavezno napisati:

- `QueueService extends Remote`;
- `QueueCallback extends Remote`;
- `Ticket implements Serializable`;
- red, mapu callback objekata i brojac tiketa;
- logiku `takeTicket` sa `offer`;
- logiku `callNext` sa `poll` i callback pozivom;
- klijentski `QueueCallbackImpl`;
- osnovni `rebind`, `lookup` i registraciju callback-a.

Ne treba pisati importe, validaciju, kompletne `try-catch` blokove ili kod za cekanje klijenta.

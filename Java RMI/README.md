# Distributed Systems - Java RMI Practice Projects

This directory contains Java Remote Method Invocation (RMI) laboratory assignments, exam tasks, and preparation materials for the **Distributed Systems** course at the **Faculty of Electronic Engineering, University of Niš** (Elektronski fakultet u Nišu).

The examples are designed to be self-contained, clean, and directly runnable from the command line without external build tools or complex frameworks.

---

## 🚀 Key Concepts & Demonstrations

- **Remote Interfaces & Exception Handling**: Creating interfaces extending `java.rmi.Remote` with methods throwing `RemoteException`.
- **Server Implementation & Remote Objects**: Implementing remote objects extending `UnicastRemoteObject` (or exporting them explicitly).
- **RMI Registry Operations**: Binding remote services using `LocateRegistry.createRegistry` / `Naming.rebind` and querying via `Naming.lookup`.
- **Data Transfer Objects**: Passing custom objects across process boundaries implementing `java.io.Serializable`.
- **Client Callback Patterns**: Registering remote client callback objects on the server to enable asynchronous push notifications and event-driven communication.
- **Remote Object References**: Passing remote objects as method arguments or return values (call by reference for remote objects).

---

## 📁 Tasks Overview

### Laboratory Exercises
- [`Lab 01/`](file:///D:/ELFAK/IV%20godina/Distribuirani%20sistemi/Racunske/Java%20RMI/Lab%2001): Mathematical quiz system with remote questions and client answers.
- [`Lab 02/`](file:///D:/ELFAK/IV%20godina/Distribuirani%20sistemi/Racunske/Java%20RMI/Lab%2002): Mobile network operator simulation managing user accounts, top-ups, and balance checks.
- [`Lab 03/`](file:///D:/ELFAK/IV%20godina/Distribuirani%20sistemi/Racunske/Java%20RMI/Lab%2003): Electronic banking simulation with deposit, withdrawal, and balance queries.
- [`Lab 04/`](file:///D:/ELFAK/IV%20godina/Distribuirani%20sistemi/Racunske/Java%20RMI/Lab%2004): Student services exam registration system with capacity tracking.
- [`Lab 05/`](file:///D:/ELFAK/IV%20godina/Distribuirani%20sistemi/Racunske/Java%20RMI/Lab%2005): Online auction platform with remote exhibit items and bid monitoring.

### Solved Exam & Colloquium Tasks
- [`Ispit APR26/`](file:///D:/ELFAK/IV%20godina/Distribuirani%20sistemi/Racunske/Java%20RMI/Ispit%20APR26): Prime number generation with asynchronous callback delivery.
- [`Ispit JUN24/`](file:///D:/ELFAK/IV%20godina/Distribuirani%20sistemi/Racunske/Java%20RMI/Ispit%20JUN24): MQTT-like publish/subscribe message broker with client callbacks.
- [`Ispit KOL124/`](file:///D:/ELFAK/IV%20godina/Distribuirani%20sistemi/Racunske/Java%20RMI/Ispit%20KOL124): Live football match score tracker with real-time goal callbacks.
- [`Ispit KOL126/`](file:///D:/ELFAK/IV%20godina/Distribuirani%20sistemi/Racunske/Java%20RMI/Ispit%20KOL126): Stock market price ticker with threshold-based client callback alerts.

### Additional Practice Exercises
- [`Dodatni 01/`](file:///D:/ELFAK/IV%20godina/Distribuirani%20sistemi/Racunske/Java%20RMI/Dodatni%2001): Electronic voting system utilizing server-side `Map` and `Set` state.
- [`Dodatni 02/`](file:///D:/ELFAK/IV%20godina/Distribuirani%20sistemi/Racunske/Java%20RMI/Dodatni%2002): Digital library system with remote book entities and serialized catalog snapshots.
- [`Dodatni 03/`](file:///D:/ELFAK/IV%20godina/Distribuirani%20sistemi/Racunske/Java%20RMI/Dodatni%2003): Service desk FIFO queue management with per-ticket callback alerts.

---

## 📚 Study Materials & Scripts

- [`ULTIMATE_RMI.md`](file:///D:/ELFAK/IV%20godina/Distribuirani%20sistemi/Racunske/Java%20RMI/ULTIMATE_RMI.md): Full exam preparation script in Serbian containing theoretical principles, core patterns, boilerplate structures, and solved exam problems.
- [`.docs/`](file:///D:/ELFAK/IV%20godina/Distribuirani%20sistemi/Racunske/Java%20RMI/.docs): Supplementary lecture notes, quiz preparation summaries, and practical insights (`moji_zakljucc_java_rmi.md`, `RMI_priprema_za_kviz.md`, `uputstvo_za_zadatak.md`).

---

## 🛠️ How to Run

Navigate into any task folder and run:

```powershell
# 1. Compile all Java sources
javac *.java

# 2. Start the Server (starts registry and binds service)
java Server

# 3. In another terminal, run the Client
java Klijent
```

Each folder contains specific run instructions or `uputstvo.txt` where custom parameters or multiple clients are used.

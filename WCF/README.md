# Distributed Systems - Windows Communication Foundation (WCF)

This directory contains Windows Communication Foundation (WCF) projects, laboratory exercises, and exam preparation materials for the **Distributed Systems** course at the **Faculty of Electronic Engineering, University of Niš** (Elektronski fakultet u Nišu).

The projects focus on building enterprise-grade service-oriented applications in **C# / .NET**, exploring service contracts, data contracts, duplex callback channels, hosting mechanisms, fault handling, and configuration bindings.

---

## 🚀 Key Concepts & Demonstrations

- **Service & Data Contracts**:
  - `[ServiceContract]`: Defining service interfaces and operation signatures.
  - `[OperationContract]`: Declaring accessible service methods (one-way, request-reply).
  - `[DataContract]` & `[DataMember]`: Serializing custom complex types across network boundaries.
- **Duplex Services & Client Callbacks**:
  - `[ServiceContract(CallbackContract = typeof(ICallback))]`: Implementing bidirectional communication where the server can asynchronously call back into connected clients.
  - `OperationContext.Current.GetCallbackChannel<T>()`: Managing active client sessions and dispatching push notifications.
- **Fault Contracts & Error Handling**:
  - `[FaultContract(typeof(CustomFault))]`: Mapping server-side exceptions into SOAP faults for controlled error propagation.
  - Handling `FaultException<T>` on the client side without dropping the communication channel.
- **Hosting & Configuration**:
  - Self-hosting via `ServiceHost` in console applications.
  - Web hosting through IIS / IIS Express (`.svc` files).
  - Endpoints configuration (Address, Binding, Contract - **ABC**) via `App.config` / `Web.config` (`basicHttpBinding`, `netTcpBinding`, `wsDualHttpBinding`).

---

## 📁 Projects Overview

| Project | Description | Architecture / Pattern |
|---|---|---|
| [`WcfVremeSolution/`](file:///D:/ELFAK/IV%20godina/Distribuirani%20sistemi/Racunske/WCF/WcfVremeSolution) | Server time service with controlled `FaultException` handling and client proxy | Request-reply, typed faults |
| [`WCFCalculatorDuplex/`](file:///D:/ELFAK/IV%20godina/Distribuirani%20sistemi/Racunske/WCF/WCFCalculatorDuplex) | Calculator service reporting computation results via duplex callbacks | `WSDualHttpBinding`, Duplex Callback |
| [`WCFChat/`](file:///D:/ELFAK/IV%20godina/Distribuirani%20sistemi/Racunske/WCF/WCFChat) | Multi-user chat room with broadcast messaging and user presence callbacks | Duplex callbacks, session management |
| [`WCFTablic/`](file:///D:/ELFAK/IV%20godina/Distribuirani%20sistemi/Racunske/WCF/WCFTablic) | Card game simulation with real-time turn notifications to players | Duplex callback, stateful game server |
| [`WCFElektronskiDnevnik/`](file:///D:/ELFAK/IV%20godina/Distribuirani%20sistemi/Racunske/WCF/WCFElektronskiDnevnik) | School grading and record management service | Complex data contracts, CRUD operations |
| [`WCFIspitnaPrijava/`](file:///D:/ELFAK/IV%20godina/Distribuirani%20sistemi/Racunske/WCF/WCFIspitnaPrijava) | University exam registration service managing student capacity | Data contracts, validation logic |
| [`WCFKviz/`](file:///D:/ELFAK/IV%20godina/Distribuirani%20sistemi/Racunske/WCF/WCFKviz) | Interactive quiz system serving questions and evaluating player scores | Session tracking, score ranking |
| [`WCFPlacanjeTroskova/`](file:///D:/ELFAK/IV%20godina/Distribuirani%20sistemi/Racunske/WCF/WCFPlacanjeTroskova) | Bill splitting and payment calculation service | Financial record calculations |
| [`WCFProizvodnja/`](file:///D:/ELFAK/IV%20godina/Distribuirani%20sistemi/Racunske/WCF/WCFProizvodnja) | Factory production tracking and inventory management | Stateful service, monitoring |
| [`WCFServiceMatrice/`](file:///D:/ELFAK/IV%20godina/Distribuirani%20sistemi/Racunske/WCF/WCFServiceMatrice) | Matrix calculation and linear algebra operations | 2D array data contracts |
| [`Primeri/`](file:///D:/ELFAK/IV%20godina/Distribuirani%20sistemi/Racunske/WCF/Primeri) | Practical exercise examples (`01_Vreme`, `Chat`, `T1Z1`, `T2CB`, `WcfServiceSokovi`) | Introductory lab implementations |

---

## 📚 Study Materials & Scripts

- [`ULTIMATE_WCF.md`](file:///D:/ELFAK/IV%20godina/Distribuirani%20sistemi/Racunske/WCF/ULTIMATE_WCF.md): Complete study and exam preparation script in Serbian covering service configuration, endpoints, duplex callbacks, and solved exam problems.
- [`SKRIPTA_ZA_KVIZ.md`](file:///D:/ELFAK/IV%20godina/Distribuirani%20sistemi/Racunske/WCF/SKRIPTA_ZA_KVIZ.md): Theoretical notes and quiz preparation questions.
- [`Materijal/`](file:///D:/ELFAK/IV%20godina/Distribuirani%20sistemi/Racunske/WCF/Materijal): Step-by-step guides for creating WCF projects, configuring duplex bindings, and solution walkthroughs (`kreiraj_projekat.md`, `WCFChat_uputstvo.md`, etc.).

---

## 🛠️ How to Run

1. Open the solution in **Visual Studio**.
2. Set the **Server / Service** project as the startup project and run it (or host it in IIS Express / self-hosted console).
3. If running duplex services, ensure the client configuration points to the valid callback port or endpoint.
4. Launch one or more **Client** projects to interact with the service.

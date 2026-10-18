# Distributed Systems - Lab & Exam Preparation

This repository contains practical laboratory exercises, exam problem solutions, and study materials for the computational/practical part of the **Distributed Systems** (*Distribuirani sistemi*) course at the **Faculty of Electronic Engineering, University of Niš** (*Elektronski fakultet u Nišu - [EF-Niš](https://www.elfak.ni.ac.rs/)*).

The repository serves as a unified workspace covering four core distributed computing technologies and communication models: **gRPC**, **Java RMI**, **MPI**, and **Windows Communication Foundation (WCF)**.

---

## 🌐 Covered Technologies

| Technology | Folder | Communication Paradigm | Key Topics |
|---|---|---|---|
| **gRPC** | [**`gRPC/`**](./gRPC) | High-Performance RPC & Streaming (HTTP/2 + Protobuf) | Protocol Buffers (`.proto`), Unary RPC, Server/Client/Bidirectional Streaming, ASP.NET Core gRPC servers, Console clients |
| **Java RMI** | [**`Java RMI/`**](./Java%20RMI) | Distributed Object Invocations (Java) | Remote Interfaces, `UnicastRemoteObject`, `rmiregistry` / `LocateRegistry`, Serializable DTOs, Asynchronous Client Callbacks |
| **MPI** | [**`MPI/`**](./MPI) | Message Passing & Parallel I/O (C / MPI-1 & MPI-2) | Process coordination (`rank`/`size`), Point-to-point & Collective routines, Derived datatypes (`vector`, `indexed`, `subarray`), Parallel File I/O (`MPI_File_*`, file views) |
| **WCF** | [**`WCF/`**](./WCF) | Service-Oriented Architecture - SOA (.NET / C#) | `[ServiceContract]`, `[DataContract]`, `[FaultContract]`, Duplex Callback Channels (`wsDualHttpBinding`), Endpoints (ABC), Self-hosting & IIS Express |

---

## 🔍 Technology Overviews

### 1. [gRPC](./gRPC)
gRPC is a language-agnostic, high-performance Remote Procedure Call (RPC) framework developed by Google. In this coursework, services are defined using Protocol Buffers (`.proto`) and implemented in C# on .NET 8.
- **Key implementations**:
  - Request-reply and streaming operations (Server streaming, Client streaming, and Bi-directional streaming).
  - Strongly typed message serialization and contract enforcement.
  - Multi-client scenarios (.NET console and desktop clients).
  - Detailed preparation guide: [`ULTIMATE_gRPC.md`](./gRPC/ULTIMATE_gRPC.md).

### 2. [Java RMI](./Java%20RMI)
Java Remote Method Invocation allows an object running in one Java Virtual Machine (JVM) to invoke methods on an object running in another JVM.
- **Key implementations**:
  - Pure Java client/server applications without heavy build dependencies.
  - Registry lookups, client callback interfaces, and real-time push event notification patterns.
  - Stateful distributed services, mobile operator systems, banking simulations, and online auctions.
  - Detailed preparation guide: [`ULTIMATE_RMI.md`](./Java%20RMI/ULTIMATE_RMI.md).

### 3. [MPI (Message Passing Interface)](./MPI)
MPI is the industry standard for distributed-memory parallel computing. Programs run multiple processes communicating via message passing.
- **Key implementations**:
  - Point-to-point (`MPI_Send`, `MPI_Recv`, `MPI_Sendrecv`) and non-blocking routines (`MPI_Isend`, `MPI_Irecv`).
  - Collective data operations (`MPI_Bcast`, `MPI_Scatterv`, `MPI_Gatherv`, `MPI_Reduce`).
  - MPI derived datatypes for non-contiguous memory layouts (`MPI_Type_vector`, `MPI_Type_create_subarray`).
  - MPI-2 Parallel File I/O (`MPI_File_open`, `MPI_File_set_view`, collective read/write operations).
  - Detailed preparation guide: [`ULTIMATE_MPI.md`](./MPI/ULTIMATE_MPI.md).

### 4. [WCF (Windows Communication Foundation)](./WCF)
WCF is Microsoft's framework for building service-oriented applications in the .NET Framework ecosystem.
- **Key implementations**:
  - Service contracts, data serialization contracts, and typed fault exception handling.
  - Duplex communication allowing the server to asynchronously notify clients via callback channels.
  - Real-world distributed examples: multi-player game servers (Tablić), chat systems, electronic gradebooks, and factory production lines.
  - Detailed preparation guide: [`ULTIMATE_WCF.md`](./WCF/ULTIMATE_WCF.md) and [`SKRIPTA_ZA_KVIZ.md`](./WCF/SKRIPTA_ZA_KVIZ.md).

---

## 📖 Study Scripts & Exam Notes

Each technology subfolder contains comprehensive exam preparation scripts (written in Serbian for exam revision, left intact as original study materials):
- [`gRPC/ULTIMATE_gRPC.md`](./gRPC/ULTIMATE_gRPC.md)
- [`Java RMI/ULTIMATE_RMI.md`](./Java%20RMI/ULTIMATE_RMI.md)
- [`MPI/ULTIMATE_MPI.md`](./MPI/ULTIMATE_MPI.md)
- [`WCF/ULTIMATE_WCF.md`](./WCF/ULTIMATE_WCF.md)

---

## ⚙️ Repository Setup & Conventions

- All build output directories (`bin/`, `obj/`, `Debug/`, `Release/`, `packages/`, `.vs/`) and compiled artifacts (`*.class`, `*.exe`, `*.dll`, `*.pdb`) are excluded via the root [`.gitignore`](./.gitignore).
- Clean, source-only project state ready for compilation and testing in standard environments (.NET SDK, JDK, MS-MPI / OpenMPI, Visual Studio).

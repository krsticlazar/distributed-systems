# Distributed Systems - gRPC Practice Projects

This directory contains .NET gRPC laboratory exercises, practice solutions, and exam preparation projects for the **Distributed Systems** course at the **Faculty of Electronic Engineering, University of Niš** (Elektronski fakultet u Nišu).

The focus is on building distributed client/server applications in **C# / .NET** using **gRPC** and **Protocol Buffers** (`.proto`), implementing service contracts, generated stubs, message streaming, and state management.

---

## 🚀 Key Concepts & Demonstrations

- **Protocol Buffers Contract Design**: Defining `.proto` schemas with services, RPC methods, message structures, `repeated` fields, enums, and `google.protobuf.Empty`.
- **gRPC Communication Patterns**:
  - **Unary RPC**: Standard request-response pattern.
  - **Server Streaming**: Server sends back a sequence of responses.
  - **Client Streaming**: Client sends a sequence of messages, server replies once.
  - **Bidirectional Streaming**: Both parties send sequences of messages concurrently.
- **ASP.NET Core Server Architecture**: Implementing gRPC service base classes, dependency injection, and in-memory thread-safe state storage.
- **Client Application Development**: Consuming generated client stubs via `GrpcChannel` in .NET console applications.

---

## 📁 Projects Overview

| Folder | Description | Key Patterns |
|---|---|---|
| [`DSLab1Grpc/`](file:///D:/ELFAK/IV%20godina/Distribuirani%20sistemi/Racunske/gRPC/DSLab1Grpc) | Main lab exercise setting up gRPC service, contracts, and console client | Unary RPC, project setup |
| [`APRIL26/`](file:///D:/ELFAK/IV%20godina/Distribuirani%20sistemi/Racunske/gRPC/APRIL26) | Calculator-style distributed service with arithmetic operations | Unary & streaming operations |
| [`JUN24/`](file:///D:/ELFAK/IV%20godina/Distribuirani%20sistemi/Racunske/gRPC/JUN24) | Message broker service with message dispatch, deletion, and retrieval | Server streaming, message lists |
| [`KOL24/`](file:///D:/ELFAK/IV%20godina/Distribuirani%20sistemi/Racunske/gRPC/KOL24) | Task management system with addition, listing, and status updates | Unary RPC, collection handling |
| [`Oktobar2/`](file:///D:/ELFAK/IV%20godina/Distribuirani%20sistemi/Racunske/gRPC/Oktobar2) | Practice project demonstrating continuous data exchange | Streaming flow & client handling |
| [`WCFService1/`](file:///D:/ELFAK/IV%20godina/Distribuirani%20sistemi/Racunske/gRPC/WCFService1) | Supplementary service and multi-client solution (Console & WinForms) | Multi-target client integration |

---

## 📚 Study Materials & Scripts

- [`ULTIMATE_gRPC.md`](file:///D:/ELFAK/IV%20godina/Distribuirani%20sistemi/Racunske/gRPC/ULTIMATE_gRPC.md): Comprehensive preparation script in Serbian containing essential theoretical principles, code skeletons, `.proto` definitions, and typical exam questions with solutions.
- [`ZAKLJUCAK.md`](file:///D:/ELFAK/IV%20godina/Distribuirani%20sistemi/Racunske/gRPC/ZAKLJUCAK.md): Quick-reference conclusions, syntax checklists, and common exam pitfalls.

---

## 🛠️ How to Run

1. Open the desired solution in **Visual Studio** (or launch via .NET CLI).
2. Start the **Server** project first:
   ```powershell
   dotnet run --project .\PROJECT_NAME\PROJECTServer\PROJECTServer.csproj
   ```
3. In a separate terminal, launch the **Client** project:
   ```powershell
   dotnet run --project .\PROJECT_NAME\PROJECTClient\PROJECTClient.csproj
   ```
4. Follow the interactive menu in the client console to trigger RPC calls.

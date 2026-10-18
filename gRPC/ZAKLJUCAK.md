# ZAKLJUCCI o gRPC-u

Nakon dodavanja service-a koji su zadati u tekstu zadatka, dodajemo poruke. Za svaki argument i povratnu vrednost treba da postoji poruka, osim ako nije `google.protobuf.Empty`. Prvo idu argumenti, a onda povratne vrednosti.

Zanemari `= 1`, `= 2` i ostalo, razmisli o tome sta se salje.

`AddTaskRequest` prima `description`, napisi `string description`, posle dodaj `= 1`.

`MarkTastAsCompleted` prima `id`, sto je `int32 id`, posle dodaj `= 1`.

Za povratne vrednosti je malo slozenije. Ako zelimo da vratimo jedan onda samo `TaskResponse`, odgovor servera posle neke akcije, i to je objekat koji ima `success`, `message` i `TaskItem`, konkretan zadatak.

`TaskItem` je "zadatak", a `TaskResponse` je "rezultat operacije nad zadatkom". Na primer: `success = true`, `message = "Zadatak je dodat"`, i unutra `task` koji je dodat.

## ZA ISPIT

Pises proto fajl, service fajl i komentar:

```xml
<ItemGroup>
    <Protobuf Include="Protos\kol1.proto" GrpcServices="Server" />
    <!-- na klijentu "Client" -->
</ItemGroup>
```

## PROTO SABLON

```proto
syntax = "proto3";

import "google/protobuf/empty.proto";

option csharp_namespace = "KOL1Server"; // za klijent KOL1Client
package kol1;

service ImeServisa {}

message ImePoruke {}
```

## SERVICE SABLON

```csharp
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

namespace KOL1Server.Services;

public class ImeServisaService : ImeServisa.CalculatorBase {}
```

## Primeri za proto + service

### Kratko pravilo

```text
returns (Message)          -> Task<Message>
returns (Empty)            -> Task<Empty>
returns (stream Message)   -> Task + responseStream
```

### Kada saljemo serveru neki info i on nam vrati rezultat, bez da ista skladisti

```proto
service Calculator {
    rpc Sabiranje(OperacijaRequest) returns (RezultatResponse);
}

message OperacijaRequest {
    int32 operand1 = 1;
    int32 operand2 = 2;
}

message RezultatResponse {
    int32 operand1 = 1;
    int32 operand2 = 2;
    string operacija = 3;
    int32 rezultat = 4;
}
```

```csharp
public override Task<RezultatResponse> Sabiranje(OperacijaRequest request, ServerCallContext context)
{
    var message = new RezultatResponse
    {
        Operand1 = request.Operand1,
        Operand2 = request.Operand2,
        Operacija = "Sabiranje",
        Rezultat = request.Operand1 + request.Operand2,
    };

    return Task.FromResult(message);
}
```

### Kada imamo poruke koje trebaju da se cuvaju u nekoj listi i funkciju vrati sve poruke kroz tok, jednu po jednu, to radimo preko stream-a

```proto
service MessageManager {
    rpc ListMessages(google.protobuf.Empty) returns (stream MessageItem);
}

message MessageItem {
    int32 id = 1;
    string text = 2;
}
```

```csharp
public override async Task ListMessages(
    Empty request,
    IServerStreamWriter<MessageItem> responseStream,
    ServerCallContext context)
{
    foreach (var message in Poruke.Instanca().Lista)
    {
        await responseStream.WriteAsync(message);
    }
}
```

### Kada imamo listu koja treba od jednom da se vrati, koristimo repeated

```proto
service TaskManager {
    rpc ListTasks(google.protobuf.Empty) returns (TaskList);
}

message TaskItem {
    int32 id = 1;
    string description = 2;
    bool completed = 3;
}

message TaskList {
    repeated TaskItem tasks = 1;
}

message TaskResponse {
    bool success = 1;
    string message = 2;
    TaskItem task = 3;
}
```

```csharp
public override Task<TaskList> ListTasks(Empty request, ServerCallContext context)
{
    var response = new TaskList();
    response.Tasks.AddRange(Zadaci.Instanca().Lista);

    return Task.FromResult(response);
}
```

### List<T>, repeated i stream

`List<T>` u pomocnoj klasi je nacin cuvanja podataka na serveru.

`repeated` lista i `stream` su nacini slanja podataka klijentu.

KOL24 trazi listu kao jedan odgovor, zato `repeated`.

JUN24 trazi tok podataka, zato `stream`.

Zato stream vracamo jednu po jednu poruku, a repeated vracamo celu poruku od jednom koja pored `success` i `message` ima i `TaskItem` koji je `repeated`.

## Kada saljemo tok i primamo tok, koristimo stream-stream

Ovo je bidirectional streaming: klijent salje vise poruka serveru kroz `RequestStream`, a server vraca vise odgovora klijentu kroz `ResponseStream`.

```proto
service ObradaManager {
    rpc ObradiTok(stream NumberPair) returns (stream NumberResult);
}

message NumberPair {
    int32 a = 1;
    int32 b = 2;
}

message NumberResult {
    int32 rezultat = 1;
}
```

Na serveru imamo ulazni tok i izlazni tok:

```csharp
public override async Task ObradiTok(
    IAsyncStreamReader<NumberPair> requestStream,
    IServerStreamWriter<NumberResult> responseStream,
    ServerCallContext context)
{
    await foreach (var request in requestStream.ReadAllAsync())
    {
        var rezultat = request.A + request.B;

        await responseStream.WriteAsync(new NumberResult
        {
            Rezultat = rezultat
        });
    }
}
```

Bitno: ako server ceka tok podataka, ne treba posle svakog `WriteAsync` odmah cekati konacan rezultat ako server logika ceka jos elemenata. Prvo posalji sta treba kroz `RequestStream`, zatim pozovi `CompleteAsync`, pa citaj odgovore iz `ResponseStream`.

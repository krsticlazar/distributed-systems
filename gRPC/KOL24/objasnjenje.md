[KOL 1 2024]
U .NET-u, koristeci gRPC, kreirati servis za upravljanje listom zadataka. Servis treba da omoguci klijentima da dodaju zadatke, prikazu sve zadatke i oznace zadatke kao zavrsene.
Zahtevi:
- Definisati proto fajl (tasks.proto) koji daje specifikaciju servisa i poruka.
- Servis treba da podrzava sledece operacije:
  - AddTask: dodaje novi zadatak na listu.
  - ListTasks: dobija listu svih zadataka.
  - MarkTaskAsCompleted: oznacava zadatak kao zavrsen po njegovom ID-u.
- Implementirati gRPC server definisan u tasks.proto fajlu.



# KOL24 - objasnjenje projekta

Zadatak: u .NET-u, koristeci gRPC, napraviti servis za upravljanje listom zadataka. Klijent treba da moze da doda zadatak, prikaze sve zadatke i oznaci zadatak kao zavrsen po ID-u.

Ovo je namerno uradjeno u istom stilu kao primeri sa vezbi: `proto` fajl, serverski servis, jednostavna klasa za cuvanje podataka i konzolni klijent.

## Sta je najbitnije

- `tasks.proto` je ugovor izmedju klijenta i servera.
- `TaskManagerService.cs` je implementacija serverskih metoda iz proto fajla.
- `Zadaci.cs` cuva listu zadataka u memoriji.
- `Program.cs` na serveru registruje gRPC i servis.
- `Program.cs` na klijentu pravi kanal, pravi stub i poziva udaljene metode.

## KOL24Server/Protos/tasks.proto

Ovo je glavni fajl za ispit.

```proto
syntax = "proto3";
```

Koristi se proto3 sintaksa.

```proto
import "google/protobuf/empty.proto";
```

Uvozimo gotovu praznu poruku. Potrebna je za metodu `ListTasks`, jer ta metoda nema stvarni ulazni podatak, ali gRPC metoda mora da ima argument.

```proto
option csharp_namespace = "KOL24Server";
package kol24;
```

`csharp_namespace` kaze u kom C# namespace-u ce biti generisane klase na serveru. `package` je proto paket i mora da se poklapa na serveru i klijentu.

```proto
service TaskManager {
  rpc AddTask(AddTaskRequest) returns (TaskResponse);
  rpc ListTasks(google.protobuf.Empty) returns (TaskList);
  rpc MarkTaskAsCompleted(TaskIdRequest) returns (TaskResponse);
}
```

Ovo su tri udaljene procedure:

- `AddTask` prima opis zadatka i vraca odgovor.
- `ListTasks` prima praznu poruku i vraca listu zadataka.
- `MarkTaskAsCompleted` prima ID zadatka i vraca odgovor.

Sve tri metode su unary RPC, jer nijedna nema `stream`. Klijent salje jednu poruku i server vraca jednu poruku.

```proto
message AddTaskRequest {
  string description = 1;
}
```

Poruka za dodavanje zadatka. Ima opis zadatka.

```proto
message TaskIdRequest {
  int32 id = 1;
}
```

Poruka koja nosi ID zadatka.

```proto
message TaskItem {
  int32 id = 1;
  string description = 2;
  bool completed = 3;
}
```

Jedan zadatak ima ID, opis i informaciju da li je zavrsen.

```proto
message TaskList {
  repeated TaskItem tasks = 1;
}
```

`repeated` znaci da se u jednoj poruci vraca vise zadataka. Ovo nije isto sto i `stream`. Da je u zadatku pisalo da treba vratiti tok zadataka, onda bi metoda bila `returns (stream TaskItem)`.

```proto
message TaskResponse {
  bool success = 1;
  string message = 2;
  TaskItem task = 3;
}
```

Opsti odgovor za dodavanje i oznacavanje zadatka. Ima informaciju o uspehu, tekst poruke i zadatak.

## KOL24Server/Zadaci.cs

Ova klasa sluzi samo da server ima gde da zapamti zadatke.

```csharp
public List<TaskItem> Lista { get; set; }
public int SledeciId { get; set; }
```

`Lista` cuva sve zadatke. `SledeciId` se koristi da svaki novi zadatak dobije novi ID.

```csharp
private static Zadaci? instanca;
```

Cuva jednu zajednicku instancu klase. Ovo je isti princip kao u primeru sa studentima, gde postoji jedna baza studenata.

```csharp
private Zadaci()
{
    Lista = new List<TaskItem>();
    SledeciId = 1;
}
```

Konstruktor pravi praznu listu i postavlja prvi ID na 1.

```csharp
public static Zadaci Instanca()
```

Vraca jednu istu instancu klase, da se lista ne izgubi izmedju poziva.

## KOL24Server/Services/TaskManagerService.cs

Ovo je serverska implementacija servisa.

```csharp
public class TaskManagerService : TaskManager.TaskManagerBase
```

`TaskManagerBase` je generisana bazna klasa iz `tasks.proto`. Mi nasledjujemo tu klasu i implementiramo metode.

```csharp
public override Task<TaskResponse> AddTask(AddTaskRequest request, ServerCallContext context)
```

Implementira RPC metodu `AddTask`.

```csharp
var task = new TaskItem
{
    Id = Zadaci.Instanca().SledeciId++,
    Description = request.Description,
    Completed = false
};
```

Pravi novi zadatak. ID se uzima iz `SledeciId`, opis dolazi od klijenta, a `Completed` je na pocetku `false`.

```csharp
Zadaci.Instanca().Lista.Add(task);
```

Dodaje zadatak u memorijsku listu.

```csharp
return Task.FromResult(new TaskResponse { ... });
```

Vraca odgovor klijentu. Posto je metoda unary, vraca se jedna poruka.

```csharp
public override Task<TaskList> ListTasks(Empty request, ServerCallContext context)
```

Implementira `ListTasks`. Prima praznu poruku i vraca `TaskList`.

```csharp
response.Tasks.AddRange(Zadaci.Instanca().Lista);
```

Sve zadatke iz liste dodaje u `repeated` polje `Tasks`.

```csharp
public override Task<TaskResponse> MarkTaskAsCompleted(TaskIdRequest request, ServerCallContext context)
```

Implementira oznacavanje zadatka kao zavrsenog.

```csharp
var task = Zadaci.Instanca().Lista.FirstOrDefault(z => z.Id == request.Id);
```

Trazi zadatak po ID-u.

```csharp
if (task == null)
```

Ako zadatak ne postoji, vraca se odgovor sa `Success = false`.

```csharp
task.Completed = true;
```

Ako zadatak postoji, menja mu se stanje na zavrsen.

## KOL24Server/Program.cs

```csharp
builder.Services.AddGrpc();
```

Dodaje gRPC podrsku serveru.

```csharp
app.MapGrpcService<TaskManagerService>();
```

Registruje nas servis.

```csharp
app.MapGet("/", () => "KOL24 gRPC server radi.");
```

Obican test endpoint. Glavna komunikacija ipak ide preko gRPC klijenta.

```csharp
app.Run();
```

Pokrece server.

## KOL24Client/Program.cs

```csharp
using var channel = GrpcChannel.ForAddress("https://localhost:7224");
```

Pravi kanal ka serveru. Port mora da odgovara server profilu u `launchSettings.json`.

```csharp
var client = new TaskManager.TaskManagerClient(channel);
```

Pravi klijentski stub. Preko njega se pozivaju udaljene procedure.

```csharp
await client.AddTaskAsync(new AddTaskRequest { Description = description ?? string.Empty });
```

Poziva `AddTask` na serveru.

```csharp
var response = await client.ListTasksAsync(new Empty());
```

Poziva `ListTasks`. Salje praznu poruku.

```csharp
foreach (var task in response.Tasks)
```

Prolazi kroz sve zadatke iz `repeated` polja.

```csharp
await client.MarkTaskAsCompletedAsync(new TaskIdRequest { Id = id });
```

Poziva metodu za oznacavanje zadatka kao zavrsenog.

## Pokretanje

1. Otvori `KOL24/KOL24.slnx`.
2. Pokreni server i klijent zajedno.
3. Server koristi `https` profil i slusa na `https://localhost:7224`.
4. Klijent se kaci na isti URL.

Test redosled:

```text
1
Nauci proto fajl
1
Uradi servis
2
3
1
2
```

Ocekivanje:

- posle prve dve komande dodata su dva zadatka;
- komanda `2` prikazuje oba zadatka;
- komanda `3` sa ID-em `1` oznacava prvi zadatak kao zavrsen;
- poslednja komanda `2` prikazuje da je prvi zadatak zavrsen.

#ZA ISPIT

U ispitnoj svesci ne pises ceo projekat. Pises proto fajl i osnovnu serversku implementaciju.

Proto fajl:

```proto
syntax = "proto3";

import "google/protobuf/empty.proto";

package kol24;

service TaskManager {
  rpc AddTask(AddTaskRequest) returns (TaskResponse);
  rpc ListTasks(google.protobuf.Empty) returns (TaskList);
  rpc MarkTaskAsCompleted(TaskIdRequest) returns (TaskResponse);
}

message AddTaskRequest {
  string description = 1;
}

message TaskIdRequest {
  int32 id = 1;
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

Sta obavezno objasniti:

- `AddTask`, `ListTasks` i `MarkTaskAsCompleted` su unary metode.
- `ListTasks` vraca `TaskList`, a `TaskList` ima `repeated TaskItem tasks`.
- `repeated` je niz/lista u jednoj poruci, nije stream.
- `google.protobuf.Empty` se koristi zato sto `ListTasks` nema ulazni parametar.
- `TaskItem` predstavlja jedan zadatak.

Serverska logika koju treba znati:

```csharp
public class TaskManagerService : TaskManager.TaskManagerBase
{
    private static List<TaskItem> tasks = new();
    private static int nextId = 1;

    public override Task<TaskResponse> AddTask(AddTaskRequest request, ServerCallContext context)
    {
        var task = new TaskItem
        {
            Id = nextId++,
            Description = request.Description,
            Completed = false
        };

        tasks.Add(task);

        return Task.FromResult(new TaskResponse
        {
            Success = true,
            Message = "Zadatak je dodat.",
            Task = task
        });
    }

    public override Task<TaskList> ListTasks(Empty request, ServerCallContext context)
    {
        var response = new TaskList();
        response.Tasks.AddRange(tasks);
        return Task.FromResult(response);
    }

    public override Task<TaskResponse> MarkTaskAsCompleted(TaskIdRequest request, ServerCallContext context)
    {
        var task = tasks.FirstOrDefault(t => t.Id == request.Id);

        if (task == null)
        {
            return Task.FromResult(new TaskResponse
            {
                Success = false,
                Message = "Zadatak ne postoji."
            });
        }

        task.Completed = true;

        return Task.FromResult(new TaskResponse
        {
            Success = true,
            Message = "Zadatak je zavrsen.",
            Task = task
        });
    }
}
```

Registracija servisa:

```csharp
builder.Services.AddGrpc();
app.MapGrpcService<TaskManagerService>();
```


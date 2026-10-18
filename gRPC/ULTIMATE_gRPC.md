# Ultimativna priprema za gRPC zadatke

Ovaj dokument je namenjen rešavanju gRPC zadataka na papiru. Ne piše se ceo Visual Studio projekat, klijentski UI, `appsettings.json` ili `launchSettings.json`. Prioritet su:

1. kompletan `.proto` fajl;
2. serverska service klasa;
3. jednostavna klasa za čuvanje podataka, samo ako zadatak zahteva stanje;
4. kratka napomena za registraciju proto fajla i servisa.

## Kako analizirati tekst zadatka

Prvo podvuci glagole. Oni uglavnom postaju RPC metode: dodaj, obriši, pronađi, prikaži, označi, iznajmi.

Zatim podvuci imenice i podatke. Oni postaju proto poruke i njihova polja: knjiga, poruka, zadatak, ID, naslov, autor.

Na kraju utvrdi način komunikacije:

| Tekst zadatka | Tip metode | Proto oblik |
| --- | --- | --- |
| Prima jedan zahtev i vraća jedan odgovor | Unary | `rpc M(Request) returns (Response);` |
| Vraća celu listu kao jedan odgovor | Unary + `repeated` | `rpc M(Empty) returns (ItemList);` |
| Vraća tok podataka | Server streaming | `rpc M(Request) returns (stream Item);` |
| Prima tok i vraća jedan odgovor | Client streaming | `rpc M(stream Item) returns (Response);` |
| Prima tok i vraća tok | Bidirectional streaming | `rpc M(stream Request) returns (stream Response);` |

### `List<T>`, `repeated` i `stream` nisu isto

- `List<T>` je način čuvanja podataka u memoriji servera.
- `repeated` znači da jedna protobuf poruka sadrži kolekciju elemenata.
- `stream` znači da se poruke prenose jedna po jedna kroz vreme.

Server može čuvati podatke u običnoj `List<T>`, a zatim ih klijentu poslati ili kao jednu `repeated` kolekciju ili kao tok. Način čuvanja ne određuje način prenosa.

### Pravila proto fajla

- Svaka RPC metoda mora imati argument i povratni tip.
- Ne piše se `rpc Metoda() returns ();`.
- Kada nema stvarnog podatka koristi se `google.protobuf.Empty`.
- Brojevi `= 1`, `= 2` i tako dalje jesu jedinstveni tagovi polja, a ne početne vrednosti.
- Proto fajl određuje ugovor. Iz njega se generišu C# poruke, klijentski stub i serverska bazna klasa.
- `package`, servis, metode i poruke moraju biti isti na klijentu i serveru.
- `option csharp_namespace` je C#-specifičan i može biti drugačiji za serverski i klijentski projekat.

### C# potpisi metoda

```text
Unary:
Task<Response>

Server streaming:
Task + IServerStreamWriter<Response>

Client streaming:
Task<Response> + IAsyncStreamReader<Request>

Bidirectional streaming:
Task + IAsyncStreamReader<Request> + IServerStreamWriter<Response>
```

### Dve kratke napomene za .NET

U serverskom `.csproj` fajlu:

```xml
<ItemGroup>
    <Protobuf Include="Protos\naziv.proto" GrpcServices="Server" />
</ItemGroup>
```

Registracija servisa u `Program.cs`:

```csharp
builder.Services.AddGrpc();
app.MapGrpcService<ImeServisaService>();
```

Ove linije je dovoljno kratko navesti. Stubove i generisane klase ne pišemo ručno.

---

# I deo: zadaci sa blanketa

## 1. KOL I 2024 - upravljanje zadacima

### Tekst zadatka

U .NET-u, koristeći gRPC, kreirati servis za upravljanje listom zadataka. Servis treba da omogući klijentima da dodaju zadatke, prikažu sve zadatke i označe zadatke kao završene.

Definisati proto fajl `tasks.proto` koji daje specifikaciju servisa i poruka. Servis treba da podržava sledeće operacije:

- `AddTask`: dodaje novi zadatak na listu;
- `ListTasks`: dobija listu svih zadataka;
- `MarkTaskAsCompleted`: označava zadatak kao završen po njegovom ID-u.

Implementirati gRPC server definisan u `tasks.proto` fajlu.

### Analiza zadatka

Potrebno je trajno stanje tokom rada servera, jer jedan poziv dodaje zadatak, a naredni pozivi moraju da ga pronađu. Za vežbe je dovoljna memorijska `List<TaskItem>`.

Sve tri metode su unary:

- `AddTask` prima jedan opis i vraća jedan rezultat operacije;
- `ListTasks` nema stvarni argument i vraća jednu poruku koja sadrži celu listu;
- `MarkTaskAsCompleted` prima jedan ID i vraća jedan rezultat operacije.

U tekstu piše lista, a ne tok. Zato `ListTasks` vraća `TaskList` sa `repeated TaskItem`.

### `tasks.proto`

```proto
syntax = "proto3";

import "google/protobuf/empty.proto";

option csharp_namespace = "KOL24Server";
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

### Objašnjenje proto fajla

`AddTaskRequest` sadrži samo ono što klijent treba da pošalje: opis. ID određuje server, a novi zadatak uvek počinje sa `completed = false`.

`TaskIdRequest` postoji zato što metoda za završavanje zadatka prima samo ID.

`TaskItem` predstavlja konkretan podatak koji se čuva u listi: ID, opis i stanje.

`TaskResponse` nije zadatak, već rezultat jedne operacije. On kaže da li je operacija uspela, daje tekstualnu poruku i po potrebi sadrži konkretan `TaskItem`.

`TaskList` je omotač oko kolekcije. `repeated TaskItem tasks` znači da svi zadaci stižu u jednoj odgovornoj poruci.

`ListTasks` prima `Empty` zato što joj klijent ne šalje filter ili drugi podatak, ali RPC metoda ipak mora imati argument.

### Memorijska klasa `Zadaci.cs`

```csharp
namespace KOL24Server;

public class Zadaci
{
    public List<TaskItem> Lista { get; set; }
    public int SledeciId { get; set; }

    private static Zadaci? instanca;

    private Zadaci()
    {
        Lista = new List<TaskItem>();
        SledeciId = 1;
    }

    public static Zadaci Instanca()
    {
        if (instanca == null)
        {
            instanca = new Zadaci();
        }

        return instanca;
    }
}
```

### Objašnjenje memorijske klase

`Lista` je stvarno mesto na serveru gde se zadaci čuvaju. `repeated` iz proto fajla ne čuva ništa; ono samo opisuje kako se lista prenosi.

`SledeciId` obezbeđuje jedinstven ID. Operator `++` najpre uzme trenutnu vrednost, a zatim je poveća za sledeći zadatak.

Privatni konstruktor sprečava pravljenje proizvoljnih instanci. `Instanca()` uvek vraća isti objekat, pa svi RPC pozivi koriste istu listu.

Za ispit i vežbe ovaj jednostavan singleton je dovoljan. U produkciji bi trebalo voditi računa o paralelnom pristupu i trajnom skladištu, ali to nije deo ovog zadatka.

### `TaskManagerService.cs`

```csharp
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

namespace KOL24Server.Services;

public class TaskManagerService : TaskManager.TaskManagerBase
{
    public override Task<TaskResponse> AddTask(
        AddTaskRequest request,
        ServerCallContext context)
    {
        var task = new TaskItem
        {
            Id = Zadaci.Instanca().SledeciId++,
            Description = request.Description,
            Completed = false
        };

        Zadaci.Instanca().Lista.Add(task);

        return Task.FromResult(new TaskResponse
        {
            Success = true,
            Message = "Zadatak je dodat.",
            Task = task
        });
    }

    public override Task<TaskList> ListTasks(
        Empty request,
        ServerCallContext context)
    {
        var response = new TaskList();
        response.Tasks.AddRange(Zadaci.Instanca().Lista);

        return Task.FromResult(response);
    }

    public override Task<TaskResponse> MarkTaskAsCompleted(
        TaskIdRequest request,
        ServerCallContext context)
    {
        var task = Zadaci.Instanca().Lista
            .FirstOrDefault(z => z.Id == request.Id);

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
            Message = "Zadatak je označen kao završen.",
            Task = task
        });
    }
}
```

### Objašnjenje servisa

`TaskManager.TaskManagerBase` je bazna klasa generisana iz `service TaskManager`. Mi je nasleđujemo i pomoću `override` implementiramo RPC metode.

Kod `AddTask`, polje `Description` dolazi iz zahteva, dok `Id` i `Completed` određuje server. Nakon upisa u listu vraća se `TaskResponse`.

Kod `ListTasks`, `response.Tasks` je C# kolekcija generisana iz `repeated` polja. `AddRange` kopira sve elemente memorijske liste u jednu odgovornu poruku.

Kod `MarkTaskAsCompleted`, `FirstOrDefault` vraća pronađeni zadatak ili `null`. Ako ne postoji, vraća se neuspešan `TaskResponse`. Ako postoji, menja se isti objekat koji se već nalazi u listi.

Sve metode vraćaju `Task<T>` zato što je to potpis generisane gRPC baze. Pošto ovde nema pravog asinhronog rada, rezultat se omotava sa `Task.FromResult`.

### Šta napisati i naglasiti na ispitu

1. Kompletan `tasks.proto`.
2. Listu i brojač ID-eva, kroz pomoćnu klasu ili kao statička polja servisa.
3. Tri `override` metode.
4. `ListTasks` koristi `repeated`, ne `stream`.
5. `TaskItem` je podatak, a `TaskResponse` je rezultat operacije nad podatkom.
6. Registracija: `app.MapGrpcService<TaskManagerService>();`.

---

## 2. JUN 2024 - upravljanje porukama

### Tekst zadatka

U .NET-u, koristeći gRPC, kreirati servis za upravljanje listom poruka. Servis treba da omogući klijentima da dodaju i brišu poruke i prikažu sve poruke.

Definisati proto fajl `message.proto` koji daje specifikaciju servisa i poruka. Servis treba da podržava sledeće operacije:

- `SendMessage`: dodaje novu poruku na listu;
- `DeleteMessage`: briše poruku sa zadatim ID-em;
- `ListMessages`: dobija tok podataka koji sadrži sve poruke.

Implementirati gRPC server definisan u `message.proto` fajlu.

### Analiza zadatka

Server mora da pamti poruke između poziva, zato koristi memorijsku listu i brojač ID-eva.

`SendMessage` i `DeleteMessage` su unary metode. `ListMessages` je server streaming zato što tekst izričito kaže „tok podataka“.

Važna razlika u odnosu na KOL 2024:

- KOL: svi zadaci se vraćaju unutar jedne `TaskList` poruke;
- JUN: svaka poruka se šalje posebno kroz `responseStream`.

### `message.proto`

```proto
syntax = "proto3";

import "google/protobuf/empty.proto";

option csharp_namespace = "JUN24Server";
package jun24;

service MessageManager {
    rpc SendMessage(MessageRequest) returns (MessageItem);
    rpc DeleteMessage(MessageId) returns (google.protobuf.Empty);
    rpc ListMessages(google.protobuf.Empty) returns (stream MessageItem);
}

message MessageRequest {
    string text = 1;
}

message MessageId {
    int32 id = 1;
}

message MessageItem {
    int32 id = 1;
    string text = 2;
}
```

### Objašnjenje proto fajla

`MessageRequest` je zahtev za unos i sadrži samo tekst. Server dodeljuje ID i vraća kompletan `MessageItem`.

`DeleteMessage` prima `MessageId`, jer je ID dovoljan za pronalaženje poruke. Vraća `Empty`, jer tekst zadatka ne zahteva potvrdu sa dodatnim podacima.

`ListMessages` prima `Empty`, a vraća `stream MessageItem`. Reč `stream` stoji uz povratni tip zato što tok ide od servera ka klijentu.

Ne pravi se `MessageList` sa `repeated`, jer zadatak nije tražio jednu listu kao odgovor već tok.

### Memorijska klasa `Poruke.cs`

```csharp
namespace JUN24Server;

public class Poruke
{
    public List<MessageItem> Lista { get; set; }
    public int SledeciId { get; set; }

    private static Poruke? instanca;

    private Poruke()
    {
        Lista = new List<MessageItem>();
        SledeciId = 1;
    }

    public static Poruke Instanca()
    {
        if (instanca == null)
        {
            instanca = new Poruke();
        }

        return instanca;
    }
}
```

Klasa ima isti princip kao `Zadaci`: jedna zajednička lista čuva stanje, dok proto određuje samo prenos.

### `MessageManagerService.cs`

```csharp
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

namespace JUN24Server.Services;

public class MessageManagerService : MessageManager.MessageManagerBase
{
    public override Task<MessageItem> SendMessage(
        MessageRequest request,
        ServerCallContext context)
    {
        var message = new MessageItem
        {
            Id = Poruke.Instanca().SledeciId++,
            Text = request.Text
        };

        Poruke.Instanca().Lista.Add(message);
        return Task.FromResult(message);
    }

    public override Task<Empty> DeleteMessage(
        MessageId request,
        ServerCallContext context)
    {
        var message = Poruke.Instanca().Lista
            .FirstOrDefault(p => p.Id == request.Id);

        if (message != null)
        {
            Poruke.Instanca().Lista.Remove(message);
        }

        return Task.FromResult(new Empty());
    }

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
}
```

### Objašnjenje servisa

`SendMessage` je unary: jedan zahtev ulazi, jedan `MessageItem` izlazi. Server pravi ID, upisuje poruku u listu i vraća baš dodat objekat.

`DeleteMessage` je takođe unary. `Empty` je stvarna generisana klasa, pa se vraća `new Empty()`, a ne `null`.

Potpis `ListMessages` nema `Task<MessageItem>` ni `Task<Empty>`. Server streaming metoda vraća običan `Task`, jer se podaci ne vraćaju preko `return`, već pozivima:

```csharp
await responseStream.WriteAsync(message);
```

`foreach` prolazi kroz memorijsku listu, a svaki element postaje posebna poruka u toku. Kada petlja završi i metoda se završi, server zatvara tok.

`async` i `await` su potrebni zato što je pisanje u mrežni tok asinhrona operacija.

### Šta napisati i naglasiti na ispitu

1. Kompletan `message.proto`.
2. `ListMessages` mora imati `returns (stream MessageItem)`.
3. Memorijsku listu i brojač ID-eva.
4. `ListMessages` mora imati `IServerStreamWriter<MessageItem>`.
5. Svaka poruka se šalje sa `await responseStream.WriteAsync(message)`.
6. Registracija: `app.MapGrpcService<MessageManagerService>();`.

---

## 3. APRIL 2026 - kalkulator

### Tekst zadatka

U .NET-u, koristeći gRPC, napisati servis koji simulira rad kalkulatora koji podržava operacije sabiranja, oduzimanja, množenja i deljenja celih brojeva. Svaka operacija prihvata dva cela broja kao argument, a povratna vrednost treba da sadrži polja `operand1`, `operand2`, `operacija` i `rezultat`.

Definisati i proto fajl `april.proto` koji daje specifikaciju servisa i poruka.

### Analiza zadatka

Svaka metoda prima tačno jednu poruku sa dva operanda i vraća tačno jednu poruku sa rezultatom. Sve četiri metode su unary RPC.

Nema liste i nema podatka koji mora da ostane zapamćen između poziva. Zato pomoćna memorijska klasa nije potrebna.

Sve metode mogu koristiti isti `OperacijaRequest` i isti `RezultatResponse`, jer imaju isti oblik ulaza i izlaza.

### `april.proto`

```proto
syntax = "proto3";

option csharp_namespace = "APRIL26Server";
package april26;

service Calculator {
    rpc Sabiranje(OperacijaRequest) returns (RezultatResponse);
    rpc Oduzimanje(OperacijaRequest) returns (RezultatResponse);
    rpc Mnozenje(OperacijaRequest) returns (RezultatResponse);
    rpc Deljenje(OperacijaRequest) returns (RezultatResponse);
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

### Objašnjenje proto fajla

`OperacijaRequest` predstavlja argument procedure. Dva odvojena C# argumenta ne pišemo direktno u `rpc`; pakujemo ih u jednu protobuf poruku.

`RezultatResponse` sadrži sve što tekst izričito zahteva. Operandi se ponavljaju u odgovoru da klijent zna nad kojim vrednostima je rezultat izračunat.

Nije potreban `Empty`, pa nema ni importa `google/protobuf/empty.proto`.

Nema `stream`, zato su sve metode unary.

### `CalculatorService.cs`

```csharp
using Grpc.Core;

namespace APRIL26Server.Services;

public class CalculatorService : Calculator.CalculatorBase
{
    public override Task<RezultatResponse> Sabiranje(
        OperacijaRequest request,
        ServerCallContext context)
    {
        return Task.FromResult(new RezultatResponse
        {
            Operand1 = request.Operand1,
            Operand2 = request.Operand2,
            Operacija = "Sabiranje",
            Rezultat = request.Operand1 + request.Operand2
        });
    }

    public override Task<RezultatResponse> Oduzimanje(
        OperacijaRequest request,
        ServerCallContext context)
    {
        return Task.FromResult(new RezultatResponse
        {
            Operand1 = request.Operand1,
            Operand2 = request.Operand2,
            Operacija = "Oduzimanje",
            Rezultat = request.Operand1 - request.Operand2
        });
    }

    public override Task<RezultatResponse> Mnozenje(
        OperacijaRequest request,
        ServerCallContext context)
    {
        return Task.FromResult(new RezultatResponse
        {
            Operand1 = request.Operand1,
            Operand2 = request.Operand2,
            Operacija = "Množenje",
            Rezultat = request.Operand1 * request.Operand2
        });
    }

    public override Task<RezultatResponse> Deljenje(
        OperacijaRequest request,
        ServerCallContext context)
    {
        if (request.Operand2 == 0)
        {
            throw new RpcException(new Status(
                StatusCode.InvalidArgument,
                "Delilac ne sme biti nula."));
        }

        return Task.FromResult(new RezultatResponse
        {
            Operand1 = request.Operand1,
            Operand2 = request.Operand2,
            Operacija = "Deljenje",
            Rezultat = request.Operand1 / request.Operand2
        });
    }
}
```

### Objašnjenje servisa

`Calculator.CalculatorBase` je generisan iz proto servisa. Svaki naziv `override` metode mora odgovarati RPC metodi.

`request.Operand1` i `request.Operand2` jesu C# svojstva generisana iz proto polja `operand1` i `operand2`. Protobuf kompajler koristi PascalCase za C# svojstva.

Sve metode vraćaju `Task<RezultatResponse>`. Nema čekanja mrežnog toka ili baze, pa koristimo `Task.FromResult`.

Kod deljenja je provera nule opravdana. `RpcException` sa statusom `InvalidArgument` šalje klijentu gRPC grešku umesto rušenja servera.

Pošto su operandi `int32`, rezultat deljenja je celobrojni. Na primer, `7 / 2` daje `3`. Ako se traži realan rezultat, u proto fajlu bi trebalo koristiti `double`.

### Šta napisati i naglasiti na ispitu

1. Kompletan `april.proto`.
2. Četiri unary metode u servisu.
3. Jedan zajednički request i jedan zajednički response tip.
4. Nema pomoćne liste jer servis ne pamti stanje.
5. Provera deljenja nulom je kratko i smisleno poboljšanje.
6. Registracija: `app.MapGrpcService<CalculatorService>();`.

---

## 4. KOL I 2026 - biblioteka knjiga

### Tekst zadatka

U .NET-u, koristeći gRPC, napisati servis za upravljanje bibliotekom knjiga. Servis treba da omogući klijentima dodavanje, pretragu i iznajmljivanje knjiga. Svaka knjiga ima polja `Id`, `title`, `author` i `available`.

Servis treba da podržava sledeće operacije:

- `AddBook`: dodaje novu knjigu u sistem;
- `SearchByAuthor`: prihvata ime autora i vraća tok podataka koji sadrži sve knjige tog autora sa svim informacijama;
- `BorrowBook`: prihvata ID knjige i postavlja polje `available` na `false`; ako knjiga nije dostupna, procedura vraća odgovarajuću grešku.

Definisati proto fajl `library.proto` koji daje specifikaciju servisa i poruka.

### Analiza zadatka

Biblioteka mora da pamti knjige i njihovo trenutno stanje dostupnosti, zato koristimo memorijsku listu i brojač ID-eva.

Tipovi metoda:

- `AddBook`: unary;
- `SearchByAuthor`: server streaming, jer vraća tok knjiga;
- `BorrowBook`: unary.

`AddBookRequest` ne mora da ima `id` ni `available`. Server generiše ID, a nova knjiga je dostupna.

Za grešku kod `BorrowBook` koristimo `RpcException` i odgovarajući gRPC status.

### `library.proto`

```proto
syntax = "proto3";

option csharp_namespace = "KOL26Server";
package kol26;

service LibraryManager {
    rpc AddBook(AddBookRequest) returns (BookResponse);
    rpc SearchByAuthor(AuthorRequest) returns (stream BookItem);
    rpc BorrowBook(BookIdRequest) returns (BookResponse);
}

message AddBookRequest {
    string title = 1;
    string author = 2;
}

message AuthorRequest {
    string author = 1;
}

message BookIdRequest {
    int32 id = 1;
}

message BookItem {
    int32 id = 1;
    string title = 2;
    string author = 3;
    bool available = 4;
}

message BookResponse {
    string description = 1;
    BookItem book = 2;
}
```

### Objašnjenje proto fajla

`BookItem` je konkretna knjiga koja se čuva i vraća klijentu.

`AddBookRequest` je odvojen od `BookItem` zato što klijent prilikom dodavanja daje samo naslov i autora. Server kontroliše ID i dostupnost.

`BookResponse` je rezultat operacije. Polje `description` daje tekstualnu potvrdu, dok polje `book` sadrži dodatu ili iznajmljenu knjigu.

`AuthorRequest` nosi kriterijum pretrage. Nije `Empty`, jer server mora znati kog autora tražimo.

`SearchByAuthor` ima `stream` samo ispred povratnog tipa. Klijent šalje jedan kriterijum, a server vraća nula, jednu ili više knjiga.

`AddBook` i `BorrowBook` vraćaju `BookResponse`. Ako knjiga ne postoji ili nije dostupna, `BorrowBook` ne vraća normalan odgovor, već gRPC status greške.

### Memorijska klasa `Knjige.cs`

```csharp
namespace KOL26Server;

public class Knjige
{
    public List<BookItem> Lista { get; set; }
    public int SledeciId { get; set; }

    private static Knjige? instanca;

    private Knjige()
    {
        Lista = new List<BookItem>();
        SledeciId = 1;
    }

    public static Knjige Instanca()
    {
        if (instanca == null)
        {
            instanca = new Knjige();
        }

        return instanca;
    }
}
```

### `LibraryManagerService.cs`

```csharp
using Grpc.Core;

namespace KOL26Server.Services;

public class LibraryManagerService : LibraryManager.LibraryManagerBase
{
    public override Task<BookResponse> AddBook(
        AddBookRequest request,
        ServerCallContext context)
    {
        var book = new BookItem
        {
            Id = Knjige.Instanca().SledeciId++,
            Title = request.Title,
            Author = request.Author,
            Available = true
        };

        Knjige.Instanca().Lista.Add(book);
        return Task.FromResult(new BookResponse
        {
            Description = "Knjiga je uspešno dodata.",
            Book = book
        });
    }

    public override async Task SearchByAuthor(
        AuthorRequest request,
        IServerStreamWriter<BookItem> responseStream,
        ServerCallContext context)
    {
        foreach (var book in Knjige.Instanca().Lista)
        {
            if (book.Author == request.Author)
            {
                await responseStream.WriteAsync(book);
            }
        }
    }

    public override Task<BookResponse> BorrowBook(
        BookIdRequest request,
        ServerCallContext context)
    {
        var book = Knjige.Instanca().Lista
            .FirstOrDefault(k => k.Id == request.Id);

        if (book == null)
        {
            throw new RpcException(new Status(
                StatusCode.NotFound,
                "Knjiga ne postoji."));
        }

        if (!book.Available)
        {
            throw new RpcException(new Status(
                StatusCode.FailedPrecondition,
                "Knjiga nije dostupna."));
        }

        book.Available = false;
        return Task.FromResult(new BookResponse
        {
            Description = "Knjiga je uspešno iznajmljena.",
            Book = book
        });
    }
}
```

### Objašnjenje servisa

`AddBook` pravi kompletan `BookItem` od podataka iz zahteva i serverskih vrednosti. `Available = true` je početno stanje nove knjige. Zatim vraća `BookResponse` sa opisom uspeha i dodatom knjigom.

`SearchByAuthor` je server streaming. Za svaku knjigu koja odgovara kriterijumu poziva se `WriteAsync`. Ako nijedna knjiga ne odgovara, metoda samo završi tok bez elemenata.

Pretraga pomoću `==` je dovoljna za ispit. U pravoj aplikaciji moglo bi se koristiti poređenje koje ne razlikuje velika i mala slova.

`BorrowBook` prvo proverava postojanje, zatim poslovni uslov dostupnosti. `NotFound` znači da ID ne postoji. `FailedPrecondition` znači da knjiga postoji, ali trenutno stanje ne dozvoljava iznajmljivanje.

Tek nakon obe provere polje `Available` postaje `false`. Menja se objekat koji je već u memorijskoj listi, a klijentu se vraća `BookResponse`.

### Šta napisati i naglasiti na ispitu

1. Kompletan `library.proto`.
2. `SearchByAuthor` mora vratiti `stream BookItem`.
3. Memorijsku listu knjiga i brojač ID-eva.
4. Server streaming implementaciju sa `IServerStreamWriter<BookItem>`.
5. `AddBook` i `BorrowBook` vraćaju `BookResponse`.
6. `BorrowBook` menja `Available` i koristi `RpcException` za grešku.
7. Registracija: `app.MapGrpcService<LibraryManagerService>();`.

---

# II deo: dodatni zadaci za vežbu

Kod sledeća tri zadatka prvo pročitaj samo tekst i pokušaj samostalno da napišeš proto fajl i potpis metode. Tek zatim pogledaj rešenje.

## 5. Dodatni zadatak - obrada toka merenja

### Tekst zadatka

U .NET-u, koristeći gRPC, napisati servis za obradu celobrojnih merenja sa senzora. Servis treba da podrži operaciju `UploadReadings`. Klijent kroz tok šalje proizvoljan broj merenja. Kada završi slanje, server vraća jednu poruku koja sadrži broj primljenih merenja, njihov zbir i prosečnu vrednost.

Definisati proto fajl `measurements.proto` i implementirati serversku metodu. Podatke nije potrebno čuvati nakon završetka poziva.

### Samostalna analiza

Pre gledanja rešenja odgovori:

1. Da li server unapred zna koliko merenja stiže?
2. Na kojoj strani proto potpisa treba da stoji `stream`?
3. Da li serverska metoda vraća `Task`, ili `Task<ReadingSummary>`?
4. Da li je potrebna singleton lista?

### Rešenje: prepoznavanje principa

Klijent šalje više poruka, a server na kraju vraća jednu sumarnu poruku. To je client streaming:

```proto
rpc UploadReadings(stream Reading) returns (ReadingSummary);
```

`stream` je samo uz argument. Server ne šalje tok, već jedan odgovor.

Singleton nije potreban, jer su zbir i broj lokalne promenljive jednog RPC poziva i ne koriste se posle njegovog završetka.

### `measurements.proto`

```proto
syntax = "proto3";

option csharp_namespace = "MeasurementsServer";
package measurements;

service MeasurementManager {
    rpc UploadReadings(stream Reading) returns (ReadingSummary);
}

message Reading {
    int32 value = 1;
}

message ReadingSummary {
    int32 count = 1;
    int64 sum = 2;
    double average = 3;
}
```

### Objašnjenje proto fajla

Svako pojedinačno merenje je jedna `Reading` poruka.

Broj poruka nije zapisan u proto fajlu. Tok se završava kada klijent signalizira da više neće slati elemente.

Za zbir koristimo `int64`, jer zbir većeg broja `int32` vrednosti može biti veći od opsega jednog `int32`.

Prosek je `double`, jer ne mora biti ceo broj.

### `MeasurementManagerService.cs`

```csharp
using Grpc.Core;

namespace MeasurementsServer.Services;

public class MeasurementManagerService
    : MeasurementManager.MeasurementManagerBase
{
    public override async Task<ReadingSummary> UploadReadings(
        IAsyncStreamReader<Reading> requestStream,
        ServerCallContext context)
    {
        int count = 0;
        long sum = 0;

        await foreach (var reading in requestStream.ReadAllAsync())
        {
            count++;
            sum += reading.Value;
        }

        return new ReadingSummary
        {
            Count = count,
            Sum = sum,
            Average = count == 0 ? 0 : (double)sum / count
        };
    }
}
```

### Objašnjenje servisa

`IAsyncStreamReader<Reading>` predstavlja ulazni tok koji dolazi od klijenta.

`ReadAllAsync()` asinhrono daje element po element dok klijent ne završi slanje.

`count` i `sum` postoje samo za vreme ovog poziva. Svaki novi poziv počinje od nule.

Metoda je `async Task<ReadingSummary>` zato što asinhrono čita tok, a nakon završetka vraća jednu poruku.

Na klijentu bi se elementi slali preko `RequestStream.WriteAsync`, a zatim bi morao da se pozove `RequestStream.CompleteAsync`. Bez završetka ulaznog toka server ne zna da može da izračuna konačan rezultat.

Provera `count == 0` sprečava deljenje nulom ako klijent ne pošalje nijedno merenje.

### Šta napisati i naglasiti na ispitu

1. `stream` stoji uz ulazni `Reading`.
2. Povratna vrednost nije stream, nego jedan `ReadingSummary`.
3. Service potpis koristi `IAsyncStreamReader<Reading>`.
4. Elementi se čitaju pomoću `await foreach`.
5. Server odgovara tek kada se ulazni tok završi.
6. Registracija: `app.MapGrpcService<MeasurementManagerService>();`.

---

## 6. Dodatni zadatak - provera parova brojeva

### Tekst zadatka

U .NET-u, koristeći gRPC, napisati servis za proveru parova celih brojeva. Klijent kroz tok šalje poruke koje sadrže brojeve `a` i `b`. Za svaki primljeni par server odmah kroz povratni tok šalje poruku sa prosleđenim brojevima i poljem `isSquare`, koje ima vrednost `true` ako je `b = a * a`, odnosno `false` u suprotnom.

Definisati proto fajl `pairs.proto` i implementirati serversku metodu `CheckPairs`. Podatke nije potrebno trajno čuvati.

### Samostalna analiza

Pre gledanja rešenja odgovori:

1. Koliko poruka šalje klijent?
2. Koliko poruka vraća server?
3. Da li se rezultat čeka do kraja celog ulaznog toka?
4. Koja dva stream interfejsa mora imati serverska metoda?

### Rešenje: prepoznavanje principa

Klijent šalje tok i server vraća tok. To je bidirectional streaming:

```proto
rpc CheckPairs(stream NumberPair) returns (stream PairResult);
```

Server može da obradi svaki par i odmah pošalje rezultat. Ulazni i izlazni tok su odvojeni.

### `pairs.proto`

```proto
syntax = "proto3";

option csharp_namespace = "PairsServer";
package pairs;

service PairManager {
    rpc CheckPairs(stream NumberPair) returns (stream PairResult);
}

message NumberPair {
    int32 a = 1;
    int32 b = 2;
}

message PairResult {
    int32 a = 1;
    int32 b = 2;
    bool is_square = 3;
}
```

### Objašnjenje proto fajla

`NumberPair` je jedna ulazna poruka. Tok čini više takvih poruka.

`PairResult` je jedna izlazna poruka. U ovom zadatku server šalje jedan rezultat za svaki primljeni par, ali gRPC generalno ne zahteva da broj ulaznih i izlaznih poruka bude isti.

`stream` se nalazi i uz argument i uz povratni tip, što direktno označava bidirectional streaming.

Proto polje `is_square` u generisanoj C# klasi postaje svojstvo `IsSquare`.

### `PairManagerService.cs`

```csharp
using Grpc.Core;

namespace PairsServer.Services;

public class PairManagerService : PairManager.PairManagerBase
{
    public override async Task CheckPairs(
        IAsyncStreamReader<NumberPair> requestStream,
        IServerStreamWriter<PairResult> responseStream,
        ServerCallContext context)
    {
        await foreach (var pair in requestStream.ReadAllAsync())
        {
            var result = new PairResult
            {
                A = pair.A,
                B = pair.B,
                IsSquare = pair.B == pair.A * pair.A
            };

            await responseStream.WriteAsync(result);
        }
    }
}
```

### Objašnjenje servisa

`requestStream` je tok od klijenta ka serveru. `responseStream` je tok od servera ka klijentu.

Metoda vraća običan `Task`, a ne `Task<PairResult>`, zato što se rezultati šalju preko `responseStream`.

Za svaki pročitani par pravi se nova izlazna poruka i odmah šalje pomoću `WriteAsync`. Server ne mora da čeka kraj celog ulaznog toka.

Na klijentu se čitanje odgovora obično pokreće kao poseban `Task`, dok glavni deo programa nastavlja da piše u `RequestStream`. Razlog je što su ulazni i izlazni tok nezavisni.

Kada klijent završi slanje, mora pozvati `RequestStream.CompleteAsync`, a zatim sačekati da se čitanje odgovora završi.

Ovo je isti princip kao u zadatku `Oktobar2`: ne treba posle svakog slanja blokirati program čekanjem završetka celog poziva.

### Šta napisati i naglasiti na ispitu

1. `stream` mora biti sa obe strane RPC potpisa.
2. Service metoda prima i `IAsyncStreamReader` i `IServerStreamWriter`.
3. Metoda vraća `Task`, jer rezultate šalje kroz izlazni tok.
4. `ReadAllAsync` čita poruke jednu po jednu.
5. `WriteAsync` odmah šalje pojedinačan odgovor.
6. Registracija: `app.MapGrpcService<PairManagerService>();`.

---

## 7. Dodatni zadatak - upravljanje proizvodima

### Tekst zadatka

U .NET-u, koristeći gRPC, napraviti servis za upravljanje proizvodima u magacinu. Svaki proizvod ima polja `id`, `name` i `quantity`.

Servis treba da podrži:

- `AddProduct`: prima naziv i količinu, dodaje proizvod i vraća dodat proizvod;
- `ChangeQuantity`: prima ID proizvoda i novu količinu, menja stanje i vraća izmenjen proizvod;
- `ListProducts`: vraća listu svih proizvoda kao jedan odgovor;
- `ListLowStock`: prima maksimalnu količinu i vraća tok svih proizvoda čija je količina manja ili jednaka zadatoj.

Ako proizvod sa zadatim ID-em ne postoji, `ChangeQuantity` treba da vrati odgovarajuću gRPC grešku.

Definisati proto fajl `inventory.proto` i implementirati gRPC server.

### Samostalna analiza

Pre gledanja rešenja odredi:

1. Koje metode su unary?
2. Gde se koristi `repeated`?
3. Gde se koristi `stream`?
4. Da li `ListProducts` i `ListLowStock` mogu čitati istu memorijsku listu?
5. Da li je potrebna klasa koja pamti stanje?

### Rešenje: prepoznavanje principa

`AddProduct`, `ChangeQuantity` i `ListProducts` su unary. `ListProducts` vraća jednu poruku sa `repeated Product`.

`ListLowStock` je server streaming, jer vraća proizvode jedan po jedan kroz tok.

Obe metode čitaju istu `List<Product>`. Razlikuje se samo način prenosa prema klijentu.

### `inventory.proto`

```proto
syntax = "proto3";

import "google/protobuf/empty.proto";

option csharp_namespace = "InventoryServer";
package inventory;

service InventoryManager {
    rpc AddProduct(AddProductRequest) returns (Product);
    rpc ChangeQuantity(ChangeQuantityRequest) returns (Product);
    rpc ListProducts(google.protobuf.Empty) returns (ProductList);
    rpc ListLowStock(StockLimitRequest) returns (stream Product);
}

message AddProductRequest {
    string name = 1;
    int32 quantity = 2;
}

message ChangeQuantityRequest {
    int32 id = 1;
    int32 quantity = 2;
}

message StockLimitRequest {
    int32 max_quantity = 1;
}

message Product {
    int32 id = 1;
    string name = 2;
    int32 quantity = 3;
}

message ProductList {
    repeated Product products = 1;
}
```

### Objašnjenje proto fajla

`AddProductRequest` nema ID jer ga određuje server.

`ChangeQuantityRequest` mora imati i ID za pronalaženje i novu količinu koja se upisuje.

`ListProducts` nema kriterijum, zato prima `Empty`. Vraća jednu `ProductList` poruku, a u njoj se nalazi `repeated Product`.

`ListLowStock` prima kriterijum `max_quantity`, pa ne koristi `Empty`. Vraća `stream Product`, jer svaki pronađen proizvod šalje zasebno.

Ovaj zadatak namerno sadrži i `repeated` i `stream` da pokaže da oba mogu koristiti istu memorijsku listu.

### Memorijska klasa `Proizvodi.cs`

```csharp
namespace InventoryServer;

public class Proizvodi
{
    public List<Product> Lista { get; set; }
    public int SledeciId { get; set; }

    private static Proizvodi? instanca;

    private Proizvodi()
    {
        Lista = new List<Product>();
        SledeciId = 1;
    }

    public static Proizvodi Instanca()
    {
        if (instanca == null)
        {
            instanca = new Proizvodi();
        }

        return instanca;
    }
}
```

### `InventoryManagerService.cs`

```csharp
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

namespace InventoryServer.Services;

public class InventoryManagerService
    : InventoryManager.InventoryManagerBase
{
    public override Task<Product> AddProduct(
        AddProductRequest request,
        ServerCallContext context)
    {
        var product = new Product
        {
            Id = Proizvodi.Instanca().SledeciId++,
            Name = request.Name,
            Quantity = request.Quantity
        };

        Proizvodi.Instanca().Lista.Add(product);
        return Task.FromResult(product);
    }

    public override Task<Product> ChangeQuantity(
        ChangeQuantityRequest request,
        ServerCallContext context)
    {
        var product = Proizvodi.Instanca().Lista
            .FirstOrDefault(p => p.Id == request.Id);

        if (product == null)
        {
            throw new RpcException(new Status(
                StatusCode.NotFound,
                "Proizvod ne postoji."));
        }

        product.Quantity = request.Quantity;
        return Task.FromResult(product);
    }

    public override Task<ProductList> ListProducts(
        Empty request,
        ServerCallContext context)
    {
        var response = new ProductList();
        response.Products.AddRange(Proizvodi.Instanca().Lista);

        return Task.FromResult(response);
    }

    public override async Task ListLowStock(
        StockLimitRequest request,
        IServerStreamWriter<Product> responseStream,
        ServerCallContext context)
    {
        foreach (var product in Proizvodi.Instanca().Lista)
        {
            if (product.Quantity <= request.MaxQuantity)
            {
                await responseStream.WriteAsync(product);
            }
        }
    }
}
```

### Objašnjenje servisa

`AddProduct` i `ChangeQuantity` su obične unary metode. Jedan objekat ulazi i jedan izlazi.

`ChangeQuantity` koristi `NotFound` ako nema proizvoda sa traženim ID-em. To je preciznije od vraćanja `null`, jer gRPC klijent dobija status i poruku greške.

`ListProducts` pravi jednu `ProductList` poruku i sa `AddRange` popunjava njeno `repeated` polje. Poziv se završava jednim odgovorom.

`ListLowStock` prolazi kroz istu listu, ali svaki element koji zadovoljava uslov šalje zasebno preko `responseStream`.

Iz proto polja `max_quantity` generiše se C# svojstvo `MaxQuantity`.

Nijedan od ova dva pristupa ne menja način čuvanja podataka. `Proizvodi.Instanca().Lista` ostaje obična memorijska lista.

### Šta napisati i naglasiti na ispitu

1. Kompletan `inventory.proto`.
2. `ProductList` koristi `repeated Product`.
3. `ListLowStock` koristi `returns (stream Product)`.
4. Oba čitaju istu memorijsku listu.
5. `ChangeQuantity` koristi `RpcException` sa `NotFound`.
6. Registracija: `app.MapGrpcService<InventoryManagerService>();`.

---

# Završna kontrolna lista za svaki novi zadatak

Pre predaje proveri sledeće:

1. Da li svaka RPC metoda ima request i response poruku?
2. Da li si dodao `Empty` import samo kada ga koristiš?
3. Da li sva polja imaju različite tagove unutar svoje poruke?
4. Da li su `int32`, `string`, `bool`, `double` i drugi tipovi pravilno izabrani?
5. Da li si razlikovao listu u jednoj poruci (`repeated`) od toka poruka (`stream`)?
6. Da li `stream` stoji na pravoj strani RPC potpisa?
7. Da li serverska klasa nasleđuje generisanu `ImeServisaBase` klasu?
8. Da li unary metoda vraća `Task<Response>`?
9. Da li server streaming metoda koristi `IServerStreamWriter<Response>` i vraća `Task`?
10. Da li client streaming metoda koristi `IAsyncStreamReader<Request>` i vraća `Task<Response>`?
11. Da li bidirectional metoda koristi oba stream interfejsa i vraća `Task`?
12. Da li postoji memorijska lista samo kada zadatak zahteva pamćenje stanja?
13. Da li je ID generisan na serveru ako klijent ne treba da ga bira?
14. Da li si obradio očigledne greške, kao što su nepostojeći ID, nedostupna knjiga ili deljenje nulom?
15. Da li si naveo `app.MapGrpcService<ImeServisaService>();`?

## Najkraći šablon za pamćenje

```proto
syntax = "proto3";

option csharp_namespace = "ServerNamespace";
package paket;

service ImeServisa {
    rpc Metoda(Request) returns (Response);
}

message Request {
    int32 vrednost = 1;
}

message Response {
    int32 rezultat = 1;
}
```

```csharp
using Grpc.Core;

public class ImeServisaService : ImeServisa.ImeServisaBase
{
    public override Task<Response> Metoda(
        Request request,
        ServerCallContext context)
    {
        return Task.FromResult(new Response
        {
            Rezultat = request.Vrednost
        });
    }
}
```

Suština svakog zadatka je: tačno prevesti tekst u proto ugovor, izabrati pravi tip RPC metode i implementirati samo serversku logiku koju taj ugovor zahteva.

---

## Puškica

```csharp
// ======================== PROTO I .NET POCETAK ========================

// Ako RPC nema stvaran ulaz ili izlaz:
// import "google/protobuf/empty.proto";
// rpc ListAll(google.protobuf.Empty) returns (Response);

// Server .csproj:
// <Protobuf Include="Protos\naziv.proto" GrpcServices="Server" />

// Klijent .csproj:
// <Protobuf Include="Protos\naziv.proto" GrpcServices="Client" />

using Grpc.Core;
using Google.Protobuf.WellKnownTypes; // samo ako koristimo Empty

// Server Program.cs:
builder.Services.AddGrpc();
app.MapGrpcService<ImeServisaService>();

// ======================== PROTO POVRATNI TIPOVI ========================

// Jedan zahtev -> jedan odgovor, unary:
// rpc Add(Request) returns (Response);

// Cela lista u JEDNOJ poruci:
// rpc ListAll(Empty) returns (ItemList);
// message ItemList { repeated Item items = 1; }

// Jedan zahtev -> elementi dolaze JEDAN PO JEDAN:
// rpc ListAll(Empty) returns (stream Item);

// Klijent salje elemente JEDAN PO JEDAN -> server vraca jedan odgovor:
// rpc Upload(stream Item) returns (Response);

// Obe strane salju tok:
// rpc Process(stream Request) returns (stream Response);

// repeated = kolekcija unutar jedne poruke; velicina/kolekcija spremna pre slanja.
// stream   = vise posebnih poruka kroz vreme; broj poruka ne mora biti poznat.
// List<T>  = cuvanje podataka na serveru; nije nacin prenosa.

// ======================== MEMORIJSKA KLASA / REPOSITORY ========================

public class Memorija
{
    public List<Item> Lista { get; set; } = new();
    public int SledeciId { get; set; } = 1;

    private static Memorija? instanca;
    private Memorija() { }

    public static Memorija Instanca()
    {
        if (instanca == null)
            instanca = new Memorija();

        return instanca;
    }
}

// Dodavanje:
var item = new Item
{
    Id = Memorija.Instanca().SledeciId++,
    Name = request.Name
};
Memorija.Instanca().Lista.Add(item);

// Pretraga po ID-u; vraca objekat ili null:
var found = Memorija.Instanca().Lista
    .FirstOrDefault(x => x.Id == request.Id);

// Item je klasa, zato je found referenca na ISTI objekat iz liste, nije kopija:
if (found != null)
    found.Available = false;

// Brisanje:
if (found != null)
    Memorija.Instanca().Lista.Remove(found);

// Filtriranje vise elemenata:
var filtered = Memorija.Instanca().Lista
    .Where(x => x.Name == request.Name);

// Ako ID ne postoji:
if (found == null)
{
    throw new RpcException(new Status(
        StatusCode.NotFound,
        "Element ne postoji."));
}

// ======================== PRIMAMO JEDAN, SALJEMO JEDAN ========================

// Proto: rpc Metoda(Request) returns (Response);
public override Task<Response> Metoda(
    Request request,
    ServerCallContext context)
{
    // Obican ulaz citamo direktno iz request objekta:
    int value = request.Value;

    var response = new Response
    {
        Result = value * 2
    };

    // Obican odgovor vracamo preko return:
    return Task.FromResult(response);
}

// Kada nemamo konkretan odgovor:
// Proto: rpc Delete(IdRequest) returns (google.protobuf.Empty);
public override Task<Empty> Delete(
    IdRequest request,
    ServerCallContext context)
{
    var itemToDelete = Memorija.Instanca().Lista
        .FirstOrDefault(x => x.Id == request.Id);

    if (itemToDelete != null)
        Memorija.Instanca().Lista.Remove(itemToDelete);

    return Task.FromResult(new Empty());
}

// ======================== REPEATED: SALJEMO CELU LISTU ODJEDNOM ========================

// Proto:
// rpc ListAll(Empty) returns (ItemList);
// message ItemList { repeated Item items = 1; }
public override Task<ItemList> ListAll(
    Empty request,
    ServerCallContext context)
{
    var response = new ItemList();
    response.Items.AddRange(Memorija.Instanca().Lista);

    return Task.FromResult(response);
}

// ======================== SERVER STREAMING: PRIMAMO JEDAN, SALJEMO TOK ========================

// Proto: rpc ListAll(Request) returns (stream Item);
public override async Task ListAllStream(
    Request request,
    IServerStreamWriter<Item> responseStream,
    ServerCallContext context)
{
    foreach (var current in Memorija.Instanca().Lista)
    {
        // WriteAsync koristimo kada SERVER salje element u izlazni tok:
        await responseStream.WriteAsync(current);
    }

    // Nema return Item/Empty; zavrsetak metode zavrsava serverski tok.
}

// ======================== CLIENT STREAMING: PRIMAMO TOK, SALJEMO JEDAN ========================

// Proto: rpc Upload(stream Item) returns (Response);
public override async Task<Response> Upload(
    IAsyncStreamReader<Item> requestStream,
    ServerCallContext context)
{
    int count = 0;

    // ReadAllAsync koristimo kada SERVER prima ulazni tok od klijenta:
    await foreach (var current in requestStream.ReadAllAsync())
    {
        count++;
        Memorija.Instanca().Lista.Add(current);
    }

    // Kada klijent zavrsi tok, server vraca jedan obican odgovor:
    return new Response { Result = count };
}

// Klijent ovim javlja da vise nema ulaznih elemenata:
await call.RequestStream.CompleteAsync();

// ======================== BIDIRECTIONAL: PRIMAMO TOK, SALJEMO TOK ========================

// Proto: rpc Process(stream Request) returns (stream Response);
public override async Task Process(
    IAsyncStreamReader<Request> requestStream,
    IServerStreamWriter<Response> responseStream,
    ServerCallContext context)
{
    // Citamo svaki zahtev koji stigne:
    await foreach (var current in requestStream.ReadAllAsync())
    {
        var response = new Response
        {
            Result = current.Value * 2
        };

        // Za svaki zahtev mozemo odmah poslati odgovor:
        await responseStream.WriteAsync(response);
    }
}

// ReadAllAsync = citanje toka koji klijent salje serveru.
// WriteAsync   = upis jedne poruke u tok koji server salje klijentu.
// Task<T>      = rezultat se vraca preko return.
// Task         = stream rezultat se salje preko responseStream, nema return poruke.

// ======================== BRZA PROVERA ========================

// Nema "tok/stream/jedan po jedan" u tekstu -> najverovatnije unary.
// "Lista svih" kao jedan odgovor             -> repeated u wrapper poruci.
// "Vraca tok"                                -> server streaming.
// "Prima tok"                                -> client streaming.
// "Prima tok i vraca tok"                    -> bidirectional streaming.
// Obican primljeni podatak cita se direktno iz request objekta.
// Primljeni tok cita se pomocu ReadAllAsync.
// Obican odgovor salje se preko return.
// Tok odgovora salje se pomocu WriteAsync.
```

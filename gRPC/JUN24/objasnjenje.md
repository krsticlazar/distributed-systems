# JUN24 - objasnjenje projekta

## Tekst zadatka

[JUN 2024]

U .NET-u, koristeci gRPC, kreirati servis za upravljanje listom poruka. Servis treba da omoguci klijentima da dodaju i brisu poruke, i prikazu sve poruke.

Definisati proto fajl (message.proto) koji daje specifikaciju servisa i poruka. Servis treba da podrzava sledece operacije:

- SendMessage: dodaje novu poruku na listu.
- DeleteMessage: brise poruku sa zadatim ID-jem.
- ListMessages: dobije tok podataka koji sadrzi sve poruke.

Implementirati gRPC server definisan u message.proto fajlu.

## Proto fajl

Glavni fajl je `message.proto`. Ovo je najbitniji deo za ispit, jer se iz njega vidi ceo gRPC ugovor.

```proto
syntax = "proto3";
```

Koristi se proto3 sintaksa.

```proto
import "google/protobuf/empty.proto";
```

Uvozimo praznu poruku `google.protobuf.Empty`. Ona se koristi kada metoda nema stvarni ulazni ili izlazni podatak, ali gRPC metoda ipak mora imati argument i povratnu vrednost.

```proto
option csharp_namespace = "JUN24Server";
package jun24;
```

`csharp_namespace` odredjuje namespace generisanih C# klasa. Na klijentu je isti proto fajl, samo je namespace `JUN24Client`. `package jun24` mora da se poklapa na serveru i klijentu.

```proto
service MessageManager {
  rpc SendMessage(MessageRequest) returns (MessageItem);
  rpc DeleteMessage(MessageId) returns (google.protobuf.Empty);
  rpc ListMessages(google.protobuf.Empty) returns (stream MessageItem);
}
```

Ovo je servis sa tri RPC metode.

`SendMessage(MessageRequest) returns (MessageItem)` je unary RPC. Klijent salje jednu poruku sa tekstom, server dodaje poruku u listu i vraca dodatu poruku sa dodeljenim ID-em.

`DeleteMessage(MessageId) returns (google.protobuf.Empty)` je unary RPC. Klijent salje ID poruke koju treba obrisati. Server ne mora da vrati konkretan podatak, zato se vraca `Empty`.

`ListMessages(google.protobuf.Empty) returns (stream MessageItem)` je server streaming RPC. Klijent salje jedan prazan zahtev, a server vraca tok poruka jednu po jednu. Ovo je najbitnija specificnost zadatka, jer tekst kaze "tok podataka koji sadrzi sve poruke".

```proto
message MessageRequest {
  string text = 1;
}
```

Ovo je argument za `SendMessage`. Sadrzi samo tekst poruke.

```proto
message MessageId {
  int32 id = 1;
}
```

Ovo je argument za `DeleteMessage`. Sadrzi ID poruke koju brisemo.

```proto
message MessageItem {
  int32 id = 1;
  string text = 2;
}
```

Ovo je model jedne poruke u listi. Ima ID i tekst.

## Zasto je ListMessages stream

Kod KOL24 zadatka `ListTasks` je vracao `TaskList` sa `repeated TaskItem`, jer je tekst rekao da treba dobiti listu.

Ovde tekst eksplicitno kaze "tok podataka", pa metoda treba da bude:

```proto
rpc ListMessages(google.protobuf.Empty) returns (stream MessageItem);
```

Razlika:

- `repeated MessageItem` znaci da se cela lista vraca kao jedna poruka.
- `stream MessageItem` znaci da server salje poruke jednu po jednu kroz tok.

## Implementiran servis

Serverski servis je `MessageManagerService`.

```csharp
public class MessageManagerService : MessageManager.MessageManagerBase
```

Klasa nasledjuje generisanu baznu klasu iz proto fajla.

`SendMessage` pravi novu poruku:

```csharp
var message = new MessageItem
{
    Id = Poruke.Instanca().SledeciId++,
    Text = request.Text
};

Poruke.Instanca().Lista.Add(message);
return Task.FromResult(message);
```

Server dodeljuje ID, upisuje tekst i dodaje poruku u memorijsku listu.

`DeleteMessage` brise poruku po ID-u:

```csharp
var message = Poruke.Instanca().Lista.FirstOrDefault(p => p.Id == request.Id);

if (message != null)
{
    Poruke.Instanca().Lista.Remove(message);
}

return Task.FromResult(new Empty());
```

Ako poruka postoji, uklanja se iz liste. Povratna vrednost je prazna poruka.

`ListMessages` vraca stream:

```csharp
public override async Task ListMessages(Empty request, IServerStreamWriter<MessageItem> responseStream, ServerCallContext context)
{
    foreach (var message in Poruke.Instanca().Lista)
    {
        await responseStream.WriteAsync(message);
    }
}
```

Ovo je server streaming implementacija. Server prolazi kroz listu i svaku poruku salje posebno kroz `responseStream`.

## Klasa za memoriju

Klasa `Poruke` cuva podatke u memoriji.

```csharp
public List<MessageItem> Lista { get; set; }
public int SledeciId { get; set; }
```

`Lista` cuva sve poruke. `SledeciId` se koristi da svaka nova poruka dobije novi ID.

```csharp
public static Poruke Instanca()
```

Vraca istu instancu klase, da se lista poruka ne izgubi izmedju RPC poziva.

## Kratak primer pokretanja

1. Otvori `JUN24/JUN24.slnx`.
2. Pokreni server i klijent zajedno.
3. Server koristi `https://localhost:7225`.
4. Klijent nudi meni.

Primer unosa:

```text
1
Prva poruka
1
Druga poruka
3
2
1
3
```

Ocekivanje:

- prve dve komande dodaju dve poruke;
- komanda `3` cita server streaming tok i prikazuje sve poruke;
- komanda `2` brise poruku sa ID-em `1`;
- poslednja komanda `3` prikazuje samo preostale poruke.

## Sta napisati na ispitu

Najbitnije je napisati proto fajl:

```proto
syntax = "proto3";

import "google/protobuf/empty.proto";

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

Zatim napisati da server ima listu poruka u memoriji i brojac ID-eva:

```csharp
private static List<MessageItem> messages = new();
private static int nextId = 1;
```

I napisati osnovu servisa:

```csharp
public override Task<MessageItem> SendMessage(MessageRequest request, ServerCallContext context)
{
    var message = new MessageItem { Id = nextId++, Text = request.Text };
    messages.Add(message);
    return Task.FromResult(message);
}

public override Task<Empty> DeleteMessage(MessageId request, ServerCallContext context)
{
    var message = messages.FirstOrDefault(m => m.Id == request.Id);
    if (message != null)
    {
        messages.Remove(message);
    }

    return Task.FromResult(new Empty());
}

public override async Task ListMessages(Empty request, IServerStreamWriter<MessageItem> responseStream, ServerCallContext context)
{
    foreach (var message in messages)
    {
        await responseStream.WriteAsync(message);
    }
}
```

Obavezno naglasiti: `ListMessages` je server streaming zato sto klijent salje jedan zahtev, a server vraca tok poruka.


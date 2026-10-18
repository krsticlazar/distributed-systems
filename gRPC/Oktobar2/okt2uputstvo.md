# Oktobar2 - sta treba da promenis

Ne diram ti projekat. Ovde ti ostavljam samo kod koji treba da zamenis i kratko objasnjenje zasto.

## 1\. Server proto

Fajl:

* `Oktobar2/Oktobar2Server/Protos/okt2.proto`

Zameni sadrzaj ovim:

```proto
syntax = "proto3";

option csharp\\\_namespace = "Oktobar2Server";
package oktobar2;

service Okt2Service {
  rpc ObradiBrojeve(stream NumberData) returns (stream RetMessage);
}

message NumberData {
  int32 a = 1;
  int32 b = 2;
}

message RetMessage {
  string message = 1;
}
```

Zasto:

* tekst zadatka kaze da jedan element toka sadrzi dva broja `a` i `b`
* zato jedna poruka mora da ima oba polja
* ne treba da saljes po jedan broj pa da server sklapa parove

## 2\. Client proto

Fajl:

* `Oktobar2/Oktobar2Client/Protos/okt2.proto`

Zameni sadrzaj ovim:

```proto
syntax = "proto3";

option csharp\\\_namespace = "Oktobar2Client";
package oktobar2;

service Okt2Service {
  rpc ObradiBrojeve(stream NumberData) returns (stream RetMessage);
}

message NumberData {
  int32 a = 1;
  int32 b = 2;
}

message RetMessage {
  string message = 1;
}
```

Napomena:

* od `package oktobar2;` pa na dole proto mora da bude isti na serveru i na klijentu
* jedino `csharp\\\_namespace` sme da bude razlicit

## 3\. Server service

Fajl:

* `Oktobar2/Oktobar2Server/Services/Oktobar2Service.cs`

Zameni logiku metodom koja za svaku primljenu poruku odmah vraca odgovor.

Koristi ovakav kod:

```csharp
using Grpc.Core;

namespace Oktobar2Server.Services;

public class Oktobar2Service : Okt2Service.Okt2ServiceBase
{
    public override async Task ObradiBrojeve(
        IAsyncStreamReader<NumberData> requestStream,
        IServerStreamWriter<RetMessage> responseStream,
        ServerCallContext context)
    {
        await foreach (var request in requestStream.ReadAllAsync())
        {
            string odgovor = request.B == request.A \\\* request.A ? "Da" : "Ne";

            await responseStream.WriteAsync(new RetMessage
            {
                Message = odgovor
            });
        }
    }
}
```

Sta ovde vise ne treba:

* `NumState`
* `redniBroj`
* cekanje drugog elementa

Zasto:

* sada jedna poruka vec sadrzi i `a` i `b`
* server zato moze odmah da obradi svaku poruku i odmah da posalje odgovor

## 4\. Client Program.cs

Fajl:

* `Oktobar2/Oktobar2Client/Program.cs`

Najveca greska ti je bila sto si radio:

```csharp
client.ObradiBrojeve()
```

unutar petlje.

To pravi novi streaming poziv za svaki unos.

Ispravno je:

* napravis jedan `call`
* kroz njega saljes vise poruka
* paralelno citas odgovore

Koristi ovakav kod:

```csharp
using Oktobar2Client;
using Grpc.Net.Client;

using var channel = GrpcChannel.ForAddress("https://localhost:7286");
var client = new Okt2Service.Okt2ServiceClient(channel);

using var call = client.ObradiBrojeve();

var citanjeOdgovora = Task.Run(async () =>
{
    await foreach (var odgovor in call.ResponseStream.ReadAllAsync())
    {
        Console.WriteLine($"Odgovor servera: {odgovor.Message}");
    }
});

while (true)
{
    Console.Write("Unesi a (ili x za kraj): ");
    string? aUnos = Console.ReadLine();

    if (string.Equals(aUnos, "x", StringComparison.OrdinalIgnoreCase))
    {
        break;
    }

    Console.Write("Unesi b: ");
    string? bUnos = Console.ReadLine();

    if (!int.TryParse(aUnos, out int a) || !int.TryParse(bUnos, out int b))
    {
        Console.WriteLine("Unesi ispravne cele brojeve.");
        continue;
    }

    await call.RequestStream.WriteAsync(new NumberData
    {
        A = a,
        B = b
    });
}

await call.RequestStream.CompleteAsync();
await citanjeOdgovora;
```

## 5\. Zasto ti je await blokirao

Problem je bio u dve stvari:

* server je cekao da skupi dva broja da bi formirao jedan par
* klijent je za svaki unos pravio potpuno novi stream

To znaci:

* prvi broj ode u prvi stream
* drugi broj ode u drugi stream
* server u jednom stream-u nikad ne dobije kompletan par

Zato ne dobijes odgovor kada ocekujes.

## 6\. Jos jedna bitna greska

U tvom starom client kodu adresa je bila:

```csharp
https://localhost:5001
```

Po `launchSettings.json` server ti slusa na:

* `https://localhost:7286`
* ili `http://localhost:5171`

Najlakse je da na klijentu koristis:

```csharp
https://localhost:7286
```

## 7\. Sta ne moras da diras

Ovo ne moras da menjas:

* `Oktobar2/Oktobar2Server/Program.cs`

ako ti vec mapira:

```csharp
app.MapGrpcService<Oktobar2Service>();
```

To je dovoljno.

## 8\. Kratko pravilo koje da zapamtis

Za ovaj zadatak:

* jedan element toka = jedan par `(a, b)`
* jedna primljena poruka = jedan odgovor `"Da"` ili `"Ne"`
* jedan `call` traje kroz celu petlju unosa

To je sustina cele ispravke.


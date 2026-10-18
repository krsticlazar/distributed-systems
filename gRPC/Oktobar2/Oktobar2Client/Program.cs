using Grpc.Core;
using Grpc.Net.Client;
using Oktobar2Client;

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

    await call.RequestStream.WriteAsync(new NumberPair
    {
        A = a,
        B = b
    });
}

await call.RequestStream.CompleteAsync();
await citanjeOdgovora;
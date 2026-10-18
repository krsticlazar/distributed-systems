using DSLab1Client;
using Grpc.Core;
using Grpc.Net.Client;

using var channel = GrpcChannel.ForAddress("http://localhost:5110");
var client = new Lab1Service.Lab1ServiceClient(channel);

string? unos;

do
{
    Console.WriteLine();
    Console.WriteLine("1 - Postavi acc na n-ti Fibonacci broj");
    Console.WriteLine("2 - Posalji tok brojeva na obradu");
    Console.WriteLine("x - Izlaz");
    Console.Write("Izbor: ");

    unos = Console.ReadLine();

    switch (unos)
    {
        case "1":
            await PostaviAcc();
            break;
        case "2":
            await ObradiTok();
            break;
    }
}
while (unos != "x");

async Task PostaviAcc()
{
    Console.Write("Unesi n: ");
    if (!int.TryParse(Console.ReadLine(), out int n))
    {
        Console.WriteLine("Unos mora biti ceo broj.");
        return;
    }

    try
    {
        await client.PostaviAccAsync(new FibRequest
        {
            N = n
        });

        Console.WriteLine($"Poziv je izvrsen. acc je postavljen na Fibonacci({n}).");
    }
    catch (RpcException ex)
    {
        Console.WriteLine($"RPC greska: {ex.Status.Detail}");
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex.Message);
    }
}

async Task ObradiTok()
{
    using var call = client.ObradiTok();

    var citanjeOdgovora = Task.Run(async () =>
    {
        await foreach (var odgovor in call.ResponseStream.ReadAllAsync())
        {
            Console.WriteLine($"Server vraca: {odgovor.Value}");
        }
    });

    Console.WriteLine("Unosi cele brojeve jedan po jedan. Za kraj unesi x.");

    while (true)
    {
        string? linija = Console.ReadLine();

        if (string.Equals(linija, "x", StringComparison.OrdinalIgnoreCase))
        {
            break;
        }

        if (!int.TryParse(linija, out int broj))
        {
            Console.WriteLine("Unesi ceo broj ili x za kraj.");
            continue;
        }

        await call.RequestStream.WriteAsync(new NumberData
        {
            Value = broj
        });
    }

    await call.RequestStream.CompleteAsync();
    await citanjeOdgovora;
}

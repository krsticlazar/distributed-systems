using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;
using JUN24Client;

using var channel = GrpcChannel.ForAddress("https://localhost:7225");
var client = new MessageManager.MessageManagerClient(channel);

string? izbor;

do
{
    Console.WriteLine();
    Console.WriteLine("1 - Posalji poruku");
    Console.WriteLine("2 - Obrisi poruku");
    Console.WriteLine("3 - Prikazi sve poruke");
    Console.WriteLine("x - Izlaz");
    Console.Write("Izbor: ");

    izbor = Console.ReadLine();

    try
    {
        switch (izbor)
        {
            case "1":
                await SendMessage();
                break;
            case "2":
                await DeleteMessage();
                break;
            case "3":
                await ListMessages();
                break;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex.Message);
    }
}
while (!string.Equals(izbor, "x", StringComparison.OrdinalIgnoreCase));

async Task SendMessage()
{
    Console.Write("Tekst poruke: ");
    string? text = Console.ReadLine();

    var response = await client.SendMessageAsync(new MessageRequest
    {
        Text = text ?? string.Empty
    });

    Console.WriteLine($"Dodata poruka: {response.Id}. {response.Text}");
}

async Task DeleteMessage()
{
    Console.Write("ID poruke: ");

    if (!int.TryParse(Console.ReadLine(), out int id))
    {
        Console.WriteLine("ID mora biti ceo broj.");
        return;
    }

    await client.DeleteMessageAsync(new MessageId
    {
        Id = id
    });

    Console.WriteLine("Brisanje je izvrseno.");
}

async Task ListMessages()
{
    using var call = client.ListMessages(new Empty());

    Console.WriteLine("Poruke:");

    await foreach (var message in call.ResponseStream.ReadAllAsync())
    {
        Console.WriteLine($"{message.Id}. {message.Text}");
    }
}


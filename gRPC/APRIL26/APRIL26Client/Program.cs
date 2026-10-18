using APRIL26Client;
using Grpc.Net.Client;

using var channel = GrpcChannel.ForAddress("https://localhost:7226");
var client = new Calculator.CalculatorClient(channel);

string? izbor;

do
{
    Console.WriteLine();
    Console.WriteLine("1 - Sabiranje");
    Console.WriteLine("2 - Oduzimanje");
    Console.WriteLine("3 - Mnozenje");
    Console.WriteLine("4 - Deljenje");
    Console.WriteLine("x - Izlaz");
    Console.Write("Izbor: ");

    izbor = Console.ReadLine();

    try
    {
        switch (izbor)
        {
            case "1":
                await PozoviOperaciju("1");
                break;
            case "2":
                await PozoviOperaciju("2");
                break;
            case "3":
                await PozoviOperaciju("3");
                break;
            case "4":
                await PozoviOperaciju("4");
                break;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex.Message);
    }
}
while (!string.Equals(izbor, "x", StringComparison.OrdinalIgnoreCase));

async Task PozoviOperaciju(string operacija)
{
    Console.Write("Operand 1: ");
    if (!int.TryParse(Console.ReadLine(), out int operand1))
    {
        Console.WriteLine("Unos mora biti ceo broj.");
        return;
    }

    Console.Write("Operand 2: ");
    if (!int.TryParse(Console.ReadLine(), out int operand2))
    {
        Console.WriteLine("Unos mora biti ceo broj.");
        return;
    }

    var request = new OperacijaRequest
    {
        Operand1 = operand1,
        Operand2 = operand2
    };

    RezultatResponse response = operacija switch
    {
        "1" => await client.SabiranjeAsync(request),
        "2" => await client.OduzimanjeAsync(request),
        "3" => await client.MnozenjeAsync(request),
        "4" => await client.DeljenjeAsync(request),
        _ => new RezultatResponse()
    };

    Console.WriteLine($"{response.Operacija} brojeva {response.Operand1} i {response.Operand2} = {response.Rezultat}.");
}

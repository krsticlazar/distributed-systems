using Google.Protobuf.WellKnownTypes;
using Grpc.Net.Client;
using KOL24Client;

using var channel = GrpcChannel.ForAddress("https://localhost:7224");
var client = new TaskManager.TaskManagerClient(channel);

string? izbor;

do
{
    Console.WriteLine();
    Console.WriteLine("1 - Dodaj zadatak");
    Console.WriteLine("2 - Prikazi sve zadatke");
    Console.WriteLine("3 - Oznaci zadatak kao zavrsen");
    Console.WriteLine("x - Izlaz");
    Console.Write("Izbor: ");

    izbor = Console.ReadLine();

    try
    {
        switch (izbor)
        {
            case "1":
                await AddTask();
                break;
            case "2":
                await ListTasks();
                break;
            case "3":
                await MarkTaskAsCompleted();
                break;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex.Message);
    }
}
while (!string.Equals(izbor, "x", StringComparison.OrdinalIgnoreCase));

async Task AddTask()
{
    Console.Write("Opis zadatka: ");
    string? description = Console.ReadLine();

    var response = await client.AddTaskAsync(new AddTaskRequest
    {
        Description = description ?? string.Empty
    });

    Console.WriteLine($"{response.Message} ID: {response.Task.Id}");
}

async Task ListTasks()
{
    var response = await client.ListTasksAsync(new Empty());

    if (response.Tasks.Count == 0)
    {
        Console.WriteLine("Lista zadataka je prazna.");
        return;
    }

    foreach (var task in response.Tasks)
    {
        string status = task.Completed ? "zavrsen" : "nije zavrsen";
        Console.WriteLine($"{task.Id}. {task.Description} [{status}]");
    }
}

async Task MarkTaskAsCompleted()
{
    Console.Write("ID zadatka: ");

    if (!int.TryParse(Console.ReadLine(), out int id))
    {
        Console.WriteLine("ID mora biti ceo broj.");
        return;
    }

    var response = await client.MarkTaskAsCompletedAsync(new TaskIdRequest
    {
        Id = id
    });

    Console.WriteLine(response.Message);
}

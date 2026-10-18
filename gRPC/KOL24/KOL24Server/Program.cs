using KOL24Server.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpc();

var app = builder.Build();

app.MapGrpcService<TaskManagerService>();
app.MapGet("/", () => "KOL24 gRPC server radi.");

app.Run();

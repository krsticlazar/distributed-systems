using JUN24Server.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpc();

var app = builder.Build();

app.MapGrpcService<MessageManagerService>();
app.MapGet("/", () => "JUN24 gRPC server radi.");

app.Run();


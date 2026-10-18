

using DSLab1Server;
using DSLab1Server.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpc();
builder.Services.AddSingleton<AccState>();

var app = builder.Build();

app.MapGrpcService<Lab1TemplateService>();
app.MapGet("/", () => "gRPC server radi.");

app.Run();

using APRIL26Server.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpc();

var app = builder.Build();

// Kada napravis service manager, registruj ga ovde:
app.MapGrpcService<CalculatorService>();
app.MapGet("/", () => "APRIL26 gRPC server radi.");

app.Run();

using Oktobar2Server;
using Oktobar2Server.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddGrpc();


var app = builder.Build();

// Configure the HTTP request pipeline.
//app.MapGrpcService<GreeterService>();

app.MapGrpcService<Oktobar2Service>();
app.MapGet("/", () => "gRPC server radi! :)");

app.Run();

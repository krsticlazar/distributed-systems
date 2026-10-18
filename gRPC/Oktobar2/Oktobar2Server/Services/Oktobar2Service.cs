
using Grpc.Core;
using System.Net.WebSockets;


namespace Oktobar2Server.Services;

public class Oktobar2Service:Okt2Service.Okt2ServiceBase
{
    private readonly NumState _num;
    public override async Task ObradiBrojeve(
        IAsyncStreamReader<NumberPair> requestStream,
        IServerStreamWriter<RetMessage> responseStream,
        ServerCallContext context)
    {

        await foreach (var request in requestStream.ReadAllAsync()) 
        {
            string odgovor =request.B == request.A * request.A ? "Da" : "Ne";
            await responseStream.WriteAsync(new RetMessage { Message = odgovor });
        }
    }
}


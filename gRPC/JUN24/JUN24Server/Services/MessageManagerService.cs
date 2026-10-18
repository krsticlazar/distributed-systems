using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

namespace JUN24Server.Services;

public class MessageManagerService : MessageManager.MessageManagerBase
{
    public override Task<MessageItem> SendMessage(MessageRequest request, ServerCallContext context)
    {
        var message = new MessageItem
        {
            Id = Poruke.Instanca().SledeciId++,
            Text = request.Text
        };

        Poruke.Instanca().Lista.Add(message);

        return Task.FromResult(message);
    }

    public override Task<Empty> DeleteMessage(MessageId request, ServerCallContext context)
    {
        var message = Poruke.Instanca().Lista.FirstOrDefault(p => p.Id == request.Id);

        if (message != null)
        {
            Poruke.Instanca().Lista.Remove(message);
        }

        return Task.FromResult(new Empty());
    }

    public override async Task ListMessages(Empty request, IServerStreamWriter<MessageItem> responseStream, ServerCallContext context)
    {
        foreach (var message in Poruke.Instanca().Lista)
        {
            await responseStream.WriteAsync(message);
        }
    }
}


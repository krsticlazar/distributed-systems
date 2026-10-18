using System;
using System.ServiceModel;
using WCFChatClient.ServiceReferenceChat;

namespace WCFChatClient
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.Write("Unesi ime: ");
            string name = Console.ReadLine();

            InstanceContext context = new InstanceContext(new CallbackHandler());
            ChatClient proxy = new ChatClient(context);

            if (!proxy.Register(name))
            {
                Console.WriteLine("Registracija nije uspela.");
                return;
            }

            Console.WriteLine("Registrovan si kao: " + name);
            Console.WriteLine("Format poruke: primalac|tekst");
            Console.WriteLine("Za poruku svima koristi: SVI|tekst");
            Console.WriteLine("Za istoriju ukucaj: /history");
            Console.WriteLine("Za izlaz ukucaj: /q");

            while (true)
            {
                string line = Console.ReadLine();

                if (line == "/q")
                {
                    break;
                }

                if (line == "/history")
                {
                    var history = proxy.MsgHistory(DateTime.Now.AddHours(-1),
                                                   DateTime.Now.AddHours(1));

                    foreach (var message in history)
                    {
                        PrintMessage(message);
                    }

                    continue;
                }

                string[] parts = line.Split(new[] { '|' }, 2);
                if (parts.Length != 2)
                {
                    Console.WriteLine("Neispravan format. Koristi primalac|tekst");
                    continue;
                }

                proxy.Send(new Message
                {
                    To = parts[0],
                    Text = parts[1]
                });
            }

            proxy.Close();
        }

        private static void PrintMessage(Message message)
        {
            Console.WriteLine();
            Console.WriteLine("[" + message.Timestamp + "]");
            Console.WriteLine(message.From + " -> " + message.To);
            Console.WriteLine(message.Text);
        }
    }

    public class CallbackHandler : IChatCallback
    {
        public void Notify(Message message)
        {
            Console.WriteLine();
            Console.WriteLine("Nova poruka:");
            Console.WriteLine("[" + message.Timestamp + "]");
            Console.WriteLine(message.From + " -> " + message.To);
            Console.WriteLine(message.Text);
            Console.Write("> ");
        }
    }
}

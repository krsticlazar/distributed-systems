# WCFChat: chat aplikacija sa callback-om

Ovo uputstvo pravi isti tip znanja kao primer `Primeri/Chat`, ali ispravljeno i prilagodjeno za console klijent. Ne kreiram projekat automatski; ovo je redosled kako ti da ga napravis u Visual Studio-u.

Cilj: napraviti WCF chat gde se vise klijenata registruje, salje poruke, a server preko callback-a obavestava primaoce.

## 1. Sta pravis

Solution neka se zove:

```text
WCFChat
```

U njemu napravi dva projekta:

```text
WCFChat
  WCFChatServer       WCF Service Application (.NET Framework 4.8)
  WCFChatClient       Console App (.NET Framework 4.8)
```

Nemoj koristiti `.NET Core`, `.NET 5+`, `.NET 8`, `.NET 9` itd. Za ove vezbe koristi klasicni `.NET Framework`.

## 2. Ideja aplikacije

Klijent radi tri stvari:

1. Registruje se imenom.
2. Salje poruku jednom korisniku ili svima.
3. Prima poruke koje server salje nazad kroz callback.

Server radi cetiri stvari:

1. Pamti registrovane korisnike.
2. Za svakog korisnika pamti callback kanal.
3. Pamti istoriju poruka.
4. Kada stigne poruka, preko callback-a je prosledjuje primaocu.

Najbitnija razlika u odnosu na obican WCF:

```text
Obican WCF:
klijent -> server

Callback WCF:
klijent -> server
server -> klijent
```

## 3. Server fajlovi

U server projektu koristi ova imena:

```text
WCFChatServer
  IChat.cs
  ChatService.svc
  ChatService.svc.cs
  Web.config
```

Ako Visual Studio napravi `IService1.cs` i `Service1.svc`, mozes ih obrisati ili preimenovati. Bitno je da `.svc`, klasa servisa i `Web.config` pokazuju na isto ime klase.

## 4. IChat.cs

Ovo je servisni ugovor i callback ugovor.

```csharp
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.ServiceModel;

namespace WCFChatServer
{
    [ServiceContract(CallbackContract = typeof(IChatCallback),
                     SessionMode = SessionMode.Required)]
    public interface IChat
    {
        [OperationContract]
        bool Register(string name);

        [OperationContract(IsOneWay = true)]
        void Send(Message message);

        [OperationContract]
        IList<Message> MsgHistory(DateTime from, DateTime to);
    }

    public interface IChatCallback
    {
        [OperationContract(IsOneWay = true)]
        void Notify(Message message);
    }

    [DataContract]
    public class Message
    {
        [DataMember]
        public string Text { get; set; }

        [DataMember]
        public string From { get; set; }

        [DataMember]
        public string To { get; set; }

        [DataMember]
        public DateTime Timestamp { get; set; }
    }
}
```

Sta je bitno:

- `IChat` je ono sto klijent poziva na serveru.
- `IChatCallback` je ono sto server poziva na klijentu.
- `CallbackContract = typeof(IChatCallback)` povezuje ova dva ugovora.
- `SessionMode.Required` je bitan jer server mora da zapamti callback kanal za konkretnog klijenta.
- `Send` i `Notify` su `IsOneWay = true` jer ne vracaju rezultat.

## 5. ChatService.svc

Sadrzaj `.svc` fajla treba da bude:

```aspx
<%@ ServiceHost Language="C#" Debug="true" Service="WCFChatServer.ChatService" CodeBehind="ChatService.svc.cs" %>
```

Ako ovde pise pogresna klasa, servis se nece podici. Ovo je cesta greska.

## 6. ChatService.svc.cs

Ovo je kompletna implementacija.

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;

namespace WCFChatServer
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerSession,
                     ConcurrencyMode = ConcurrencyMode.Reentrant)]
    public class ChatService : IChat
    {
        private static readonly object LockObj = new object();
        private static readonly Dictionary<string, List<IChatCallback>> Users =
            new Dictionary<string, List<IChatCallback>>();
        private static readonly List<Message> Messages = new List<Message>();

        private string registeredName;

        public bool Register(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return false;
            }

            name = name.Trim();
            IChatCallback callback =
                OperationContext.Current.GetCallbackChannel<IChatCallback>();

            lock (LockObj)
            {
                registeredName = name;

                if (!Users.ContainsKey(name))
                {
                    Users[name] = new List<IChatCallback>();
                }

                Users[name].Add(callback);
            }

            return true;
        }

        public void Send(Message message)
        {
            if (message == null || string.IsNullOrWhiteSpace(registeredName))
            {
                return;
            }

            message.From = registeredName;
            message.Timestamp = DateTime.Now;

            List<IChatCallback> targets;

            lock (LockObj)
            {
                Messages.Add(message);
                targets = FindTargets(message);
            }

            foreach (IChatCallback target in targets)
            {
                TryNotify(target, message);
            }
        }

        public IList<Message> MsgHistory(DateTime from, DateTime to)
        {
            lock (LockObj)
            {
                return Messages
                    .Where(m => m.Timestamp >= from &&
                                m.Timestamp <= to &&
                               (m.To == registeredName ||
                                m.To == "SVI" ||
                                m.From == registeredName))
                    .ToList();
            }
        }

        private static List<IChatCallback> FindTargets(Message message)
        {
            var targets = new List<IChatCallback>();

            if (message.To == "SVI")
            {
                foreach (var callbacks in Users.Values)
                {
                    targets.AddRange(callbacks);
                }

                return targets;
            }

            if (!string.IsNullOrWhiteSpace(message.To) &&
                Users.ContainsKey(message.To))
            {
                targets.AddRange(Users[message.To]);
            }

            return targets;
        }

        private static void TryNotify(IChatCallback callback, Message message)
        {
            try
            {
                callback.Notify(message);
            }
            catch (CommunicationException)
            {
            }
            catch (TimeoutException)
            {
            }
        }
    }
}
```

Sta je bitno:

- `Users` je staticki dictionary jer svi korisnici moraju da vide istu listu registrovanih klijenata.
- Kljuc je username.
- Vrednost je lista callback kanala, jer isti username moze imati vise otvorenih klijenata.
- `registeredName` je obicno polje jer je servis `PerSession`; svaka sesija ima svoje ime.
- `GetCallbackChannel<IChatCallback>()` uzima kanal nazad ka klijentu koji se registrovao.
- `ConcurrencyMode.Reentrant` se koristi jer servis tokom obrade poziva poziva klijenta nazad.
- Callback se poziva van glavnog `lock` dela da se smanji rizik od zastoja.

## 7. Web.config za server

U `Web.config` ostavi `appSettings`, `system.web` i `system.webServer`, a `system.serviceModel` podesi ovako:

```xml
<system.serviceModel>
  <behaviors>
    <serviceBehaviors>
      <behavior name="ChatServiceBehavior">
        <serviceMetadata httpGetEnabled="true" httpsGetEnabled="true"/>
        <serviceDebug includeExceptionDetailInFaults="false"/>
      </behavior>
    </serviceBehaviors>
  </behaviors>

  <bindings>
    <wsDualHttpBinding>
      <binding name="WSDualHttpBinding_IChat">
        <security mode="None"/>
      </binding>
    </wsDualHttpBinding>
  </bindings>

  <services>
    <service name="WCFChatServer.ChatService"
             behaviorConfiguration="ChatServiceBehavior">
      <endpoint address=""
                binding="wsDualHttpBinding"
                bindingConfiguration="WSDualHttpBinding_IChat"
                contract="WCFChatServer.IChat" />

      <endpoint address="mex"
                binding="mexHttpBinding"
                contract="IMetadataExchange" />
    </service>
  </services>

  <serviceHostingEnvironment aspNetCompatibilityEnabled="true"
                             multipleSiteBindingsEnabled="true" />
</system.serviceModel>
```

Najbitnije:

- callback zahteva `wsDualHttpBinding`;
- `contract` mora biti `WCFChatServer.IChat`;
- `service name` mora biti `WCFChatServer.ChatService`;
- `mex` endpoint pomaze Visual Studio-u da napravi Service Reference.

## 8. Dodavanje Service Reference u klijent

1. Pokreni server.
2. Otvori browser adresu, na primer:

```text
http://localhost:PORT/ChatService.svc
```

3. U `WCFChatClient` klikni desni klik:

```text
Add > Service Reference
```

ili:

```text
Add > Connected Service > Microsoft WCF Web Service Reference Provider
```

4. Unesi adresu servisa.
5. Namespace stavi:

```text
ServiceReferenceChat
```

6. Klikni `Go`, pa `OK` / `Finish`.

Ako kasnije promenis `IChat`, uvek uradi:

```text
Update Service Reference
```

## 9. Program.cs u klijentu

Ovo je konzolni klijent. Pokreni dve instance ovog programa da vidis callback.

```csharp
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
```

Ako dobijes gresku da `IChatCallback` ne postoji, nisi update-ovao Service Reference posle dodavanja callback interfejsa.

## 10. App.config u klijentu

Visual Studio ce uglavnom sam dodati `system.serviceModel`. Ocekuj nesto ovog tipa:

```xml
<system.serviceModel>
  <bindings>
    <wsDualHttpBinding>
      <binding name="WSDualHttpBinding_IChat">
        <security mode="None"/>
      </binding>
    </wsDualHttpBinding>
  </bindings>

  <client>
    <endpoint address="http://localhost:PORT/ChatService.svc"
              binding="wsDualHttpBinding"
              bindingConfiguration="WSDualHttpBinding_IChat"
              contract="ServiceReferenceChat.IChat"
              name="WSDualHttpBinding_IChat" />
  </client>
</system.serviceModel>
```

Ako pokreces vise klijenata i dobijes problem oko callback adrese, mozes u binding dodati `clientBaseAddress`, ali svaki pokrenuti klijent mora imati drugaciji port:

```xml
<binding name="WSDualHttpBinding_IChat"
         clientBaseAddress="http://localhost:8001/WCFChatClient/">
  <security mode="None"/>
</binding>
```

Za drugi klijent bi onda stavio drugi port, npr. `8002`. Ako pokreces samo jedan klijent, ovo obicno nije potrebno.

## 11. Kako testiras

1. Pokreni server.
2. Otvori:

```text
http://localhost:PORT/ChatService.svc
```

3. U klijentu uradi `Update Service Reference`.
4. Pokreni prvi klijent:

```text
Unesi ime: Lazar
```

5. Pokreni drugi klijent:

```text
Unesi ime: Marko
```

6. U Lazarovom prozoru ukucaj:

```text
Marko|Cao Marko
```

7. U Markovom prozoru treba da se pojavi callback:

```text
Nova poruka:
Lazar -> Marko
Cao Marko
```

8. Poruka svima:

```text
SVI|Pozdrav svima
```

9. Istorija:

```text
/history
```

## 12. Najcesce greske

Ako servis nece da se otvori:

- proveri `ChatService.svc`;
- proveri `service name` u `Web.config`;
- proveri `contract` u endpoint-u.

Ako klijent nema callback interfejs:

- pokreni server;
- uradi `Update Service Reference`.

Ako callback ne stize:

- proveri da li je binding `wsDualHttpBinding`;
- proveri da li servis ima `CallbackContract`;
- proveri da li klijent pravi `new ChatClient(new InstanceContext(...))`;
- proveri da li je korisnik registrovan pre slanja poruke.

Ako se desi problem sa portom callback-a:

- koristi jedan klijent za prvi test;
- ili dodaj `clientBaseAddress` sa razlicitim portom za svaku instancu klijenta.

## 13. Sta treba da znas iz ovog primera

Za ispit/lab je najbitnije da mozes da objasnis:

- zasto callback trazi poseban callback interfejs;
- zasto koristimo `CallbackContract`;
- zasto je potreban `SessionMode.Required`;
- zasto `basicHttpBinding` nije dovoljan;
- sta radi `GetCallbackChannel<IChatCallback>()`;
- zasto klijent pravi `InstanceContext`;
- zasto dictionary cuva korisnike i njihove callback kanale.

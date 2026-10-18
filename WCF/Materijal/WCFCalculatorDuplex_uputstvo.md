# WCFCalculatorDuplex: Microsoft calculator primer sa callback-om

Ovo uputstvo je priprema za duplex calculator zadatak koji se cesto pominje uz WCF. Osnova je Microsoft Learn primer `Duplex Services`, ali ovde je prilagodjen za vezbe: `.NET Framework`, WCF Service Application server i console klijent.

Korisna referenca:

https://learn.microsoft.com/en-us/dotnet/framework/wcf/feature-details/duplex-services

## 1. Sta pravis

Solution neka se zove:

```text
WCFCalculatorDuplex
```

U njemu napravi:

```text
WCFCalculatorDuplex
  WCFCalculatorDuplexService    WCF Service Application (.NET Framework 4.8)
  WCFCalculatorDuplexClient     Console App (.NET Framework 4.8)
```

Ovo je duplex servis:

- klijent poziva kalkulator operacije na serveru;
- server preko callback-a vraca rezultat i tekst jednacine;
- stanje kalkulatora se pamti po sesiji.

## 2. Kako aplikacija radi

Klijent salje operacije:

```text
AddTo(100)
SubtractFrom(25)
MultiplyBy(2)
DivideBy(5)
Clear()
```

Server pamti:

```text
result = trenutni rezultat
equation = tekst dosadasnje jednacine
```

Posle svake operacije server ne vraca rezultat kroz return, nego poziva callback:

```csharp
Callback.Equals(result);
Callback.Equation(equation);
```

To je glavna poenta duplex-a.

## 3. Server fajlovi

U server projektu koristi:

```text
WCFCalculatorDuplexService
  ICalculatorDuplex.cs
  CalculatorService.svc
  CalculatorService.svc.cs
  Web.config
```

## 4. ICalculatorDuplex.cs

Ovo je ugovor servisa i callback ugovor.

```csharp
using System.ServiceModel;

namespace WCFCalculatorDuplexService
{
    [ServiceContract(SessionMode = SessionMode.Required,
                     CallbackContract = typeof(ICalculatorDuplexCallback))]
    public interface ICalculatorDuplex
    {
        [OperationContract(IsOneWay = true)]
        void Clear();

        [OperationContract(IsOneWay = true)]
        void AddTo(double n);

        [OperationContract(IsOneWay = true)]
        void SubtractFrom(double n);

        [OperationContract(IsOneWay = true)]
        void MultiplyBy(double n);

        [OperationContract(IsOneWay = true)]
        void DivideBy(double n);
    }

    public interface ICalculatorDuplexCallback
    {
        [OperationContract(IsOneWay = true)]
        void Equals(double result);

        [OperationContract(IsOneWay = true)]
        void Equation(string equation);

        [OperationContract(IsOneWay = true)]
        void Error(string message);
    }
}
```

Sta je bitno:

- `ICalculatorDuplex` je servisni ugovor.
- `ICalculatorDuplexCallback` je callback ugovor.
- `SessionMode.Required` je bitan jer kalkulator mora da pamti rezultat po klijentu.
- Sve operacije su `IsOneWay = true`, jer rezultat stize kroz callback.
- Dodali smo `Error(string message)` da deljenje nulom ne bude obican exception.

## 5. CalculatorService.svc

```aspx
<%@ ServiceHost Language="C#" Debug="true" Service="WCFCalculatorDuplexService.CalculatorService" CodeBehind="CalculatorService.svc.cs" %>
```

Opet, `.svc` mora da pokazuje na pravu servisnu klasu.

## 6. CalculatorService.svc.cs

```csharp
using System.ServiceModel;

namespace WCFCalculatorDuplexService
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerSession)]
    public class CalculatorService : ICalculatorDuplex
    {
        private double result = 0.0;
        private string equation = "0";

        public void Clear()
        {
            Callback.Equation(equation + " = " + result);
            result = 0.0;
            equation = "0";
            Callback.Equals(result);
            Callback.Equation(equation);
        }

        public void AddTo(double n)
        {
            result += n;
            equation += " + " + n;
            SendState();
        }

        public void SubtractFrom(double n)
        {
            result -= n;
            equation += " - " + n;
            SendState();
        }

        public void MultiplyBy(double n)
        {
            result *= n;
            equation += " * " + n;
            SendState();
        }

        public void DivideBy(double n)
        {
            if (n == 0)
            {
                Callback.Error("Deljenje nulom nije dozvoljeno. Rezultat nije promenjen.");
                SendState();
                return;
            }

            result /= n;
            equation += " / " + n;
            SendState();
        }

        private void SendState()
        {
            Callback.Equals(result);
            Callback.Equation(equation);
        }

        private ICalculatorDuplexCallback Callback
        {
            get
            {
                return OperationContext.Current
                    .GetCallbackChannel<ICalculatorDuplexCallback>();
            }
        }
    }
}
```

Sta je bitno:

- `result` i `equation` su obicna polja, ali rade jer je servis `PerSession`.
- Svaki klijent dobija svoju servisnu instancu.
- `Callback` property svaki put uzima kanal ka klijentu koji je trenutno u sesiji.
- `DivideBy(0)` ne baca exception, nego salje kontrolisanu poruku preko callback-a.

## 7. Web.config za server

U `system.serviceModel` stavi:

```xml
<system.serviceModel>
  <behaviors>
    <serviceBehaviors>
      <behavior name="CalculatorServiceBehavior">
        <serviceMetadata httpGetEnabled="true" httpsGetEnabled="true"/>
        <serviceDebug includeExceptionDetailInFaults="false"/>
      </behavior>
    </serviceBehaviors>
  </behaviors>

  <bindings>
    <wsDualHttpBinding>
      <binding name="WSDualHttpBinding_ICalculatorDuplex">
        <security mode="None"/>
      </binding>
    </wsDualHttpBinding>
  </bindings>

  <services>
    <service name="WCFCalculatorDuplexService.CalculatorService"
             behaviorConfiguration="CalculatorServiceBehavior">
      <endpoint address=""
                binding="wsDualHttpBinding"
                bindingConfiguration="WSDualHttpBinding_ICalculatorDuplex"
                contract="WCFCalculatorDuplexService.ICalculatorDuplex" />

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

- `wsDualHttpBinding` je obavezan za ovaj stil callback-a.
- `contract` je servisni interfejs, ne callback interfejs.
- Callback interfejs se vidi iz `CallbackContract` atributa, ne iz endpoint `contract` polja.

## 8. Dodavanje Service Reference

1. Pokreni server.
2. Otvori:

```text
http://localhost:PORT/CalculatorService.svc
```

3. U klijentskom projektu dodaj Service Reference.
4. Namespace stavi:

```text
CalculatorServiceReference
```

5. Ako posle toga promenis ugovor, uradi:

```text
Update Service Reference
```

## 9. Program.cs u klijentu

```csharp
using System;
using System.ServiceModel;
using WCFCalculatorDuplexClient.CalculatorServiceReference;

namespace WCFCalculatorDuplexClient
{
    internal class Program
    {
        static void Main(string[] args)
        {
            InstanceContext instanceContext =
                new InstanceContext(new CallbackHandler());

            CalculatorDuplexClient proxy =
                new CalculatorDuplexClient(instanceContext);

            proxy.AddTo(100);
            proxy.SubtractFrom(25);
            proxy.MultiplyBy(2);
            proxy.DivideBy(5);
            proxy.DivideBy(0);
            proxy.Clear();

            Console.WriteLine();
            Console.WriteLine("Interaktivni mod.");
            Console.WriteLine("Unesi: + broj, - broj, * broj, / broj, c, q");

            while (true)
            {
                Console.Write("> ");
                string line = Console.ReadLine();

                if (line == "q")
                {
                    break;
                }

                if (line == "c")
                {
                    proxy.Clear();
                    continue;
                }

                string[] parts = line.Split(' ');
                if (parts.Length != 2)
                {
                    Console.WriteLine("Format je: operator broj");
                    continue;
                }

                if (!double.TryParse(parts[1], out double value))
                {
                    Console.WriteLine("Broj nije ispravan.");
                    continue;
                }

                switch (parts[0])
                {
                    case "+":
                        proxy.AddTo(value);
                        break;
                    case "-":
                        proxy.SubtractFrom(value);
                        break;
                    case "*":
                        proxy.MultiplyBy(value);
                        break;
                    case "/":
                        proxy.DivideBy(value);
                        break;
                    default:
                        Console.WriteLine("Nepoznata operacija.");
                        break;
                }
            }

            proxy.Close();
        }
    }

    public class CallbackHandler : ICalculatorDuplexCallback
    {
        public void Equals(double result)
        {
            Console.WriteLine("Result: " + result);
        }

        public void Equation(string equation)
        {
            Console.WriteLine("Equation: " + equation);
        }

        public void Error(string message)
        {
            Console.WriteLine("Error: " + message);
        }
    }
}
```

Ako se generisana callback metoda ne zove tacno `Equals`, Visual Studio moze zbog imena iz `object.Equals` da je generise malo drugacije. Ako se to desi, vidi sta pise u `Reference.cs` ili IntelliSense-u i implementiraj tacan potpis koji trazi generisani interfejs.

## 10. App.config u klijentu

Ocekuj `wsDualHttpBinding`:

```xml
<system.serviceModel>
  <bindings>
    <wsDualHttpBinding>
      <binding name="WSDualHttpBinding_ICalculatorDuplex">
        <security mode="None"/>
      </binding>
    </wsDualHttpBinding>
  </bindings>

  <client>
    <endpoint address="http://localhost:PORT/CalculatorService.svc"
              binding="wsDualHttpBinding"
              bindingConfiguration="WSDualHttpBinding_ICalculatorDuplex"
              contract="CalculatorServiceReference.ICalculatorDuplex"
              name="WSDualHttpBinding_ICalculatorDuplex" />
  </client>
</system.serviceModel>
```

Ako dobijes gresku oko callback adrese, dodaj `clientBaseAddress`:

```xml
<binding name="WSDualHttpBinding_ICalculatorDuplex"
         clientBaseAddress="http://localhost:8010/WCFCalculatorDuplexClient/">
  <security mode="None"/>
</binding>
```

Ako pokreces vise klijenata u isto vreme, svaki mora imati drugi `clientBaseAddress` port.

## 11. Ocekivan izlaz

Prvi deo programa treba da ispise nesto slicno:

```text
Result: 100
Equation: 0 + 100
Result: 75
Equation: 0 + 100 - 25
Result: 150
Equation: 0 + 100 - 25 * 2
Result: 30
Equation: 0 + 100 - 25 * 2 / 5
Error: Deljenje nulom nije dozvoljeno. Rezultat nije promenjen.
Result: 30
Equation: 0 + 100 - 25 * 2 / 5
Equation: 0 + 100 - 25 * 2 / 5 = 30
Result: 0
Equation: 0
```

Ovo dokazuje:

- klijent zove server;
- server pamti rezultat po sesiji;
- server poziva callback;
- greska za deljenje nulom je kontrolisana.

## 12. Veza sa lokalnim `Primeri/T2CB`

U `Primeri/T2CB` vec postoji slican primer.

Sta je dobro:

- `CallbackContract = typeof(ICalculatorDuplexCallback)`;
- `SessionMode.Required`;
- `InstanceContextMode.PerSession`;
- callback metode `Equals` i `Equation`;
- klijent pravi `new CalculatorClient(new InstanceContext(new CallbackHandler()))`.

Sta treba popraviti / dopuniti:

- ne oslanjati se na obicno deljenje nulom;
- dodati `Error` callback;
- drzati `Clear` kao one-way operaciju koja rezultat salje preko callback-a;
- ocistiti probni kod koji nije deo kalkulatora.

## 13. Sta treba da znas za ispit

Ako dobijem duplex calculator:

1. Pisem servisni interfejs.
2. U `[ServiceContract]` dodajem:

```csharp
SessionMode = SessionMode.Required,
CallbackContract = typeof(ICalculatorDuplexCallback)
```

3. Pisem callback interfejs.
4. Operacije su uglavnom `IsOneWay = true`.
5. Servisna klasa je:

```csharp
[ServiceBehavior(InstanceContextMode = InstanceContextMode.PerSession)]
```

6. U servisu uzimam callback:

```csharp
OperationContext.Current.GetCallbackChannel<ICalculatorDuplexCallback>();
```

7. U config-u koristim:

```xml
binding="wsDualHttpBinding"
```

8. U klijentu pravim:

```csharp
new CalculatorDuplexClient(new InstanceContext(new CallbackHandler()))
```

Ako ovo znam, znam sustinu duplex WCF zadatka.

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

            //proxy.AddTo(100);
            //proxy.SubtractFrom(25);
            //proxy.MultiplyBy(2);
            //proxy.DivideBy(5);
            //proxy.DivideBy(0);
            //proxy.Clear();

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

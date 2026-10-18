using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using ConsoleApp1.CalculatorServiceReference;

namespace ConsoleApp1
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // Construct InstanceContext to handle messages on callback interface
            InstanceContext instanceContext = new InstanceContext(new CallbackHandler());

            // Create a client
            CalculatorClient client = new CalculatorClient(instanceContext);

            ////
            CalculatorClient proxy = new CalculatorClient(new InstanceContext(new CallbackHandler()));

            proxy.AddTo(100);
            proxy.SubtractFrom(50);
            proxy.DivideBy(5);
            proxy.MultiplyBy(2.5);
            var r = proxy.Clear();
            Console.WriteLine( r );
            ////

            Primer p = new Primer()
            {
                MyProperty = 100,
                Test = "String primer bezveze",
            };

            Console.WriteLine(p);


            while (true)
            {
                string broj = Console.ReadLine();
                double d = 0;
                double.TryParse(broj, out d);
                string operacija = Console.ReadLine();
                switch(operacija)
                {
                    case "+":
                        client.AddTo(d);
                        break;

                    case "-":
                        client.SubtractFrom(d);
                        break;

                    case "*":
                        client.MultiplyBy(d);
                        break;

                    case "/":
                        client.DivideBy(d);
                        break;
                        case "q":
                        return;
                    default:
                        break;
                }
                
            }
        }
    }

    public class CallbackHandler : ICalculatorCallback
    {
        public void Equals(double result)
        {
            Console.WriteLine("Equals({0})", result);
        }

        public void Equation(string eqn)
        {
            Console.WriteLine("Equation({0})", eqn);
        }
    }

    public class Primer
    {
        public int MyProperty { get; set; }
        public string Test { get; set; }
    }
}

using System;
using System.ServiceModel;
using WcfService1;

namespace ConsoleApp1
{
    internal class Program
    {
        static void Main(string[] args)
        {
            ChannelFactory<IServiceMatrice> factory = null;
            IServiceMatrice proxy = null;

            try
            {
                factory = new ChannelFactory<IServiceMatrice>("BasicHttpBinding_IServiceMatrice");
                proxy = factory.CreateChannel();

                Matrica serverska = CreateMatrix(2, 2, 1, 2, 3, 4);
                Matrica operand = CreateMatrix(2, 2, 10, 20, 30, 40);
                Matrica operandZaMnozenje = CreateMatrix(2, 2, 5, 6, 7, 8);
                Matrica losOperand = CreateMatrix(2, 3, 1, 2, 3, 4, 5, 6);

                PrintResult("Set serverske matrice", proxy.Set(serverska));
                PrintResult("Take serverske matrice", proxy.Take());
                PrintResult("Sabiranje serverske matrice i operanda", proxy.Add(operand));
                PrintResult("Mnozenje serverske matrice skalarom 3", proxy.MultiplyScalar(3));
                PrintResult("Mnozenje serverske matrice drugom matricom", proxy.Multiply(operandZaMnozenje));
                PrintResult("Transponovanje serverske matrice", proxy.Transpond());
                PrintResult("Primer greske - sabiranje matrica razlicitih dimenzija", proxy.Add(losOperand));

                ((IClientChannel)proxy).Close();
                factory.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Doslo je do greske u klijentu: " + ex.Message);

                if (proxy != null)
                {
                    ((IClientChannel)proxy).Abort();
                }

                if (factory != null)
                {
                    factory.Abort();
                }
            }

            Console.WriteLine();
            Console.WriteLine("Pritisni Enter za izlaz.");
            Console.ReadLine();
        }

        private static Matrica CreateMatrix(int rows, int columns, params decimal[] values)
        {
            return new Matrica
            {
                Rows = rows,
                Columns = columns,
                Values = values
            };
        }

        private static void PrintResult(string title, Result result)
        {
            Console.WriteLine();
            Console.WriteLine(title);

            if (result.IsError)
            {
                Console.WriteLine("Greska: " + result.ErrorMessage);
                return;
            }

            PrintMatrix(result.Matrica);
        }

        private static void PrintMatrix(Matrica m)
        {
            if (m == null)
            {
                Console.WriteLine("(nema matrice)");
                return;
            }

            for (int i = 0; i < m.Rows; i++)
            {
                for (int j = 0; j < m.Columns; j++)
                {
                    Console.Write(m.Get(i, j).ToString().PadLeft(8));
                }

                Console.WriteLine();
            }
        }
    }
}

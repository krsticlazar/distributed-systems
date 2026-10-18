namespace WcfService1
{
    public class ServiceMatrice : IServiceMatrice
    {
        private static readonly object LockObj = new object();
        private static Matrica matrica;

        public Result Set(Matrica m)
        {
            string error = ValidateMatrix(m);
            if (error != null)
            {
                return Error(error);
            }

            lock (LockObj)
            {
                matrica = Copy(m);
                return Success(Copy(matrica));
            }
        }

        public Result Add(Matrica m)
        {
            string error = ValidateMatrix(m);
            if (error != null)
            {
                return Error(error);
            }

            lock (LockObj)
            {
                if (matrica == null)
                {
                    return Error("Serverska matrica nije setovana.");
                }

                if (matrica.Rows != m.Rows || matrica.Columns != m.Columns)
                {
                    return Error("Za sabiranje matrice moraju imati isti broj redova i kolona.");
                }

                Matrica result = Create(matrica.Rows, matrica.Columns);
                for (int i = 0; i < matrica.Rows; i++)
                {
                    for (int j = 0; j < matrica.Columns; j++)
                    {
                        result.SetValue(i, j, matrica.Get(i, j) + m.Get(i, j));
                    }
                }

                return Success(result);
            }
        }

        public Result Multiply(Matrica m)
        {
            string error = ValidateMatrix(m);
            if (error != null)
            {
                return Error(error);
            }

            lock (LockObj)
            {
                if (matrica == null)
                {
                    return Error("Serverska matrica nije setovana.");
                }

                if (matrica.Columns != m.Rows)
                {
                    return Error("Za mnozenje matrica mora da vazi: broj kolona prve matrice = broj redova druge matrice.");
                }

                Matrica result = Create(matrica.Rows, m.Columns);
                for (int i = 0; i < matrica.Rows; i++)
                {
                    for (int j = 0; j < m.Columns; j++)
                    {
                        decimal sum = 0;
                        for (int k = 0; k < matrica.Columns; k++)
                        {
                            sum += matrica.Get(i, k) * m.Get(k, j);
                        }

                        result.SetValue(i, j, sum);
                    }
                }

                return Success(result);
            }
        }

        public Result MultiplyScalar(decimal d)
        {
            lock (LockObj)
            {
                if (matrica == null)
                {
                    return Error("Serverska matrica nije setovana.");
                }

                Matrica result = Create(matrica.Rows, matrica.Columns);
                for (int i = 0; i < matrica.Rows; i++)
                {
                    for (int j = 0; j < matrica.Columns; j++)
                    {
                        result.SetValue(i, j, matrica.Get(i, j) * d);
                    }
                }

                return Success(result);
            }
        }

        public Result Transpond()
        {
            lock (LockObj)
            {
                if (matrica == null)
                {
                    return Error("Serverska matrica nije setovana.");
                }

                Matrica result = Create(matrica.Columns, matrica.Rows);
                for (int i = 0; i < matrica.Rows; i++)
                {
                    for (int j = 0; j < matrica.Columns; j++)
                    {
                        result.SetValue(j, i, matrica.Get(i, j));
                    }
                }

                return Success(result);
            }
        }

        public Result Take()
        {
            lock (LockObj)
            {
                if (matrica == null)
                {
                    return Error("Serverska matrica nije setovana.");
                }

                return Success(Copy(matrica));
            }
        }

        private static string ValidateMatrix(Matrica m)
        {
            if (m == null)
            {
                return "Matrica nije poslata.";
            }

            if (m.Rows <= 0 || m.Columns <= 0)
            {
                return "Broj redova i kolona mora biti veci od nule.";
            }

            if (m.Values == null)
            {
                return "Niz vrednosti matrice nije poslat.";
            }

            if (m.Values.Length != m.Rows * m.Columns)
            {
                return "Broj vrednosti u nizu ne odgovara dimenzijama matrice.";
            }

            return null;
        }

        private static Matrica Create(int rows, int columns)
        {
            return new Matrica
            {
                Rows = rows,
                Columns = columns,
                Values = new decimal[rows * columns]
            };
        }

        private static Matrica Copy(Matrica source)
        {
            Matrica copy = Create(source.Rows, source.Columns);
            source.Values.CopyTo(copy.Values, 0);
            return copy;
        }

        private static Result Success(Matrica result)
        {
            return new Result
            {
                IsError = false,
                ErrorMessage = null,
                Matrica = result
            };
        }

        private static Result Error(string message)
        {
            return new Result
            {
                IsError = true,
                ErrorMessage = message,
                Matrica = null
            };
        }
    }
}

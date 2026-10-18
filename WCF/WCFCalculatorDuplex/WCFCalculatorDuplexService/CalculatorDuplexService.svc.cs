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

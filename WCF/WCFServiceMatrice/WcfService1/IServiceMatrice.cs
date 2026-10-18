using System.Runtime.Serialization;
using System.ServiceModel;

namespace WcfService1
{
    [ServiceContract]
    public interface IServiceMatrice
    {
        [OperationContract]
        Result Set(Matrica m);

        [OperationContract]
        Result Add(Matrica m);

        [OperationContract]
        Result Multiply(Matrica m);

        [OperationContract]
        Result MultiplyScalar(decimal d);

        [OperationContract]
        Result Transpond();

        [OperationContract]
        Result Take();
    }

    [DataContract]
    public class Matrica
    {
        [DataMember]
        public int Rows { get; set; }

        [DataMember]
        public int Columns { get; set; }

        [DataMember]
        public decimal[] Values { get; set; }

        public decimal Get(int row, int column)
        {
            return Values[row * Columns + column];
        }

        public void SetValue(int row, int column, decimal value)
        {
            Values[row * Columns + column] = value;
        }
    }

    [DataContract]
    public class Result
    {
        [DataMember]
        public bool IsError { get; set; }

        [DataMember]
        public string ErrorMessage { get; set; }

        [DataMember]
        public Matrica Matrica { get; set; }
    }
}

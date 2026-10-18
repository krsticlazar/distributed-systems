using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;

namespace WcfServiceSokovi
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "IService1" in both code and config file together.
    [ServiceContract]
    public interface ISokovi
    {
        [OperationContract]
        void dodaj(Sastojak s);
        [OperationContract]
        void flasiraj(decimal z);
        [OperationContract]
        void isprazni();
        [OperationContract]
        Stanje VratiStanje();
        [OperationContract]
        IList<Promena> Promene();
    }


    // Use a data contract as illustrated in the sample below to add composite types to service operations.
    [DataContract]
    public class Sastojak
    {
        [DataMember]
        public string Naziv { get; set; }
        [DataMember]
        public decimal Zapremina { get; set; }
        [DataMember]
        public decimal Gustina { get; set; }
    }

    [DataContract]
    public class Stanje
    {
        [DataMember]
        public decimal Zauzece { get; set; }

        [DataMember]
        public decimal Gustina { get; set; }
    }

    [DataContract]
    public class Promena
    {
        [DataMember]
        public DateTime Datum { get; set; }
        [DataMember]
        public Sastojak Sastojak { get; set; }
        [DataMember]
        public string TipPromena { get; set; }
        [DataMember]
        public decimal Zapremina { get; set; }
    }
}

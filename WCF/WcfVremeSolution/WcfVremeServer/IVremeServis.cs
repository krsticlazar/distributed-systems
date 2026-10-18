using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.ServiceModel;

namespace WcfVremeServer
{
    [ServiceContract]
    public interface IVremeServis
    {
        [OperationContract]
        DateTime TrenutnoVreme();

        [OperationContract]
        DateTime VremePokretanja();

        [OperationContract]
        [FaultContract(typeof(GreskaSabiranja))]
        RezultatSabiranja Saberi(ZahtevSabiranje zahtev);

        [OperationContract]
        string Opis();
    }

    [DataContract]
    public class ZahtevSabiranje
    {
        [DataMember]
        public int A { get; set; }

        [DataMember]
        public int B { get; set; }
    }

    [DataContract]
    public class RezultatSabiranja
    {
        [DataMember]
        public int A { get; set; }

        [DataMember]
        public int B { get; set; }

        [DataMember]
        public int Zbir { get; set; }

        [DataMember]
        public DateTime VremeObrade { get; set; }
    }

    [DataContract]
    public class GreskaSabiranja
    {
        [DataMember]
        public string Sifra { get; set; }

        [DataMember]
        public string Poruka { get; set; }
    }
}

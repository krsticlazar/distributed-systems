using System.Collections.Generic;
using System.Runtime.Serialization;
using System.ServiceModel;

namespace WCFKvizServer
{
    [ServiceContract]
    public interface IKvizService
    {
        [OperationContract]
        OperationResult DodajPitanje(PitanjeInput pitanje);

        [OperationContract]
        OperationResult IzmeniPitanje(int id, PitanjeInput pitanje);

        [OperationContract]
        List<PitanjeAdmin> VratiPitanjaZaAdministraciju();

        [OperationContract]
        List<PitanjeZaKviz> VratiPitanjaZaKviz();

        [OperationContract]
        RezultatKviza ValidirajOdgovore(List<Odgovor> odgovori);
    }

    [DataContract]
    public class PitanjeInput
    {
        [DataMember]
        public string Tekst { get; set; }

        [DataMember]
        public string[] Opcije { get; set; }

        [DataMember]
        public int TacanOdgovor { get; set; }
    }

    [DataContract]
    public class PitanjeAdmin : PitanjeInput
    {
        [DataMember]
        public int Id { get; set; }
    }

    [DataContract]
    public class PitanjeZaKviz
    {
        [DataMember]
        public int Id { get; set; }

        [DataMember]
        public string Tekst { get; set; }

        [DataMember]
        public string[] Opcije { get; set; }
    }

    [DataContract]
    public class Odgovor
    {
        [DataMember]
        public int PitanjeId { get; set; }

        [DataMember]
        public int OdabraniOdgovor { get; set; }
    }

    [DataContract]
    public class RezultatKviza
    {
        [DataMember]
        public bool IsError { get; set; }

        [DataMember]
        public string ErrorMessage { get; set; }

        [DataMember]
        public int UkupnoPitanja { get; set; }

        [DataMember]
        public int TacnihOdgovora { get; set; }

        [DataMember]
        public decimal Procenat { get; set; }
    }

    [DataContract]
    public class OperationResult
    {
        [DataMember]
        public bool IsError { get; set; }

        [DataMember]
        public string Message { get; set; }
    }
}

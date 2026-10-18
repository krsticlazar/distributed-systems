using System.Collections.Generic;
using System.Runtime.Serialization;
using System.ServiceModel;

namespace WCFIspitnaPrijavaServer
{
    [ServiceContract]
    public interface IIspitnaPrijavaService
    {
        [OperationContract]
        IspitnaPrijava VratiIspitnuPrijavu(string brojIndeksa);

        [OperationContract]
        OperationResult DodajIspitUPrijavu(string brojIndeksa, int ispitId);

        [OperationContract]
        OperationResult ObrisiIspitIzPrijave(string brojIndeksa, int ispitId);

        [OperationContract]
        string VratiFormatiraneNaziveIspita(string brojIndeksa);

        [OperationContract]
        List<Ispit> VratiIspite();
    }

    [DataContract]
    public class Ispit
    {
        [DataMember]
        public int Id { get; set; }

        [DataMember]
        public string Naziv { get; set; }

        [DataMember]
        public string Profesor { get; set; }
    }

    [DataContract]
    public class IspitnaPrijava
    {
        [DataMember]
        public string BrojIndeksa { get; set; }

        [DataMember]
        public string ImePrezime { get; set; }

        [DataMember]
        public List<Ispit> Ispiti { get; set; }
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

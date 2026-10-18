using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.ServiceModel;

namespace WCFPlacanjeTroskovaServer
{
    [ServiceContract]
    public interface IPlacanjeService
    {
        [OperationContract]
        OperationResult DodajRacun(string brojRacuna, string naziv, decimal pocetniBilans);

        [OperationContract]
        List<Racun> VratiRacune();

        [OperationContract]
        PlacanjeResult PlatiTrosak(string brojRacuna, decimal iznos, string opis);

        [OperationContract]
        List<Placanje> ListingPlacanja(string brojRacuna);
    }

    [DataContract]
    public class Racun
    {
        [DataMember]
        public string BrojRacuna { get; set; }

        [DataMember]
        public string Naziv { get; set; }

        [DataMember]
        public decimal Bilans { get; set; }
    }

    [DataContract]
    public class Placanje
    {
        [DataMember]
        public int Id { get; set; }

        [DataMember]
        public string BrojRacuna { get; set; }

        [DataMember]
        public decimal Iznos { get; set; }

        [DataMember]
        public string Opis { get; set; }

        [DataMember]
        public DateTime Vreme { get; set; }
    }

    [DataContract]
    public class OperationResult
    {
        [DataMember]
        public bool IsError { get; set; }

        [DataMember]
        public string Message { get; set; }
    }

    [DataContract]
    public class PlacanjeResult : OperationResult
    {
        [DataMember]
        public Racun Racun { get; set; }

        [DataMember]
        public Placanje Placanje { get; set; }
    }
}

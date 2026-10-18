using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.ServiceModel;

namespace WCFElektronskiDnevnikServer
{
    [ServiceContract(SessionMode = SessionMode.Required, CallbackContract = typeof(IDnevnikCallback))]
    public interface IDnevnikService
    {
        [OperationContract]
        OperationResult RegistrujProfesora(string ime, string predmet);

        [OperationContract]
        OperationResult RegistrujRoditelja(string prezime);

        [OperationContract]
        List<Ucenik> VratiUcenike();

        [OperationContract]
        OperationResult DodajUcenikaNaPredmet(int ucenikId);

        [OperationContract]
        OperationResult DajOcenu(int ucenikId, int ocena);

        [OperationContract]
        OperationResult DodajDete(int ucenikId);

        [OperationContract]
        List<Ocena> VratiOceneZaDete(int ucenikId, string predmet);
    }

    public interface IDnevnikCallback
    {
        [OperationContract(IsOneWay = true)]
        void Obavestenje(string tekst);
    }

    [DataContract]
    public class Ucenik
    {
        [DataMember]
        public int Id { get; set; }

        [DataMember]
        public string Ime { get; set; }

        [DataMember]
        public string Prezime { get; set; }
    }

    [DataContract]
    public class Ocena
    {
        [DataMember]
        public int UcenikId { get; set; }

        [DataMember]
        public string Ucenik { get; set; }

        [DataMember]
        public string Predmet { get; set; }

        [DataMember]
        public int Vrednost { get; set; }

        [DataMember]
        public string Profesor { get; set; }

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
}

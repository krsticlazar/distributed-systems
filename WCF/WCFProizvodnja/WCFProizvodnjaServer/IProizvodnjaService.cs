using System.Collections.Generic;
using System.Runtime.Serialization;
using System.ServiceModel;

namespace WCFProizvodnjaServer
{
    [ServiceContract(SessionMode = SessionMode.Required, CallbackContract = typeof(IProizvodnjaCallback))]
    public interface IProizvodnjaService
    {
        [OperationContract]
        OperationResult RegistrujKorisnika(string ime);

        [OperationContract]
        OperationResult DodajProizvod(string naziv);

        [OperationContract]
        List<Proizvod> VratiProizvode();

        [OperationContract]
        OperationResult ZakljucajProizvod(int proizvodId);

        [OperationContract]
        OperationResult PovecajProizvodnju(int proizvodId, int kolicina);

        [OperationContract]
        OperationResult OtkljucajProizvod(int proizvodId);
    }

    public interface IProizvodnjaCallback
    {
        [OperationContract(IsOneWay = true)]
        void Obavestenje(string tekst);
    }

    [DataContract]
    public class Proizvod
    {
        [DataMember]
        public int Id { get; set; }

        [DataMember]
        public string Naziv { get; set; }

        [DataMember]
        public int ProizvedenaKolicina { get; set; }

        [DataMember]
        public string ZakljucaoKorisnik { get; set; }
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

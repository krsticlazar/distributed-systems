using System.Collections.Generic;
using System.Runtime.Serialization;
using System.ServiceModel;

namespace WCFTablicServer
{
    [ServiceContract(SessionMode = SessionMode.Required, CallbackContract = typeof(ITablicCallback))]
    public interface ITablicService
    {
        [OperationContract]
        OperationResult PrijaviSeZaIgru(string ime);

        [OperationContract]
        OperationResult PrijaviSeKaoPosmatrac(string ime);

        [OperationContract]
        OperationResult BaciKartu(string karta);

        [OperationContract]
        OperationResult Nosi(string kartaIzRuke, List<string> karteSaTalona);

        [OperationContract]
        string VratiStanje();
    }

    public interface ITablicCallback
    {
        [OperationContract(IsOneWay = true)]
        void Obavestenje(string tekst);
    }

    [DataContract]
    public class OperationResult
    {
        [DataMember]
        public bool IsError { get; set; }

        [DataMember]
        public string Message { get; set; }

        [DataMember]
        public string State { get; set; }
    }
}

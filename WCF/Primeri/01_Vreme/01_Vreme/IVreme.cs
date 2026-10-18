using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;

namespace _01_Vreme
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "IService1" in both code and config file together.
    [ServiceContract(SessionMode = SessionMode.Required)]
    public interface IVreme
    {

        [OperationContract]
        DateTime GetVreme();

        [OperationContract]
        VremeDodatno GetDataUsingDataContract(VremeDodatno composite);

        // TODO: Add your service operations here
    }


    // Use a data contract as illustrated in the sample below to add composite types to service operations.
    [DataContract]
    public class VremeDodatno
    {
        string stringValue = "Hello ";

        [DataMember]
        public DateTime PrvoPokretanje { get; set; }

        [DataMember]
        public string StringValue
        {
            get { return stringValue; }
            set { stringValue = value; }
        }
    }
}

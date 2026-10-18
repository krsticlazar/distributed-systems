using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;

namespace Chat
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "IService1" in both code and config file together.
    [ServiceContract(CallbackContract = typeof(IChatCallback), SessionMode = SessionMode.Required)]
    public interface IChat
    {
        [OperationContract]
        void register(string name);

        [OperationContract]
        void Send(Message m);

        [OperationContract]
        IList<Message> MsgHistory(DateTime from, DateTime to);
    }

    public interface IChatCallback
    {
        [OperationContract]
        void notify(Message m);
    }

    [DataContract]
    public class Message
    {
        [DataMember]
        public string Text { get; set; }
        [DataMember]
        public string From { get; set; }
        [DataMember]
        public string To { get; set; }

        [DataMember]
        public DateTime Timestamp { get; set; }
    }
}


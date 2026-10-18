using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.ServiceModel;

namespace WCFChatServer
{
    [ServiceContract(CallbackContract = typeof(IChatCallback),
                     SessionMode = SessionMode.Required)]
    public interface IChat
    {
        [OperationContract]
        bool Register(string name);

        [OperationContract(IsOneWay = true)]
        void Send(Message message);

        [OperationContract]
        IList<Message> MsgHistory(DateTime from, DateTime to);
    }

    public interface IChatCallback
    {
        [OperationContract(IsOneWay = true)]
        void Notify(Message message);
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

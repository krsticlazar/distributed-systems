using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;

namespace WCFChatServer
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerSession,
                     ConcurrencyMode = ConcurrencyMode.Reentrant)]
    public class ChatService : IChat
    {
        private static readonly object LockObj = new object();
        private static readonly Dictionary<string, List<IChatCallback>> Users = new Dictionary<string, List<IChatCallback>>();
        private static readonly List<Message> Messages = new List<Message>();

        private string registeredName;

        public bool Register(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return false;
            }

            name = name.Trim();
            IChatCallback callback =
                OperationContext.Current.GetCallbackChannel<IChatCallback>();

            lock (LockObj)
            {
                registeredName = name;

                if (!Users.ContainsKey(name))
                {
                    Users[name] = new List<IChatCallback>();
                }

                Users[name].Add(callback);
            }

            return true;
        }

        public void Send(Message message)
        {
            if (message == null || string.IsNullOrWhiteSpace(registeredName))
            {
                return;
            }

            message.From = registeredName;
            message.Timestamp = DateTime.Now;

            List<IChatCallback> targets;

            lock (LockObj)
            {
                Messages.Add(message);
                targets = FindTargets(message);
            }

            foreach (IChatCallback target in targets)
            {
                TryNotify(target, message);
            }
        }

        public IList<Message> MsgHistory(DateTime from, DateTime to)
        {
            lock (LockObj)
            {
                return Messages
                    .Where(m => m.Timestamp >= from &&
                                m.Timestamp <= to &&
                               (m.To == registeredName ||
                                m.To == "SVI" ||
                                m.From == registeredName))
                    .ToList();
            }
        }

        private static List<IChatCallback> FindTargets(Message message)
        {
            var targets = new List<IChatCallback>();

            if (message.To == "SVI")
            {
                foreach (var callbacks in Users.Values)
                {
                    targets.AddRange(callbacks);
                }

                return targets;
            }

            if (!string.IsNullOrWhiteSpace(message.To) &&
                Users.ContainsKey(message.To))
            {
                targets.AddRange(Users[message.To]);
            }

            return targets;
        }

        private static void TryNotify(IChatCallback callback, Message message)
        {
            try
            {
                callback.Notify(message);
            }
            catch (CommunicationException)
            {
            }
            catch (TimeoutException)
            {
            }
        }
    }
}

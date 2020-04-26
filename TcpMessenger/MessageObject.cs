using System;
using System.ServiceModel;

namespace TcpMessenger
{
    [ServiceContract]
    public interface IMessageObject
    {
        [OperationContract]
        string SendMessage(string message);
    }


    public class MessageObject : IMessageObject
    {
        public string SendMessage(string message)
        {
            return $"Got your message:{message}";
        }

    }
}

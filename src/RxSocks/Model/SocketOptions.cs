namespace RxSocks.Model
{
    public class SocketOptions
    {
        public SocketOptions(MessageType messageType)
        {
            MessageType = messageType;
        }
        
        public MessageType MessageType { get; }
    }
}
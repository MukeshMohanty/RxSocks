namespace RxSocks.Model
{
    public class SocketResult
    {
        public SocketResult(int count, bool endOfMessage)
        {
            Count = count;
            EndOfMessage = endOfMessage;
        }

        public int Count { get; }

        public bool EndOfMessage { get; }
    }
}
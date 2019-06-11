using System;
using System.Threading.Tasks;

namespace RxSocks.Api
{
    public interface IBinarySocketClient : IConnectAsyncSocket
    {
        IObservable<byte[]> GetResponseStream();
        Task SendMessageAsync(byte[] message);
    }
}
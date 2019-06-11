using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using RxSocks.Model;

namespace RxSocks.Api
{
    public interface IReactClientSocket : IDisposable
    {
        Task ConnectAsync(Uri uri, CancellationToken token, string userAgent);

        Task SendAsync(ArraySegment<byte> buffer, MessageType messageType, bool endOfMessage,
            CancellationToken cancellationToken);

        Task<SocketResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken token);

        Task CloseAsync();

        WebSocketState State { get; }
    }
}
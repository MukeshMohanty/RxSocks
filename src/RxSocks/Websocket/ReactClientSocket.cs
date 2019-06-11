using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using RxSocks.Api;
using RxSocks.Model;

namespace RxSocks.Websocket
{
    internal class ReactClientSocket : IReactClientSocket
    {
        private readonly ClientWebSocketExtend _underlyingWebsocket = new ClientWebSocketExtend();

        public void Dispose()
        {
            _underlyingWebsocket.Dispose();
        }

        public Task ConnectAsync(Uri uri, CancellationToken token, string userAgent)
        {
            _underlyingWebsocket.Options.RequestHeaders.Add("user-agent", userAgent);
            return _underlyingWebsocket.ConnectAsync(uri, token);
        }

        public Task SendAsync(ArraySegment<byte> buffer, MessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
        {
            return _underlyingWebsocket.SendAsync(buffer, messageType == MessageType.Binary ? WebSocketMessageType.Binary : WebSocketMessageType.Text, true, CancellationToken.None);
        }

        public Task<SocketResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken token)
        {
            return _underlyingWebsocket.ReceiveAsync(buffer, token).ContinueWith(task =>
            {
                var result = task.Result;
                return new SocketResult(result.Count, result.EndOfMessage);
            }, token);
        }

        public Task CloseAsync()
        {
            return _underlyingWebsocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty,
                CancellationToken.None);
        }

        public WebSocketState State => (WebSocketState)_underlyingWebsocket.State;
    }
}
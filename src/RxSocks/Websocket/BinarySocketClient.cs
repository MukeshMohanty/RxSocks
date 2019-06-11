using System;
using System.Net.WebSockets;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using RxSocks.Api;
using RxSocks.Model;

namespace RxSocks.Websocket
{
    public class BinarySocketClient : IBinarySocketClient
    {
        private readonly SocketOptions _options;
        private readonly IReactClientSocket _webSocket;
        private readonly Uri _uri;
        private ReplaySubject<byte[]> _dataStream;
        private BehaviorSubject<Status> _statusStream;

        public BinarySocketClient(IReactClientSocket webSocket, Uri uri, SocketOptions options)
        {
            _webSocket = webSocket;
            _uri = uri;
            _options = options;
            Initialize();
        }

        public IObservable<Status> StatusStream => _statusStream;

        public async Task<bool> ConnectAsync(string userAgent)
        {
            PublishStatus(ConnectionState.Connecting, $"Connecting to {_uri}");

            try
            {
                await _webSocket.ConnectAsync(_uri, CancellationToken.None, userAgent);
                StartListening();
                PublishStatus(ConnectionState.Connected, $"Connected to {_uri}");
                return true;
            }
            catch (Exception exception)
            {
                PublishStatus(ConnectionState.Disconnected, exception);
                return false;
            }
        }

        private async Task<bool> Reconnect(string userAgent)
        {
            Dispose();
            PublishStatus(ConnectionState.Connecting, $"Reconnecting to {_uri}");
            Initialize();
            var result = await ConnectAsync(userAgent);
            return result;
        }


        public Task CloseAsync()
        {
            return _webSocket.CloseAsync();
        }

        public IObservable<byte[]> GetResponseStream()
        {
            return _dataStream.AsObservable();
        }

        public async Task SendMessageAsync(byte[] message)
        {
            await _webSocket.SendAsync(new ArraySegment<byte>(message), _options.MessageType, true,
                CancellationToken.None);
        }

        public void Dispose()
        {
            _webSocket?.Dispose();
            _dataStream?.Dispose();
            _statusStream.Dispose();
        }

        private void Initialize()
        {
            _dataStream = new ReplaySubject<byte[]>(1);
            _statusStream = new BehaviorSubject<Status>(new Status { ConnectionState = ConnectionState.Disconnected });
        }


        private void PublishStatus(ConnectionState state, string message)
        {
            PublishStatus(state, message, null);
        }

        private void PublishStatus(ConnectionState state, Exception exception)
        {
            PublishStatus(state, "", exception);
        }

        private void PublishStatus(ConnectionState state, string message, Exception exception)
        {
            _statusStream.OnNext(new Status
            {
                ConnectionState = state,
                Message = message,
                Error = exception
            });
        }

        //async void is bad but there is no other option in this scenario. 
        //It is equivalent to event handler, caller is not interested in task
        private async void StartListening()
        {
            var receiveBuffer = new byte[4096 * 20];
            while (_webSocket.State == WebSocketState.Open)
            {
                var totalBytes = new byte[0];
                SocketResult result;
                do
                {
                    result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), CancellationToken.None);

                    var existingSize = totalBytes.Length;
                    totalBytes = new byte[existingSize + result.Count];
                    Buffer.BlockCopy(receiveBuffer, 0, totalBytes, existingSize, result.Count);

                } while (!result.EndOfMessage);

                _dataStream.OnNext(totalBytes);
            }
        }


    }
}
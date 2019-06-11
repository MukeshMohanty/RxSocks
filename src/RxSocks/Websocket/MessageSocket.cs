using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using System.Windows.Forms.VisualStyles;
using RxSocks.Api;
using RxSocks.Helper;
using RxSocks.Model;

namespace RxSocks.Websocket
{
    public class MessageSocket<TRequestType, TResponseType> : IMessageSocket<TRequestType, TResponseType>
    {
        private readonly IBinaryMessageSerializer<TRequestType> _serializer;
        private readonly IBinaryMessageDeserializer<TResponseType> _deserializer;
        private readonly IBinarySocketClient _communicator;
        private Status CurrentStatus => StatusStream.FirstAsync().Wait();

        public MessageSocket(IBinaryMessageSerializer<TRequestType> serializer, IBinaryMessageDeserializer<TResponseType> deserializer,
            IBinarySocketClient communicator)
        {
            _serializer = serializer;
            _deserializer = deserializer;
            _communicator = communicator;
        }

        public void Dispose()
        {
            _communicator.Dispose();
        }

        public IObservable<Status> StatusStream => _communicator.StatusStream;

        public Task<bool> ConnectAsync(string userAgent = null)
        {
            return _communicator.ConnectAsync(userAgent);
        }


        public Task CloseAsync()
        {
            return _communicator.CloseAsync();
        }

        public Task<TResponseType> GetResponseAsync(Predicate<TResponseType> filter)
        {
            if (filter == null) filter = type => true;
            return _communicator.GetResponseStream().ConnectedObservable(CurrentStatus)
                .Select(TryDeSerialize)
                .Where(payLoad => payLoad != null && filter(payLoad))
                .FirstAsync().ToTask();
        }

        public async Task<TResponseType> GetResponseAsync(TRequestType requestPayload, Predicate<TResponseType> filter)
        {
            if(filter == null) filter = type => true;
            if (CurrentStatus.ConnectionState != ConnectionState.Connected)
            {
                throw Helpers.ThrowNotConnectedException();
            }
            await SendRequestAsync(requestPayload);
            var reponse = await _communicator.GetResponseStream().Select(TryDeSerialize)
                .Where(payLoad => payLoad != null && filter(payLoad))
                .FirstAsync().ToTask();
            return reponse;
        }

        public IObservable<TResponseType> GetObservable(TRequestType requestPayload, Predicate<TResponseType> filter)
        {
            if (filter == null) filter = type => true;
            return Observable.Create<TResponseType>(observer =>
            {
                if (CurrentStatus.ConnectionState != ConnectionState.Connected)
                {
                    observer.OnError(Helpers.ThrowNotConnectedException());
                    return Disposable.Empty;
                }
                SendRequestAsync(requestPayload)
                    .ContinueWith(task =>
                    {
                        if (task.IsFaulted)
                            if (task.Exception != null) throw new Exception(task.Exception.Message);
                    });
                return _communicator.GetResponseStream()
                    .ConnectedObservable(CurrentStatus)
                    .Select(TryDeSerialize)
                    .Where(payLoad => payLoad != null && filter(payLoad))
                    .Subscribe(observer);
            });
        }

        public IObservable<TResponseType> GetObservable(Predicate<TResponseType> filter)
        {
            if (filter == null) filter = type => true;
            return _communicator.GetResponseStream().ConnectedObservable(CurrentStatus)
                .Select(TryDeSerialize)
                .Where(payLoad => payLoad != null && filter(payLoad));
        }

        private TResponseType TryDeSerialize(byte[] bytes)
        {
            return _deserializer.TryDeserialize(bytes, out TResponseType result) ? result : default(TResponseType);
        }

        private async Task SendRequestAsync(TRequestType requestPayload)
        {
            var bytes = _serializer.Serialize(requestPayload);
            await _communicator.SendMessageAsync(bytes);
        }
    }
}
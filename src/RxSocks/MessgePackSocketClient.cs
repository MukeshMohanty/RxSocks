using System;
using RxSocks.Api;
using RxSocks.Model;
using RxSocks.Transformer;
using RxSocks.Websocket;

namespace RxSocks
{
    public class MessgePackSocketClient<TReq, TRes> : MessageSocket<TReq, TRes>
    {
        public MessgePackSocketClient(Uri uri, IBinaryMessageDeserializer<TRes> deserializer = null) :
            base(new MessagePackSocketSerializer<TReq>(), deserializer ?? new MessagePackSocketDeserializer<TRes>(), 
                new BinarySocketClient(new ReactClientSocket(), uri, new SocketOptions(MessageType.Binary)))
        {

        }
    }
}
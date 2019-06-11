using MessagePack;
using MessagePack.Resolvers;
using RxSocks.Api;

namespace RxSocks.Transformer
{
    public class MessagePackSocketSerializer<T> : IBinaryMessageSerializer<T>
    {
        public byte[] Serialize(T payload)
        {
            return MessagePackSerializer.Serialize(payload, StandardResolverAllowPrivate.Instance);
        }
    }
}
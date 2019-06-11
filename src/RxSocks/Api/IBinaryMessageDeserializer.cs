namespace RxSocks.Api
{
    public interface IBinaryMessageDeserializer<TRes>
    {
        bool TryDeserialize(byte[] bytes, out TRes result);
    }
}
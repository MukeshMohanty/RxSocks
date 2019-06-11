namespace RxSocks.Api
{
    public interface IBinaryMessageSerializer<in TReq>
    {
        byte[] Serialize(TReq payload);
    }
}
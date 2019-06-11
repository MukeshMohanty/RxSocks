using System;
using MessagePack;
using Newtonsoft.Json;
using RxSocks.Api;

namespace RxSocks.Transformer
{
    public class MessagePackSocketDeserializer<T> : IBinaryMessageDeserializer<T>
    {
        public T DeserializePayload(byte[] bytes)
        {
            try
            {
                return MessagePackSerializer.Deserialize<T>(bytes);
            }
            catch(Exception ex)
            {
                var json = MessagePackSerializer.ToJson(bytes);
                //Console.WriteLine($"Falling to json, msg: {ex.Message} \n{ex.InnerException?.Message}");
                return JsonConvert.DeserializeObject<T>(json);
            }
        }


        public bool TryDeserialize(byte[] bytes, out T result)
        {
            result = default(T);
            try
            {
                result = DeserializePayload(bytes);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
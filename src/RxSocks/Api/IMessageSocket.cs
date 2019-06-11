using System;
using System.Threading.Tasks;
using RxSocks.Model;

namespace RxSocks.Api
{
    public interface IMessageSocket<in TReq, TRes> : IDisposable
    {
        IObservable<Status> StatusStream { get; }
        Task<bool> ConnectAsync(string userAgent = null);
        Task CloseAsync();
        Task<TRes> GetResponseAsync(Predicate<TRes> filter = null);

        Task<TRes> GetResponseAsync(TReq requestPayload, Predicate<TRes> filter = null);

        IObservable<TRes> GetObservable(TReq requestPayload, Predicate<TRes> filter = null);

        IObservable<TRes> GetObservable(Predicate<TRes> filter = null);

    }
}
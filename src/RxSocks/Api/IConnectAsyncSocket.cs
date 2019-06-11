using System;
using System.Threading.Tasks;
using RxSocks.Model;

namespace RxSocks.Api
{
    public interface IConnectAsyncSocket : IDisposable
    {
        IObservable<Status> StatusStream { get; }
        Task<bool> ConnectAsync(string userAgent);
        Task CloseAsync();
    }
}

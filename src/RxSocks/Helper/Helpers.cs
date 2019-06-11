using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using RxSocks.Model;

namespace RxSocks.Helper
{
    public static class Helpers
    {
        public static IObservable<T> ConnectedObservable<T>(this IObservable<T> source, Status status)
        {
            return Observable.Create<T>(observer =>
            {
                if (status.ConnectionState == ConnectionState.Connected) return source.Subscribe(observer);
                observer.OnError(ThrowNotConnectedException());
                return Disposable.Empty;
            });
        }

        public static Exception ThrowNotConnectedException()
        {
            return new InvalidOperationException("Not connected yet");
        }
    }
}
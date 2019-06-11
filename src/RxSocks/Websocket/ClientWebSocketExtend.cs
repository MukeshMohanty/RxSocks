﻿using System;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace RxSocks.Websocket
{
    public sealed class ClientWebSocketExtend : WebSocket
    {
        private readonly ClientWebSocketOptionsExtend options;
        private WebSocket innerWebSocket;
        private readonly CancellationTokenSource cts;
        private int state;
        private const int created = 0;
        private const int connecting = 1;
        private const int connected = 2;
        private const int disposed = 3;
        public ClientWebSocketOptionsExtend Options => this.options;

        public override WebSocketCloseStatus? CloseStatus
        {
            get
            {
                if (this.innerWebSocket != null)
                {
                    return this.innerWebSocket.CloseStatus;
                }
                return null;
            }
        }
        public override string CloseStatusDescription
        {
            get
            {
                if (this.innerWebSocket != null)
                {
                    return this.innerWebSocket.CloseStatusDescription;
                }
                return null;
            }
        }
        public override string SubProtocol
        {
            get
            {
                if (this.innerWebSocket != null)
                {
                    return this.innerWebSocket.SubProtocol;
                }
                return null;
            }
        }
        public override WebSocketState State
        {
            get
            {
                if (this.innerWebSocket != null)
                {
                    return this.innerWebSocket.State;
                }
                switch (this.state)
                {
                    case 0:
                        return WebSocketState.None;
                    case 1:
                        return WebSocketState.Connecting;
                    case 3:
                        return WebSocketState.Closed;
                }
                return WebSocketState.Closed;
            }
        }
        public ClientWebSocketExtend()
        {
            this.state = 0;
            this.options = new ClientWebSocketOptionsExtend();
            this.cts = new CancellationTokenSource();
        }
        public Task ConnectAsync(Uri uri, CancellationToken cancellationToken)
        {
            if (uri == null)
            {
                throw new ArgumentNullException("uri");
            }
            if (!uri.IsAbsoluteUri)
            {
                throw new ArgumentException("net_uri_NotAbsolute");
            }
            if (String.IsNullOrWhiteSpace(uri.Scheme) || (!uri.Scheme.Equals("ws", StringComparison.OrdinalIgnoreCase) && !uri.Scheme.Equals("wss", StringComparison.OrdinalIgnoreCase)))
            {
                throw new ArgumentException("net_WebSockets_Scheme");
            }
            int num = Interlocked.CompareExchange(ref this.state, 1, 0);
            if (num == 3)
            {
                throw new ObjectDisposedException(base.GetType().FullName);
            }
            if (num != 0)
            {
                throw new InvalidOperationException("net_WebSockets_AlreadyStarted");
            }
            return this.ConnectAsyncCore(uri, cancellationToken);
        }
        public override Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
        {
            this.ThrowIfNotConnected();
            return this.innerWebSocket.SendAsync(buffer, messageType, endOfMessage, cancellationToken);
        }
        public override Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken)
        {
            this.ThrowIfNotConnected();
            return this.innerWebSocket.ReceiveAsync(buffer, cancellationToken);
        }
        public override Task CloseAsync(WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellationToken)
        {
            this.ThrowIfNotConnected();
            return this.innerWebSocket.CloseAsync(closeStatus, statusDescription, cancellationToken);
        }
        public override Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellationToken)
        {
            this.ThrowIfNotConnected();
            return this.innerWebSocket.CloseOutputAsync(closeStatus, statusDescription, cancellationToken);
        }
        public override void Abort()
        {
            if (this.state == 3)
            {
                return;
            }
            if (this.innerWebSocket != null)
            {
                this.innerWebSocket.Abort();
            }
            this.Dispose();
        }
        public override void Dispose()
        {
            int num = Interlocked.Exchange(ref this.state, 3);
            if (num == 3)
            {
                return;
            }
            this.cts.Cancel(false);
            this.cts.Dispose();
            innerWebSocket?.Dispose();
        }
        static ClientWebSocketExtend()
        {
            RegisterPrefixes();
        }
        private async Task ConnectAsyncCore(Uri uri, CancellationToken cancellationToken)
        {
            HttpWebResponse httpWebResponse = null;
            CancellationTokenRegistration cancellationTokenRegistration = default(CancellationTokenRegistration);
            try
            {
                HttpWebRequest httpWebRequest = this.CreateAndConfigureRequest(uri);

                cancellationTokenRegistration = cancellationToken.Register(new Action<object>(this.AbortRequest), httpWebRequest, false);
                httpWebResponse = ((await httpWebRequest.GetResponseAsync()) as HttpWebResponse);

                string subProtocol = this.ValidateResponse(httpWebRequest, httpWebResponse);
                this.innerWebSocket = CreateClientWebSocket(httpWebResponse.GetResponseStream(), subProtocol, this.options.ReceiveBufferSize, this.options.SendBufferSize, this.options.KeepAliveInterval, false, this.options.GetOrCreateBuffer());

                if (Interlocked.CompareExchange(ref this.state, 2, 1) != 1)
                {
                    throw new ObjectDisposedException(base.GetType().FullName);
                }
            }
            catch (WebException innerException)
            {
                this.ConnectExceptionCleanup(httpWebResponse);
                WebSocketException ex = new WebSocketException("net_webstatus_ConnectFailure", innerException);
                throw ex;
            }
            catch (Exception e)
            {
                this.ConnectExceptionCleanup(httpWebResponse);
                throw;
            }
            finally
            {
                cancellationTokenRegistration.Dispose();
            }
        }
        private void ConnectExceptionCleanup(HttpWebResponse response)
        {
            Dispose();
            response?.Dispose();
        }
        private HttpWebRequest CreateAndConfigureRequest(Uri uri)
        {
            HttpWebRequest httpWebRequest = WebRequest.Create(uri) as HttpWebRequest;
            if (httpWebRequest == null)
            {
                throw new InvalidOperationException("net_WebSockets_InvalidRegistration");
            }
            foreach (string name in this.options.RequestHeaders.Keys)
            {
                if (name.Equals("user-agent", StringComparison.OrdinalIgnoreCase))
                {
                    httpWebRequest.UserAgent = this.options.RequestHeaders[name];
                }
                else
                    httpWebRequest.Headers.Add(name, this.options.RequestHeaders[name]);
            }
            if (this.options.RequestedSubProtocols.Count > 0)
            {
                httpWebRequest.Headers.Add("Sec-WebSocket-Protocol", string.Join(", ", this.options.RequestedSubProtocols));
            }
            if (this.options.UseDefaultCredentials)
            {
                httpWebRequest.UseDefaultCredentials = true;
            }
            else
            {
                if (this.options.Credentials != null)
                {
                    httpWebRequest.Credentials = this.options.Credentials;
                }
            }
            if (this.options.InternalClientCertificates != null)
            {
                httpWebRequest.ClientCertificates = this.options.InternalClientCertificates;
            }
            httpWebRequest.Proxy = this.options.Proxy;
            httpWebRequest.CookieContainer = this.options.Cookies;
            this.cts.Token.Register(new Action<object>(this.AbortRequest), httpWebRequest, false);
            return httpWebRequest;
        }
        private string ValidateResponse(HttpWebRequest request, HttpWebResponse response)
        {
            if (response.StatusCode != HttpStatusCode.SwitchingProtocols)
            {
                throw new WebSocketException("net_WebSockets_Connect101Expected");
            }
            string text = response.Headers["Upgrade"];
            if (!string.Equals(text, "websocket", StringComparison.OrdinalIgnoreCase))
            {
                throw new WebSocketException("net_WebSockets_InvalidResponseHeader");
            }
            string text2 = response.Headers["Connection"];
            if (!string.Equals(text2, "Upgrade", StringComparison.OrdinalIgnoreCase))
            {
                throw new WebSocketException("net_WebSockets_InvalidResponseHeader");
            }
            string text3 = response.Headers["Sec-WebSocket-Accept"];
            string secWebSocketAcceptString = ClientWebSocketOptionsExtend.GetSecWebSocketAcceptString(request.Headers["Sec-WebSocket-Key"]);
            if (!string.Equals(text3, secWebSocketAcceptString, StringComparison.OrdinalIgnoreCase))
            {
                throw new WebSocketException("net_WebSockets_InvalidResponseHeader");
            }
            string text4 = response.Headers["Sec-WebSocket-Protocol"];
            if (!string.IsNullOrWhiteSpace(text4) && this.options.RequestedSubProtocols.Count > 0)
            {
                bool flag = false;
                foreach (string current in this.options.RequestedSubProtocols)
                {
                    if (string.Equals(current, text4, StringComparison.OrdinalIgnoreCase))
                    {
                        flag = true;
                        break;
                    }
                }
                if (!flag)
                {
                    throw new WebSocketException("net_WebSockets_AcceptUnsupportedProtocol");
                }
            }
            if (!string.IsNullOrWhiteSpace(text4))
            {
                return text4;
            }
            return null;
        }
        private void AbortRequest(object obj)
        {
            HttpWebRequest httpWebRequest = (HttpWebRequest)obj;
            httpWebRequest.Abort();
        }
        private void ThrowIfNotConnected()
        {
            if (this.state == 3)
            {
                throw new ObjectDisposedException(base.GetType().FullName);
            }
            if (this.state != 2)
            {
                throw new InvalidOperationException("net_WebSockets_NotConnected");
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace RxSocks.Websocket
{
    public class ClientWebSocketOptionsExtend
    {
        internal const string SecWebSocketKeyGuid = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
        internal const string WebSocketUpgradeToken = "websocket";
        internal const int DefaultReceiveBufferSize = 16 * 1024;
        internal const int DefaultClientSendBufferSize = 16 * 1024;
        internal const int MaxControlFramePayloadLength = 123;

        public int ReceiveBufferSize { get; set; }
        public int SendBufferSize { get; set; }
        public TimeSpan KeepAliveInterval { get; set; }
        public Dictionary<String, String> RequestHeaders { get; private set; }
        public List<String> RequestedSubProtocols { get; private set; }
        public bool UseDefaultCredentials { get; set; }
        public ICredentials Credentials { get; set; }
        public X509CertificateCollection InternalClientCertificates { get; set; }
        public IWebProxy Proxy { get; set; }
        public CookieContainer Cookies { get; set; }

        private ArraySegment<byte>? buffer;

        public ClientWebSocketOptionsExtend()
        {
            ReceiveBufferSize = DefaultReceiveBufferSize;
            SendBufferSize = DefaultClientSendBufferSize;
            KeepAliveInterval = WebSocket.DefaultKeepAliveInterval;
            RequestedSubProtocols = new List<string>();
            RequestHeaders = new Dictionary<string, string>();
            this.Proxy = WebRequest.DefaultWebProxy;
        }

        public ArraySegment<byte> GetOrCreateBuffer()
        {
            if (!this.buffer.HasValue)
            {
                this.buffer = new ArraySegment<byte>?(WebSocket.CreateClientBuffer(this.ReceiveBufferSize, this.SendBufferSize));
            }
            return this.buffer.Value;
        }

        internal static string GetSecWebSocketAcceptString(string secWebSocketKey)
        {
            string result;
            using (SHA1 sha = SHA1.Create())
            {
                string acceptString = secWebSocketKey + SecWebSocketKeyGuid;
                byte[] toHash = Encoding.UTF8.GetBytes(acceptString);
                result = Convert.ToBase64String(sha.ComputeHash(toHash));
            }
            return result;
        }

    }
}
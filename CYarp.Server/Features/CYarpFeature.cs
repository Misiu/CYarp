﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Timeouts;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CYarp.Server.Features
{
    /// <summary>
    /// cyarp协议分析特征
    /// </summary>
    sealed partial class CYarpFeature : ICYarpFeature
    {
        private const string CYarp = "CYarp";
        private readonly Func<Task<Stream>>? acceptAsyncFunc;

        /// <summary>
        /// 是否为cyarp请求
        /// </summary>
        public bool IsCYarpRequest => this.acceptAsyncFunc != null;

        /// <summary>
        /// 传输协议
        /// </summary>
        public TransportProtocol Protocol { get; }

        public CYarpFeature(HttpContext context)
        {
            if (TryGetWebSocketFeature(context, out var protocol, out var acceptAsync) ||
                TryGetHttp2Feature(context, out protocol, out acceptAsync) ||
                TryGetHttp11Feature(context, out protocol, out acceptAsync))
            {
                this.Protocol = protocol;
                this.acceptAsyncFunc = acceptAsync;
            }
        }

        private static bool TryGetWebSocketFeature(
            HttpContext context,
            [MaybeNullWhen(false)] out TransportProtocol protocol,
            [MaybeNullWhen(false)] out Func<Task<Stream>>? acceptAsync)
        {
            var webSocketManager = context.WebSockets;
            if (webSocketManager.IsWebSocketRequest &&
                webSocketManager.WebSocketRequestedProtocols.Contains(CYarp, StringComparer.InvariantCultureIgnoreCase))
            {
                protocol = context.Request.Protocol == HttpProtocol.Http2
                    ? TransportProtocol.WebSocketWithHttp2
                    : TransportProtocol.WebSocketWithHttp11;

                acceptAsync = AcceptAsync;
                return true;
            }
            else
            {
                protocol = default;
                acceptAsync = default;
                return default;
            }

            async Task<Stream> AcceptAsync()
            {
                var webSocket = await webSocketManager.AcceptWebSocketAsync(CYarp);
                return new WebSocketStream(webSocket);
            }
        }

        /// <summary>
        /// :method = CONNECT
        /// :protocol = CYarp
        /// :scheme = https
        /// </summary>
        private static bool TryGetHttp2Feature(
            HttpContext context,
            [MaybeNullWhen(false)] out TransportProtocol protocol,
            [MaybeNullWhen(false)] out Func<Task<Stream>>? acceptAsync)
        {
            var http2Feature = context.Features.Get<IHttpExtendedConnectFeature>();
            if (http2Feature != null &&
                http2Feature.IsExtendedConnect &&
                string.Equals(CYarp, http2Feature.Protocol, StringComparison.InvariantCultureIgnoreCase))
            {
                protocol = TransportProtocol.Http2;
                acceptAsync = AcceptAsync;
                return true;
            }
            else
            {
                protocol = default;
                acceptAsync = default;
                return default;
            }

            async Task<Stream> AcceptAsync()
            {
                var stream = await http2Feature.AcceptAsync();
                context.Features.Get<IHttpRequestTimeoutFeature>()?.DisableTimeout();
                return stream;
            }
        }

        /// <summary>
        /// Get {PATH} HTTP/1.1
        /// Connection: Upgrade
        /// Upgrade: CYarp  
        /// </summary>
        private static bool TryGetHttp11Feature(
            HttpContext context,
            [MaybeNullWhen(false)] out TransportProtocol protocol,
            [MaybeNullWhen(false)] out Func<Task<Stream>>? acceptAsync)
        {
            var http11Feature = context.Features.GetRequiredFeature<IHttpUpgradeFeature>();
            if (http11Feature.IsUpgradableRequest &&
                string.Equals(CYarp, context.Request.Headers.Upgrade, StringComparison.InvariantCultureIgnoreCase))
            {
                protocol = TransportProtocol.Http11;
                acceptAsync = AcceptAsync;
                return true;
            }
            else
            {
                protocol = default;
                acceptAsync = default;
                return default;
            }

            async Task<Stream> AcceptAsync()
            {
                var stream = await http11Feature.UpgradeAsync();
                context.Features.Get<IHttpRequestTimeoutFeature>()?.DisableTimeout();
                return stream;
            }
        }

        public Task<Stream> AcceptAsStreamAsync()
        {
            return this.acceptAsyncFunc == null
                ? throw new InvalidOperationException("Not a CYarp request")
                : this.acceptAsyncFunc();
        }

        public async Task<Stream> AcceptAsSafeWriteStreamAsync()
        {
            var stream = await this.AcceptAsStreamAsync();
            return new SafeWriteStream(stream);
        }
    }
}

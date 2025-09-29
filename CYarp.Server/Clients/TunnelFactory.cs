﻿using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace CYarp.Server.Clients
{
    /// <summary>
    /// TunnelFactory
    /// </summary> 
    sealed class TunnelFactory
    {
        private readonly ConcurrentDictionary<TunnelId, TaskCompletionSource<Tunnel>> tunnelCompletionSources = new();

        public ILogger<Tunnel> Logger { get; }

        public TunnelFactory(ILogger<Tunnel> logger)
        {
            this.Logger = logger;
        }

        /// <summary>
        /// CreateTunnel
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="tunnelType"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<Tunnel> CreateTunnelAsync(ClientConnection connection, TunnelType tunnelType, CancellationToken cancellationToken)
        {
            var tunnelId = connection.NewTunnelId(tunnelType);
            var tunnelSource = new TaskCompletionSource<Tunnel>();
            if (this.tunnelCompletionSources.TryAdd(tunnelId, tunnelSource) == false)
            {
                throw new SystemException($"Tunnel with tunnelId {tunnelId} already exists in the system");
            }

            try
            {
                TunnelLog.LogTunnelCreating(this.Logger, connection.ClientId, tunnelId);
                await connection.CreateTunnelAsync(tunnelId, cancellationToken);
                return await tunnelSource.Task.WaitAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                TunnelLog.LogTunnelCreateFailure(this.Logger, connection.ClientId, tunnelId, "Remote operation timeout");
                throw;
            }
            catch (Exception ex)
            {
                TunnelLog.LogTunnelCreateFailure(this.Logger, connection.ClientId, tunnelId, ex.Message);
                throw;
            }
            finally
            {
                this.tunnelCompletionSources.TryRemove(tunnelId, out _);
            }
        }

        public bool Contains(TunnelId tunnelId)
        {
            return this.tunnelCompletionSources.ContainsKey(tunnelId);
        }

        public bool SetResult(Tunnel httpTunnel)
        {
            return this.tunnelCompletionSources.TryRemove(httpTunnel.Id, out var source) && source.TrySetResult(httpTunnel);
        }
    }
}

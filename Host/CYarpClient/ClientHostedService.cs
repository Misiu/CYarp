﻿using CYarp.Client;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CYarpServer
{
    sealed class ClientHostedService : BackgroundService
    {
        private readonly IOptionsMonitor<CYarpClientOptions> clientOptions;
        private readonly ILogger<CYarp.Client.CYarpClient> logger;

        public ClientHostedService(
            IOptionsMonitor<CYarpClientOptions> clientOptions,
            ILogger<CYarp.Client.CYarpClient> logger)
        {
            this.clientOptions = clientOptions;
            this.logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (stoppingToken.IsCancellationRequested == false)
            {
                try
                {
                    var options = this.clientOptions.CurrentValue;
                    using var client = new CYarp.Client.CYarpClient(options, this.logger);
                    await client.TransportAsync(stoppingToken);

                    this.logger.LogInformation($"传输已被关闭，5秒后重新连接");
                    await Task.Delay(TimeSpan.FromSeconds(5d), stoppingToken);
                }
                catch (ArgumentException)
                {
                    throw;
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (CYarpConnectException ex) when (ex.ErrorCode >= CYarpConnectError.Unauthorized)
                {
                    this.logger.LogError(ex, ex.Message);
                    break;
                }
                catch (Exception ex)
                {
                    this.logger.LogError(ex, "连接异常，10秒后重新连接");
                    await Task.Delay(TimeSpan.FromSeconds(10d), stoppingToken);
                }
            }
        }
    }
}

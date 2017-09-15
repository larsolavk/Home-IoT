using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MqttNetAdapter;
using MQTTnet.Core;
using MQTTnet.Core.Client;
using MQTTnet.Core.Packets;
using MQTTnet.Core.Protocol;

namespace Humidor.Mqtt
{
    public class MqttService : HostedService, IMqttService
    {
        private readonly ILogger<MqttService> _logger;
        private IMqttClient _client;

        public MqttService(ILogger<MqttService> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Resolving MQTT Broker information...");

            var hostInfo = await MqttBrokerResolver.ResolveMqttBroker();

            _logger.LogInformation($"Connecting to MQTT broker: {hostInfo.DisplayName} - {hostInfo.IpAddress}:{hostInfo.Port}");

            _client = new CustomMqttClientFactory().CreateMqttClient(new MqttClientOptions
            {
                Server = hostInfo.IpAddress,
                Port = hostInfo.Port,
                TlsOptions = new MqttClientTlsOptions
                {
                    UseTls = true,
                    Certificates = new List<byte[]>
                    {
                        new X509Certificate2(@"c:\source\cert\LarsOlav-PC.crt.pfx")
                            .Export(X509ContentType.SerializedCert)
                    },
                    CheckCertificateRevocation = true
                }
            });

            _client.Connected += async (sender, eventArgs) =>
            {
                _logger.LogInformation("### CONNECTED WITH SERVER ###");

                await _client.SubscribeAsync(new List<TopicFilter>
                {
                    new TopicFilter("humidor/#", MqttQualityOfServiceLevel.AtMostOnce)
                });

                _logger.LogInformation("### SUBSCRIBED ###");
            };

            _client.Disconnected += async (s, e) =>
            {
                _logger.LogInformation("### DISCONNECTED FROM SERVER ###");
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);

                try
                {
                    await _client.ConnectAsync();
                }
                catch
                {
                    _logger.LogInformation("### RECONNECTING FAILED ###");
                }
            };

            _client.ApplicationMessageReceived += (sender, eventArgs) =>
            {
                _logger.LogInformation($"{eventArgs.ApplicationMessage.Topic} {Encoding.UTF8.GetString(eventArgs.ApplicationMessage.Payload)}");
            };

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await _client.ConnectAsync();
                }
                catch (Exception e)
                {
                    _logger.LogError("### CONNECTING FAILED ###");
                    _logger.LogError(e.ToString());
                }

                await Task.WhenAny(Task.Delay(-1, cancellationToken));
            }
        }

        public Task Publish(string topic, string msg)
        {
            if (_client == null || !_client.IsConnected)
                return Task.CompletedTask;

            return _client.PublishAsync(new MqttApplicationMessage(topic, Encoding.UTF8.GetBytes(msg),
                MqttQualityOfServiceLevel.AtMostOnce, false));
        }
    }
}

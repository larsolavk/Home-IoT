using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HomeIot.Infrastructure.Mqtt.MqttNetAdapter;
using Microsoft.Extensions.Logging;
using MQTTnet.Core;
using MQTTnet.Core.Client;
using MQTTnet.Core.Packets;
using MQTTnet.Core.Protocol;
using Microsoft.Extensions.Options;

namespace HomeIot.Infrastructure.Mqtt
{
    public class MqttService : HostedService, IMqttService
    {
        private readonly MqttServiceConfig _config;
        private readonly ILogger<MqttService> _logger;
        private readonly Func<Type, IEnumerable<IMqttEventHandler>> _eventHandlerFactory;
        private readonly Func<Type, IEnumerable<IMqttMessageEnricher>> _messageEnricherFactory;
        private readonly IMqttMessageSerializer _messageSerializer;
        private readonly Func<string, Type> _messageTypeMap;
        private IMqttClient _client;

        public MqttService(
            IOptions<MqttServiceConfig> config,
            ILogger<MqttService> logger, 
            Func<Type, IEnumerable<IMqttEventHandler>> eventHandlerFactory,
            IMqttMessageSerializer messageSerializer, 
            Func<string, Type> messageTypeMap, 
            Func<Type, IEnumerable<IMqttMessageEnricher>> messageEnricherFactory)
        {
            _config = config.Value;
            _logger = logger;
            _eventHandlerFactory = eventHandlerFactory;
            _messageSerializer = messageSerializer;
            _messageTypeMap = messageTypeMap;
            _messageEnricherFactory = messageEnricherFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            MqttBrokerInfo hostInfo;
            if (!string.IsNullOrWhiteSpace(_config.BrokerEndpoint))
            {
                hostInfo = new MqttBrokerInfo(_config.BrokerEndpoint.Split(':')[0],
                    int.Parse(_config.BrokerEndpoint.Split(':')[1]), "BrokerEndpoint");
            }
            else
            {
                _logger.LogInformation($"Resolving MQTT Broker with DNS name: {_config.BrokerServiceDnsName}");
                hostInfo = await MqttBrokerResolver.ResolveMqttBroker(_config.BrokerServiceDnsName);
            }

            _logger.LogInformation($"Connecting to MQTT broker: {hostInfo.DisplayName} - {hostInfo.IpAddress}:{hostInfo.Port}");

            var certs = new X509Certificate2Collection {new X509Certificate2(_config.ClientCertificatePath, "")};
            _client = new CustomMqttClientFactory().CreateMqttClient(new MqttClientOptions
            {
                Server = hostInfo.IpAddress,
                Port = hostInfo.Port,
                TlsOptions = new MqttClientTlsOptions
                {
                    UseTls = true,
                    //Certificates = new List<byte[]>
                    //{
                    //    X509Certificate.CreateFromSignedFile(_config.ClientCertificatePath).GetRawCertData()
                    //    //new X509Certificate(_config.ClientCertificatePath).Export(X509ContentType.Cert)
                    //        //.Export(X509ContentType.Cert)
                    //},
                    CheckCertificateRevocation = true
                }
            }, certs);

            _client.Connected += async (sender, eventArgs) =>
            {
                _logger.LogInformation("### CONNECTED WITH SERVER ###");

                var topicFilters = _config.Topics.Select(topic =>
                        new TopicFilter(topic, MqttQualityOfServiceLevel.AtMostOnce))
                    .ToList();

                await _client.SubscribeAsync(topicFilters);

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

            _client.ApplicationMessageReceived += async (sender, eventArgs) =>
            {
                _logger.LogDebug($"{eventArgs.ApplicationMessage.Topic} {Encoding.UTF8.GetString(eventArgs.ApplicationMessage.Payload)}");

                var type = _messageTypeMap(eventArgs.ApplicationMessage.Topic);
                var message = _messageSerializer.Deserialize(Encoding.UTF8.GetString(eventArgs.ApplicationMessage.Payload), type);

                await InvokeEnrichers(message as IMqttEvent);
                await InvokeHandlers(message as IMqttEvent);
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

        private async Task InvokeEnrichers<T>(T message) where T: IMqttEvent
        {
            var enrichers = _messageEnricherFactory(message.GetType());

            foreach (var enricher in enrichers)
            {
                await (Task) enricher.GetType().GetMethod("Enrich").Invoke(enricher, new object[] { message });
            }
        }

        private async Task InvokeHandlers<T>(T message) where T : IMqttEvent
        {
            var handlers = _eventHandlerFactory(message.GetType());
            var handlerTasks = new List<Task>();

            foreach (var handler in handlers)
            {
                handlerTasks.Add((Task)handler.GetType().GetMethod("Handle").Invoke(handler, new object[] { message }));
            }

            try
            {
                await Task.WhenAll(handlerTasks);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error occured during message handling");
            }
        }
    }
}

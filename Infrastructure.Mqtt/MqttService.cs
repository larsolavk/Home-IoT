using System;
using System.Collections.Generic;
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

namespace HomeIot.Infrastructure.Mqtt
{
    public class MqttService : HostedService, IMqttService
    {
        private readonly ILogger<MqttService> _logger;
        private readonly Func<Type, IEnumerable<IMqttEventHandler>> _eventHandlerFactory;
        private readonly Func<Type, IEnumerable<IMqttMessageEnricher>> _messageEnricherFactory;
        private readonly IMqttMessageSerializer _messageSerializer;
        private readonly Func<string, Type> _messageTypeMap;
        private IMqttClient _client;

        public MqttService(
            ILogger<MqttService> logger, 
            Func<Type, IEnumerable<IMqttEventHandler>> eventHandlerFactory,
            IMqttMessageSerializer messageSerializer, 
            Func<string, Type> messageTypeMap, 
            Func<Type, IEnumerable<IMqttMessageEnricher>> messageEnricherFactory)
        {
            _logger = logger;
            _eventHandlerFactory = eventHandlerFactory;
            _messageSerializer = messageSerializer;
            _messageTypeMap = messageTypeMap;
            _messageEnricherFactory = messageEnricherFactory;
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

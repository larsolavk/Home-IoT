using System;
using System.Security.Cryptography.X509Certificates;
using MQTTnet.Core.Adapter;
using MQTTnet.Core.Client;
using MQTTnet.Core.Serializer;

namespace HomeIot.Infrastructure.Mqtt.MqttNetAdapter
{
    public class CustomMqttClientFactory
    {
        public IMqttClient CreateMqttClient(MqttClientOptions options, X509CertificateCollection certificateCollection)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            return new MqttClient(options, new MqttChannelCommunicationAdapter(new CustomMqttTcpChannel(certificateCollection), new MqttPacketSerializer()));
        }
    }
}

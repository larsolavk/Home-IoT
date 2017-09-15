using System;
using MQTTnet.Core.Adapter;
using MQTTnet.Core.Client;
using MQTTnet.Core.Serializer;

namespace MqttNetAdapter
{
    public class CustomMqttClientFactory
    {
        public IMqttClient CreateMqttClient(MqttClientOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            return new MqttClient(options, new MqttChannelCommunicationAdapter(new CustomMqttTcpChannel(), new MqttPacketSerializer()));
        }
    }
}

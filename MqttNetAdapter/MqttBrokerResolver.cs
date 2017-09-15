using System;
using System.Linq;
using System.Threading.Tasks;
using Zeroconf;

namespace MqttNetAdapter
{
    public static class MqttBrokerResolver
    {
        private const string ServiceName = "_mqtt._tcp.local.";

        public static async Task<MqttBrokerInfo> ResolveMqttBroker()
        {
            var mqttHosts = await ZeroconfResolver.ResolveAsync(ServiceName);
            var mqttHost = mqttHosts?.SingleOrDefault();

            if (mqttHosts == null || !mqttHosts.Any())
                throw new ArgumentNullException(nameof(mqttHost), "Could not find any MQTT brokers via mDNS");

            if (mqttHost == null)
                throw new ArgumentOutOfRangeException(nameof(mqttHost), "Found more than one MQTT brokers via mDNS");

            return new MqttBrokerInfo(mqttHost.IPAddress, mqttHost.Services[ServiceName].Port, mqttHost.DisplayName);
        }
    }
}

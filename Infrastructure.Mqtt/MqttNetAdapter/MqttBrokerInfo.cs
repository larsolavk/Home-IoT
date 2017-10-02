namespace HomeIot.Infrastructure.Mqtt.MqttNetAdapter
{
    public class MqttBrokerInfo
    {
        public string IpAddress { get; }
        public int Port { get; }
        public string DisplayName { get; }

        public MqttBrokerInfo(string ipAddress, int port, string displayName)
        {
            IpAddress = ipAddress;
            Port = port;
            DisplayName = displayName;
        }
    }
}

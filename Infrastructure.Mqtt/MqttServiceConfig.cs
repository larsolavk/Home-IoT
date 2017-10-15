using System.Collections.Generic;

namespace HomeIot.Infrastructure.Mqtt
{
    public class MqttServiceConfig
    {
        public string BrokerServiceDnsName { get; set; }
        public string BrokerEndpoint { get; set; }
        public string ClientCertificatePath { get; set; }
        public IEnumerable<string> Topics { get; set; }
    }
}

using System;
using System.Threading.Tasks;
using Humidor.Model;
using HomeIot.Infrastructure.Mqtt;

namespace Humidor.Mqtt.Enrichers
{
    public class UtcDateTimeEnricher : IMqttMessageEnricher<HumidorSensorData>
    {
        public Task<HumidorSensorData> Enrich(HumidorSensorData message)
        {
            message.UtcDateTime = DateTime.UtcNow;
            return Task.FromResult(message);
        }
    }
}

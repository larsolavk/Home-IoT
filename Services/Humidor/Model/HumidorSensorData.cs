using System;
using HomeIot.Infrastructure.Mqtt;

namespace Humidor.Model
{
    public class HumidorSensorData : IMqttEvent
    {
        public decimal Humidity { get; set; }
        public decimal Temperature { get; set; }
        public decimal Voltage { get; set; }
        public DateTime UtcDateTime { get; set; }
    }
}

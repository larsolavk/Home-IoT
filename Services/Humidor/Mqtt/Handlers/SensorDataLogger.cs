using System.Threading.Tasks;
using Humidor.Model;
using HomeIot.Infrastructure.Mqtt;
using Microsoft.Extensions.Logging;

namespace Humidor.Mqtt.Handlers
{
    public class SensorDataLogger : IMqttEventHandler<HumidorSensorData>
    {
        private readonly ILogger<SensorDataLogger> _logger;

        public SensorDataLogger(ILogger<SensorDataLogger> logger)
        {
            _logger = logger;
        }

        public Task Handle(HumidorSensorData sensorData)
        {
            _logger.LogInformation($"SensorData received: TS:{sensorData.UtcDateTime:O} H:{sensorData.Humidity} T:{sensorData.Temperature} V:{sensorData.Voltage}");
            return Task.CompletedTask;
        }
    }
}

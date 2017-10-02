using System.Threading.Tasks;

namespace HomeIot.Infrastructure.Mqtt
{
    public interface IMqttService
    {
        Task Publish(string topic, string msg);
    }
}

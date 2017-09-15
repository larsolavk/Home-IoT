using System.Threading.Tasks;

namespace Humidor.Mqtt
{
    public interface IMqttService
    {
        Task Publish(string topic, string msg);
    }
}

using System.Threading.Tasks;

namespace HomeIot.Infrastructure.Mqtt
{
    public interface IMqttEventHandler
    {
    }

    public interface IMqttEventHandler<in TEvent> : IMqttEventHandler where TEvent : IMqttEvent
    {
        Task Handle(TEvent @event);
    }
}

using System.Threading.Tasks;

namespace HomeIot.Infrastructure.Mqtt
{
    public interface IMqttMessageEnricher
    {
    }

    public interface IMqttMessageEnricher<TMessage> : IMqttMessageEnricher where TMessage : IMqttMessage
    {
        Task<TMessage> Enrich(TMessage message);
    }
}

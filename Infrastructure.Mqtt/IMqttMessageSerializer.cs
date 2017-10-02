using System;

namespace HomeIot.Infrastructure.Mqtt
{
    public interface IMqttMessageSerializer
    {
        string Serialize(object message);
        string Serialize<TMessage>(TMessage message) where TMessage : IMqttMessage;
        object Deserialize(string messageString, Type type);
        TMessage Deserialize<TMessage>(string messageString) where TMessage : IMqttMessage;
    }
}

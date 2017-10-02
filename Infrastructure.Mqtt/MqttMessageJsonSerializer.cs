using System;
using Newtonsoft.Json;

namespace HomeIot.Infrastructure.Mqtt
{
    public class MqttMessageJsonSerializer : IMqttMessageSerializer
    {
        public string Serialize(object message)
        {
            return JsonConvert.SerializeObject(message);
        }

        public string Serialize<TMessage>(TMessage message) where TMessage : IMqttMessage
        {
            return JsonConvert.SerializeObject(message);
        }

        public object Deserialize(string messageString, Type type)
        {
            return JsonConvert.DeserializeObject(messageString, type);
        }

        public TMessage Deserialize<TMessage>(string messageString) where TMessage : IMqttMessage
        {
            return JsonConvert.DeserializeObject<TMessage>(messageString);
        }
    }
}

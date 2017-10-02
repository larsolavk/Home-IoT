namespace HomeIot.Infrastructure.Mqtt
{
    public interface IMqttMessage
    {
    }

    public interface IMqttEvent : IMqttMessage
    {
    }

    public interface IMqttCommand : IMqttMessage
    {
    }
}

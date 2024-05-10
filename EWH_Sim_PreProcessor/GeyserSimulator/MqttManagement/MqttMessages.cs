using InfluxDB.Client.Core;

namespace GeyserSimulator.mqttManagement;

[Serializable]
public class MqttMessages
{
    [Column("type", IsTag = true)] public string? Type { get; set; }
    [Column("uid", IsMeasurement = true)] public string? Uid { get; set; }
    [Column(IsTimestamp = true)] public DateTime Timestamp { get; set; }
    public MqttMessages()
    {
        Timestamp = DateTime.Now.ToLocalTime();
    }
}

[Serializable]
class SetMessage : MqttMessages
{
    public string? Target { get; set; }
    public object? Value { get; set; }
}

[Serializable]
class EventMessage : MqttMessages
{
    public string? State { get; set; }
}

[Serializable]
class InfoMessage : MqttMessages
{
    public string? Description { get; set; }
}

[Serializable]
class DataMessage : MqttMessages
{
    [Column]
    public decimal ThermostatTemp { get; set; }
    [Column]
    public decimal InternalEnergy { get; set; }
    [Column]
    public decimal CoilPower { get; set; }
    [Column]
    public decimal AmbientTemp { get; set; }
    [Column]
    public decimal SOC { get; set; }
    [Column]
    public List<decimal>? TempProfile { get; set; }
}

[Serializable]
class AddUserMessage : MqttMessages
{
    public string? Name { get; set; }
    public BrokerCredentials? Credentials { get; set; }
}

[Serializable]
class BrokerCredentials
{
    public string? BrokerUrl { get; set; }
    public int Port { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
}
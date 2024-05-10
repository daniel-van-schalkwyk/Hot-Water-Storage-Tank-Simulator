namespace GeyserSimulator.mqttManagement;

[Serializable]
public class MqttMessages
{
    public string? Type { get; set; }
    public string? Uid { get; set; }
    public DateTime Timestamp { get; set; }
    
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
    public decimal ThermostatTemp { get; set; }
    public decimal InternalEnergy { get; set; }
    public decimal CoilPower { get; set; }
    public decimal AmbientTemp { get; set; }
    public decimal SOC { get; set; }
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
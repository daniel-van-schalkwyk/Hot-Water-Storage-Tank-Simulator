using InfluxDB.Client.Core;

namespace GeyserSimulator.mqttManagement;

[Serializable]
public class MqttMessages
{
    [Column(IsTag = true)] public string? Type { get; set; }
    [Column(IsMeasurement = true)] public string? Uid { get; set; }
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
public class EventMessage : MqttMessages
{
    public string? State { get; set; }
}

[Serializable]
public class InfoMessage : MqttMessages
{
    public string? Description { get; set; }
}

[Serializable]
public class DataMessage : MqttMessages
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
public class AddUserMessage : MqttMessages
{
    public string Name { get; set; }
    public BrokerCredentials Credentials { get; set; }
    
    public AddUserMessage(string name, BrokerCredentials credentials)
    {
        Name = name;
        Credentials = credentials;
    }
}

[Serializable]
public class BrokerCredentials
{
    public string BrokerUrl { get; set; }
    public int Port { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    
    public BrokerCredentials(string brokerUrl, int port, string username, string password)
    {
        BrokerUrl = brokerUrl;
        Port = port;
        Username = username;
        Password = password;
    }
}
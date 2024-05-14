using GeyserSimulator.ConfigStructures;
using GeyserSimulator.SimThreadsManager;
using InfluxDB.Client.Core;

namespace GeyserSimulator.mqttManagement;

[Serializable]
public class MqttMessages
{
    [Column(IsTag = true)] public string? Type { get; set; }
    [Column(IsMeasurement = true)] public string Uid { get; set; }
    [Column(IsTimestamp = true)] public DateTime Timestamp { get; set; }
    public MqttMessages()
    {
        Uid = "";
        Timestamp = DateTime.Now.ToLocalTime();
    }
}

[Serializable]
class SetMessage : MqttMessages
{
    public List<NameValuePair> Targets;

    public SetMessage()
    {
        Targets = new List<NameValuePair>();
    }
}

class DeleteMessage : MqttMessages
{
    public DateTime StartTime { get; set; }
    public DateTime stopTime { get; set; }
    public string? Predicate { get; set; }
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
    public GeyserInputs inputs { get; set; }

    public InfoMessage(string type = "Info")
    {
        Type = type;
        inputs = new GeyserInputs();
    }
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
    public bool CoilState { get; set; }
    [Column]
    public decimal AmbientTemp { get; set; }
    [Column]
    public decimal SOC { get; set; }
    [Column]
    public List<decimal>? T_Profile { get; set; }

    public DataMessage(string type = "Data")
    {
        Type = type;
    }
}

[Serializable]
public class UserMessage : MqttMessages
{
    public string Name { get; set; }
    public bool IsAdmin { get; set; }
    public BrokerCredentials Credentials { get; set; }
    
    public UserMessage(string name, BrokerCredentials credentials)
    {
        Name = name;
        Credentials = credentials;
    }
}
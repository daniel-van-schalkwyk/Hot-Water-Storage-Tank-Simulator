using Newtonsoft.Json;

namespace GeyserSimulator.mqttManagement;

public static class MqttExtensions
{
    public static string Serialize(this MqttMessages mqttMessage)
    {
        return JsonConvert.SerializeObject(mqttMessage);
    }
}
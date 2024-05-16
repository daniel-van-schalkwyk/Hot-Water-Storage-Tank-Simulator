using Newtonsoft.Json;

namespace GeyserSimulator.mqttManagement;

public static class MqttExtensions
{
    public static string Serialize(this MqttMessages mqttMessage, JsonSerializerSettings? settings = null)
    {
        settings ??= new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore
        };
        return JsonConvert.SerializeObject(mqttMessage, settings);
    }
    
    public static string Serialize(this object dataMessage)
    {
        return JsonConvert.SerializeObject(dataMessage);
    }
    
    public static T? Deserialize<T>(this string mqttMessageString)
    {
        return JsonConvert.DeserializeObject<T>(mqttMessageString);
    }
}
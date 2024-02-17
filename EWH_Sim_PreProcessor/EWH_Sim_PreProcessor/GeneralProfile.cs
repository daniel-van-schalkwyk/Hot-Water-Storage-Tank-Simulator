using Newtonsoft.Json;

namespace EWH_Sim_PreProcessor;

[Serializable]
public class GeneralProfile<TValueType>
{
    /// <summary>
    /// 
    /// </summary>
    [JsonProperty("values")]
    public List<TValueType> Values { get; set; }
    
    /// <summary>
    /// 
    /// </summary>
    [JsonProperty("unit")]
    public string? Unit { get; set; }

    public GeneralProfile()
    {
        Unit = "";
        Values = new List<TValueType>();
    }
}
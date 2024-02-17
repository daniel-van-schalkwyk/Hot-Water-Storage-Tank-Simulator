using Newtonsoft.Json;

namespace EWH_Sim_PreProcessor.ConfigStructures;

[Serializable]
public class SimulationConfig
{
    [JsonProperty("general")]
    public General General { get; set; }
    
    [JsonProperty("config")]
    public Config Config { get; set; }
    
    [JsonProperty("simParameters")]
    public SimParameters SimParameters { get; set; }
    
    [JsonProperty("input")]
    public Input Input { get; set; }
    
    [JsonProperty("profiles")]
    public SimInputProfiles Profiles { get; set; }
}
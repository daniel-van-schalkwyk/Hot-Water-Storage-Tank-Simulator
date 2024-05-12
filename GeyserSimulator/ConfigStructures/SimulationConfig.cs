using Newtonsoft.Json;

namespace EWH_Sim_PreProcessor.ConfigStructures;

/// <summary>
/// 
/// </summary>
[Serializable]
public class SimulationConfig
{
    /// <summary>
    /// 
    /// </summary>
    [JsonProperty("general")]
    public General General { get; set; }
    
    /// <summary>
    /// 
    /// </summary>
    [JsonProperty("config")]
    public Config Config { get; set; }
    
    /// <summary>
    /// 
    /// </summary>
    [JsonProperty("simParameters")]
    public SimParameters SimParameters { get; set; }
    
    /// <summary>
    /// 
    /// </summary>
    [JsonProperty("input")]
    public Input Input { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public SimulationConfig()
    {
        General = new General();
        Config = new Config();
        SimParameters = new SimParameters();
        Input = new Input();
    }
}
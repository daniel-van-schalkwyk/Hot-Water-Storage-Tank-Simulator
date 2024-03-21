using EWH_Sim_PreProcessor.ProfileManagement;
using Newtonsoft.Json;

namespace EWH_Sim_PreProcessor;

[Serializable]
public class SimInputProfiles
{
    [JsonProperty("time")]
    public GeneralProfile<DateTime> Time { get; set; }
    
    [JsonProperty("coilPower")]
    public GeneralProfile<decimal> CoilPowerProfile { get; set; }
    
    [JsonProperty("ambientTemp")]
    public GeneralProfile<decimal> AmbientTempProfile { get; set; }
    
    [JsonProperty("setTemp")]
    public GeneralProfile<decimal> SetTempProfile { get; set; }
    
    [JsonProperty("flowRate")]
    public GeneralProfile<decimal> FlowProfile { get; set; }
    
    [JsonProperty("inletTemp")]
    public GeneralProfile<decimal> inletTempProfile { get; set; }
    
    [JsonProperty("powerAvailable")]
    public GeneralProfile<bool> PowerAvailableProfile { get; set; }
}
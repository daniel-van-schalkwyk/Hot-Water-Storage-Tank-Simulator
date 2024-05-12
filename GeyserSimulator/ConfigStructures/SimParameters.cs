namespace EWH_Sim_PreProcessor.ConfigStructures;

[Serializable]
public class SimParameters
{
    public DateTime startTime { get; set; }
    public DateTime stopTime { get; set; }
    public decimal dt { get; set; }
    public object tempInit { get; set; }
}
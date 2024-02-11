namespace EWH_Sim_PreProcessor.ConfigStructures;

[Serializable]
public class SimParameters
{
    public string? startTime { get; set; }
    public string? StopTime { get; set; }
    public decimal? dt { get; set; }
    public object? tempInit { get; set; }
}
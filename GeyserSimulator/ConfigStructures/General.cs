namespace EWH_Sim_PreProcessor.ConfigStructures;

[Serializable]
public class General
{
    public string? simName { get; set; }
    public string? userName { get; set; }
    public DateTime? date { get; set; }
    public bool? saveData { get; set; }
    public int? id { get; set; }
}
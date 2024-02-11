namespace EWH_Sim_PreProcessor.ConfigStructures;

[Serializable]
public class GeneralEvent
{
    public string? start { get; set; }
    public string? stop { get; set; }
    public ValueWithUnit? duration { get; set; }
}
namespace EWH_Sim_PreProcessor.ConfigStructures;

[Serializable]
public class GeneralEvent
{
    public DateTime start { get; set; }
    public DateTime stop { get; set; }
    public ValueWithUnit? duration { get; set; }
}
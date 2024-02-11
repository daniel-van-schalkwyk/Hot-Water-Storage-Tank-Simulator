namespace EWH_Sim_PreProcessor.ConfigStructures;

[Serializable]
public class ValueWithUnit
{
    public string? unit { get; set; }
    public object? value { get; set; }
}
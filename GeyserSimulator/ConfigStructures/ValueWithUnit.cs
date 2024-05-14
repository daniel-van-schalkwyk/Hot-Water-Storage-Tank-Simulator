namespace EWH_Sim_PreProcessor.ConfigStructures;

[Serializable]
public class ValueWithUnit
{
    public string? unit { get; set; }
    public decimal value { get; set; }
}
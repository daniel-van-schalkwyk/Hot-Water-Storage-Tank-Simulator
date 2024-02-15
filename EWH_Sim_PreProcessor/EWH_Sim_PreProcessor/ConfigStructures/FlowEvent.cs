namespace EWH_Sim_PreProcessor.ConfigStructures;

[Serializable]
public class FlowEvent : GeneralEvent
{
    public ValueWithUnit inletTemp { get; set; }
    public ValueWithUnit flowRate { get; set; }
}
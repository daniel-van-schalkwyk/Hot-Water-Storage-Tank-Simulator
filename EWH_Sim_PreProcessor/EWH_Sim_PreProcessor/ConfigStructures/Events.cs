namespace EWH_Sim_PreProcessor.ConfigStructures;

[Serializable]
public class Events
{
    public List<FlowEvent>? discharge { get; set; }
    public List<FlowEvent>? charge { get; set; }
    public List<GeneralEvent>? powerOff { get; set; }
}
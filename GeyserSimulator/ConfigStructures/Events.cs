namespace EWH_Sim_PreProcessor.ConfigStructures;

[Serializable]
public class Events
{
    public Events(List<FlowEvent> discharge, List<FlowEvent> charge, List<GeneralEvent> powerOff)
    {
        this.discharge = discharge;
        this.charge = charge;
        this.powerOff = powerOff;
    }

    public List<FlowEvent> discharge { get; set; }
    public List<FlowEvent> charge { get; set; }
    public List<GeneralEvent> powerOff { get; set; }
}
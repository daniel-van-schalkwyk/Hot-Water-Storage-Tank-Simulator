namespace EWH_Sim_PreProcessor.ConfigStructures;

[Serializable]
public class Input
{
    public Source source { get; set; }
    public ValueWithUnit tempSet { get; set; }
    public ValueWithUnit coilPower { get; set; }
    public ValueWithUnit ambientTemp { get; set; }
    public Events events { get; set; }
}
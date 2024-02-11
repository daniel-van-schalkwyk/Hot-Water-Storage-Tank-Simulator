namespace EWH_Sim_PreProcessor;

public class SimInputProfiles
{
    public GeneralProfile<decimal> CoilPowerProfile { get; set; }
    public GeneralProfile<decimal> AmbientTempProfile { get; set; }
    public GeneralProfile<decimal> SetTempProfile { get; set; }
    public GeneralProfile<decimal> FlowProfile { get; set; }
    public GeneralProfile<bool> PowerAvailableProfile { get; set; }
}
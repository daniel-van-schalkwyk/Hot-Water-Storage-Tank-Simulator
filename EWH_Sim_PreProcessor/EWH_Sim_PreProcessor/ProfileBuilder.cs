using EWH_Sim_PreProcessor.ConfigStructures;

namespace EWH_Sim_PreProcessor;

public class ProfileBuilder
{
    public SimulationConfig SimConfig { get; set; }
    public SimInputProfiles SimInputProfiles { get; set; }
    public DateTime SimStartTime { get; set; }
    public DateTime SimStopTime { get; set; }
    public TimeSpan DeltaTime { get; set; }
    public List<DateTime> TimeStamps { get; set; }
    
    public ProfileBuilder(SimulationConfig simConfig)
    {
        // Instantiate all appropriate variables
        SimConfig = simConfig;
        SimInputProfiles = new SimInputProfiles();
        SimStartTime = SimConfig.simParameters.startTime;
        SimStopTime = SimConfig.simParameters.stopTime;
        DeltaTime = TimeSpan.FromSeconds((double)SimConfig.simParameters.dt);
        
        // Create TimeStamp Vector of simulation
        TimeStamps = GenerateTimeStampProfile(SimStartTime, SimStopTime, DeltaTime);
        
        // Build power availability profile
        SimInputProfiles.PowerAvailableProfile = BuildPowerAvailabilityProfile();
        
        // Build ambient profile
        SimInputProfiles.AmbientTempProfile = BuildAmbientProfile();

        // Build flow profile
        SimInputProfiles.FlowProfile = BuildFlowProfile();
        
        // Build input coil profiles
        SimInputProfiles.CoilPowerProfile = BuildInputCoilProfile();
    }

    private GeneralProfile<bool> BuildPowerAvailabilityProfile()
    {
        GeneralProfile<bool> powerProfile = new();

        // Create Default Profile
        powerProfile.TimeStamps = TimeStamps;
        for (int i = 0; i < TimeStamps.Count; i++)
        {
            powerProfile.Values.Add(true);
        }
        
        // Get the power off events 
        List<GeneralEvent> powerOffEvents = SimConfig.input.events.powerOff;
        foreach (GeneralEvent eventEntry in powerOffEvents)
        {
            int startIndex = powerProfile.TimeStamps.IndexOf(eventEntry.start);
            int stopIndex = powerProfile.TimeStamps.IndexOf(eventEntry.stop);
            int indexRange = stopIndex - startIndex;

            // Set the values of the event to false to indicate no power
            for (int i = startIndex; i < startIndex + indexRange && i < powerProfile.Values.Count; i++)
            {
                powerProfile.Values[i] = false;
            }
        }
        
        return powerProfile;
    }
    
    private GeneralProfile<decimal> BuildAmbientProfile()
    {
        GeneralProfile<decimal> ambientProfile = new();
        const decimal defaultValue = 20;
        
        // Create Default Profile
        ambientProfile.TimeStamps = TimeStamps;
        ambientProfile.Values = ambientProfile.Values.Select(_ => defaultValue).ToList();

        return ambientProfile;
    }
    
    private GeneralProfile<decimal> BuildInputCoilProfile()
    {
        GeneralProfile<decimal> inputCoilProfile = new();
        const decimal defaultValue = 3000;
        
        // Create Default Profile
        inputCoilProfile.TimeStamps = TimeStamps;
        inputCoilProfile.Values = inputCoilProfile.Values.Select(_ => defaultValue).ToList();
        
        return inputCoilProfile;
    }
    
    private GeneralProfile<decimal> BuildFlowProfile()
    {
        GeneralProfile<decimal> flowProfile = new();
        

        return flowProfile;
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="start"></param>
    /// <param name="stop"></param>
    /// <param name="delta"></param>
    /// <returns></returns>
    private static List<DateTime> GenerateTimeStampProfile(DateTime start, DateTime stop, TimeSpan delta)
    {
        List<DateTime> dates = new();

        // Loop and create DateTime array based on dt value
        for (DateTime current = start; current <= stop; current += delta)
        {
            dates.Add(current);
        }

        return dates;
    }
}
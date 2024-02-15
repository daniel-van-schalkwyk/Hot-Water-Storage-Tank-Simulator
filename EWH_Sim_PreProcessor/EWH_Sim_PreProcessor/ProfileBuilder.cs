using EWH_Sim_PreProcessor.ConfigStructures;

namespace EWH_Sim_PreProcessor;

public class ProfileBuilder
{
    public SimulationConfig SimConfig { get; }
    public SimInputProfiles SimInputProfiles { get; }
    public DateTime SimStartTime { get; }
    public DateTime SimStopTime { get; }
    public TimeSpan DeltaTime { get; }
    public List<DateTime> TimeStamps { get; }
    
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
        decimal defaultValue = SimConfig.input.ambientTemp.value;
        
        // Create Default Profile
        ambientProfile.TimeStamps = TimeStamps;
        for (int i = 0; i < TimeStamps.Count; i++)
        {
            ambientProfile.Values.Add(defaultValue);
        }

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
        const decimal defaultValue = (decimal)0.00;
        
        // Create Default Profile
        flowProfile.TimeStamps = TimeStamps;
        for (int i = 0; i < TimeStamps.Count; i++)
        {
            flowProfile.Values.Add(defaultValue);
        }
        
        // Insert discharge events into the profile
        AddEventsToFlowProfile(flowProfile, SimConfig.input.events.discharge);
        
        // Insert charge events into the profile if available
        AddEventsToFlowProfile(flowProfile, SimConfig.input.events.charge, false);

        return flowProfile;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="flowProfile"></param>
    /// <param name="flowEvents"></param>
    /// <param name="discharge"></param>
    private void AddEventsToFlowProfile(GeneralProfile<decimal> flowProfile, IEnumerable<FlowEvent> flowEvents, bool discharge = true)
    {
        foreach (FlowEvent eventEntry in flowEvents)
        {
            int startIndex = flowProfile.TimeStamps.IndexOf(eventEntry.start);
            int stopIndex = flowProfile.TimeStamps.IndexOf(eventEntry.stop);
            int indexRange = stopIndex - startIndex;

            // Set the values of the event to the provided value
            for (int i = startIndex; i < startIndex + indexRange && i < flowProfile.Values.Count; i++)
            {
                if (discharge)
                {
                    flowProfile.Values[i] = eventEntry.flowRate.value;
                }
                else
                {
                    if(eventEntry.flowRate.value < 0)
                        flowProfile.Values[i] = eventEntry.flowRate.value;
                    else
                        flowProfile.Values[i] = -eventEntry.flowRate.value;
                }
            }
        }
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
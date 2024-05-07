using System.Globalization;
using CsvHelper;
using CsvHelper.TypeConversion;
using EWH_Sim_PreProcessor.ConfigStructures;

namespace EWH_Sim_PreProcessor.ProfileManagement;

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
        // Initialise all appropriate variables
        SimConfig = simConfig;
        SimInputProfiles = new SimInputProfiles();

        if (simConfig.Input.source.csv == true)
        {
            try
            {
                if (simConfig.Input.source.filePath != null)
                    ExtractProfileFromCsv(simConfig.Input.source.filePath);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
        else
        {
            SimStartTime = SimConfig.SimParameters.startTime;
            SimStopTime = SimConfig.SimParameters.stopTime;
            DeltaTime = TimeSpan.FromSeconds((double)SimConfig.SimParameters.dt);
        
            // Create TimeStamp Vector of simulation
            TimeStamps = GenerateTimeStampProfile(SimStartTime, SimStopTime, DeltaTime);
        
            // Assign time stamp profiles
            SimInputProfiles.Time = new GeneralProfile<DateTime>
            {
                Values = TimeStamps,
                Unit = "YYYY-MM-DDTHH:mm:ss"
            };

            // Build power availability profile
            SimInputProfiles.PowerAvailableProfile = BuildPowerAvailabilityProfile();
        
            // Build ambient profile
            SimInputProfiles.AmbientTempProfile = BuildAmbientProfile();

            // Build flow profile
            SimInputProfiles.FlowProfile = BuildFlowProfile();

            SimInputProfiles.inletTempProfile = BuildInletTempProfile();
        
            // Build input coil profiles
            SimInputProfiles.CoilPowerProfile = BuildInputCoilProfile();
        
            // Build Set temperature profile
            SimInputProfiles.SetTempProfile = BuildSetTempProfile();
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="csvFilePath"></param>
    private void ExtractProfileFromCsv(string csvFilePath)
    {
        using StreamReader reader = new(csvFilePath);
        using CsvReader csv = new(reader, CultureInfo.InvariantCulture);
        csv.Context.TypeConverterOptionsCache.AddOptions<DateTime>(new TypeConverterOptions { Formats = new[] { "dd/MM/yyyy  HH:mm:ss" } });
        List<CsvProfile> records = csv.GetRecords<CsvProfile>().ToList();

        SimInputProfiles.Time = new GeneralProfile<DateTime>
        {
            Values = new List<DateTime>(records.Select(r => r.Time).ToArray()),
            Unit = "YYYY-MM-DDTHH:mm:ss"
        };
        SimInputProfiles.CoilPowerProfile = new GeneralProfile<decimal>
        {
            Values = new List<decimal>(records.Select(r => r.CoilPower).ToArray()),
            Unit = GetUnitValue("coilPower")
        };
        SimInputProfiles.AmbientTempProfile = new GeneralProfile<decimal>
        {
            Values = new List<decimal>(records.Select(r => r.AmbientTemp).ToArray()),
            Unit = GetUnitValue("ambientTemp")
        };
        SimInputProfiles.SetTempProfile = new GeneralProfile<decimal>
        {
            Values = new List<decimal>(records.Select(r => r.TempSet).ToArray()),
            Unit = GetUnitValue("tempSet")
        };
        SimInputProfiles.FlowProfile = new GeneralProfile<decimal>
        {
            Values = new List<decimal>(records.Select(r => r.FlowRate).ToArray()),
            Unit = GetUnitValue("flowRate")
        };
        SimInputProfiles.inletTempProfile = new GeneralProfile<decimal>
        {
            Values = new List<decimal>(records.Select(r => r.InletTemp).ToArray()),
            Unit = GetUnitValue("inletTemp")
        };
        
        SimInputProfiles.PowerAvailableProfile = new GeneralProfile<bool>
        {
            Values = new List<bool>(records.Select(r => r.PowerAvailable).ToArray()),
            Unit = GetUnitValue("powerAvailable")
        };
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="fieldName"></param>
    /// <returns></returns>
    private string? GetUnitValue(string fieldName)
    {
        return SimConfig.Input.source.units
            .Where(u => u.name.Contains(fieldName, StringComparison.InvariantCultureIgnoreCase)).Select(u => u.value)
            .FirstOrDefault();
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    private GeneralProfile<decimal> BuildInletTempProfile()
    {
        GeneralProfile<decimal> inletTempProfile = new();
        const decimal defaultValue = 0;
        
        // Create Default Profile
        for (int i = 0; i < TimeStamps.Count; i++)
        {
            inletTempProfile.Values.Add(defaultValue);
        }
        
        // Insert discharge events into the profile
        AddEventsToInletTempProfile(inletTempProfile, SimConfig.Input.events.discharge);
        
        // Insert charge events into the profile if available
        AddEventsToInletTempProfile(inletTempProfile, SimConfig.Input.events.charge);

        return inletTempProfile;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    private GeneralProfile<decimal> BuildSetTempProfile()
    {
        GeneralProfile<decimal> setTempProfile = new();
        decimal defaultValue = SimConfig.Input.tempSet.value;
        
        // Create Default Profile
        for (int i = 0; i < TimeStamps.Count; i++)
        {
            setTempProfile.Values.Add(defaultValue);
        }

        // Set the unit
        setTempProfile.Unit = SimConfig.Input.tempSet.unit;

        return setTempProfile;
    }

    private GeneralProfile<bool> BuildPowerAvailabilityProfile()
    {
        GeneralProfile<bool> powerProfile = new();

        // Create Default Profile
        for (int i = 0; i < TimeStamps.Count; i++)
        {
            powerProfile.Values.Add(true);
        }
        
        // Get the power off events 
        List<GeneralEvent> powerOffEvents = SimConfig.Input.events.powerOff;
        foreach (GeneralEvent eventEntry in powerOffEvents)
        {
            int startIndex = TimeStamps.IndexOf(eventEntry.start);
            int stopIndex = TimeStamps.IndexOf(eventEntry.stop);
            int indexRange = stopIndex - startIndex;

            // Set the values of the event to false to indicate no power
            for (int i = startIndex; i < startIndex + indexRange && i < powerProfile.Values.Count; i++)
            {
                powerProfile.Values[i] = false;
            }
        }
        
        // Set the unit
        powerProfile.Unit = "bool";
        
        return powerProfile;
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    private GeneralProfile<decimal> BuildAmbientProfile()
    {
        GeneralProfile<decimal> ambientProfile = new();
        decimal defaultValue = SimConfig.Input.ambientTemp.value;
        
        // Create Default Profile
        for (int i = 0; i < TimeStamps.Count; i++)
        {
            ambientProfile.Values.Add(defaultValue);
        }
        
        // Set the unit
        ambientProfile.Unit = SimConfig.Input.ambientTemp.unit;

        return ambientProfile;
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    private GeneralProfile<decimal> BuildInputCoilProfile()
    {
        GeneralProfile<decimal> inputCoilProfile = new();
        decimal defaultValue = SimConfig.Input.coilPower.value;
        
        // Create Default Profile
        for (int i = 0; i < TimeStamps.Count; i++)
        {
            inputCoilProfile.Values.Add(defaultValue);
        }
        
        // Set the unit
        inputCoilProfile.Unit = SimConfig.Input.coilPower.unit;
        
        return inputCoilProfile;
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    private GeneralProfile<decimal> BuildFlowProfile()
    {
        GeneralProfile<decimal> flowProfile = new();
        const decimal defaultValue = (decimal)0.00;
        
        // Create Default Profile
        for (int i = 0; i < TimeStamps.Count; i++)
        {
            flowProfile.Values.Add(defaultValue);
        }
        
        // Insert discharge events into the profile
        AddEventsToFlowProfile(flowProfile, SimConfig.Input.events.discharge);
        
        // Insert charge events into the profile if available
        AddEventsToFlowProfile(flowProfile, SimConfig.Input.events.charge, false);

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
            int startIndex = TimeStamps.IndexOf(eventEntry.start);
            int stopIndex = TimeStamps.IndexOf(eventEntry.stop);
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
            
            // Set the unit
            flowProfile.Unit = SimConfig.Input.events.discharge.FirstOrDefault()?.flowRate.unit;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="inletTempProfile"></param>
    /// <param name="flowEvents"></param>
    private void AddEventsToInletTempProfile(GeneralProfile<decimal> inletTempProfile, IEnumerable<FlowEvent> flowEvents)
    {
        foreach (FlowEvent eventEntry in flowEvents)
        {
            int startIndex = TimeStamps.IndexOf(eventEntry.start);
            int stopIndex = TimeStamps.IndexOf(eventEntry.stop);
            int indexRange = stopIndex - startIndex;

            // Set the values of the event to the provided value
            for (int i = startIndex; i < startIndex + indexRange && i < inletTempProfile.Values.Count; i++)
            {
                inletTempProfile.Values[i] = eventEntry.inletTemp.value;
            }
            
            // Set the unit
            inletTempProfile.Unit = SimConfig.Input.events.discharge.FirstOrDefault()?.inletTemp.unit;
        }
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="start"></param>
    /// <param name="stop"></param>
    /// <param name="delta"></param>
    /// <returns></returns>
    private List<DateTime> GenerateTimeStampProfile(DateTime start, DateTime stop, TimeSpan delta)
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
using System.Timers;
using EWH_Sim_PreProcessor.ConfigStructures;
using GeyserSimulator.FileManagement;
using GeyserSimulator.mqttManagement;
using IniFileParser.Model;
using MQTTnet.Client;
using Newtonsoft.Json;
using Timer = System.Timers.Timer;

namespace GeyserSimulator.SimThreadsManager;

public class GeyserInstance
{
    private readonly string _userId;
    private readonly GeyserStates _geyserStates;
    private readonly IMqttClient _mqttUserClient;

    public GeyserInstance(string instanceId, ref GeyserStates geyserStates, IMqttClient mqttClient, IniData settings)
    {
        _userId = instanceId;
        _geyserStates = geyserStates;
        _mqttUserClient = mqttClient;
        
        // Initialise fileWorker
        FileWorker fileWorker = new();

        // Set JSON Serialization settings
        JsonSerializerSettings serialisationSettings = new()
        {
            FloatParseHandling = FloatParseHandling.Decimal,
            Formatting = Formatting.Indented
        };
        
        // Deserialise config file into Configuration object
        SimulationConfig configJson = JsonConvert.DeserializeObject<SimulationConfig>(fileWorker.ReadAllText(settings["FilePaths"]["configFilePath"].Trim('"')), serialisationSettings) 
                                      ?? throw new Exception("Could not deserialise the configuration file");

    }

    public void StartSim()
    {
        SetDataUpdateTimer(_userId);
                                
        while (true)
        {
                                    
        }
    }
    
    private static void SetDataUpdateTimer(string message)
    {
        // Create a timer with a two second interval.
        Timer aTimer = new(3000);
        // Hook up the Elapsed event for the timer. 
        aTimer.Elapsed += (sender, e) => OnTimedEvent(sender, e, message);
        aTimer.AutoReset = true;
        aTimer.Enabled = true;
    }
    
    private static void OnTimedEvent(object source, ElapsedEventArgs e, string messageUid)
    {
        Console.WriteLine($"Hi, thread [{messageUid}] still alive {DateTime.Now}");
    }
}
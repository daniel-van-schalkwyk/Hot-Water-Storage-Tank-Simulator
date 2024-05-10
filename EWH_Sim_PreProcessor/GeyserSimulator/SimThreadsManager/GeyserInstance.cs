using EWH_Sim_PreProcessor.ConfigStructures;
using EWH_Sim_PreProcessor.ScriptCallManagement;
using GeyserSimulator.FileManagement;
using IniFileParser.Model;
using MQTTnet.Client;
using Newtonsoft.Json;

namespace GeyserSimulator.SimThreadsManager;

public class GeyserInstance
{
    public GeyserInstance(string instanceId, ref GeyserStates geyserStates, IMqttClient mqttClient, IniData settings)
    {
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

    public void Start()
    {
        
    }
}
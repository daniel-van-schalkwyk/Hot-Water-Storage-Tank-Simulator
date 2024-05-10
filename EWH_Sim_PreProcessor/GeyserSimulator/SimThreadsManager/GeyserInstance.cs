using EWH_Sim_PreProcessor.ScriptCallManagement;
using IniFileParser.Model;
using MQTTnet.Client;

namespace GeyserSimulator.SimThreadsManager;

public class GeyserInstance
{
    public GeyserInstance(string instanceId, ref GeyserStates geyserStates, IMqttClient mqttClient, IniData settings)
    {
        // ScriptCaller scriptCaller = new(settings["FilePaths"]["exeSimPath"].Trim('"'), "Add arguments here");
        // scriptCaller.CallScript();
    }
}
using System.Text;
using GeyserSimulator.mqttManagement;
using IniFileParser.Model;
using MQTTnet.Client;

namespace GeyserSimulator.SimThreadsManager;

public class SimThreadsManager
{
    public Dictionary<string, GeyserStates> AllGeyserStates { get; set; }

    private readonly MqttManager _masterConnection;
    private readonly IniData _settings;
    private readonly string _masterInAddTopic;
    private readonly string _masterInGetTopic;
    private readonly string _masterInSetTopic;
    private readonly string _masterOutInfoTopic;
    private readonly string _masterOutDataTopic;
    private readonly string _masterOutEventTopic;

    public SimThreadsManager(MqttManager masterConnection, IniData settings)
    {
        _masterConnection = masterConnection;
        _settings = settings;
        
        // Get Subscribe Topics
        try
        {
            _masterInAddTopic = _settings["TopicsMasterSub"]["geyserMasterAdd"].Trim('"');
            _masterInSetTopic = _settings["TopicsMasterSub"]["geyserMasterSet"].Trim('"');
            _masterInGetTopic = _settings["TopicsMasterSub"]["geyserMasterGet"].Trim('"');
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
        
        // Get Publish Topics
        try
        {
            _masterOutInfoTopic = _settings["TopicsMasterPub"]["geyserMasterInfo"].Trim('"');
            _masterOutEventTopic = _settings["TopicsMasterPub"]["geyserMasterEvent"].Trim('"');
            _masterOutDataTopic = _settings["TopicsMasterPub"]["geyserMasterData"].Trim('"');
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }

        AllGeyserStates = new Dictionary<string, GeyserStates>();
        // Connect to Master broker
        ConnectMasterBroker();
    }

    /// <summary>
    /// Connection to master broker
    /// </summary>
    private async void ConnectMasterBroker()
    {
        // Connect to broker
        await _masterConnection.Connect();

        // Assign Callback method for received messages
        await _masterConnection.AssignCallBackMethod(IncomingMessageHandler);

        // Subscribe to all topics from settings
        foreach (KeyData? subscribingTopic in _settings.Sections["TopicsMasterSub"])
        {
            await _masterConnection.Subscribe(subscribingTopic.Value);
        }
        
        // Publish the start of the manager
        await _masterConnection.Publish(_masterOutInfoTopic, new InfoMessage{Description = "Geyser Simulation Manager started.", Type = "INFO"}.Serialize());
    }
    
    private Task IncomingMessageHandler(MqttApplicationMessageReceivedEventArgs e)
    {
        Console.WriteLine($"Received message: {Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment)}");
        if (e.ApplicationMessage.Topic.Equals(_masterInAddTopic))
        {
            // New user needs to be added to simulator and new simulator instance needs to be created
            
        }
        else if(e.ApplicationMessage.Topic.Equals(_masterInSetTopic))
        {
            
        }
        else
        {
            Console.WriteLine($"Topic message: {e.ApplicationMessage.Topic}");
        }
        return Task.CompletedTask;
    }
}
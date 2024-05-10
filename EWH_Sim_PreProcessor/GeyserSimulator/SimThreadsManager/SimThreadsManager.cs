using System.Text;
using EWH_Sim_PreProcessor.ScriptCallManagement;
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

    private async Task<IMqttClient?> ConnectToUserBroker(AddUserMessage message)
    {
        try
        {
            AllGeyserStates.TryAdd(message.Uid, new GeyserStates());
                    
            // Attempt to connect to new MQTT broker URL
            MqttManager mqttInstance = new(message.Credentials.BrokerUrl, message.Credentials.Port, message.Credentials.Username, message.Credentials.Password);
            await mqttInstance.Connect();
            
            return mqttInstance.Client;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
    
    /// <summary>
    /// Handler function for the Master Broker incoming messages
    /// </summary>
    /// <param name="e"></param>
    /// <returns></returns>
    private async Task<Task> IncomingMessageHandler(MqttApplicationMessageReceivedEventArgs e)
    {
        Console.WriteLine($"Received message: {Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment)}");
        if (e.ApplicationMessage.Topic.Equals(_masterInAddTopic))
        {
            try
            {
                // New user needs to be added to simulator and new simulator instance needs to be created
                AddUserMessage? message = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment).Deserialize<AddUserMessage>();
                if (message?.Uid != null)
                {
                    GeyserStates geyserState = new();

                    // Connect to user broker
                    IMqttClient? userClient = await ConnectToUserBroker(message);
                    
                    // Call the script in a separate thread
                    Thread geyserSimThread = new(() =>
                    {
                        if (userClient != null)
                        {
                            GeyserInstance geyserSimInstance = new(message.Uid, ref geyserState, userClient, _settings);
                        }
                    });
                    
                    // Start the MATLAB thread
                    geyserSimThread.Start();
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
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
using System.Reflection;
using System.Text;
using System.Timers;
using GeyserSimulator.DbManagement;
using GeyserSimulator.mqttManagement;
using IniFileParser.Model;
using MQTTnet.Client;
using Timer = System.Timers.Timer;

namespace GeyserSimulator.SimThreadsManager;

public class SimThreadsManager
{
    public Dictionary<string, GeyserStates?> AllGeyserStates { get; set; }

    private IList<Thread> _threadPool;
    private readonly MqttManager _masterBrokerManager;
    private readonly InfluxDbManager _dbManager;
    private readonly IniData _settings;
    private readonly string _masterInAddTopic;
    private readonly string _masterInGetTopic;
    private readonly string _masterInSetTopic;
    private readonly string _masterOutInfoTopic;
    private readonly string _masterOutDataTopic;
    private readonly string _masterOutEventTopic;

    public SimThreadsManager(MqttManager masterBrokerManager, InfluxDbManager dbManager, IniData settings)
    {
        _masterBrokerManager = masterBrokerManager;
        _dbManager = dbManager;
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

        AllGeyserStates = new Dictionary<string, GeyserStates?>();
        _threadPool = new List<Thread>();
    }

    public void Start()
    {
        // Connect to Master broker and listen
        ConnectMasterBroker();
    }

    /// <summary>
    /// Connection to master broker
    /// </summary>
    private async void ConnectMasterBroker()
    {
        // Connect to broker
        await _masterBrokerManager.Connect();

        // Assign Callback method for received messages
        await _masterBrokerManager.AssignCallBackMethod(IncomingMasterMessageHandler);

        // Subscribe to all topics from settings
        foreach (KeyData? subscribingTopic in _settings.Sections["TopicsMasterSub"])
        {
            await _masterBrokerManager.Subscribe(subscribingTopic.Value);
        }
        
        // Publish the start of the manager
        await _masterBrokerManager.Publish(_masterOutInfoTopic, new InfoMessage{Description = "Geyser Simulation Manager started.", Type = "INFO"}.Serialize());
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    private async Task<IMqttClient?> ConnectToUserBroker(AddUserMessage message)
    {
        if (message.Uid == null) throw new InvalidOperationException("No UID was provided in message");
        
        // Add new geyser state
        AllGeyserStates.TryAdd(message.Uid, new GeyserStates());

        // Attempt to connect to new MQTT broker URL
        MqttManager mqttInstance = new(message.Credentials.BrokerUrl, message.Credentials.Port, message.Credentials.Username, message.Credentials.Password);
        await mqttInstance.Connect();
        await mqttInstance.AssignCallBackMethod(IncomingUserMessageHandler);
        await mqttInstance.Publish(_settings.Sections["TopicsPub"]["geyserInfo"],
            new InfoMessage
            {
                Description = $"Geyser simulator connected to {message.Name}'s broker", Uid = message.Uid,
                Type = "INFO"
            }.Serialize());
        
        // Subscribe to all topics from settings
        foreach (KeyData? subscribingTopic in _settings.Sections["TopicsSub"])
        {
            await mqttInstance.Subscribe(subscribingTopic.Value);
        }
        
        // Return client
        return mqttInstance.Client;
    }

    private async Task<Task> IncomingUserMessageHandler(MqttApplicationMessageReceivedEventArgs e)
    {
        // Handle User incoming message
        Console.WriteLine($"Received message: {Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment)}");
        if(e.ApplicationMessage.Topic.Equals("GeyserIn/Set"))
        {
            // New user needs to be added to simulator and new simulator instance needs to be created
            SetMessage? setMessage = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment).Deserialize<SetMessage>();

            if (setMessage?.Uid == null) return Task.CompletedTask;
            
            // Get the user's current geyser state 
            AllGeyserStates.TryGetValue(setMessage.Uid, out GeyserStates? state);
            
            // Get the field by name using reflection
            if (setMessage.Target == null) return Task.CompletedTask;
            PropertyInfo? property = typeof(GeyserStates).GetProperty(setMessage.Target);

            // Get the value of the field
            property?.SetValue(state, setMessage.Value);
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// Handler function for the Master Broker incoming messages
    /// </summary>
    /// <param name="e"></param>
    /// <returns></returns>
    private async Task<Task> IncomingMasterMessageHandler(MqttApplicationMessageReceivedEventArgs e)
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
                    // Create new geyser state
                    GeyserStates geyserState = new();
                    
                    try
                    {
                        // Connect to user broker
                        IMqttClient? userClient = await ConnectToUserBroker(message);
                        
                        // Call the script in a separate thread
                        Thread geyserSimThread = new(() =>
                        {
                            if (userClient == null) return;
                            GeyserInstance geyserSimInstance = new(message.Uid, ref geyserState, userClient, _settings);
                            geyserSimInstance.StartSim();
                        })
                        {
                            Name = $"{message.Uid}",
                            IsBackground = true
                        };

                        // Add thread to pool so that it can be managed
                        _threadPool.Add(geyserSimThread);
                        
                        // Start the simulation thread
                        geyserSimThread.Start();
                    }
                    catch (Exception exception)
                    {
                        Console.WriteLine(exception);
                        return Task.CompletedTask;
                    }
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }
        else if(e.ApplicationMessage.Topic.Equals(_masterInSetTopic))
        {
            // New user needs to be added to simulator and new simulator instance needs to be created
            SetMessage? setMessage = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment).Deserialize<SetMessage>();
            if (setMessage?.Target == null) return Task.CompletedTask;
            PropertyInfo? property = typeof(GeyserStates).GetProperty(setMessage.Target);
            
            
            if (setMessage?.Uid == null)
            {
                // Set all geyser state targets based on Master
                foreach (KeyValuePair<string, GeyserStates?> geyserState in AllGeyserStates)
                {
                    // Get the field by name using reflection
                    if (setMessage?.Target == null) return Task.CompletedTask;
                    

                    // Get the value of the field
                    property?.SetValue(geyserState.Value, setMessage.Value);
                }
                return Task.CompletedTask;
            }
            
            // Get the user's current geyser state 
            AllGeyserStates.TryGetValue(setMessage.Uid, out GeyserStates? state);
            
            // Get the field by name using reflection
            if (setMessage.Target == null) return Task.CompletedTask;

            // Get the value of the field
            property?.SetValue(state, setMessage.Value);
        }
        return Task.CompletedTask;
    }
}
using System.Reflection;
using System.Text;
using System.Timers;
using GeyserSimulator.ConfigStructures;
using GeyserSimulator.DbManagement;
using GeyserSimulator.FileManagement;
using GeyserSimulator.mqttManagement;
using IniFileParser.Model;
using MQTTnet.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
    private readonly string _userListPath;
    private JToken _userList;

    public SimThreadsManager(MqttManager masterBrokerManager, InfluxDbManager dbManager, IniData settings)
    {
        _masterBrokerManager = masterBrokerManager;
        _dbManager = dbManager;
        _settings = settings;
        _userListPath = settings["FilePaths"]["userListPath"];
        _userList = new JArray();
        
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

    /// <summary>
    /// A method that is used to read existing users and connect the app to their brokers
    /// </summary>
    private void StartUpExistingUsers()
    {
        FileWorker fileWorker = new();
        _userList = fileWorker.ReadJson(_userListPath) ?? new JArray();

        foreach (JToken user in _userList)
        {
            // Create new geyser state
            GeyserStates geyserState = new();

            // Add new geyser state
            UserMessage? userObj = user.ToObject<UserMessage>();
            if (userObj?.Uid != null)
                if(!AllGeyserStates.TryAdd(userObj.Uid, geyserState))
                    return;

            // Create a geyser simulation instance
            try
            {
                // Call the script in a separate thread
                Thread geyserSimThread = new(() =>
                {
                    if (userObj == null) return;
                    GeyserSimInstance geyserSimSimInstance = new(userObj, ref geyserState, _settings);
                    geyserSimSimInstance.StartSim();
                })
                {
                    Name = $"{userObj?.Uid}",
                    IsBackground = true
                };
                
                // Add thread to pool so that it can be managed
                _threadPool.Add(geyserSimThread);
                        
                // Start the simulation thread
                geyserSimThread.Start();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }

    public void Start()
    {
        // Connect to Master broker and listen
        ConnectMasterBroker();
        
        // Connect existing users
        StartUpExistingUsers();
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
    /// Handler function for the Master Broker incoming messages
    /// </summary>
    /// <param name="e"></param>
    /// <returns></returns>
    private Task<Task> IncomingMasterMessageHandler(MqttApplicationMessageReceivedEventArgs e)
    {
        Console.WriteLine($"Received message: {Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment)}");
        if (e.ApplicationMessage.Topic.Equals(_masterInAddTopic))
        {
            try
            {
                // New user needs to be added to simulator and new simulator instance needs to be created
                UserMessage? message = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment).Deserialize<UserMessage>();
                if (message?.Uid != null)
                {
                    // Create new geyser state
                    GeyserStates geyserState = new();
                    
                    // Add new geyser state
                    AllGeyserStates.TryAdd(message.Uid, geyserState);

                    try
                    {
                        // Update user list
                        if (_userList.Any(p => p["Uid"]?.Value<string>() == message.Uid))
                        {
                            Console.WriteLine($"{message.Uid} already exists, user will be updated");
                            _userList.FirstOrDefault(p => p["Uid"].Value<string>() == message.Uid)?.Remove();
                        }
                        
                        // Add user
                        ((JArray)_userList).Add(JToken.FromObject(message));
                        new FileWorker().WriteJson(_userListPath, _userList, new JsonSerializerSettings{Formatting = Formatting.Indented});
                        
                        // Call the script in a separate thread
                        Thread geyserSimThread = new(() =>
                        {
                            GeyserSimInstance geyserSimSimInstance = new(message, ref geyserState, _settings);
                            geyserSimSimInstance.StartSim();
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
                        return Task.FromResult(Task.CompletedTask);
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
            foreach (NameValuePair nameValuePair in setMessage?.Targets)
            {
                if (nameValuePair?.name == null) return Task.FromResult(Task.CompletedTask);
                PropertyInfo? property = typeof(GeyserStates).GetProperty(nameValuePair.name);
            
            
                if (setMessage?.Uid == null)
                {
                    // Set all geyser state targets based on Master
                    foreach (KeyValuePair<string, GeyserStates?> geyserState in AllGeyserStates)
                    {
                        // Get the field by name using reflection
                        if (nameValuePair?.name == null) return Task.FromResult(Task.CompletedTask);
                    
                        // Get the value of the field
                        property?.SetValue(geyserState.Value, nameValuePair.value);
                    }
                    return Task.FromResult(Task.CompletedTask);
                }
            
                // Get the user's current geyser state 
                AllGeyserStates.TryGetValue(setMessage.Uid, out GeyserStates? state);
            
                // Get the field by name using reflection
                if (nameValuePair?.name == null) return Task.FromResult(Task.CompletedTask);

                // Get the value of the field
                property?.SetValue(state, nameValuePair.value);
            }
        }
        return Task.FromResult(Task.CompletedTask);
    }
}
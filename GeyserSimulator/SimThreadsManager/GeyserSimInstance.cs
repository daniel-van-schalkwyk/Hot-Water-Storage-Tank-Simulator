using System.Reflection;
using System.Text;
using System.Timers;
using EWH_Sim_PreProcessor.ConfigStructures;
using GeyserSimulator.ConfigStructures;
using GeyserSimulator.FileManagement;
using GeyserSimulator.mqttManagement;
using IniFileParser.Model;
using MQTTnet.Client;
using Newtonsoft.Json;
using Timer = System.Timers.Timer;

namespace GeyserSimulator.SimThreadsManager;

public class GeyserSimInstance
{
    private readonly UserMessage _user;
    private GeyserInputs _geyserInputs;
    private MqttManager? _mqttUserContainer;
    private readonly IniData _settings;
    private string _infoOutTopic;

    public GeyserSimInstance(UserMessage user, ref GeyserInputs geyserInputs, IniData settings)
    {
        _user = user;
        _geyserInputs = geyserInputs;
        _settings = settings;
        _infoOutTopic = settings["TopicsPub"]["geyserInfo"];
        // Initialise fileWorker
        FileWorker fileWorker = new();

        // Set JSON Serialization settings
        JsonSerializerSettings serialisationSettings = new()
        {
            FloatParseHandling = FloatParseHandling.Decimal,
            Formatting = Formatting.Indented
        };
    }

    public async void StartSim()
    {
        _mqttUserContainer = await ConnectToUserBroker(_user);
        SetKeepALiveTimer(_user.Uid);
                                
        while (true)
        {
                                    
        }
    }
    
    private void SetKeepALiveTimer(string messageUid)
    {
        // Create a timer with a two second interval.
        Timer aTimer = new(10000);
        // Hook up the Elapsed event for the timer. 
        aTimer.Elapsed += (sender, e) => OnTimedEvent(sender, e, _mqttUserContainer, messageUid);
        aTimer.AutoReset = true;
        aTimer.Enabled = true;
    }
    
    private static async void OnTimedEvent(object source, ElapsedEventArgs e, MqttManager? mqttContainer, string messageUid)
    {
        await mqttContainer.Publish("GeyserOut/Info", new InfoMessage{Description = "Session active", Uid = messageUid}.Serialize());
        Console.WriteLine($"Hi, thread [{messageUid}] still alive {DateTime.Now}");
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    private async Task<MqttManager?> ConnectToUserBroker(UserMessage? message)
    {
        if (message is null)
            return default;
        
        if (message.Uid == null) 
            throw new InvalidOperationException("No UID was provided in message");

        // Attempt to connect to new MQTT broker URL
        try
        {
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
            return mqttInstance;
        }
        catch (Exception)
        {
            Console.WriteLine($"Could not connect to client broker [client uid: {message.Uid}]");
            return default;
        }
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

            if (!await CheckUid(setMessage.Uid)) return Task.CompletedTask;
            
            // Set the input of the geyser
            SetGeyserState(setMessage, ref _geyserInputs);
            if (_mqttUserContainer is not null)
            {
                await _mqttUserContainer.Publish(_infoOutTopic,
                    new InfoMessage
                        {
                            Description = "SET command received for geyser input", Uid = setMessage.Uid,
                            inputs = _geyserInputs
                        }
                        .Serialize());
            }
        }
        return Task.CompletedTask;
    }

    private async Task<bool> CheckUid(string uidMessage)
    {
        if (uidMessage == _user.Uid) return true;
        if (_mqttUserContainer is null) return false;
        await _mqttUserContainer.Publish(_infoOutTopic,
            new InfoMessage
                    { Description = $"SET command received with incorrect UID, please use {_user.Uid} as your UID", Uid = _user.Uid, inputs = _geyserInputs}
                .Serialize());
        return false;

    }

    private static void SetGeyserState(SetMessage setMessage, ref GeyserInputs input)
    {
        // Get the field by name using reflection
        foreach (NameValuePair targetPair in setMessage.Targets)
        {
            if (targetPair.name == null);
            PropertyInfo? property = typeof(GeyserInputs).GetProperty(targetPair.name);

            // Get the value of the field
            property?.SetValue(input, targetPair.value);
        }
    }
}
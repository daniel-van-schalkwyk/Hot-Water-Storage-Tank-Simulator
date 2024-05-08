namespace GeyserSimulator.mqttManagement;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;

public class MqttManager
{
    public List<string> Topics { get; set; }
    public IMqttClient? Client { get; set; }
    
    private readonly string _broker;
    private readonly int _port;
    private readonly string _username;
    private readonly string _password;
    private readonly string _certPath;
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="broker"></param>
    /// <param name="port"></param>
    /// <param name="username"></param>
    /// <param name="password"></param>
    /// <param name="certificatePath"></param>
    public MqttManager(string broker, int port, string username, string password, string certificatePath)
    {
        _broker = broker;
        _port = port;
        _username = username;
        _password = password;
        _certPath = certificatePath;
        Topics = new List<string>();
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public async Task<bool> Connect()
    {
        // Generate unique ID
        string clientId = Guid.NewGuid().ToString();

        // Create a MQTT client factory
        MqttFactory factory = new();

        // Create a MQTT client instance
        Client = factory.CreateMqttClient();

        // Create MQTT client options
        MqttClientOptions? options = new MqttClientOptionsBuilder()
            .WithTcpServer(_broker, _port) // MQTT broker address and port
            .WithCredentials(_username, _password) // Set username and password
            .WithClientId(clientId)
            .WithCleanStart()
            .WithTls(
                o =>
                {
                    // The used public broker sometimes has invalid certificates. This sample accepts all
                    // certificates. This should not be used in live environments.
                    o.CertificateValidationHandler = _ => true;
                
                    // The default value is determined by the OS. Set manually to force version.
                    o.SslProtocol = SslProtocols.Tls12; 
                
                    // Certificate
                    X509Certificate certificate = new(_certPath, "");
                    o.Certificates = new List<X509Certificate> { certificate };
                }
            )
            .Build();

        // Connect to MQTT broker
        MqttClientConnectResult? connectResult = await Client.ConnectAsync(options);
        Console.WriteLine(connectResult.ResultCode == MqttClientConnectResultCode.Success
            ? "Connected to MQTT broker!"
            : "Could not connect to MQTT broker...");
        return connectResult.ResultCode == MqttClientConnectResultCode.Success;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="topic"></param>
    public async Task Subscribe(string topic)
    {
        // Subscribe to a topic
        await Client.SubscribeAsync(topic);

        // Callback function when a message is received
        if (Client != null)
        {
            Client.ApplicationMessageReceivedAsync += e =>
            {
                // Interpret messages
                IncomingMessageHandler(e);
                return Task.CompletedTask;
            };
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="topic"></param>
    /// <param name="payload"></param>
    public async Task Publish(string topic, string payload)
    {
        MqttApplicationMessage? message = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(payload)
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
            .WithRetainFlag()
            .Build();

        if (Client != null) await Client.PublishAsync(message);
    }
    
    private void IncomingMessageHandler(MqttApplicationMessageReceivedEventArgs e)
    {
        Console.WriteLine($"Received message: {Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment)}");
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="topic"></param>
    public async Task Unsubscribe(string topic)
    {
        // Unsubscribe
        await Client.UnsubscribeAsync(topic);
    }

    /// <summary>
    /// 
    /// </summary>
    public async Task Disconnect()
    {
        await Client.DisconnectAsync();
        Console.WriteLine("Disconnected from MQTT broker.");
    }
}
namespace GeyserSimulator.mqttManagement;

[Serializable]
public class BrokerCredentials
{
    public string BrokerUrl { get; set; }
    public int Port { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    
    public BrokerCredentials(string brokerUrl, int port, string username, string password)
    {
        BrokerUrl = brokerUrl;
        Port = port;
        Username = username;
        Password = password;
    }
}
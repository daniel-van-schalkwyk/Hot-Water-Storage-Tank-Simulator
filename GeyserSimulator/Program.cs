using GeyserSimulator.DbManagement;
using GeyserSimulator.mqttManagement;
using GeyserSimulator.SimThreadsManager;
using IniFileParser.Model;

// Import Settings
IniFileParser.IniFileParser fileParser = new();
IniData? settingsData = fileParser.ReadFile(@"C:\Users\DanielvanSchalkwyk\OneDrive - Stellenbosch University\PhD\EWH simulator\GeyserSimulator\Settings\Settings.ini");
SectionDataCollection? settings = settingsData.Sections;

string brokerUrl = settings["ConnectionDetails"]["masterBrokerUrl"].Trim('"');
int port = int.Parse(settings["ConnectionDetails"]["port"]);
string username = settings["ConnectionDetails"]["username"].Trim('"');
string password = settings["ConnectionDetails"]["password"].Trim('"');

// Initialise Master MQTT connection parameters
MqttManager mqttManager = new(new BrokerCredentials(brokerUrl, port, username, password));

// Create connection wih InfluxDB
InfluxDbManager dbManager = new(settingsData);

// Create threads manager
SimThreadsManager threadsManager = new(mqttManager, dbManager, settingsData);
threadsManager.Start();

Console.WriteLine("Master listening...");
while (true)
{
    
}

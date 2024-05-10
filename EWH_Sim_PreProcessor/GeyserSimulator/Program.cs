using EWH_Sim_PreProcessor.ConfigStructures;
using GeyserSimulator.FileManagement;
using GeyserSimulator.mqttManagement;
using GeyserSimulator.SimThreadsManager;
using IniFileParser.Model;
using Newtonsoft.Json;
using Formatting = Newtonsoft.Json.Formatting;

// Import Settings
IniFileParser.IniFileParser fileParser = new();
IniData? settingsData = fileParser.ReadFile(@"C:\Users\DanielvanSchalkwyk\OneDrive - Stellenbosch University\PhD\EWH simulator\EWH_Sim_PreProcessor\GeyserSimulator\Settings\Settings.ini");
SectionDataCollection? settings = settingsData.Sections;

string brokerUrl = settings["ConnectionDetails"]["masterBrokerUrl"].Trim('"');
int port = int.Parse(settings["ConnectionDetails"]["port"]);
string username = settings["ConnectionDetails"]["username"].Trim('"');
string password = settings["ConnectionDetails"]["password"].Trim('"');
string certPath = settings["ConnectionDetails"]["certPath"].Trim('"');

// Initialise MQTT connection parameters
MqttManager mqttManager = new(brokerUrl, port, username, password, certPath);

// Create threads manager
SimThreadsManager threadsManager = new(mqttManager, settingsData);
threadsManager.Start();

Console.WriteLine("Main thread finished.");

Console.WriteLine("Listening...");
while (true)
{
    
}

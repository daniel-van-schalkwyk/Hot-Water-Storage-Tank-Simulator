using EWH_Sim_PreProcessor.ConfigStructures;
using GeyserSimulator;
using GeyserSimulator.FileManagement;
using GeyserSimulator.mqttManagement;
using GeyserSimulator.SimThreadsManager;
using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using IniFileParser.Model;

// Import Settings
IniFileParser.IniFileParser fileParser = new();
IniData? settingsData = fileParser.ReadFile(@"C:\Users\DanielvanSchalkwyk\OneDrive - Stellenbosch University\PhD\EWH simulator\EWH_Sim_PreProcessor\GeyserSimulator\Settings\Settings.ini");
SectionDataCollection? settings = settingsData.Sections;

// You can generate an API token from the "API Tokens Tab" in the UI
string token = settings["InfluxDB"]["token"];
string bucket = settings["InfluxDB"]["bucket"];
string hostUrl = $"{settings["InfluxDB"]["host"]}:{settings["InfluxDB"]["port"]}";
using InfluxDBClient client = new(hostUrl, token);

DataMessage dataMessage = new() { Uid = "U0001", Type = "Data", Timestamp = DateTime.UtcNow, AmbientTemp = 20, CoilPower = 3000, InternalEnergy = 1000, ThermostatTemp = 55, SOC = (decimal)30.45};

using (WriteApi? writeApi = client.GetWriteApi())
{
    writeApi.WriteMeasurement(dataMessage, WritePrecision.Ns, bucket, "IPS");
}

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

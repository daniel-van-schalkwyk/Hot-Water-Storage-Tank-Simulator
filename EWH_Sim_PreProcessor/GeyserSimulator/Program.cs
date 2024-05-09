using EWH_Sim_PreProcessor.ConfigStructures;
using GeyserSimulator.FileManagement;
using GeyserSimulator.mqttManagement;
using IniFileParser.Model;
using Newtonsoft.Json;
using Formatting = Newtonsoft.Json.Formatting;

// Import Settings
IniFileParser.IniFileParser fileParser = new();
IniData? iniData = fileParser.ReadFile(@"C:\Users\DanielvanSchalkwyk\OneDrive - Stellenbosch University\PhD\EWH simulator\EWH_Sim_PreProcessor\GeyserSimulator\Settings\Settings.ini");
SectionDataCollection? settings = iniData.Sections;

// Initialise fileWorker
FileWorker fileWorker = new();

// Set JSON Serialization settings
JsonSerializerSettings serialisationSettings = new()
{
    FloatParseHandling = FloatParseHandling.Decimal,
    Formatting = Formatting.Indented
};

// Deserialise config file into Configuration object
SimulationConfig configJson = JsonConvert.DeserializeObject<SimulationConfig>(fileWorker.ReadAllText(settings["FilePaths"]["configFilePath"].Trim('"')), serialisationSettings) 
                              ?? throw new Exception("Could not deserialise the configuration file");

// // Call the script in a separate thread
// Thread matlabThread = new(() =>
// {
//     ScriptCaller scriptCaller = new(settings["FilePaths"]["exeSimPath"].Trim('"'), "Add arguments here");
//     scriptCaller.CallScript();
// });
//
// // Start the MATLAB thread
// matlabThread.Start();
//
// // Do other work in the main thread if needed
// Console.WriteLine("Main thread...");
//
// // Wait for the MATLAB thread to finish (optional)
// matlabThread.Join();

Console.WriteLine("Main thread finished.");

string brokerUrl = settings["ConnectionDetails"]["brokerUrl"].Trim('"');;
int port = int.Parse(settings["ConnectionDetails"]["port"]);
string username = settings["ConnectionDetails"]["username"].Trim('"');;
string password = settings["ConnectionDetails"]["password"].Trim('"');;
string certPath = settings["ConnectionDetails"]["certPath"].Trim('"');

// Initialise MQTT connection parameters
MqttManager mqttManager = new(brokerUrl, port, username, password, certPath);

// Connect to broker
await mqttManager.Connect();

// Assign Callback method for received messages
await mqttManager.AssignCallBackMethod();

// Subscribe to topics
await mqttManager.Subscribe(settings["MqttTopics"]["GeyserSet"].Trim('"'));

Console.WriteLine("Listening...");
while (true)
{
    
}

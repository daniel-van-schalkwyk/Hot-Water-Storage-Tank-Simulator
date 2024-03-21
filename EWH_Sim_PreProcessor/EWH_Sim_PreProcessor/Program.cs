using EWH_Sim_PreProcessor;using EWH_Sim_PreProcessor.ConfigStructures;
using EWH_Sim_PreProcessor.FileManagement;
using EWH_Sim_PreProcessor.ProfileManagement;
using EWH_Sim_PreProcessor.ScriptCallManagement;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

const string filePath = @"C:\Users\DanielvanSchalkwyk\OneDrive - Stellenbosch University\PhD\EWH simulator\EWH_Sim.config.json";
const string exeSimPath = @"C:\Users\DanielvanSchalkwyk\OneDrive - Stellenbosch University\PhD\EWH simulator\EHWST_Simulation_Start\for_redistribution_files_only\EHWST_Simulation_Start.exe";
    
FileWorker fileWorker = new();

JsonSerializerSettings serialisationSettings = new()
{
    FloatParseHandling = FloatParseHandling.Decimal,
    Formatting = Formatting.Indented
};

// Deserialise config file into Configuration object
SimulationConfig configJson = JsonConvert.DeserializeObject<SimulationConfig>(fileWorker.ReadAllText(filePath), serialisationSettings) 
                              ?? throw new Exception("Could not deserialise the configuration file");

string simConfigFile = "";
try
{
    ProfileBuilder profileBuilder = new(configJson);
    configJson.Profiles = profileBuilder.SimInputProfiles;
    simConfigFile = $"{filePath.Replace(".json", "")}_Profiles.json";
    fileWorker.WriteJson(simConfigFile, JObject.FromObject(configJson), serialisationSettings);
    
}
catch (Exception e)
{
    Console.WriteLine(e);
}

// Call the script in a separate thread
Thread matlabThread = new(() =>
{
    ScriptCaller scriptCaller = new(exeSimPath, $"\"{simConfigFile}\"");
    scriptCaller.CallScript();
});

// Start the MATLAB thread
matlabThread.Start();

// Do other work in the main thread if needed
Console.WriteLine("Main thread is doing some other work...");

// Wait for the MATLAB thread to finish (optional)
matlabThread.Join();

Console.WriteLine("Main thread finished.");
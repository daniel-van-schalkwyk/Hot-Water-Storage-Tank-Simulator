using EWH_Sim_PreProcessor;using EWH_Sim_PreProcessor.ConfigStructures;
using EWH_Sim_PreProcessor.FileManagement;
using Newtonsoft.Json;

const string filePath = @"C:\Users\DanielvanSchalkwyk\OneDrive - Stellenbosch University\PhD\EWH simulator\EWH_Sim.config.json";
FileWorker fileWorker = new();

JsonSerializerSettings serialisationSettings = new()
{
    FloatParseHandling = FloatParseHandling.Decimal
};

// Deserialise config file into Configuration object
SimulationConfig configJson = JsonConvert.DeserializeObject<SimulationConfig>(fileWorker.ReadAllText(filePath), serialisationSettings) 
                              ?? throw new Exception("Could not deserialise the configuration file");
try
{
    ProfileBuilder profileBuilder = new(configJson);
}
catch (Exception e)
{
    Console.WriteLine(e);
}

Console.WriteLine("e");
using EWH_Sim_PreProcessor;using EWH_Sim_PreProcessor.ConfigStructures;
using EWH_Sim_PreProcessor.FileManagement;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

const string filePath = @"C:\Users\DanielvanSchalkwyk\OneDrive - Stellenbosch University\PhD\EWH simulator\EWH_Sim.config.json";
FileWorker fileWorker = new();

JsonSerializerSettings serialisationSettings = new()
{
    FloatParseHandling = FloatParseHandling.Decimal,
    Formatting = Formatting.Indented
};

// Deserialise config file into Configuration object
SimulationConfig configJson = JsonConvert.DeserializeObject<SimulationConfig>(fileWorker.ReadAllText(filePath), serialisationSettings) 
                              ?? throw new Exception("Could not deserialise the configuration file");
try
{
    ProfileBuilder profileBuilder = new(configJson);
    configJson.Profiles = profileBuilder.SimInputProfiles;
    fileWorker.WriteJson($"{filePath.Replace(".json", "")}_Profiles.json", JObject.FromObject(configJson), serialisationSettings);
    
}
catch (Exception e)
{
    Console.WriteLine(e);
}

Console.WriteLine("e");
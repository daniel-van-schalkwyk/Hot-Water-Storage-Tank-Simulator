using EWH_Sim_PreProcessor.ConfigStructures;
using EWH_Sim_PreProcessor.FileManagement;
using Newtonsoft.Json;

const string filePath = @"C:\Users\DanielvanSchalkwyk\OneDrive - Stellenbosch University\PhD\EWH simulator\EWH_Sim.config.json";
FileWorker fileWorker = new();

// Deserialise config file into Configuration object
SimulationConfig? result = JsonConvert.DeserializeObject<SimulationConfig>(fileWorker.ReadAllText(filePath));






Console.Write("Done");
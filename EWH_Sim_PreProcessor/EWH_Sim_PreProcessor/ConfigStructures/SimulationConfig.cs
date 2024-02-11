namespace EWH_Sim_PreProcessor.ConfigStructures;

[Serializable]
public class SimulationConfig
{
    public General general { get; set; }
    public Config config { get; set; }
    public SimParameters simParameters { get; set; }
    public Input input { get; set; }
}
namespace EWH_Sim_PreProcessor.ConfigStructures;

[Serializable]
public class Config
{
    public string? orientation { get; set; }
    public decimal? tWall { get; set; }
    public decimal? longLength { get; set; }
    public decimal? diameter { get; set; }
    public decimal? capacity { get; set; }
    public string? layerConfig { get; set; }
    public int? nodeNumber { get; set; }
}
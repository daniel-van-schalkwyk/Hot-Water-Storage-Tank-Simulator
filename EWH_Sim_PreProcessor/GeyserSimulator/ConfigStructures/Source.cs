namespace EWH_Sim_PreProcessor.ConfigStructures;

[Serializable]
public class Source
{
    public bool? csv { get; set; }
    public string? filePath { get; set; }
    public List<NameValuePair> units { get; set; }
}

[Serializable]
public class NameValuePair
{
    public string name { get; set; }
    public string value { get; set; }
}
using System.Globalization;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;

namespace EWH_Sim_PreProcessor.ProfileManagement;

public class CsvProfile
{
    [Name("time")]
    public DateTime Time { get; set; }

    [Name("coilPower")]
    public decimal CoilPower { get; set; }

    [Name("ambientTemp")]
    public decimal AmbientTemp { get; set; }

    [Name("tempSet")]
    public decimal TempSet { get; set; }

    [Name("flowRate")]
    public decimal FlowRate { get; set; }

    [Name("inletTemp")]
    public decimal InletTemp { get; set; }

    [Name("powerAvailable")]
    public bool PowerAvailable { get; set; }
    
}
namespace EWH_Sim_PreProcessor;

public class GeneralProfile<TValueType>
{
    public List<DateTime> TimeStamps { get; set; }
    public List<TValueType> Values { get; set; }

    public GeneralProfile()
    {
        TimeStamps = new List<DateTime>();
        Values = new List<TValueType>();
    }
}
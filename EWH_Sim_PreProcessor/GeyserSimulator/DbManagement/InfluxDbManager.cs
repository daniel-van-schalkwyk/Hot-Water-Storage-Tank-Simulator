using GeyserSimulator.mqttManagement;
using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using IniFileParser.Model;

namespace GeyserSimulator.DbManagement;

public class InfluxDbManager
{
    private readonly string _bucket;
    private readonly string _org;
    private InfluxDBClient Client { get; }
    
    public InfluxDbManager(IniData settings)
    {
        string token = settings["InfluxDB"]["token"];
        string hostUrl = $"{settings["InfluxDB"]["host"]}:{settings["InfluxDB"]["port"]}";
        _bucket = settings["InfluxDB"]["bucket"];
        _org = settings["InfluxDB"]["org"];
        
        // Attempt connection 
        try
        {
            Client = new InfluxDBClient(hostUrl, token);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="data"></param>
    public async Task WriteData(MqttMessages data)
    {
        await Client.GetWriteApiAsync().WriteMeasurementAsync(data, WritePrecision.Ns, _bucket, _org);
    }

    public async Task QueryData()
    {
        
    }
}
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

    /// <summary>
    /// Method used to query data from the database
    /// </summary>
    /// <param name="queryString">Query string [FluxQL]</param>
    /// <typeparam name="T">Data structure results need to be casted to</typeparam>
    /// <returns>A list of data objects casted to the type </returns>
    public async Task<List<T>> QueryData<T>(string queryString)
    {
        return await Client.GetQueryApi().QueryAsync<T>(queryString);
    }
    
    /// <summary>
    /// A method used to delete data from the InfluxDB database using a range and a predicate
    /// </summary>
    /// <param name="start">Start of delete range</param>
    /// <param name="stop">Stop of delete ranges</param>
    /// <param name="predicate">A condition for which data to delete</param>
    public async Task DeleteData(DateTime start, DateTime stop, string predicate)
    {
        await Client.GetDeleteApi().Delete(start, stop, predicate, _bucket, _org);
    }
}
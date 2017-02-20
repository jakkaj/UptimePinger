using System.Net;

public static async Task<HttpResponseMessage> Run(HttpRequestMessage req,ICollector<UptimePing> outputTable, TraceWriter log)
{
    log.Info("C# HTTP trigger function processed a request.");
    
    // Get request body
    var data = await req.Content.ReadAsAsync<PingSource>();
    log.Info($"Machine: {data.MachineName}");

    var dtMax = (DateTime.MaxValue.Ticks - DateTime.UtcNow.Ticks).ToString("d19");

    var ping = new UptimePing{
        RowKey = dtMax,
        PartitionKey = data.MachineName,
        PingTime = DateTime.UtcNow
    };
    
    outputTable.Add(ping);

    return req.CreateResponse(HttpStatusCode.OK, "Logged");
}

public class PingSource{
    public string MachineName{get;set;}
}

public class UptimePing{
    public string PartitionKey { get; set; }
    public string RowKey { get; set; }
    public DateTime PingTime{get;set;}
    
}
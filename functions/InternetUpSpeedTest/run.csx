#r "Microsoft.WindowsAzure.Storage"

using System.Net;

using System;

using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

private static string key = TelemetryConfiguration.Active.InstrumentationKey = "<telemetrykey>";
private static TelemetryClient tc = new TelemetryClient();

public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, ICollector<SpeedTestTableResult> outputTable, TraceWriter log)
{
    log.Info("C# HTTP trigger function processed a request.");
    
    // Get request body
    var data = await req.Content.ReadAsAsync<SpeedTestResult>();

    if(data == null)
    {
        return req.CreateResponse(HttpStatusCode.BadRequest, "SpeedTestResult");
    }

    var dtMax = (DateTime.MaxValue.Ticks - DateTime.UtcNow.Ticks).ToString("d19");    

    var saveSpeedTest = new SpeedTestTableResult{
        UploadSpeed = data.UploadSpeed,
        DownloadSpeed = data.DownloadSpeed,
        ServerInfo = data.ServerInfo,
        PartitionKey = data.TestName,
        RowKey = dtMax
    };

     tc.TrackMetric($"UploadSpeed_{data.TestName}", data.UploadSpeed);
     tc.TrackMetric($"DownloadSpeed_{data.TestName}", data.DownloadSpeed); 

    outputTable.Add(saveSpeedTest);

    return req.CreateResponse(HttpStatusCode.OK, "Good. Speedy");    
}

public class SpeedTestTableResult : TableEntity{
    public double UploadSpeed {get;set;}
    public double DownloadSpeed{get;set;}
    public string ServerInfo{get;set;}
    
}

public class SpeedTestResult{
    public double UploadSpeed {get;set;}
    public double DownloadSpeed{get;set;}
    public string ServerInfo{get;set;}
    public string TestName{get;set;}
}
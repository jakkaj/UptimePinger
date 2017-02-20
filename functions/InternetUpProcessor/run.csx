#r "Microsoft.WindowsAzure.Storage"


using System;

using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

using ExtensionGoo.Standard.Extensions;

private static string key = TelemetryConfiguration.Active.InstrumentationKey = "<telemetrykey>";
private static TelemetryClient tc = new TelemetryClient();

public static void Run(TimerInfo myTimer, IQueryable<UptimePing> inputTable, IQueryable<IsUp> uptimeTableIn, ICollector<IsUp> outputTable, TraceWriter log)
{
    var nowSub = DateTime.UtcNow.AddMinutes(-2);

    var countNew = inputTable.Where(p => p.PingTime > nowSub && p.PartitionKey=="Beastly").ToList().Count;

    log.Info($"Count: {countNew}");

    bool up = countNew > 0;

    var dtMax = (DateTime.MaxValue.Ticks - DateTime.UtcNow.Ticks).ToString("d19");       
    
    var isUp = new IsUp{
        Up = up, 
        RowKey = dtMax,
        PartitionKey = "UpTimeHome"
    };

    var firstInTable = uptimeTableIn.FirstOrDefault();

    var pushUrl = "<yourpushurl>";
    
    if(up){
        tc.TrackEvent("Up");
        tc.TrackMetric("DownTime", 0);

        //Send a push if its changing
        if(firstInTable!=null && !firstInTable.Up){
            //pushUrl.Post("{\"value1\":\"Internet is up\"}");
        }
    }else{
        tc.TrackEvent("Down");
        //add metrics for downtime
        var first = inputTable.Where(p=>p.PartitionKey=="Beastly").FirstOrDefault();

        if(first != null){
            var timeAgo = DateTime.UtcNow - first.Timestamp;
            log.Info($"DownTime: {(int)timeAgo.TotalSeconds}");
            tc.TrackMetric("DownTime", (int)timeAgo.TotalSeconds); 
            isUp.DownTime = (int)timeAgo.TotalSeconds;
        }

        if(firstInTable!=null && firstInTable.Up){
            //pushUrl.Post("{\"value1\":\"Internet is down\"}");
        }
    }    

    outputTable.Add(isUp);    

    tc.TrackEvent("BoolState", new Dictionary<string, string>{{"state", up ? "true" : "false"}});
    tc.TrackMetric("MetricState", up ? 1 : 0);

    log.Info($"C# Timer trigger function executed at: {DateTime.Now}");    
}



public class UptimePing : TableEntity{
    
    public DateTime PingTime{get;set;}
    
}

public class IsUp : TableEntity{
    public bool Up{get;set;}
    public int DownTime{get;set;}   
}
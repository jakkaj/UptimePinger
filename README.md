Since getting an upgrade recently my home internet has been very unstable with up to 39 drop outs a day. 

I did the usual thing and rang my provider - only to be told that "it's currently working" and there is not much they can do. Of course, it was working when I called. 

So I called when it wasn't. The tech comes out a couple of days later. "Oh, it's working fine". He tinkered with it and left. 

It's still dropping out to the point that I'm having to tether my phone to my home PC. 

So I figured I'd collect some data. Lots of data. 

I of course had a look around to see if something could do it - the solutions I found were non-intuitive or cost money. Nope - CUSTOM BUILD TIME. There, justified. 

I had a think around how I might go about this - I didn't want to spend too much time on something that was already costing me time. How to throw together a monitoring system without spending too much time?

The system I came up with uses Azure Functions, Table Storage, Application Insights, Powershell and Windows Task Scheduler. I threw it together in a couple of hours at most. 

### Basic Flow
The process starts with a [PowerShell script](https://github.com/jakkaj/UptimePinger/blob/master/pingsource/pinger.ps1) that is fired by Task Scheduler on Windows every 1 minute. 

This script calls the [UptimeLogger Azure Function](https://github.com/jakkaj/UptimePinger/blob/master/functions/InternetUpLogger/run.csx) which logs the data to an Azure Storage Table. 

I then have a [Processor Azure Function](https://github.com/jakkaj/UptimePinger/blob/master/functions/InternetUpProcessor/run.csx) that runs every minute to check to see if there are any new entries in the Azure Table. If not - then we know that there has been some downtime. 

This processor function sends the results about up-time to [Application Insights](https://azure.microsoft.com/en-au/services/application-insights/). 

### In Depth

#### Table Storage
Set up an Azure Storage Account. 

[Click here](https://ms.portal.azure.com/#create/Microsoft.StorageAccount-ARM) to get started. 

Enter a name for your new storage account (I called mine internetuptime - all lower case!).

Create a new Resource Group called *intetnetmonitoring* which will allow you to keep all your bits for this project in the same place. 

Once that is created you should be able to install [StorageExplorer](http://storageexplorer.com/) and browse to it. This will be a good way to debug your service later. 
 
#### Azure Functions
If you know about functions, grab the function code from [here](https://github.com/jakkaj/UptimePinger/tree/master/functions) and set up up. 
There are a couple of inputs and outputs to think about and some project.json files to work with for Nugets FYI.

<a href="#postfunctions">I know about functions, skip me to the next bit</a>

Next you need to set up the Azure Functions. These provide an end point for your pings as well as background processing that does the actual up/down calculation and sends data to Application Insights. 

They are super easy to get going - [click here](https://ms.portal.azure.com/#create/Microsoft.FunctionApp) to get started. 

Select consumtion plan if you're not sure what to select there. 
Use your new Resource Group called intetnetmonitoring so you can group all the services for this project in the one place. 

![img1](https://cloud.githubusercontent.com/assets/5225782/23111786/30a59d78-f77f-11e6-8376-4ace8e709803.PNG)


Next go in to the editor so you can start editing your new functions. If you can't locate it, look under the [App Services](https://ms.portal.azure.com/#blade/HubsExtension/Resources/resourceType/Microsoft.Web%2Fsites) section. 

Add a new function called *InternetUpLogger*. 

Filter by C# and API & WebHooks then select the HttpTrigger-CSharp option. Enter the *InternetUpLogger* and click Create. 

This will create a new function that will act as a web endpoint that you can call. 

<iframe width="560" height="315" src="https://www.youtube.com/embed/Yn9P7hEWVVE" frameborder="0" allowfullscreen></iframe>

You will see a place to drop some code. This is an basic azure function. 

Before you can edit the code you need to add some outputs. 

Azure Functions can look after some inputs and outputs for you - so you don't have to write a lot of code and config to say read from a database and write to a table. 

Click on Integrate, then click *New Output*. Select Azure Table Storage from the list and click *Select*.

Next you'll need to set up the connection to your table storage if you've not done so already in this Function. 

Click *New* next to Storage account aonnection and select the account from the list. 

Once that is completed, click *Save*. 

You can expand the documentation to see some examples of how to use the new output. 

<iframe width="560" height="315" src="https://www.youtube.com/embed/cjB6Y8BZwJg" frameborder="0" allowfullscreen></iframe>

Now you can paste in the function code from [here](https://github.com/jakkaj/UptimePinger/blob/master/functions/InternetUpLogger/run.csx)

##### Some points of interest

Note the ICollector<UptimePing> is passed in to run automatically. This is the param you configured. In my video it's called outTable, you may need to change it OOPS!

```csharp
public static async Task<HttpResponseMessage> Run(HttpRequestMessage req,ICollector<UptimePing> outputTable, TraceWriter log)
{
}
```
The next interesting thing is the RowKey I'm setting.

```csharp
var dtMax = (DateTime.MaxValue.Ticks - DateTime.UtcNow.Ticks).ToString("d19");

var ping = new UptimePing{
    RowKey = dtMax,
    PartitionKey = data.MachineName,
    PingTime = DateTime.UtcNow
};
    
outputTable.Add(ping);
```

You cannot sort on Azure Table rows by default using LINQ etc. RowKey are auto ordered in descending format. So we make sure that the newer the row, the higher the number is by subtracting from DateTime.MaxValue. 

Once that is done we pop it in to the ICollector which will go and add it to the table for us! Too easy!

<a name="postfunctions"></a>

#### PowerShell
Copy the URL of your function from just above the code bit on the Develop tab - you'll need this in a second. 

Grab the [PS1 file](https://github.com/jakkaj/UptimePinger/blob/master/pingsource/pinger.ps1) and copy it somewhere on your machine (or just run it from you GitHub checkout place).

Next create a new Scheduler Job - from Start Menu search for *Task Scheduler*.

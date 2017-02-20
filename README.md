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

- Enter a name for your new storage account (I called mine internetuptime - all lower case!).

- Create a new Resource Group called *intetnetmonitoring* which will allow you to keep all your bits for this project in the same place. 

- Once that is created you should be able to install [StorageExplorer](http://storageexplorer.com/) and browse to it. This will be a good way to debug your service later. 
 
#### Azure Functions
If you know about functions, grab the function code from [here](https://github.com/jakkaj/UptimePinger/tree/master/functions) and set up up. 
There are a couple of inputs and outputs to think about and some project.json files to work with for Nugets FYI.

<a href="#postfunctions">I know about functions, skip me to the next bit</a>

Next you need to set up the Azure Functions. These provide an end point for your pings as well as background processing that does the actual up/down calculation and sends data to Application Insights. 

- They are super easy to get going - [click here](https://ms.portal.azure.com/#create/Microsoft.FunctionApp) to get started. 

Select consumtion plan if you're not sure what to select there. 
Use your new Resource Group called intetnetmonitoring so you can group all the services for this project in the one place. 

![img1](https://cloud.githubusercontent.com/assets/5225782/23111786/30a59d78-f77f-11e6-8376-4ace8e709803.PNG)


- Next go in to the editor so you can start editing your new functions. If you can't locate it, look under the [App Services](https://ms.portal.azure.com/#blade/HubsExtension/Resources/resourceType/Microsoft.Web%2Fsites) section. 

- Add a new function called *InternetUpLogger*. 

- Filter by C# and API & WebHooks then select the HttpTrigger-CSharp option. Enter the *InternetUpLogger* and click Create. 

This will create a new function that will act as a web endpoint that you can call. 


[![Create the Function](http://img.youtube.com/vi/Yn9P7hEWVVE/0.jpg)](http://www.youtube.com/watch?v=Yn9P7hEWVVE "Create the function")
*YouTube*

You will see a place to drop some code. This is a basic azure function. 

Before you can edit the code you need to add some outputs. 

Azure Functions can look after some inputs and outputs for you - so you don't have to write a lot of code and config to say read from a database and write to a table. 

- Click on Integrate, then click *New Output*. Select Azure Table Storage from the list and click *Select*.

Next you'll need to set up the connection to your table storage if you've not done so already in this Function. 

- Click *New* next to Storage account aonnection and select the account from the list. 

You may want to change the output table name here to something like pingTable. You will need to remember this for later when we take this new table as input in another function. 

- Once that is completed, click *Save*. 


You can expand the documentation to see some examples of how to use the new output.

[![Add The Output](http://img.youtube.com/vi/cjB6Y8BZwJg/0.jpg)](http://www.youtube.com/watch?v=cjB6Y8BZwJg "Add the output")
*YouTube*

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

You cannot sort on Azure Table rows by default using LINQ etc. RowKey are auto ordered in descending format. So we make sure that the newer the row, the higher the number is by subtracting from DateTime.MaxValue. This will be handy later when we want to get out the latest pings to analyse recent data.

Once that is done we pop it in to the ICollector which will go and add it to the table for us! Too easy!

<a name="postfunctions"></a>

#### PowerShell

The next step is to set up the PowerShell script to call the function on a scheduler. 

- Copy the URL of your function from just above the code bit on the Develop tab - you'll need this in a second. 

- Grab the [PS1 file](https://github.com/jakkaj/UptimePinger/blob/master/pingsource/pinger.ps1) and copy it somewhere on your machine (or just run it from you GitHub checkout place).

- Edit it to insert your function URL in to the indicated spot. 

![Ping PowerShell Script](https://cloud.githubusercontent.com/assets/5225782/23115206/88e01cbc-f797-11e6-9aea-3993c6793b4d.JPG)

- Jump in to PowerShell and try it out (hint, go to the directory and type PowerShell in the explorer bar at the top).

```
.\pinger.ps1
```

Make sure that it prints out something saying that it worked :P

- Next create a new Scheduler Job - from Start Menu search for *Task Scheduler*.

[![Add The Task](http://img.youtube.com/vi/zkf4gDA1hEk/0.jpg)](http://www.youtube.com/watch?v=zkf4gDA1hEk "Add the task")

*YouTube*

- For the trigger, select any start time (in the past works best) and then have it repeat every 1 minute. 

- For the action, have it call powershell.exe and pass in the argument -ExecutionPolicy Bypass <full path to ps1 file>

Now you can check if it's working by going back in to the Azure Function and watching the log. Also, you can check that your table was created by exploring with [Azure Storage Explorer](http://storageexplorer.com/).

#### Background task to process the data

In order to know if we're up or down, then do stuff based on that we need something to process our data. 

Azure Functions can be called via HTTP (as we are above) - but they can also be called in many other ways - including on a schedule. 

- Create a new function called *InternetUpProcessor* that is a TimerTrigger-CSharp.

[![Create the processor function](http://img.youtube.com/vi/CHgwn-T8v1Q/0.jpg)](http://www.youtube.com/watch?v=CHgwn-T8v1Q "Create the processor function")

- Set the cron expression to one minute:

```
0 */1 * * * *
``` 

- You'll also need to pass the table that you created as the output in the first function to the input of this function. In the YouTube video I called it *outTable*, but you may have renamed it to *pingTable* or something. 

- Next you need to add another **different** output table to store the actual up-time/down-time results. 

- Create a new output to an Azure Table called uptimeTable. This will be passed in to the function. 

- At the same time you'll need to create another table input that **also** points to uptimeTable... this is so we can check it to see if the system was already down or not and do extra processing. 

[![Create uptime inputs and outputs](http://img.youtube.com/vi/qd2PrqLhTwg/0.jpg)](http://www.youtube.com/watch?v=qd2PrqLhTwg "Create uptime inputs and outputs")

*YouTube*
- Now you can copy in the code for the function from [here](https://github.com/jakkaj/UptimePinger/blob/master/functions/InternetUpProcessor/run.csx).

You may note that the function is not building. That's becuse it uses some Nuget packages that are not available by default. 

To add nuget packages you first need to add a new file to your function called project.json.

[![Add nugets](http://img.youtube.com/vi/pAMgNigokec/0.jpg)](http://www.youtube.com/watch?v=pAMgNigokec "Add nugets")

*YouTube*

- Click on *View Files* and add a new file called project.json. Paste in the content from [here](https://github.com/jakkaj/UptimePinger/blob/master/functions/InternetUpProcessor/project.json) and save. 
- You should see packages restoring when you view the log. 

#### Application Insights

Next we need to create a new Application Insights app in Azure. 

- Click [here](https://ms.portal.azure.com/#create/Microsoft.AppInsights) to create a new Application Insights app. 
- Leave the values default and choose the resource group you used for your other things. 
- Once you has been created you can collect you instrumentation key from the properties tab on the new Application Insights resource. 
- Paste that key in to the indicated spot in the function code. 

![aiproperties](https://cloud.githubusercontent.com/assets/5225782/23118772/6dae0992-f7a9-11e6-87a6-56868e09daab.JPG)

Once you're recieving telemtry you can do a couple of searches in the Application Insights interface to visualise your internet connection stability. 

![graphs](https://cloud.githubusercontent.com/assets/5225782/23119471/7e1eb396-f7ac-11e6-8712-93e6605b4635.JPG)

I went in and added a couple of metric graphs. 

- Click on Metrics Explorer. If there is not already a graph to edit, click to add one. 

I added two. 

![downtime](https://cloud.githubusercontent.com/assets/5225782/23119556/d9cced2a-f7ac-11e6-877d-a66cdc0f8e9e.JPG)

This graph shows downtime in minutes. So you can see over time how many minutes your system is out. 

![metricstate](https://cloud.githubusercontent.com/assets/5225782/23119580/f0180cae-f7ac-11e6-86a0-31b65c3ca9c9.JPG)

This one is the state (1 or 0) of the connection over time. Somtimes it will show as in between 1 and 0 - this is the average of the state during the measurement window. 

If you want to see actual downtime events you can add a new search. 

- Click on Search from the Overview panel of Application Insights. 

![state filter](https://cloud.githubusercontent.com/assets/5225782/23119671/43fb8a1c-f7ad-11e6-8756-63c74797f019.JPG)

- Click on filters and search for state. Select false. 

This will filter down to events that have a state of false... i.e. INTERNET DOWN. You could also look for the *InternetDown* event which will show the times when the internet went down as opposed to the timeranges it was down. 

![outputinaction](https://cloud.githubusercontent.com/assets/5225782/23119798/c4592c14-f7ad-11e6-88a8-2bcdd5cb1b8c.JPG)

This isn't that the internet went down 96 times, it's that it was down during 96 sampling periods. The *InternetDown* event shows the amount of times it went down. 

That's prety much it! You're done. 

#### Extra Credit - SpeedTest

I added a speed test using this same project for s&g's. 

- There is another function [here](https://github.com/jakkaj/UptimePinger/tree/master/functions/InternetUpSpeedTest) that you can install. 

- Then grab the code from [here](https://github.com/jakkaj/NSpeedTest). 
- Edit Upload.cs and paste in your new Speedtest function url. 
- Build it and create a new Scheduled Task for every 15 mins (or what ever). 
- In Application Insights metrics explorer, add a new graph of UploadSpeed_MachineName and DownloadSpeed_MachineName (same graph, they can overlay). 

#### Extra Credit - Push

I've set my system up to create pushes. 

I did this by creating a new maker url call back channel on IFTTT which passes through the value to a push notification. This then sends the push to the IFTTT app on my phone without me needing to write an app just to recive a push. 

It's outside the scope of this article to go though that, but you can see the remenants of it in the *InternetUptimeProcessor* funcion. 

![pushsetting](https://cloud.githubusercontent.com/assets/5225782/23120119/392d1efa-f7af-11e6-889e-4b44f8434f7f.JPG)

If you get stuck, ping me - I'd be happy to expand this article to include it later. 



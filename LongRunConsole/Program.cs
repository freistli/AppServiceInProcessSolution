// See https://aka.ms/new-console-template for more information
using Serilog;
using Serilog.Core;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;
using Windows.Storage;


[DllImport("kernel32.dll")]
static extern uint GetCurrentThreadId();

StorageFolder storageFolder = Windows.Storage.ApplicationData.Current.LocalFolder;

StorageFile storageFile = storageFolder.CreateFileAsync("temp.txt", CreationCollisionOption.ReplaceExisting).AsTask().Result;

string dpath = Path.GetDirectoryName(storageFile.Path);

string logFilePath = dpath + "\\longrunlog.txt";
Logger logger = new LoggerConfiguration()
    .WriteTo.File(logFilePath)
    .CreateLogger();

AppServiceConnection inProcessService = new AppServiceConnection();
inProcessService.ServiceClosed += AppServiceConnection_ServiceClosed;

void AppServiceConnection_ServiceClosed(AppServiceConnection sender, AppServiceClosedEventArgs args)
{
    logger.Information($"AppService is Closed {Enum.GetName(typeof(AppServiceClosedStatus), args.Status)}");
    Environment.Exit(0);
}

// Here, we use the app service name defined in the app service 
// provider's Package.appxmanifest file in the <Extension> section.
inProcessService.AppServiceName = "InProcessAppService2";

// Use Windows.ApplicationModel.Package.Current.Id.FamilyName 
// within the app service provider to get this value.
inProcessService.PackageFamilyName = "AppServiceInProcessDemo.Pack_2dhr6hz02r3tt";

var status = await inProcessService.OpenAsync();

if (status != AppServiceConnectionStatus.Success)
{
    logger.Error($"Failed to connect {inProcessService.AppServiceName}");
}

var message = new ValueSet();
message.Add("Request", "LongRun");
var response = await inProcessService.SendMessageAsync(message);
string result = "";

if (response.Status == AppServiceResponseStatus.Success)
{
    // Get the data  that the service sent to us.
    if (response.Message["Result"] as string == "OK")
    {
        result = response.Message["Response"] as string;
    }
    logger.Information(result);  
}


System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
{
    FileName = dpath,
    UseShellExecute = true,
    Verb = "open"
});

while (true)
{
    logger.Information(Process.GetCurrentProcess().Id + " " + GetCurrentThreadId() + " " + Thread.CurrentThread.ManagedThreadId + " Longrun is alive");
    await Task.Delay(5000);
}

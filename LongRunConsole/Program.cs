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
    .WriteTo.Console()
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


try
{
    //var semaphore = Semaphore.OpenExisting("appservicemain");

    var ewh = EventWaitHandle.OpenExisting("eventwaithandle_1234");
    ewh.Set();
    logger.Information(Process.GetCurrentProcess().Id + " " + GetCurrentThreadId() + " " + Thread.CurrentThread.ManagedThreadId + " opened");
}
catch (Exception ex)
{
    logger.Information(Process.GetCurrentProcess().Id + " " + GetCurrentThreadId() + " " + Thread.CurrentThread.ManagedThreadId + " eventwaithandle_1234 " + ex.ToString());
}


string peerSemaphore = @"AppContainerNamedObjects\S-1-15-2-3989185819-1529894802-462717500-670407784-4191515574-2726099911-3004833844\appservicemain";
try
{
    var semaphore = Semaphore.OpenExisting(peerSemaphore);

    logger.Information(Process.GetCurrentProcess().Id + " " + GetCurrentThreadId() + " " + Thread.CurrentThread.ManagedThreadId + $" {peerSemaphore} semaphore is opened");

    /*
    semaphore.Release();
    logger.Information("semaphore is released 1");
    semaphore.Release();
    logger.Information("semaphore is released 2");
    semaphore.Release();
    logger.Information("semaphore is released 3");
    */
 
}
catch (Exception ex)
{
    logger.Information(Process.GetCurrentProcess().Id + " " + GetCurrentThreadId() + " " + Thread.CurrentThread.ManagedThreadId + " " + ex.ToString());
}

logger.Information(System.Security.Principal.WindowsIdentity.GetCurrent().Name);
logger.Information(System.Security.Principal.WindowsIdentity.GetCurrent().User.Value);

System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
{
    FileName = dpath,
    UseShellExecute = true,
    Verb = "open"
});


while (true)
{
    logger.Information(Process.GetCurrentProcess().Id + " " + GetCurrentThreadId() + " " + Thread.CurrentThread.ManagedThreadId + " Longrun is alive");
    await Task.Delay(50000);
}

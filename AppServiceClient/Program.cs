// See https://aka.ms/new-console-template for more information
using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;

AppServiceConnection inProcessService = new AppServiceConnection();

// Here, we use the app service name defined in the app service 
// provider's Package.appxmanifest file in the <Extension> section.
inProcessService.AppServiceName = "InProcessAppService2";

// Use Windows.ApplicationModel.Package.Current.Id.FamilyName 
// within the app service provider to get this value.
inProcessService.PackageFamilyName = "AppServiceInProcessDemo.Pack_2dhr6hz02r3tt";

var status = await inProcessService.OpenAsync();

if (status != AppServiceConnectionStatus.Success)
{
    Console.Write("Failed to connect");

}

var message = new ValueSet();
message.Add("Request", "Start");
var response = await inProcessService.SendMessageAsync(message);
var result = "";

if (response.Status == AppServiceResponseStatus.Success)
{
    // Get the data  that the service sent to us.
    if (response.Message["Result"] as string == "OK")
    {
        result = response.Message["Response"] as string;
    }
}


Console.WriteLine(result);


await Task.Delay(5000);

string peerSemaphore = @"AppContainerNamedObjects\S-1-15-2-3989185819-1529894802-462717500-670407784-4191515574-2726099911-3004833844\appservicemain";
try
{
    var semaphore = Semaphore.OpenExisting(peerSemaphore);
    semaphore.Release();     
    Console.WriteLine("semaphore is released"); 
}
catch (Exception ex)
{
    Console.WriteLine(ex.ToString());
}

await Task.Delay(100000);
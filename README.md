# AppServiceInProcessSolution

##Environment
VS2022, .Net 6, Win11

### AppServiceInProcess
A UWP in process AppService. It receives messages from two clients indeed, one is from AppServiceClient, another is from LongRunConsole. 

It can check the appservice connection status
with LongRunConsole, to make sure LongRunConsole running always if need.
### LongRunConsole 
Background no-UI process, is called and monitored by AppServiceInProcess
### ApPServiceInProcess.Pack 
packages AppServiceInProcess, LongRunConsole.
### AppServiceClient 
Console Appm, is used to launch AppServiceInProcess.

### NOTE
This is for demo purpse only. Developer should properly handle system resources used by LongRunConsole based on the real situation.

Should not put heavy tasks in LongRunConsole role in real situaion. 

Should be able to arrange its executing status if system power status gets changed.

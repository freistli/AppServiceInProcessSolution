# AppServiceInProcessSolution

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

NOTE: This is for demo purpse only. Developer should properly handle system resources used by LongRunConsole based on the real situation.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.Background;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;
using System.Runtime.InteropServices;
using Windows.Storage;
using Serilog;
using Serilog.Core;

namespace AppServiceInProcess
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {

        [DllImport("kernel32.dll")]
        static extern uint GetCurrentThreadId();

        private AppServiceConnection _appServiceConnection;
        private BackgroundTaskDeferral _appServiceDeferral;
        private Logger logger;
        private Dictionary<IBackgroundTaskInstance, BackgroundTaskDeferral> _taskInstances = new Dictionary<IBackgroundTaskInstance, BackgroundTaskDeferral>();
        private AppServiceConnection _longRunAppServiceConnection;
        private Semaphore semaphore;
        private EventWaitHandle ewh;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            this.Suspending += OnSuspending;
            this.UnhandledException += App_UnhandledException;

            StorageFolder storageFolder = Windows.Storage.ApplicationData.Current.LocalFolder;

            StorageFile storageFile = storageFolder.CreateFileAsync("temp.txt", CreationCollisionOption.ReplaceExisting).AsTask().Result;

            string dpath = Path.GetDirectoryName(storageFile.Path);

            string logFilePath = dpath + "\\log.txt";
            logger = new LoggerConfiguration()
                .WriteTo.File(logFilePath)
                .CreateLogger();

            Windows.ApplicationModel.FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppAsync();

        }

        private void App_UnhandledException(object sender, Windows.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            logger.Error(e.ToString());
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            Frame rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: Load state from previously suspended application
                }

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;

            }

            if (e.PrelaunchActivated == false)
            {
                if (rootFrame.Content == null)
                {
                    // When the navigation stack isn't restored navigate to the first page,
                    // configuring the new page by passing required information as a navigation
                    // parameter
                    rootFrame.Navigate(typeof(MainPage), e.Arguments);
                }
                // Ensure the current window is active
                Window.Current.Activate();
            }
        }

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            //TODO: Save application state and stop any background activity
            deferral.Complete();
        }
        void TestEventWaitHandle()
        {
            try
            {
                bool result = EventWaitHandle.TryOpenExisting("eventwaithandle_1234", out ewh);
                if (!result)
                {
                    ewh = new EventWaitHandle(false, EventResetMode.AutoReset, "eventwaithandle_1234");
                    logger.Information(Process.GetCurrentProcess().Id + " " + GetCurrentThreadId() + " " + Thread.CurrentThread.ManagedThreadId + " eventwaithandle_1234 cannot be opened, create a new");
                    ewh.Set();
                }
                else
                {
                    logger.Information(Process.GetCurrentProcess().Id + " " + GetCurrentThreadId() + " " + Thread.CurrentThread.ManagedThreadId + " eventwaithandle_1234 is opened");
                    ewh.Set();
                }
            }
            catch (Exception ex)
            {
                logger.Information(Process.GetCurrentProcess().Id + " " + GetCurrentThreadId() + " " + Thread.CurrentThread.ManagedThreadId + ex.ToString());
            }
        }
        void TestSemaphore()
        {
            try
            {
                bool result = Semaphore.TryOpenExisting("appservicemain", out semaphore);
                if (!result)
                {
                    logger.Information(Process.GetCurrentProcess().Id + " " + GetCurrentThreadId() + " " + Thread.CurrentThread.ManagedThreadId + " appservicemain semaphore cannot be opened, create a new");
                    semaphore = new Semaphore(0, 1, "appservicemain");

                }
                else
                {
                    logger.Information(Process.GetCurrentProcess().Id + " " + GetCurrentThreadId() + " " + Thread.CurrentThread.ManagedThreadId + " appservicemain semaphore is opened");
                }
                logger.Information(Process.GetCurrentProcess().Id + " " + GetCurrentThreadId() + " " + Thread.CurrentThread.ManagedThreadId + " Semahpore is waiting");
                semaphore.WaitOne();

            }
            catch (Exception ex)
            {
                logger.Information(Process.GetCurrentProcess().Id + " " + GetCurrentThreadId() + " " + Thread.CurrentThread.ManagedThreadId + ex.ToString());
            }
        }
        protected override void OnBackgroundActivated(BackgroundActivatedEventArgs args)
        {
            base.OnBackgroundActivated(args);
            IBackgroundTaskInstance taskInstance = args.TaskInstance;
            AppServiceTriggerDetails appService = taskInstance.TriggerDetails as AppServiceTriggerDetails;
            _appServiceDeferral = taskInstance.GetDeferral();
            taskInstance.Canceled += OnAppServicesCanceled;
            _appServiceConnection = appService.AppServiceConnection;
            _appServiceConnection.RequestReceived += OnAppServiceRequestReceived;
            _appServiceConnection.ServiceClosed += AppServiceConnection_ServiceClosed;

            _taskInstances.Add(taskInstance, _appServiceDeferral);
        }

        private async void OnAppServiceRequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            AppServiceDeferral messageDeferral = args.GetDeferral();
            ValueSet message = args.Request.Message;
            string text = message["Request"] as string;

            if ("Start" == text)
            {
                logger.Information(Process.GetCurrentProcess().Id + " " + GetCurrentThreadId() + " " + Thread.CurrentThread.ManagedThreadId + " Service received Start message");
                ValueSet returnMessage = new ValueSet();
                returnMessage.Add("Result", "OK");
                returnMessage.Add("Response", "True");
                await args.Request.SendResponseAsync(returnMessage);
                 
                TestEventWaitHandle();
            }

            if ("LongRun" == text)
            {
                logger.Information(Process.GetCurrentProcess().Id + " " + GetCurrentThreadId() + " " + Thread.CurrentThread.ManagedThreadId + " Longrun Instance started and got its message");
                ValueSet returnMessage = new ValueSet();
                returnMessage.Add("Result", "OK");
                returnMessage.Add("Response", "Service knows LongRun instance is started");
                await args.Request.SendResponseAsync(returnMessage);
                _longRunAppServiceConnection = sender;

                TestSemaphore();

                logger.Information(Process.GetCurrentProcess().Id + " " + GetCurrentThreadId() + " " + Thread.CurrentThread.ManagedThreadId + " Semahpore is released");

            }

            try
            {
                Task.Run(async () =>
                {
                    while (true)
                    {
                       
                        await Task.Delay(50000);
                        logger.Information(Process.GetCurrentProcess().Id + " " + GetCurrentThreadId() + " " + Thread.CurrentThread.ManagedThreadId + " Service is alive");
                    }
                });
            }
            catch (Exception ex)
            {
                logger.Information(ex.ToString());
            }
            messageDeferral.Complete();
        }

        private void OnAppServicesCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            logger?.Information($"{Process.GetCurrentProcess().Id} {GetCurrentThreadId()} An appService Canceled with reason {Enum.GetName(typeof(BackgroundTaskCancellationReason), reason)}");
            //_appServiceDeferral.Complete();
            _taskInstances[sender].Complete();

            var appService = sender.TriggerDetails as AppServiceTriggerDetails;
            if (appService.AppServiceConnection == _longRunAppServiceConnection)
            {

                //logger?.Information($"The LongRun AppService is Canceled, restart it");
                //Windows.ApplicationModel.FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppAsync();

                logger?.Information($"The LongRun AppService is Canceled, stop this main UWP as well");
                Environment.Exit(0);
            }
            else
            {
                logger?.Information($"The canceled AppService is not from LongRun, take no action");
            }
        }

        private void AppServiceConnection_ServiceClosed(AppServiceConnection sender, AppServiceClosedEventArgs args)
        {
            //_appServiceDeferral.Complete();

            foreach(var item in _taskInstances)
            {
                var appService = item.Key.TriggerDetails as AppServiceTriggerDetails;
                if (appService.AppServiceConnection == sender)
                {
                    logger.Information($"AppService is Closed {Enum.GetName(typeof(AppServiceClosedStatus), args.Status)}");
                    item.Value.Complete();
                }
            }

            if (sender == _longRunAppServiceConnection)
            {
                logger.Information($"LongRun AppService is Closed, restart it");
                Windows.ApplicationModel.FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppAsync();
            }
        }
    }
}


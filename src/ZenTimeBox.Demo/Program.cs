namespace ZenTimeBox.Demo;

static class Program
{
    private const string SingleInstanceMutexName = @"Local\ZenTimeBox.Demo";

    [STAThread]
    static void Main()
    {
        ImeSuppressor.DisableForCurrentThread();
        ApplicationConfiguration.Initialize();
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, args) => AppDiagnostics.LogException(args.Exception, "WinForms thread exception");
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            if (args.ExceptionObject is Exception exception)
            {
                AppDiagnostics.LogException(exception, "AppDomain unhandled exception");
            }
        };

        using Mutex singleInstanceMutex = new(initiallyOwned: true, SingleInstanceMutexName, out bool createdNew);
        if (!createdNew)
        {
            AppDiagnostics.LogInfo("Second instance detected; exiting.");
            return;
        }

        try
        {
            AppDiagnostics.LogInfo("ZenTimeBox starting.");
            Application.Run(new TrayApplicationContext());
            AppDiagnostics.LogInfo("ZenTimeBox stopped normally.");
        }
        catch (Exception exception)
        {
            AppDiagnostics.LogException(exception, "Application.Run failed");
            throw;
        }
    }
}

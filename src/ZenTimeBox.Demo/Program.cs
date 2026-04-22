namespace ZenTimeBox.Demo;

static class Program
{
    private const string SingleInstanceMutexName = @"Local\ZenTimeBox.Demo";

    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        using Mutex singleInstanceMutex = new(initiallyOwned: true, SingleInstanceMutexName, out bool createdNew);
        if (!createdNew)
        {
            return;
        }

        Application.Run(new TrayApplicationContext());
    }
}

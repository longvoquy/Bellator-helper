using System.Globalization;
using BHelper.App.Tray;
using BHelper.App.Utils;

namespace BHelper;

static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        SetDefaultCulture();

        var debugMode = args.Contains("--debug", StringComparer.OrdinalIgnoreCase);

        if (debugMode)
        {
            ApplicationConfiguration.Initialize();
            var path = HardwareDebugger.DumpAll();
            if (Environment.UserInteractive)
            {
                MessageBox.Show(
                    $"Hardware dump saved to:\n{path}",
                    $"{AppBranding.ShortName} Debug",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }

            return;
        }

        using var mutex = new Mutex(true, AppBranding.AppId, out bool isNew);
        if (!isNew)
        {
            if (Environment.UserInteractive)
            {
                MessageBox.Show(
                    $"{AppBranding.ShortName} is already running in the system tray.",
                    AppBranding.ShortName,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }

            return;
        }

        ApplicationConfiguration.Initialize();
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new TrayApp());
    }

    private static void SetDefaultCulture()
    {
        var culture = new CultureInfo("en-US");
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        Thread.CurrentThread.CurrentCulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;
    }
}

using LecooHelper.App.Tray;
using LecooHelper.App.Utils;

namespace LecooHelper;

static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        var debugMode = args.Contains("--debug", StringComparer.OrdinalIgnoreCase);

        if (debugMode)
        {
            ApplicationConfiguration.Initialize();
            var path = HardwareDebugger.DumpAll();
            if (Environment.UserInteractive)
            {
                MessageBox.Show(
                    $"Hardware dump saved to:\n{path}",
                    "LecooHelper Debug",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }

            return;
        }

        using var mutex = new Mutex(true, "LecooHelper", out bool isNew);
        if (!isNew)
            return;

        ApplicationConfiguration.Initialize();
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new TrayApp());
    }
}

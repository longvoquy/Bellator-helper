using System.Diagnostics;
using System.Security;
using System.Text;
using Microsoft.Win32;

namespace BHelper.App.Utils;

public static class StartupHelper
{
    private const string TaskName = AppBranding.AppId;
    private const string RegistryRunKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

    public static bool Enable()
    {
        var exePath = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(exePath))
            return false;

        RemoveLegacyRegistryEntry();

        var userId = $"{Environment.UserDomainName}\\{Environment.UserName}";
        var command = SecurityElement.Escape(exePath);
        var workingDir = SecurityElement.Escape(Path.GetDirectoryName(exePath) ?? exePath);

        var xml = $"""
            <?xml version="1.0" encoding="UTF-16"?>
            <Task version="1.2" xmlns="http://schemas.microsoft.com/windows/2004/02/mit/task">
              <Triggers>
                <LogonTrigger>
                  <Enabled>true</Enabled>
                </LogonTrigger>
              </Triggers>
              <Principals>
                <Principal id="Author">
                  <UserId>{SecurityElement.Escape(userId)}</UserId>
                  <LogonType>InteractiveToken</LogonType>
                  <RunLevel>HighestAvailable</RunLevel>
                </Principal>
              </Principals>
              <Settings>
                <MultipleInstancesPolicy>IgnoreNew</MultipleInstancesPolicy>
                <ExecutionTimeLimit>PT0S</ExecutionTimeLimit>
                <DisallowStartIfOnBatteries>false</DisallowStartIfOnBatteries>
                <StopIfGoingOnBatteries>false</StopIfGoingOnBatteries>
              </Settings>
              <Actions Context="Author">
                <Exec>
                  <Command>{command}</Command>
                  <WorkingDirectory>{workingDir}</WorkingDirectory>
                </Exec>
              </Actions>
            </Task>
            """;

        var tmpXml = Path.Combine(Path.GetTempPath(), "bhelper_task.xml");
        try
        {
            File.WriteAllText(tmpXml, xml, Encoding.Unicode);
            return RunSchtasks($"/create /tn \"{TaskName}\" /xml \"{tmpXml}\" /f");
        }
        finally
        {
            try { File.Delete(tmpXml); } catch { /* ignore */ }
        }
    }

    public static bool Disable()
    {
        RemoveLegacyRegistryEntry();
        if (!IsEnabled())
            return true;

        return RunSchtasks($"/delete /tn \"{TaskName}\" /f");
    }

    public static bool IsEnabled() =>
        RunSchtasks($"/query /tn \"{TaskName}\"");

    private static void RemoveLegacyRegistryEntry()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryRunKey, writable: true);
            key?.DeleteValue(TaskName, throwOnMissingValue: false);
        }
        catch
        {
            // ignore
        }
    }

    private static bool RunSchtasks(string args)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "schtasks.exe",
            Arguments = args,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            UseShellExecute = false
        };

        using var p = Process.Start(psi);
        if (p is null)
            return false;

        p.WaitForExit();
        return p.ExitCode == 0;
    }
}

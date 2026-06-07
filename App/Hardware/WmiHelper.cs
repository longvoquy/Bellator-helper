using System.Management;

namespace BHelper.App.Hardware;

internal static class WmiHelper
{
    public static bool TryExecute(string queryKey, Func<bool> action)
    {
        if (IsDisabled(queryKey))
            return false;

        try
        {
            return action();
        }
        catch (ManagementException)
        {
            MarkDisabled(queryKey);
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            MarkDisabled(queryKey);
            return false;
        }
        catch (Exception ex) when (ex is System.Runtime.InteropServices.COMException or PlatformNotSupportedException)
        {
            MarkDisabled(queryKey);
            return false;
        }
    }

    private static readonly HashSet<string> DisabledQueries = new(StringComparer.OrdinalIgnoreCase);

    private static bool IsDisabled(string queryKey) => DisabledQueries.Contains(queryKey);

    private static void MarkDisabled(string queryKey) => DisabledQueries.Add(queryKey);
}

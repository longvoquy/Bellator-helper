using System.Management;

namespace BHelper.App.Power;

internal enum BellatorSystemPerMode : byte
{
    BalanceMode = 0,
    PerformanceMode = 1,
    QuietMode = 2,
    FullspeedMode = 3
}

internal static class BellatorWmiClient
{
    private const byte MethodGet = 250;
    private const byte MethodSet = 251;
    private const byte SystemPerModeId = 8;
    private const byte CpuGpuSysFanSpeedId = 13;
    private const byte MaxFanSpeedSwitchId = 20;
    private const byte MaxFanSpeedId = 21;
    private const string WmiPath = @"root\WMI:MICommonInterface.InstanceName='ACPI\PNP0C14\MIFS_0'";

    public static bool TryGetSystemPerMode(out BellatorSystemPerMode mode)
    {
        mode = BellatorSystemPerMode.BalanceMode;
        if (!TryInvoke(BuildRequest(MethodGet, SystemPerModeId), out var response))
            return false;

        mode = (BellatorSystemPerMode)response[4];
        return IsKnownMode(mode);
    }

    public static bool TrySetSystemPerMode(BellatorSystemPerMode mode)
    {
        if (!IsKnownMode(mode))
            return false;

        var request = BuildRequest(MethodSet, SystemPerModeId);
        request[4] = (byte)mode;
        return TryInvoke(request, out _);
    }

    public static bool TryGetFanSpeeds(out int cpuFanRpm, out int gpuFanRpm, out int sysFanRpm)
    {
        cpuFanRpm = 0;
        gpuFanRpm = 0;
        sysFanRpm = 0;

        if (!TryInvoke(BuildRequest(MethodGet, CpuGpuSysFanSpeedId), out var response) || response.Length < 12)
            return false;

        cpuFanRpm = (response[5] << 8) + response[4];
        gpuFanRpm = (response[7] << 8) + response[6];
        sysFanRpm = (response[11] << 8) + response[10];
        return true;
    }

    // fanType: 0 = CPU/GPU fan, 1 = system fan. Turns the manual max-RPM cap on or off.
    public static bool TrySetMaxFanSwitch(byte fanType, bool enabled) =>
        TrySetWithParams(MaxFanSpeedSwitchId, fanType, enabled ? (byte)1 : (byte)0);

    // The value is the RPM cap in hundreds of RPM (24 = 2400 RPM).
    public static bool TrySetMaxFanSpeed(byte fanType, byte value) =>
        TrySetWithParams(MaxFanSpeedId, fanType, value);

    private static bool TrySetWithParams(byte methodName, byte param0, byte param1)
    {
        var request = BuildRequest(MethodSet, methodName);
        request[4] = param0;
        request[5] = param1;
        return TryInvoke(request, out _);
    }

    public static PowerModeKind ToPowerModeKind(BellatorSystemPerMode mode) => mode switch
    {
        BellatorSystemPerMode.QuietMode => PowerModeKind.Silent,
        BellatorSystemPerMode.BalanceMode => PowerModeKind.Balanced,
        BellatorSystemPerMode.PerformanceMode => PowerModeKind.Beast,
        BellatorSystemPerMode.FullspeedMode => PowerModeKind.Battle,
        _ => PowerModeKind.Balanced
    };

    public static BellatorSystemPerMode FromPowerModeKind(PowerModeKind mode) => mode switch
    {
        PowerModeKind.Silent => BellatorSystemPerMode.QuietMode,
        PowerModeKind.Balanced => BellatorSystemPerMode.BalanceMode,
        PowerModeKind.Beast => BellatorSystemPerMode.PerformanceMode,
        PowerModeKind.Battle => BellatorSystemPerMode.FullspeedMode,
        _ => BellatorSystemPerMode.BalanceMode
    };

    private static bool IsKnownMode(BellatorSystemPerMode mode) =>
        mode is BellatorSystemPerMode.BalanceMode
            or BellatorSystemPerMode.PerformanceMode
            or BellatorSystemPerMode.QuietMode
            or BellatorSystemPerMode.FullspeedMode;

    private static byte[] BuildRequest(byte methodType, byte methodName)
    {
        var request = new byte[32];
        request[1] = methodType;
        request[3] = methodName;
        return request;
    }

    private static bool TryInvoke(byte[] request, out byte[] response)
    {
        response = [];
        if (request.Length != 32)
            return false;

        try
        {
            using var managementObject = new ManagementObject(WmiPath);
            using var inParams = managementObject.GetMethodParameters("MiInterface");
            inParams["InData"] = request;
            using var outParams = managementObject.InvokeMethod("MiInterface", inParams, null);
            if (outParams?["OutData"] is not byte[] outData || outData.Length < 5)
                return false;

            response = outData;
            return true;
        }
        catch (ManagementException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
        catch (Exception ex) when (ex is System.Runtime.InteropServices.COMException or PlatformNotSupportedException)
        {
            return false;
        }
    }
}

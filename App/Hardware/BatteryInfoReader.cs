using System.Runtime.InteropServices;

namespace BHelper.App.Hardware;

// Reads static battery data (designed / full capacity, cycle count) from the Windows battery driver
// through IOCTL_BATTERY_QUERY_INFORMATION. The root\WMI battery classes fail on some laptops.
internal static class BatteryInfoReader
{
    private const uint DigcfPresent = 0x2;
    private const uint DigcfDeviceInterface = 0x10;
    private const uint GenericRead = 0x80000000;
    private const uint GenericWrite = 0x40000000;
    private const uint FileShareRead = 1;
    private const uint FileShareWrite = 2;
    private const uint OpenExisting = 3;
    private const uint IoctlBatteryQueryTag = 0x294040;
    private const uint IoctlBatteryQueryInformation = 0x294044;

    private static readonly Guid GuidDevClassBattery = new("72631E54-78A4-11D0-BCF7-00AA00B7B32A");
    private static readonly IntPtr InvalidHandle = new(-1);

    public readonly record struct BatteryStaticInfo(int DesignedCapacityMwh, int FullChargedCapacityMwh, int CycleCount);

    public static bool TryRead(out BatteryStaticInfo info)
    {
        info = default;

        var guid = GuidDevClassBattery;
        var deviceInfoSet = SetupDiGetClassDevs(ref guid, IntPtr.Zero, IntPtr.Zero, DigcfPresent | DigcfDeviceInterface);
        if (deviceInfoSet == InvalidHandle)
            return false;

        try
        {
            var interfaceData = new SpDeviceInterfaceData { CbSize = Marshal.SizeOf<SpDeviceInterfaceData>() };
            if (!SetupDiEnumDeviceInterfaces(deviceInfoSet, IntPtr.Zero, ref guid, 0, ref interfaceData))
                return false;

            var path = GetDevicePath(deviceInfoSet, ref interfaceData);
            return path is not null && TryQuery(path, out info);
        }
        finally
        {
            SetupDiDestroyDeviceInfoList(deviceInfoSet);
        }
    }

    private static string? GetDevicePath(IntPtr deviceInfoSet, ref SpDeviceInterfaceData interfaceData)
    {
        SetupDiGetDeviceInterfaceDetail(deviceInfoSet, ref interfaceData, IntPtr.Zero, 0, out var required, IntPtr.Zero);
        if (required <= 0)
            return null;

        var buffer = Marshal.AllocHGlobal(required);
        try
        {
            // SP_DEVICE_INTERFACE_DETAIL_DATA.cbSize is 8 on 64-bit and 6 on 32-bit processes.
            Marshal.WriteInt32(buffer, IntPtr.Size == 8 ? 8 : 6);
            if (!SetupDiGetDeviceInterfaceDetail(deviceInfoSet, ref interfaceData, buffer, required, out _, IntPtr.Zero))
                return null;

            return Marshal.PtrToStringUni(IntPtr.Add(buffer, 4));
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static bool TryQuery(string devicePath, out BatteryStaticInfo info)
    {
        info = default;

        var handle = CreateFile(devicePath, GenericRead | GenericWrite, FileShareRead | FileShareWrite,
            IntPtr.Zero, OpenExisting, 0, IntPtr.Zero);
        if (handle == InvalidHandle)
            return false;

        try
        {
            var wait = 0;
            if (!DeviceIoControl(handle, IoctlBatteryQueryTag, ref wait, sizeof(int), out var tag, sizeof(int), out _, IntPtr.Zero)
                || tag == 0)
                return false;

            var query = new BatteryQueryInformation { BatteryTag = tag, InformationLevel = 0, AtRate = 0 };
            if (!DeviceIoControl(handle, IoctlBatteryQueryInformation, ref query, Marshal.SizeOf<BatteryQueryInformation>(),
                    out BatteryInformation battery, Marshal.SizeOf<BatteryInformation>(), out _, IntPtr.Zero))
                return false;

            info = new BatteryStaticInfo((int)battery.DesignedCapacity, (int)battery.FullChargedCapacity, (int)battery.CycleCount);
            return true;
        }
        finally
        {
            CloseHandle(handle);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SpDeviceInterfaceData
    {
        public int CbSize;
        public Guid InterfaceClassGuid;
        public int Flags;
        public UIntPtr Reserved;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BatteryQueryInformation
    {
        public uint BatteryTag;
        public int InformationLevel;
        public int AtRate;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BatteryInformation
    {
        public uint Capabilities;
        public byte Technology;
        public byte Reserved0;
        public byte Reserved1;
        public byte Reserved2;
        public uint Chemistry;
        public uint DesignedCapacity;
        public uint FullChargedCapacity;
        public uint DefaultAlert1;
        public uint DefaultAlert2;
        public uint CriticalBias;
        public uint CycleCount;
    }

    [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr SetupDiGetClassDevs(ref Guid classGuid, IntPtr enumerator, IntPtr hwndParent, uint flags);

    [DllImport("setupapi.dll", SetLastError = true)]
    private static extern bool SetupDiEnumDeviceInterfaces(IntPtr deviceInfoSet, IntPtr deviceInfoData,
        ref Guid interfaceClassGuid, uint memberIndex, ref SpDeviceInterfaceData deviceInterfaceData);

    [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool SetupDiGetDeviceInterfaceDetail(IntPtr deviceInfoSet, ref SpDeviceInterfaceData deviceInterfaceData,
        IntPtr deviceInterfaceDetailData, int deviceInterfaceDetailDataSize, out int requiredSize, IntPtr deviceInfoData);

    [DllImport("setupapi.dll", SetLastError = true)]
    private static extern bool SetupDiDestroyDeviceInfoList(IntPtr deviceInfoSet);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr CreateFile(string fileName, uint desiredAccess, uint shareMode, IntPtr securityAttributes,
        uint creationDisposition, uint flagsAndAttributes, IntPtr templateFile);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool DeviceIoControl(IntPtr device, uint ioControlCode, ref int inBuffer, int inBufferSize,
        out uint outBuffer, int outBufferSize, out int bytesReturned, IntPtr overlapped);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool DeviceIoControl(IntPtr device, uint ioControlCode, ref BatteryQueryInformation inBuffer,
        int inBufferSize, out BatteryInformation outBuffer, int outBufferSize, out int bytesReturned, IntPtr overlapped);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr handle);
}

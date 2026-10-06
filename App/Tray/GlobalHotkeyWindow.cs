using System.Runtime.InteropServices;

namespace BHelper.App.Tray;

internal sealed class GlobalHotkeyWindow : NativeWindow, IDisposable
{
    private const int WmHotkey = 0x0312;
    private const int HotkeyId = 1;
    private const int ModNoRepeat = 0x4000;

    private bool _registered;
    private bool _disposed;

    public event EventHandler? Pressed;

    public GlobalHotkeyWindow()
    {
        CreateHandle(new CreateParams());
    }

    public bool Register(int modifiers, int key)
    {
        Unregister();
        _registered = RegisterHotKey(Handle, HotkeyId, (uint)(modifiers | ModNoRepeat), (uint)key);
        return _registered;
    }

    public void Unregister()
    {
        if (!_registered)
            return;

        UnregisterHotKey(Handle, HotkeyId);
        _registered = false;
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WmHotkey && m.WParam.ToInt32() == HotkeyId)
            Pressed?.Invoke(this, EventArgs.Empty);

        base.WndProc(ref m);
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;

        Unregister();
        DestroyHandle();
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}

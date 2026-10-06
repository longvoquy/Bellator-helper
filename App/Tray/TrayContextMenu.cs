namespace BHelper.App.Tray;

// The tray menu is a top-level window without an owner. Unless it is a tool window,
// Windows gives it a button on the taskbar while it is open.
internal sealed class TrayContextMenu : ContextMenuStrip
{
    private const int WsExToolWindow = 0x00000080;
    private const int WsExAppWindow = 0x00040000;

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ExStyle |= WsExToolWindow;
            cp.ExStyle &= ~WsExAppWindow;
            return cp;
        }
    }
}

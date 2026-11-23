using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace DotaAssistant.App.Services;

public enum UiDisplayMode
{
    Overlay,
    SeparateWindow
}

/// <summary>
/// Switches between transparent topmost overlay and a separate (non-topmost) window.
/// Window always uses AllowsTransparency + WindowStyle=None (required by WPF);
/// separate mode shows a custom title bar instead of OS chrome.
/// </summary>
public sealed class WindowChromeService
{
    private const int GwlExstyle = -20;
    private const int WsExTransparent = 0x00000020;
    private const int WsExToolwindow = 0x00000080;

    private readonly Window _window;
    private bool _clickThrough;

    public WindowChromeService(Window window)
    {
        _window = window;
    }

    public UiDisplayMode Mode { get; private set; } = UiDisplayMode.SeparateWindow;

    public bool ClickThrough
    {
        get => _clickThrough;
        set
        {
            _clickThrough = value && Mode == UiDisplayMode.Overlay;
            ApplyExtendedStyles();
        }
    }

    public void Apply(UiDisplayMode mode)
    {
        Mode = mode;
        if (mode == UiDisplayMode.Overlay)
        {
            _window.Topmost = true;
            _window.ShowInTaskbar = false;
        }
        else
        {
            _window.Topmost = false;
            _window.ShowInTaskbar = true;
            _clickThrough = false;
        }

        ApplyExtendedStyles();
    }

    public void ApplyExtendedStyles()
    {
        var helper = new WindowInteropHelper(_window);
        if (helper.Handle == IntPtr.Zero)
        {
            return;
        }

        var ex = GetWindowLong(helper.Handle, GwlExstyle);
        if (Mode == UiDisplayMode.Overlay)
        {
            ex |= WsExToolwindow;
            if (_clickThrough)
            {
                ex |= WsExTransparent;
            }
            else
            {
                ex &= ~WsExTransparent;
            }
        }
        else
        {
            ex &= ~WsExToolwindow;
            ex &= ~WsExTransparent;
        }

        _ = SetWindowLong(helper.Handle, GwlExstyle, ex);
    }

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
}

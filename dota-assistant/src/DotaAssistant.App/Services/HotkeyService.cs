using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace DotaAssistant.App.Services;

/// <summary>
/// Global Win32 hotkey registration (hide/show overlay).
/// </summary>
public sealed class HotkeyService : IDisposable
{
    public const int HotkeyId = 0xD07A; // arbitrary app id
    private const uint ModControl = 0x0002;
    private const uint ModShift = 0x0004;
    private const uint VkH = 0x48;

    private readonly Window _window;
    private HwndSource? _source;
    private bool _registered;

    public event Action? HotkeyPressed;

    public HotkeyService(Window window)
    {
        _window = window;
    }

    public void Register()
    {
        var helper = new WindowInteropHelper(_window);
        helper.EnsureHandle();
        _source = HwndSource.FromHwnd(helper.Handle);
        _source?.AddHook(WndProc);

        _registered = RegisterHotKey(helper.Handle, HotkeyId, ModControl | ModShift, VkH);
        if (!_registered)
        {
            // Keep UI usable even if another app owns the chord.
        }
    }

    public bool IsRegistered => _registered;

    public string ShortcutDisplay => "Ctrl+Shift+H";

    public void Dispose()
    {
        if (_source is not null)
        {
            var helper = new WindowInteropHelper(_window);
            if (helper.Handle != IntPtr.Zero && _registered)
            {
                UnregisterHotKey(helper.Handle, HotkeyId);
            }

            _source.RemoveHook(WndProc);
            _source = null;
        }
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        const int wmHotkey = 0x0312;
        if (msg == wmHotkey && wParam.ToInt32() == HotkeyId)
        {
            HotkeyPressed?.Invoke();
            handled = true;
        }

        return IntPtr.Zero;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}

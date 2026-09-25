using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using OsbizTaxation.Services;
using WinForms = System.Windows.Forms;

namespace OsbizTaxation.Helpers;

/// <summary>
/// Icone de la zone de notification basee sur Shell_NotifyIcon (plus fiable que NotifyIcon).
/// </summary>
public sealed class TrayIcon : IDisposable
{
    private const int WmCallback = 0x8000 + 1;
    private const int WM_LBUTTONUP = 0x0202;
    private const int WM_LBUTTONDBLCLK = 0x0203;
    private const int WM_RBUTTONUP = 0x0205;

    private const int NIM_ADD = 0;
    private const int NIM_MODIFY = 1;
    private const int NIM_DELETE = 2;

    private const int NIF_MESSAGE = 0x01;
    private const int NIF_ICON = 0x02;
    private const int NIF_TIP = 0x04;
    private const int NIF_INFO = 0x10;

    private const int NIIF_INFO = 0x01;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NOTIFYICONDATA
    {
        public int cbSize;
        public IntPtr hWnd;
        public int uID;
        public int uFlags;
        public int uCallbackMessage;
        public IntPtr hIcon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string szTip;
        public int dwState;
        public int dwStateMask;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string szInfo;
        public int uVersion;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string szInfoTitle;
        public int dwInfoFlags;
        public Guid guidItem;
        public IntPtr hBalloonIcon;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool Shell_NotifyIcon(int dwMessage, ref NOTIFYICONDATA lpData);

    private const int IconId = 1;

    private IntPtr _hwnd;
    private HwndSource? _source;
    private NOTIFYICONDATA _data;
    private Action? _onOpen;
    private Action? _onDoubleClick;
    private Action? _onQuit;
    private bool _added;
    private bool _disposed;

    public bool IsVisible => _added;

    public bool Show(Window owner, string tooltip, System.Drawing.Icon icon, Action onOpen, Action onDoubleClick, Action onQuit)
    {
        try
        {
            _onOpen = onOpen;
            _onDoubleClick = onDoubleClick;
            _onQuit = onQuit;

            _hwnd = new WindowInteropHelper(owner).EnsureHandle();
            if (_hwnd == IntPtr.Zero)
            {
                AppLog.Write("TrayIcon : handle de fenetre introuvable.");
                return false;
            }

            if (_source == null)
            {
                _source = HwndSource.FromHwnd(_hwnd);
                _source?.AddHook(WndProc);
            }

            _data = new NOTIFYICONDATA
            {
                cbSize = Marshal.SizeOf<NOTIFYICONDATA>(),
                hWnd = _hwnd,
                uID = IconId,
                uFlags = NIF_MESSAGE | NIF_ICON | NIF_TIP,
                uCallbackMessage = WmCallback,
                hIcon = icon.Handle,
                szTip = tooltip ?? string.Empty,
                szInfo = string.Empty,
                szInfoTitle = string.Empty,
            };

            if (!_added)
            {
                _added = Shell_NotifyIcon(NIM_ADD, ref _data);
                if (!_added)
                {
                    AppLog.Write($"TrayIcon : Shell_NotifyIcon(NIM_ADD) a echoue (code {Marshal.GetLastWin32Error()}).");
                    return false;
                }
            }
            else if (!Shell_NotifyIcon(NIM_MODIFY, ref _data))
            {
                AppLog.Write("TrayIcon : Shell_NotifyIcon(NIM_MODIFY) a echoue.");
            }

            return true;
        }
        catch (Exception ex)
        {
            AppLog.WriteException("TrayIcon.Show", ex);
            return false;
        }
    }

    public bool ShowBalloon(string title, string text)
    {
        if (!_added)
            return false;

        try
        {
            var data = _data;
            data.uFlags = NIF_INFO;
            data.szInfo = text ?? string.Empty;
            data.szInfoTitle = title ?? string.Empty;
            data.dwInfoFlags = NIIF_INFO;
            var ok = Shell_NotifyIcon(NIM_MODIFY, ref data);
            if (!ok)
                AppLog.Write("TrayIcon : affichage du ballon impossible.");
            return ok;
        }
        catch (Exception ex)
        {
            AppLog.WriteException("TrayIcon.ShowBalloon", ex);
            return false;
        }
    }

    public void Hide()
    {
        if (!_added)
            return;

        try
        {
            var data = _data;
            Shell_NotifyIcon(NIM_DELETE, ref data);
        }
        catch
        {
        }

        _added = false;
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg != WmCallback)
            return IntPtr.Zero;

        switch (lParam.ToInt32() & 0xFFFF)
        {
            case WM_LBUTTONDBLCLK:
                handled = true;
                _onDoubleClick?.Invoke();
                break;
            case WM_LBUTTONUP:
                handled = true;
                _onOpen?.Invoke();
                break;
            case WM_RBUTTONUP:
                handled = true;
                ShowMenu();
                break;
        }

        return IntPtr.Zero;
    }

    private void ShowMenu()
    {
        try
        {
            var menu = new WinForms.ContextMenuStrip();
            menu.Items.Add("Ouvrir", null, (_, _) => _onOpen?.Invoke());
            menu.Items.Add(new WinForms.ToolStripSeparator());
            menu.Items.Add("Quitter", null, (_, _) => _onQuit?.Invoke());
            menu.Show(WinForms.Cursor.Position);
        }
        catch (Exception ex)
        {
            AppLog.WriteException("TrayIcon.ShowMenu", ex);
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        Hide();

        if (_source != null)
        {
            _source.RemoveHook(WndProc);
            _source = null;
        }
    }
}

using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using MinimalSnapTimer.Infrastructure;
using MinimalSnapTimer.Models;

namespace MinimalSnapTimer.Services;

public sealed class HotkeyService : IDisposable
{
    private readonly Dictionary<int, string> _registeredActions = new();
    private HwndSource? _hwndSource;
    private Action<string>? _handler;

    public bool Register(Window window, IEnumerable<HotkeyBinding> bindings, Action<string> handler, out string? errorMessage)
    {
        errorMessage = null;
        _handler = handler;

        var handle = new WindowInteropHelper(window).Handle;
        if (handle == IntPtr.Zero)
        {
            errorMessage = "窗口句柄尚未准备好。";
            return false;
        }

        _hwndSource = HwndSource.FromHwnd(handle);
        _hwndSource?.AddHook(WndProc);

        var id = 0x5100;
        foreach (var binding in bindings.Where(b => !string.IsNullOrWhiteSpace(b.Gesture)))
        {
            if (!TryParseGesture(binding.Gesture, out var modifiers, out var key))
            {
                continue;
            }

            if (!NativeMethods.RegisterHotKey(handle, id, modifiers, (uint)KeyInterop.VirtualKeyFromKey(key)))
            {
                errorMessage ??= $"快捷键注册失败：{binding.Gesture}";
                continue;
            }

            _registeredActions[id] = binding.Action;
            id++;
        }

        return _registeredActions.Count > 0;
    }

    public void Unregister(Window window)
    {
        var handle = new WindowInteropHelper(window).Handle;
        foreach (var id in _registeredActions.Keys.ToList())
        {
            NativeMethods.UnregisterHotKey(handle, id);
        }

        _registeredActions.Clear();
        _hwndSource?.RemoveHook(WndProc);
        _hwndSource = null;
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == NativeMethods.WmHotkey)
        {
            var id = wParam.ToInt32();
            if (_registeredActions.TryGetValue(id, out var action))
            {
                _handler?.Invoke(action);
                handled = true;
            }
        }

        return IntPtr.Zero;
    }

    private static bool TryParseGesture(string input, out uint modifiers, out Key key)
    {
        modifiers = 0;
        key = Key.None;
        var parts = input.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var part in parts)
        {
            switch (part.ToLower(CultureInfo.InvariantCulture))
            {
                case "ctrl":
                case "control":
                    modifiers |= 0x0002;
                    break;
                case "shift":
                    modifiers |= 0x0004;
                    break;
                case "alt":
                    modifiers |= 0x0001;
                    break;
                case "win":
                    modifiers |= 0x0008;
                    break;
                default:
                    if (!Enum.TryParse(part, true, out key))
                    {
                        return false;
                    }

                    break;
            }
        }

        return key != Key.None;
    }

    public void Dispose()
    {
        _registeredActions.Clear();
        _hwndSource = null;
    }
}

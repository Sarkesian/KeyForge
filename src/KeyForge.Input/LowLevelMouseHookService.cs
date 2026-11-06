using System.ComponentModel;
using System.Runtime.InteropServices;
using KeyForge.Core.Input;

namespace KeyForge.Input;

public sealed class LowLevelMouseHookService : IInputHook
{
    private readonly Win32KeyboardNative.LowLevelMouseProc _callback;
    private nint _hookHandle;
    private bool _disposed;

    public LowLevelMouseHookService()
    {
        _callback = HookCallback;
    }

    public event EventHandler<InputHookEventArgs>? InputEvent;

    public bool IsRunning => _hookHandle != 0;

    public void Start()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (IsRunning)
        {
            return;
        }

        _hookHandle = Win32KeyboardNative.SetWindowsHookEx(
            Win32KeyboardNative.WhMouseLl,
            _callback,
            Win32KeyboardNative.GetModuleHandle(null),
            0);

        if (_hookHandle == 0)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Unable to install the mouse hook.");
        }
    }

    public void Stop()
    {
        if (_hookHandle == 0)
        {
            return;
        }

        if (!Win32KeyboardNative.UnhookWindowsHookEx(_hookHandle))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Unable to remove the mouse hook.");
        }

        _hookHandle = 0;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (_hookHandle != 0)
        {
            Win32KeyboardNative.UnhookWindowsHookEx(_hookHandle);
            _hookHandle = 0;
        }

        _disposed = true;
    }

    private nint HookCallback(int nCode, nint wParam, nint lParam)
    {
        if (nCode < 0)
        {
            return Win32KeyboardNative.CallNextHookEx(_hookHandle, nCode, wParam, lParam);
        }

        var message = wParam.ToInt32();
        var input = message switch
        {
            Win32KeyboardNative.WmLButtonDown or Win32KeyboardNative.WmLButtonUp => InputCatalog.MouseLeft,
            Win32KeyboardNative.WmRButtonDown or Win32KeyboardNative.WmRButtonUp => InputCatalog.MouseRight,
            Win32KeyboardNative.WmMButtonDown or Win32KeyboardNative.WmMButtonUp => InputCatalog.MouseMiddle,
            _ => null
        };
        if (input is null)
        {
            return Win32KeyboardNative.CallNextHookEx(_hookHandle, nCode, wParam, lParam);
        }

        var isDown = message is Win32KeyboardNative.WmLButtonDown or
            Win32KeyboardNative.WmRButtonDown or
            Win32KeyboardNative.WmMButtonDown;
        var data = Marshal.PtrToStructure<Win32KeyboardNative.MsLlHookStruct>(lParam);
        var injected = (data.Flags & (Win32KeyboardNative.LlmhfInjected | Win32KeyboardNative.LlmhfLowerIlInjected)) != 0;
        var args = new InputHookEventArgs(input, isDown, injected, message);
        try
        {
            InputEvent?.Invoke(this, args);
        }
        catch
        {
            args.Suppress = false;
        }

        return args.Suppress ? 1 : Win32KeyboardNative.CallNextHookEx(_hookHandle, nCode, wParam, lParam);
    }
}

using System.Runtime.InteropServices;
using System.ComponentModel;
using KeyForge.Core.Input;

namespace KeyForge.Input;

public sealed class SendInputSender : IInputSender
{
    private readonly object _gate = new();
    private readonly HashSet<string> _pressedInputs = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyCollection<string> PressedInputs
    {
        get
        {
            lock (_gate)
            {
                return _pressedInputs.ToList().AsReadOnly();
            }
        }
    }

    public void InputDown(string input)
    {
        var normalized = InputCatalog.Normalize(input);
        Send(normalized, inputUp: false);
        lock (_gate)
        {
            _pressedInputs.Add(normalized);
        }
    }

    public void InputUp(string input)
    {
        var normalized = InputCatalog.Normalize(input);
        Send(normalized, inputUp: true);
        lock (_gate)
        {
            _pressedInputs.Remove(normalized);
        }
    }

    public async Task PressInputAsync(string input, int holdMs = 12, CancellationToken cancellationToken = default)
    {
        InputDown(input);
        await Task.Delay(Math.Max(1, holdMs), cancellationToken);
        InputUp(input);
    }

    public void ReleaseAll()
    {
        List<string> inputs;
        lock (_gate)
        {
            inputs = _pressedInputs.Reverse().ToList();
            _pressedInputs.Clear();
        }

        foreach (var input in inputs)
        {
            Send(input, inputUp: true);
        }
    }

    private static void Send(string input, bool inputUp)
    {
        if (InputCatalog.IsMouseButton(input))
        {
            SendMouse(input, inputUp);
            return;
        }

        SendKeyboard(input, inputUp);
    }

    private static void SendKeyboard(string key, bool keyUp)
    {
        if (!InputKeyMap.TryGetVirtualKey(key, out var virtualKey))
        {
            throw new InvalidOperationException($"Cannot send unknown key '{key}'.");
        }

        var flags = keyUp ? Win32KeyboardNative.KeyEventFKeyUp : 0;
        var scanCode = (ushort)Win32KeyboardNative.MapVirtualKey(virtualKey.VirtualKey, Win32KeyboardNative.MapVkToVsc);
        if (virtualKey.IsExtended)
        {
            flags |= Win32KeyboardNative.KeyEventFExtendedKey;
        }

        if (scanCode != 0)
        {
            flags |= Win32KeyboardNative.KeyEventFScancode;
        }

        var inputs = new[]
        {
            new Win32KeyboardNative.Input
            {
                Type = Win32KeyboardNative.InputKeyboard,
                U = new Win32KeyboardNative.InputUnion
                {
                    Ki = new Win32KeyboardNative.KeybdInput
                    {
                        WVk = scanCode == 0 ? virtualKey.VirtualKey : (ushort)0,
                        WScan = scanCode,
                        DwFlags = flags,
                        Time = 0,
                        DwExtraInfo = 0
                    }
                }
            }
        };

        var sent = Win32KeyboardNative.SendInput(1, inputs, Marshal.SizeOf<Win32KeyboardNative.Input>());
        if (sent != 1)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), $"SendInput failed for {key}.");
        }
    }

    private static void SendMouse(string button, bool buttonUp)
    {
        var flags = InputCatalog.Normalize(button) switch
        {
            InputCatalog.MouseLeft => buttonUp ? Win32KeyboardNative.MouseEventFLeftUp : Win32KeyboardNative.MouseEventFLeftDown,
            InputCatalog.MouseRight => buttonUp ? Win32KeyboardNative.MouseEventFRightUp : Win32KeyboardNative.MouseEventFRightDown,
            InputCatalog.MouseMiddle => buttonUp ? Win32KeyboardNative.MouseEventFMiddleUp : Win32KeyboardNative.MouseEventFMiddleDown,
            _ => throw new InvalidOperationException($"Cannot send unknown mouse button '{button}'.")
        };

        var inputs = new[]
        {
            new Win32KeyboardNative.Input
            {
                Type = Win32KeyboardNative.InputMouse,
                U = new Win32KeyboardNative.InputUnion
                {
                    Mi = new Win32KeyboardNative.MouseInput
                    {
                        Dx = 0,
                        Dy = 0,
                        MouseData = 0,
                        DwFlags = flags,
                        Time = 0,
                        DwExtraInfo = 0
                    }
                }
            }
        };

        var sent = Win32KeyboardNative.SendInput(1, inputs, Marshal.SizeOf<Win32KeyboardNative.Input>());
        if (sent != 1)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), $"SendInput failed for {button}.");
        }
    }
}

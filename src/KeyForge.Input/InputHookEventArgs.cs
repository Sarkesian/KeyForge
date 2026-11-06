namespace KeyForge.Input;

public sealed class InputHookEventArgs : EventArgs
{
    public InputHookEventArgs(string input, bool isInputDown, bool isInjected, int nativeCode)
    {
        Input = input;
        IsInputDown = isInputDown;
        IsInjected = isInjected;
        NativeCode = nativeCode;
    }

    public string Input { get; }

    public bool IsInputDown { get; }

    public bool IsInputUp => !IsInputDown;

    public bool IsInjected { get; }

    public int NativeCode { get; }

    public bool Suppress { get; set; }
}

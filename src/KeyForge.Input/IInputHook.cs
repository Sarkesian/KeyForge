namespace KeyForge.Input;

public interface IInputHook : IDisposable
{
    event EventHandler<InputHookEventArgs>? InputEvent;

    bool IsRunning { get; }

    void Start();

    void Stop();
}

namespace KeyForge.Input;

public sealed class CompositeInputHookService : IInputHook
{
    private readonly IReadOnlyList<IInputHook> _hooks;
    private bool _disposed;

    public CompositeInputHookService(params IInputHook[] hooks)
    {
        _hooks = hooks;
        foreach (var hook in _hooks)
        {
            hook.InputEvent += OnInputEvent;
        }
    }

    public event EventHandler<InputHookEventArgs>? InputEvent;

    public bool IsRunning => _hooks.Count > 0 && _hooks.All(hook => hook.IsRunning);

    public void Start()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        try
        {
            foreach (var hook in _hooks)
            {
                hook.Start();
            }
        }
        catch
        {
            foreach (var hook in _hooks.Where(hook => hook.IsRunning))
            {
                hook.Stop();
            }

            throw;
        }
    }

    public void Stop()
    {
        foreach (var hook in _hooks.Where(hook => hook.IsRunning))
        {
            hook.Stop();
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        foreach (var hook in _hooks)
        {
            hook.InputEvent -= OnInputEvent;
            hook.Dispose();
        }

        _disposed = true;
    }

    private void OnInputEvent(object? sender, InputHookEventArgs e)
    {
        InputEvent?.Invoke(this, e);
    }
}

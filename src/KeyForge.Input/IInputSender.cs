namespace KeyForge.Input;

public interface IInputSender
{
    IReadOnlyCollection<string> PressedInputs { get; }

    void InputDown(string input);

    void InputUp(string input);

    Task PressInputAsync(string input, int holdMs = 12, CancellationToken cancellationToken = default);

    void ReleaseAll();
}

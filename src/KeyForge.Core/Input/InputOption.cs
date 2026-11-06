namespace KeyForge.Core.Input;

public sealed record InputOption(string Code, string Label, string Group)
{
    public string DisplayLabel => $"{Group} - {Label}";

    public override string ToString() => Label;
}

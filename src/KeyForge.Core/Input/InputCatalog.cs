using System.Collections.ObjectModel;
using KeyForge.Core.Keyboard;

namespace KeyForge.Core.Input;

public static class InputCatalog
{
    public const string MouseLeft = "MouseLeft";
    public const string MouseRight = "MouseRight";
    public const string MouseMiddle = "MouseMiddle";

    private static readonly Dictionary<string, string> MouseLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        [MouseLeft] = "Left Click",
        [MouseRight] = "Right Click",
        [MouseMiddle] = "Middle Click"
    };

    private static readonly Dictionary<string, string> KeyboardOptionLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Esc"] = "Esc",
        ["Backquote"] = "Backquote (`)",
        ["Minus"] = "Minus",
        ["Equal"] = "Equal (=)",
        ["Backspace"] = "Backspace",
        ["Tab"] = "Tab",
        ["LeftBracket"] = "Left Bracket ([)",
        ["RightBracket"] = "Right Bracket (])",
        ["Backslash"] = "Backslash (\\)",
        ["CapsLock"] = "Caps Lock",
        ["Semicolon"] = "Semicolon (;)",
        ["Quote"] = "Quote (')",
        ["Enter"] = "Main Enter",
        ["LeftShift"] = "Left Shift",
        ["Comma"] = "Comma (,)",
        ["Period"] = "Period (.)",
        ["Slash"] = "Slash (/)",
        ["RightShift"] = "Right Shift",
        ["LeftCtrl"] = "Left Ctrl",
        ["LeftWin"] = "Left Win",
        ["LeftAlt"] = "Left Alt",
        ["Space"] = "Space",
        ["RightAlt"] = "Right Alt",
        ["RightWin"] = "Right Win",
        ["Menu"] = "Menu",
        ["RightCtrl"] = "Right Ctrl",
        ["PrintScreen"] = "Print Screen",
        ["ScrollLock"] = "Scroll Lock",
        ["Pause"] = "Pause",
        ["Insert"] = "Insert",
        ["Delete"] = "Delete",
        ["Home"] = "Home",
        ["End"] = "End",
        ["PageUp"] = "Page Up",
        ["PageDown"] = "Page Down",
        ["Up"] = "Up Arrow",
        ["Down"] = "Down Arrow",
        ["Left"] = "Left Arrow",
        ["Right"] = "Right Arrow",
        ["NumLock"] = "Num Lock",
        ["NumpadDivide"] = "Numpad /",
        ["NumpadMultiply"] = "Numpad *",
        ["NumpadMinus"] = "Numpad -",
        ["NumpadPlus"] = "Numpad +",
        ["NumpadEnter"] = "Numpad Enter",
        ["NumpadDecimal"] = "Numpad ."
    };

    private static readonly HashSet<string> MouseButtons = new(MouseLabels.Keys, StringComparer.OrdinalIgnoreCase);

    private static readonly ReadOnlyCollection<InputOption> AllOptions = BuildOptions().ToList().AsReadOnly();

    public static IReadOnlyList<InputOption> All => AllOptions;

    public static IReadOnlyList<KeyVisualDefinition> MouseVisualButtons { get; } =
    [
        new(MouseLeft, "Left", 24.4, 2.0, 0.95, 1.65),
        new(MouseRight, "Right", 25.55, 2.0, 0.95, 1.65),
        new(MouseMiddle, "Mid", 25.16, 2.0, 0.32, 1.1)
    ];

    public static bool IsKnown(string? input) =>
        !string.IsNullOrWhiteSpace(input) &&
        (IsMouseButton(input) || KeyCatalog.IsKnown(input));

    public static bool IsKeyboardKey(string? input) =>
        !string.IsNullOrWhiteSpace(input) && KeyCatalog.IsKnown(input);

    public static bool IsMouseButton(string? input) =>
        !string.IsNullOrWhiteSpace(input) && MouseButtons.Contains(NormalizeMouse(input));

    public static bool IsModifier(string? input) =>
        IsKeyboardKey(input) && KeyCatalog.IsModifier(input);

    public static bool IsRisky(string? input) =>
        IsKeyboardKey(input) && KeyCatalog.IsRisky(input);

    public static string Normalize(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        var mouse = NormalizeMouse(input);
        if (MouseButtons.Contains(mouse))
        {
            return mouse;
        }

        return KeyCatalog.Normalize(input);
    }

    public static string LabelFor(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        var normalized = Normalize(input);
        if (MouseLabels.TryGetValue(normalized, out var mouseLabel))
        {
            return mouseLabel;
        }

        return LabelForKeyboard(normalized);
    }

    public static InputOption? OptionFor(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return null;
        }

        var normalized = Normalize(input);
        return AllOptions.FirstOrDefault(option =>
            string.Equals(option.Code, normalized, StringComparison.OrdinalIgnoreCase));
    }

    private static IEnumerable<InputOption> BuildOptions()
    {
        foreach (var key in KeyCatalog.All)
        {
            yield return new InputOption(key.Code, LabelForKeyboard(key.Code), KeyboardGroupFor(key.Code));
        }

        yield return new InputOption(MouseLeft, MouseLabels[MouseLeft], "Mouse");
        yield return new InputOption(MouseRight, MouseLabels[MouseRight], "Mouse");
        yield return new InputOption(MouseMiddle, MouseLabels[MouseMiddle], "Mouse");
    }

    private static string LabelForKeyboard(string key)
    {
        var normalized = KeyCatalog.Normalize(key);
        if (KeyboardOptionLabels.TryGetValue(normalized, out var label))
        {
            return label;
        }

        if (normalized.StartsWith('D') && normalized.Length == 2 && char.IsDigit(normalized[1]))
        {
            return normalized[1].ToString();
        }

        if (normalized.StartsWith("Numpad", StringComparison.OrdinalIgnoreCase) &&
            normalized.Length == "Numpad0".Length &&
            char.IsDigit(normalized[^1]))
        {
            return $"Numpad {normalized[^1]}";
        }

        return KeyCatalog.LabelFor(normalized);
    }

    private static string KeyboardGroupFor(string key)
    {
        var normalized = KeyCatalog.Normalize(key);
        if (normalized.StartsWith("Numpad", StringComparison.OrdinalIgnoreCase) || normalized == "NumLock")
        {
            return "Numpad";
        }

        if (normalized is "Left" or "Right" or "Up" or "Down" or
            "Insert" or "Delete" or "Home" or "End" or "PageUp" or "PageDown")
        {
            return "Navigation";
        }

        if (normalized.StartsWith('F') &&
            normalized.Length is 2 or 3 &&
            int.TryParse(normalized[1..], out var functionKey) &&
            functionKey is >= 1 and <= 12)
        {
            return "Function";
        }

        if (KeyCatalog.IsModifier(normalized))
        {
            return "Modifiers";
        }

        return "Keyboard";
    }

    private static string NormalizeMouse(string input)
    {
        return input.Trim().ToLowerInvariant() switch
        {
            "mouseleft" or "leftclick" or "left click" or "lbutton" or "leftbutton" => MouseLeft,
            "mouseright" or "rightclick" or "right click" or "rbutton" or "rightbutton" => MouseRight,
            "mousemiddle" or "middleclick" or "middle click" or "mbutton" or "middlebutton" => MouseMiddle,
            _ => input.Trim()
        };
    }
}

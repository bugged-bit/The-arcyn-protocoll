using System;

namespace ARCYN.Core.Models;

/// <summary>
/// Keyboard modifier flags that can appear in a shortcut combo.
/// </summary>
[Flags]
public enum KeyModifiers
{
    None = 0,
    Ctrl = 1 << 0,
    Alt = 1 << 1,
    Shift = 1 << 2,
    Meta = 1 << 3,
    Super = 1 << 4
}

/// <summary>
/// A small, framework-agnostic set of keys ARCYN understands for shortcuts.
/// The Avalonia layer maps these to <c>Avalonia.Input.Key</c> when installing
/// <c>KeyBinding</c>s on the main window.
/// </summary>
public enum LogicalKey
{
    None = 0,

    // Digits
    D0, D1, D2, D3, D4, D5, D6, D7, D8, D9,

    // Letters
    A, B, C, D, E, F, G, H, I, J, K, L, M,
    N, O, P, Q, R, S, T, U, V, W, X, Y, Z,

    // Function keys
    F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12,
    F13, F14, F15, F16, F17, F18, F19, F20, F21, F22, F23, F24,

    // Navigation and editing
    Escape, Tab, Space, Enter, Backspace,
    Insert, Delete, Home, End,
    PageUp, PageDown,
    Up, Down, Left, Right
}

/// <summary>
/// A parsed keyboard shortcut: zero or more modifiers plus a single logical key.
/// Independent of any specific UI framework so the same parsed value can be
/// re-used by the Avalonia layer, tests, and future global hotkey plumbing.
/// </summary>
public readonly record struct KeyCombo(KeyModifiers Modifiers, LogicalKey Key)
{
    public const char Separator = '+';

    /// <summary>
    /// Tries to parse a string like <c>"Ctrl+Alt+1"</c>, <c>"ctrl+shift+F5"</c>,
    /// <c>"Super+K"</c>, or <c>"F12"</c> into a <see cref="KeyCombo"/>.
    /// </summary>
    public static bool TryParse(string? text, out KeyCombo combo)
    {
        combo = default;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        var parts = text.Split(Separator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
            return false;

        // The last segment is the key; everything before it must be a modifier.
        // This rejects both empty trailing keys ("Ctrl+") and reversed order ("1+Ctrl").
        var keySegment = parts[^1];
        if (parts.Length > 1 && IsModifierToken(keySegment))
            return false;

        if (!TryParseKey(keySegment, out var key))
            return false;

        var modifiers = KeyModifiers.None;
        for (var i = 0; i < parts.Length - 1; i++)
        {
            if (!TryParseModifier(parts[i], out var mod))
                return false;
            // Don't allow duplicate modifiers, e.g. "Ctrl+Ctrl+1".
            if ((modifiers & mod) != 0)
                return false;
            modifiers |= mod;
        }

        combo = new KeyCombo(modifiers, key);
        return true;
    }

    /// <summary>
    /// Returns a normalized display string such as <c>"Ctrl+Alt+1"</c>.
    /// </summary>
    public override string ToString()
    {
        if (Key == LogicalKey.None)
            return string.Empty;

        var parts = new System.Collections.Generic.List<string>(6);
        if ((Modifiers & KeyModifiers.Ctrl) != 0) parts.Add("Ctrl");
        if ((Modifiers & KeyModifiers.Alt) != 0) parts.Add("Alt");
        if ((Modifiers & KeyModifiers.Shift) != 0) parts.Add("Shift");
        if ((Modifiers & KeyModifiers.Meta) != 0) parts.Add("Meta");
        if ((Modifiers & KeyModifiers.Super) != 0) parts.Add("Super");
        parts.Add(KeyName(Key));
        return string.Join(Separator, parts);
    }

    private static bool IsModifierToken(string token)
    {
        return token.Equals("Ctrl", StringComparison.OrdinalIgnoreCase)
            || token.Equals("Alt", StringComparison.OrdinalIgnoreCase)
            || token.Equals("Shift", StringComparison.OrdinalIgnoreCase)
            || token.Equals("Meta", StringComparison.OrdinalIgnoreCase)
            || token.Equals("Super", StringComparison.OrdinalIgnoreCase)
            || token.Equals("Cmd", StringComparison.OrdinalIgnoreCase)
            || token.Equals("Win", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryParseModifier(string token, out KeyModifiers modifier)
    {
        switch (token.ToLowerInvariant())
        {
            case "ctrl":
            case "control":
                modifier = KeyModifiers.Ctrl;
                return true;
            case "alt":
                modifier = KeyModifiers.Alt;
                return true;
            case "shift":
                modifier = KeyModifiers.Shift;
                return true;
            case "meta":
                modifier = KeyModifiers.Meta;
                return true;
            case "super":
            case "cmd":
            case "win":
                modifier = KeyModifiers.Super;
                return true;
            default:
                modifier = KeyModifiers.None;
                return false;
        }
    }

    private static bool TryParseKey(string token, out LogicalKey key)
    {
        if (token.Length == 1)
        {
            var c = token[0];
            if (char.IsDigit(c))
            {
                key = (LogicalKey)((int)LogicalKey.D0 + (c - '0'));
                return true;
            }
            if (c >= 'A' && c <= 'Z')
            {
                key = (LogicalKey)((int)LogicalKey.A + (c - 'A'));
                return true;
            }
            if (c >= 'a' && c <= 'z')
            {
                key = (LogicalKey)((int)LogicalKey.A + (c - 'a'));
                return true;
            }
        }

        switch (token.ToLowerInvariant())
        {
            case "esc":
            case "escape":
                key = LogicalKey.Escape;
                return true;
            case "tab":
                key = LogicalKey.Tab;
                return true;
            case "space":
            case "spacebar":
                key = LogicalKey.Space;
                return true;
            case "enter":
            case "return":
                key = LogicalKey.Enter;
                return true;
            case "backspace":
            case "bs":
                key = LogicalKey.Backspace;
                return true;
            case "insert":
            case "ins":
                key = LogicalKey.Insert;
                return true;
            case "delete":
            case "del":
                key = LogicalKey.Delete;
                return true;
            case "home":
                key = LogicalKey.Home;
                return true;
            case "end":
                key = LogicalKey.End;
                return true;
            case "pageup":
            case "pgup":
                key = LogicalKey.PageUp;
                return true;
            case "pagedown":
            case "pgdn":
            case "pgdown":
                key = LogicalKey.PageDown;
                return true;
            case "up":
            case "arrowup":
                key = LogicalKey.Up;
                return true;
            case "down":
            case "arrowdown":
                key = LogicalKey.Down;
                return true;
            case "left":
            case "arrowleft":
                key = LogicalKey.Left;
                return true;
            case "right":
            case "arrowright":
                key = LogicalKey.Right;
                return true;
        }

        if (token.Length >= 2 && (token[0] == 'F' || token[0] == 'f') &&
            int.TryParse(token.AsSpan(1), out var fNum) &&
            fNum >= 1 && fNum <= 24)
        {
            key = (LogicalKey)((int)LogicalKey.F1 + (fNum - 1));
            return true;
        }

        key = LogicalKey.None;
        return false;
    }

    private static string KeyName(LogicalKey key)
    {
        // Two-character tokens (digits, letters) use the canonical display form.
        if (key >= LogicalKey.D0 && key <= LogicalKey.D9)
            return ((char)('0' + (int)(key - LogicalKey.D0))).ToString();
        if (key >= LogicalKey.A && key <= LogicalKey.Z)
            return ((char)('A' + (int)(key - LogicalKey.A))).ToString();
        if (key >= LogicalKey.F1 && key <= LogicalKey.F24)
            return "F" + ((int)(key - LogicalKey.F1) + 1).ToString();

        return key switch
        {
            LogicalKey.Escape => "Escape",
            LogicalKey.Tab => "Tab",
            LogicalKey.Space => "Space",
            LogicalKey.Enter => "Enter",
            LogicalKey.Backspace => "Backspace",
            LogicalKey.Insert => "Insert",
            LogicalKey.Delete => "Delete",
            LogicalKey.Home => "Home",
            LogicalKey.End => "End",
            LogicalKey.PageUp => "PageUp",
            LogicalKey.PageDown => "PageDown",
            LogicalKey.Up => "Up",
            LogicalKey.Down => "Down",
            LogicalKey.Left => "Left",
            LogicalKey.Right => "Right",
            _ => string.Empty
        };
    }
}

using System.Diagnostics.CodeAnalysis;

namespace ARCYN.Core.Utils;

/// <summary>
/// Shared string, path quoting, and validation helpers.
/// </summary>
public static class SharedHelper
{
    /// <summary>
    /// Wraps a path in double quotes if it contains spaces.
    /// </summary>
    /// <param name="path">The file system path to quote.</param>
    /// <returns>The quoted path if it contains spaces, otherwise the original path.</returns>
    public static string QuotePath(string path)
    {
        return path.Contains(' ') ? $"\"{path}\"" : path;
    }

    /// <summary>
    /// Determines whether the specified string is a valid absolute HTTP or HTTPS URL.
    /// </summary>
    public static bool IsValidUrl([NotNullWhen(true)] string? url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    /// <summary>
    /// Truncates the specified string to the given maximum length.
    /// Appends "..." if the string was truncated.
    /// </summary>
    public static string Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || maxLength <= 0)
            return string.Empty;
        if (value.Length <= maxLength)
            return value;
        return value[..Math.Max(0, maxLength - 3)] + "...";
    }

    /// <summary>
    /// Returns the string or an empty string if it is null.
    /// </summary>
    public static string OrEmpty([NotNullWhen(false)] this string? value)
    {
        return value ?? string.Empty;
    }

    /// <summary>
    /// Returns the string trimmed, or an empty string if it is null or whitespace.
    /// </summary>
    public static string TrimToEmpty([NotNullWhen(false)] this string? value)
    {
        return value?.Trim() ?? string.Empty;
    }

    /// <summary>
    /// Indicates whether the string is null, empty, or consists only of white-space characters.
    /// </summary>
    public static bool IsBlank([NotNullWhen(false)] this string? value)
    {
        return string.IsNullOrWhiteSpace(value);
    }
}

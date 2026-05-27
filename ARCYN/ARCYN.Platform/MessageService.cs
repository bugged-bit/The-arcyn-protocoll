using System;

namespace ARCYN.Platform;

/// <summary>
/// Simple console‑based message service. UI projects can provide richer implementations.
/// </summary>
public class MessageService : IMessageService
{
    public void ShowError(string title, string message)
    {
        Console.Error.WriteLine($"[ERROR] {title}: {message}");
    }

    public void ShowInfo(string title, string message)
    {
        Console.WriteLine($"[INFO] {title}: {message}");
    }
}
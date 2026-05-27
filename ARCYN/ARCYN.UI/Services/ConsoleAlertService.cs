using System;

namespace ARCYN.UI.Services;

internal class ConsoleAlertService : IAlertService
{
    public void Show(string message, string title = "ARCYN")
    {
        // Write to standard error with a simple prefix.
        Console.Error.WriteLine($"{title}: {message}");
    }
}

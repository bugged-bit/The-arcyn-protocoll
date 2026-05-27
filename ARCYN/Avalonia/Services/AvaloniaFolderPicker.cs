using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using ARCYN.Core.Services;

namespace ARCYN.Avalonia.Services;

public class AvaloniaFolderPicker : IFolderPicker
{
    public string? PickFolder(string title = "Select a folder")
    {
        // Attempt to obtain the current desktop window as parent.
        Window? parent = null;
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            parent = desktop.MainWindow;
        }

        var dialog = new OpenFolderDialog
        {
            Title = title
        };

        // ShowAsync returns a Task<string?>. Block synchronously for simplicity.
        var task = dialog.ShowAsync(parent ?? new Window());
        return task.GetAwaiter().GetResult();
    }
}

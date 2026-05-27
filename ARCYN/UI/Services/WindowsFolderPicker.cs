using System.IO;
using Microsoft.Win32;
using ARCYN.Core.Services;

namespace ARCYN.UI.Services;

public class WindowsFolderPicker : IFolderPicker
{
    public string? PickFolder(string title = "Select a folder")
    {
        var dialog = new OpenFileDialog
        {
            Title = title,
            ValidateNames = false,
            CheckFileExists = false,
            CheckPathExists = true,
            FileName = "Select"
        };

        if (dialog.ShowDialog() == true)
        {
            var dir = Path.GetDirectoryName(dialog.FileName);
            if (!string.IsNullOrEmpty(dir))
                return dir;
        }
        return null;
    }
}

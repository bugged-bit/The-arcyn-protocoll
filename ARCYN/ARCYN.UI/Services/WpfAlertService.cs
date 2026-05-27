using System.Windows;

namespace ARCYN.UI.Services;

internal class WpfAlertService : IAlertService
{
    public void Show(string message, string title = "ARCYN")
    {
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
    }
}

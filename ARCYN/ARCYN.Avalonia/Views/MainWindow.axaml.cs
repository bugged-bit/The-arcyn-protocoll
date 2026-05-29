using Avalonia;
using Avalonia.Controls;
using ARCYN.UI.ViewModels;

namespace ARCYN.Avalonia.Views;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _vm;

    public MainWindow()
    {
        InitializeComponent();
        _vm = new MainWindowViewModel();
        DataContext = _vm;
    }
}

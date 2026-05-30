using Avalonia.Controls;
using Avalonia.Input;
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

    private void Window_KeyDown(object? sender, KeyEventArgs e)
    {
    }

    private void RootGrid_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
    }
}

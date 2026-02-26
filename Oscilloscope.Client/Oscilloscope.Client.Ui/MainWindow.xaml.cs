using System.Windows;
using Oscilloscope.Client.Ui.ViewModels;

namespace Oscilloscope.Client.Ui;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }
}

using Avalonia.Controls;

namespace DeskQuit.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }
    
    protected override void OnClosing(WindowClosingEventArgs e)
    {
        base.OnClosing(e);
        
        e.Cancel = true;
        Hide();
    }
}
using System;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using DeskQuit.ViewModels.Notifications;

namespace DeskQuit.Views.Notifications;

public partial class SoftPersistentNotificationWindow : Window
{
    public event Action? DoneClicked;
    public event Action<TimeSpan>? SnoozeClicked;

    public SoftPersistentNotificationWindow()
    {
        InitializeComponent();
    }

    public SoftPersistentNotificationWindow(string title, string body)
    {
        InitializeComponent();
        
        var vm = new SoftPersistentNotificationViewModel(title, body);
        vm.DoneRequested += () => 
        {
            DoneClicked?.Invoke();
            Close();
        };
        vm.SnoozeRequested += (time) => 
        {
            SnoozeClicked?.Invoke(time);
            Close();
        };
        
        DataContext = vm;
        SetPosition();
    }

    private void SetPosition()
    {
        var screen = Screens.Primary;
        if (screen != null)
        {
            var workingArea = screen.WorkingArea;
            var windowWidth = 380;
            var windowHeight = 170;
            // 20px padding from the bottom right corner
            Position = new Avalonia.PixelPoint(workingArea.Right - windowWidth - 20, workingArea.Bottom - windowHeight - 20);
        }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}

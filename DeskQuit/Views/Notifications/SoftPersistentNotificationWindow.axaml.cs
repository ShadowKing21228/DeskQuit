using System;
using Avalonia;
using Avalonia.Controls;
using DeskQuit.ViewModels.Notifications;

namespace DeskQuit.Views.Notifications;

public partial class SoftPersistentNotificationWindow : Window
{
    public event Action? DoneClicked;
    public event Action<TimeSpan>? SnoozeClicked;

    private readonly SoftPersistentNotificationViewModel _viewModel;

    public SoftPersistentNotificationWindow(string title, string body)
    {
        _viewModel = new SoftPersistentNotificationViewModel(title, body);
        _viewModel.DoneRequested += () =>
        {
            DoneClicked?.Invoke();
            Close();
        };
        _viewModel.SnoozeRequested += snooze =>
        {
            SnoozeClicked?.Invoke(snooze);
            Close();
        };

        DataContext = _viewModel;
        
        InitializeComponent();
        Opened += (_, _) => PositionWindow();
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (!_viewModel.CanClose)
        {
            e.Cancel = true;
            return;
        }

        base.OnClosing(e);
    }

    protected override void OnClosed(EventArgs e)
    {
        _viewModel.Dispose();
        base.OnClosed(e);
    }

    private void PositionWindow()
    {
        var screen = Screens.ScreenFromWindow(this) ?? Screens.Primary;
        if (screen is null)
            return;

        var area = screen.WorkingArea;
        var x = area.X + area.Width - (int)Width - 20;
        var y = area.Y + 20;
        Position = new PixelPoint(x, y);
    }
}

using System;
using Avalonia.Controls;
using DeskQuit.ViewModels.Notifications;

namespace DeskQuit.Views.Notifications;

public partial class AggressiveBlockingNotificationWindow : Window
{
    public event Action? BreakStarted;

    private readonly AggressiveBlockingNotificationViewModel _viewModel;

    public AggressiveBlockingNotificationWindow(string title, string body)
    {
        _viewModel = new AggressiveBlockingNotificationViewModel(title, body);
        _viewModel.BreakStartedRequested += () =>
        {
            BreakStarted?.Invoke();
            Close();
        };

        DataContext = _viewModel;
        InitializeComponent();
        Opened += (_, _) => _viewModel.StartCooldown();
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (!_viewModel.CanClose)
        {
            e.Cancel = true;
            return;
        }

        _viewModel.StopCooldown();
        base.OnClosing(e);
    }

    protected override void OnClosed(EventArgs e)
    {
        _viewModel.Dispose();
        base.OnClosed(e);
    }
}

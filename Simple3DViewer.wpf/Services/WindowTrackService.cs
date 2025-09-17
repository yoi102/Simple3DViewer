using System.Windows;

namespace Simple3DViewer.wpf.Services;

internal interface IWindowTrackService
{
    Window LastActivatedWindow { get; }
}

internal class WindowTrackService : IWindowTrackService
{
    private Window? lastActivatedWindow;

    public WindowTrackService()
    {
        EventManager.RegisterClassHandler(typeof(Window), Window.LoadedEvent,
    new RoutedEventHandler(WindowLoaded));
        EventManager.RegisterClassHandler(typeof(Window), Window.UnloadedEvent,
                                          new RoutedEventHandler(WindowUnloaded));
    }

    public Window LastActivatedWindow
    {
        get
        {
            if (lastActivatedWindow is null)
            {
                return Application.Current.MainWindow;
            }
            return lastActivatedWindow;
        }
    }

    private void Window_Activated(object? sender, EventArgs e)
    {
        if (sender is Window window)
        {
            lastActivatedWindow = window;
        }
    }

    private void Window_Deactivated(object? sender, EventArgs e)
    {
        if (sender is Window window)
        {
            lastActivatedWindow = null;
        }
    }

    private void WindowLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is Window window)
        {
            window.Activated -= Window_Activated;
            window.Deactivated -= Window_Deactivated;
            window.Activated += Window_Activated;
            window.Deactivated += Window_Deactivated;
        }
    }

    private void WindowUnloaded(object sender, RoutedEventArgs e)
    {
        if (sender is Window window)
        {
            window.Activated -= Window_Activated;
            window.Deactivated -= Window_Deactivated;
        }
    }
}
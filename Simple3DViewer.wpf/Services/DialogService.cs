using MaterialDesignThemes.Wpf;
using Simple3DViewer.wpf.Extensions;
using Simple3DViewer.wpf.Misc;
using System.Windows;

namespace Simple3DViewer.wpf.Services;

internal interface IDialogService
{
    IDisposable ShowProgressDialog();

    IDisposable ShowProgressDialogWithCancelButton(Action? cancelButtonCallBack = null);

    void Close();

    void Close(object dialogIdentifier);
}

internal class DialogService : IDialogService
{
    private readonly IWindowTrackService windowTrackService;

    public DialogService(IWindowTrackService windowTrackService)
    {
        this.windowTrackService = windowTrackService;
    }

    public IDisposable ShowProgressDialog()
    {
        Window activeWindow = windowTrackService.LastActivatedWindow;

        DialogHost? dialogHost = GetFirstDialogHost(activeWindow);
        if (dialogHost is null)
            throw new InvalidOperationException("No DialogHost found in the active window.");

        // 关闭当前打开的对话框，确保新的对话框可以正确显示
        object? dialogIdentifier = dialogHost.Identifier;
        if (dialogIdentifier is null)
            throw new InvalidOperationException("DialogHost does not have an identifier.");

        var dialogSession = DialogHost.GetDialogSession(dialogIdentifier);
        View.Dialogs.ProgressDialog progressDialog = new(false);

        if (dialogSession is not null)
        {
            dialogSession.UpdateContent(progressDialog);
        }
        else
        {
            DialogHost.Show(progressDialog, dialogIdentifier);
        }

        return new DeferredScope(() => { Close(dialogIdentifier); });
    }

    public IDisposable ShowProgressDialogWithCancelButton(Action? cancelButtonCallBack = null)
    {
        Window activeWindow = windowTrackService.LastActivatedWindow;

        DialogHost? dialogHost = GetFirstDialogHost(activeWindow);
        if (dialogHost is null)
            throw new InvalidOperationException("No DialogHost found in the active window.");

        // 关闭当前打开的对话框，确保新的对话框可以正确显示
        object? identifier = dialogHost.Identifier;
        if (identifier is null)
            throw new InvalidOperationException("DialogHost does not have an identifier.");

        DialogSession? dialogSession = DialogHost.GetDialogSession(identifier);
        dialogSession?.Close();
        View.Dialogs.ProgressDialog progressDialog = new(true);
        progressDialog.CancelButtonClickedCallBack = () =>
        {
            cancelButtonCallBack?.Invoke();
            Close(identifier);
        };
        dialogHost.ShowDialog(progressDialog);
        return new DeferredScope(() => { Close(identifier); });
    }

    public void Close()
    {
        Window activeWindow = windowTrackService.LastActivatedWindow;

        DialogHost? dialogHost = GetFirstDialogHost(activeWindow);
        if (dialogHost is null)
            return;
        object? identifier = dialogHost.Identifier;
        if (identifier is null)
            return;

        Close(identifier);
    }

    public void Close(object dialogIdentifier)
    {
        DialogSession? dialogSession = DialogHost.GetDialogSession(dialogIdentifier);
        dialogSession?.Close();
    }

    private static DialogHost? GetFirstDialogHost(Window window)
    {
        if (window is null) throw new ArgumentNullException(nameof(window));

        DialogHost? dialogHost = window.VisualDepthFirstTraversal().OfType<DialogHost>().FirstOrDefault();

        return dialogHost;
    }
}
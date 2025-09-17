using System.Windows;
using System.Windows.Controls;

namespace Simple3DViewer.wpf.View.Dialogs;

/// <summary>
/// ProgressDialog.xaml 的交互逻辑
/// </summary>
public partial class ProgressDialog : UserControl
{
    public ProgressDialog(bool cancelButtonIsVisable)
    {
        InitializeComponent();
        if (!cancelButtonIsVisable)
            CancelButton.Visibility = Visibility.Collapsed;
    }

    public Action? CancelButtonClickedCallBack;

    private void CancelButtonClicked(object sender, RoutedEventArgs e)
    {
        CancelButtonClickedCallBack?.Invoke();
    }
}
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;

namespace Simple3DViewer.wpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = App.Current.Services.GetService<MainWindowViewModel>();
        }

        private void IconClicked(object sender, System.Windows.RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("https://github.com/yoi102/Simple3DViewer") { UseShellExecute = true });
        }
    }
}
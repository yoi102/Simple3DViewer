using Microsoft.Extensions.DependencyInjection;
using Simple3DViewer.Shared.Services;
using Simple3DViewer.wpf.Services;
using System.IO;
using System.Runtime.Loader;
using System.Windows;

namespace Simple3DViewer.wpf
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static readonly string OdaDir = Path.Combine(AppContext.BaseDirectory, "OdaDlls");

        public App()
        {
            string lang = System.Globalization.CultureInfo.CurrentCulture.Name;
            var culture = new System.Globalization.CultureInfo(lang);
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;
            I18NExtension.Culture = culture;
            Services = ConfigureServices();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            AssemblyLoadContext.Default.Resolving += (ctx, asmName) =>
            {
                string path = Path.Combine(OdaDir, asmName.Name + ".dll");
                return File.Exists(path) ? ctx.LoadFromAssemblyPath(path) : null;
            };

            OdaService.Initialize();
        }

        public IServiceProvider Services { get; }

        /// <summary>
        /// Gets the current <see cref="App"/> instance in use
        /// </summary>
        public new static App Current => (App)Application.Current;

        /// <summary>
        /// Configures the services for the application.
        /// </summary>
        private static IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();

            services.AddSingleton<MainWindowViewModel>();
            services.AddSingleton<IWindowTrackService>(new WindowTrackService());
            services.AddSingleton<IDialogService, DialogService>();
            services.AddSingleton<IThemeSettingService, ThemeSettingService>();
            services.AddSingleton<ICultureSettingService, CultureSettingService>();

            return services.BuildServiceProvider();
        }
    }
}
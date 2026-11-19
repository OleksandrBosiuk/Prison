using Microsoft.Extensions.DependencyInjection;
using Prison.Core;
using System.Windows;

namespace Prison.WpfApp
{
    public partial class App : Application
    {
        private ServiceProvider? _serviceProvider;

        public const string DefaultConnectionString =
            @"Server=(localdb)\MSSQLLocalDB;Database=PrisonBlock13Db;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;";

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var services = new ServiceCollection();

            services.AddSingleton<JsonWorldStorage>();
            services.AddSingleton<XmlWorldStorage>();
            services.AddSingleton(_ => new SqlWorldStorage(DefaultConnectionString));

            services.AddSingleton<IWorldStorage>(sp => sp.GetRequiredService<SqlWorldStorage>());

            services.AddTransient<MainViewModel>();
            services.AddTransient<MainWindow>();

            _serviceProvider = services.BuildServiceProvider();

            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _serviceProvider?.Dispose();
            base.OnExit(e);
        }
    }
}

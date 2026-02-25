using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using HospitalManagementAvolonia.Data;
using HospitalManagementAvolonia.Services;
using HospitalManagementAvolonia.ViewModels;

namespace HospitalManagementAvolonia;

public partial class App : Application
{
    public static IServiceProvider? Services { get; private set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        Services = services.BuildServiceProvider();

        // Initialize DB asynchronously without blocking UI thread fully.
        var db = Services.GetRequiredService<IDatabaseService>();
        _ = db.InitializeDatabaseAsync();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindow = new MainWindow();
            // We lazily fire and forget the async initialization, 
            // the UI will render and load data when ready.
            _ = mainWindow.InitializeAsync();
            desktop.MainWindow = mainWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // Database
        services.AddSingleton<IDatabaseService>(new DatabaseManager("hospital.db"));

        // Business Services
        services.AddSingleton<IPatientService, PatientService>();
        services.AddSingleton<IDoctorService, DoctorService>();
        services.AddSingleton<IAppointmentService, AppointmentService>();

        // ViewModels
        services.AddTransient<MainViewModel>();
    }
}

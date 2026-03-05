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

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindow = new MainWindow();
            desktop.MainWindow = mainWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // Database — Singleton: tek bir DB bağlantısı yeterli
        services.AddSingleton<IDatabaseService>(new DatabaseManager("hospital.db"));

        // Business Services — Singleton: state tutuyorlar (cache/dictionary)
        services.AddSingleton<IPatientService, PatientService>();
        services.AddSingleton<IDoctorService, DoctorService>();
        services.AddSingleton<IAppointmentService, AppointmentService>();
        services.AddSingleton<IDepartmentService, DepartmentService>();
        services.AddSingleton<IEmergencyService, EmergencyService>();
        services.AddSingleton<IUndoService, UndoService>();

        // MainViewModel — Transient: her seferinde taze instance
        // ✅ IDatabaseService artık MainViewModel'e de inject ediliyor
        services.AddTransient<MainViewModel>();
    }
}

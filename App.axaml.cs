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

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        Services = services.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.MainWindow = new MainWindow();

        base.OnFrameworkInitializationCompleted();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // ── Infrastructure ────────────────────────────────────────────────────
        services.AddSingleton<IDatabaseService>(new DatabaseManager("hospital.db"));

        // ── Navigation ────────────────────────────────────────────────────────
        // ✅ NEW: NavigationService registered as Singleton
        services.AddSingleton<NavigationService>();
        services.AddSingleton<INavigationService>(sp => sp.GetRequiredService<NavigationService>());

        // ── Domain Services ───────────────────────────────────────────────────
        services.AddSingleton<IDepartmentService,  DepartmentService>();
        services.AddSingleton<IPatientService,     PatientService>();
        services.AddSingleton<IDoctorService,      DoctorService>();
        services.AddSingleton<IAppointmentService, AppointmentService>();
        services.AddSingleton<IEmergencyService,   EmergencyService>();
        services.AddSingleton<IUndoService,        UndoService>();

        // ✅ NEW: Services that previously were bypassed via IDatabaseService in ViewModels
        services.AddSingleton<IBillingService,      BillingService>();
        services.AddSingleton<IPrescriptionService, PrescriptionService>();
        services.AddSingleton<IShiftService,        ShiftService>();

        // ── Presentation ──────────────────────────────────────────────────────
        services.AddTransient<MainViewModel>();
    }
}

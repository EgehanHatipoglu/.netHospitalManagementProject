using FluentAssertions;
using HospitalManagementAvolonia.Data;
using HospitalManagementAvolonia.Models;
using HospitalManagementAvolonia.Services;
using Moq;

namespace HospitalManagementAvolonia.Tests.Services;

public class AppointmentServiceTests
{
    private readonly Mock<IDatabaseService> _mockDb;
    private readonly Mock<IPatientService> _mockPatientService;
    private readonly Mock<IDoctorService> _mockDoctorService;
    private readonly AppointmentService _service;

    private readonly Patient _patient;
    private readonly Doctor _doctor;
    private readonly Doctor _doctor2;

    public AppointmentServiceTests()
    {
        _mockDb = new Mock<IDatabaseService>();
        _mockPatientService = new Mock<IPatientService>();
        _mockDoctorService = new Mock<IDoctorService>();

        _mockDb.Setup(db => db.LoadAppointmentsAsync())
            .ReturnsAsync(new List<(int, int, int, string, string, string)>());

        _patient = TestHelpers.CreatePatient(1);
        _doctor = TestHelpers.CreateDoctor(1, "Mehmet", "Öz");
        _doctor2 = TestHelpers.CreateDoctor(2, "Ayşe", "Kara");

        _service = new AppointmentService(_mockDb.Object, _mockPatientService.Object, _mockDoctorService.Object);
    }

    // ============ HAS CONFLICT ============

    [Fact]
    public async Task HasConflict_SameDoctorOverlappingTime_ShouldReturnTrue()
    {
        await _service.InitializeAsync();

        var dt = new DateTime(2026, 3, 5, 10, 0, 0);
        await _service.CreateAppointmentAsync(_patient, _doctor, dt);

        // Same doctor, 10 minutes later (within the appointment duration)
        _service.HasConflict(_doctor.Id, dt.AddMinutes(10)).Should().BeTrue();
    }

    [Fact]
    public async Task HasConflict_SameDoctorNonOverlappingTime_ShouldReturnFalse()
    {
        await _service.InitializeAsync();

        var dt = new DateTime(2026, 3, 5, 10, 0, 0);
        await _service.CreateAppointmentAsync(_patient, _doctor, dt);

        // After the appointment ends (AppointmentDuration is 19 min)
        _service.HasConflict(_doctor.Id, dt.AddMinutes(30)).Should().BeFalse();
    }

    [Fact]
    public async Task HasConflict_DifferentDoctorSameTime_ShouldReturnFalse()
    {
        await _service.InitializeAsync();

        var dt = new DateTime(2026, 3, 5, 10, 0, 0);
        await _service.CreateAppointmentAsync(_patient, _doctor, dt);

        _service.HasConflict(_doctor2.Id, dt).Should().BeFalse();
    }

    // ============ HAS PATIENT CONFLICT ============

    [Fact]
    public async Task HasPatientConflict_SamePatientOverlappingTime_ShouldReturnTrue()
    {
        await _service.InitializeAsync();

        var dt = new DateTime(2026, 3, 5, 10, 0, 0);
        await _service.CreateAppointmentAsync(_patient, _doctor, dt);

        _service.HasPatientConflict(_patient.Id, dt.AddMinutes(5)).Should().BeTrue();
    }

    [Fact]
    public async Task HasPatientConflict_SamePatientNonOverlapping_ShouldReturnFalse()
    {
        await _service.InitializeAsync();

        var dt = new DateTime(2026, 3, 5, 10, 0, 0);
        await _service.CreateAppointmentAsync(_patient, _doctor, dt);

        _service.HasPatientConflict(_patient.Id, dt.AddHours(1)).Should().BeFalse();
    }

    // ============ CREATE ============

    [Fact]
    public async Task CreateAppointmentAsync_ShouldSaveAndReturnAppointment()
    {
        await _service.InitializeAsync();

        var dt = new DateTime(2026, 3, 5, 10, 0, 0);
        var app = await _service.CreateAppointmentAsync(_patient, _doctor, dt);

        app.Should().NotBeNull();
        app.Patient.Should().BeSameAs(_patient);
        app.Doctor.Should().BeSameAs(_doctor);
        app.Start.Should().Be(dt);

        _mockDb.Verify(db => db.SaveAppointmentAsync(It.IsAny<Appointment>()), Times.Once);
    }

    // ============ DELETE ============

    [Fact]
    public async Task DeleteAppointmentAsync_ShouldRemoveAndCallDb()
    {
        await _service.InitializeAsync();

        var app = await _service.CreateAppointmentAsync(_patient, _doctor, DateTime.Now);
        await _service.DeleteAppointmentAsync(app.Id);

        var all = await _service.GetAllAppointmentsAsync();
        all.Should().BeEmpty();

        _mockDb.Verify(db => db.DeleteAppointmentAsync(app.Id), Times.Once);
    }

    // ============ GET ALL ============

    [Fact]
    public async Task GetAllAppointmentsAsync_ShouldReturnAllCreated()
    {
        await _service.InitializeAsync();

        await _service.CreateAppointmentAsync(_patient, _doctor, new DateTime(2026, 3, 5, 10, 0, 0));
        await _service.CreateAppointmentAsync(_patient, _doctor2, new DateTime(2026, 3, 5, 11, 0, 0));

        var all = await _service.GetAllAppointmentsAsync();
        all.Should().HaveCount(2);
    }
}

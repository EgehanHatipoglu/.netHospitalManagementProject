using FluentAssertions;
using HospitalManagementAvolonia.Data;
using HospitalManagementAvolonia.Models;
using HospitalManagementAvolonia.Services;
using Moq;

namespace HospitalManagementAvolonia.Tests.Services;

public class PatientServiceTests
{
    private readonly Mock<IDatabaseService> _mockDb;
    private readonly PatientService _service;

    public PatientServiceTests()
    {
        _mockDb = new Mock<IDatabaseService>();
        // By default, LoadPatientsAsync returns empty list for clean state
        _mockDb.Setup(db => db.LoadPatientsAsync())
            .ReturnsAsync(new List<(int, string, string, string, string, string)>());

        _service = new PatientService(_mockDb.Object);
    }

    // ============ ADD ============

    [Fact]
    public async Task AddPatientAsync_ShouldAddToCollectionAndCallSave()
    {
        await _service.InitializeAsync();

        var patient = await _service.AddPatientAsync("Ali", "Yılmaz", "12345678901", "5551234567", new DateTime(1990, 1, 1));

        patient.Should().NotBeNull();
        patient.FirstName.Should().Be("Ali");
        patient.LastName.Should().Be("Yılmaz");

        _mockDb.Verify(db => db.SavePatientAsync(It.Is<Patient>(p => p.FirstName == "Ali")), Times.Once);
    }

    [Fact]
    public async Task AddPatientAsync_ShouldBeSearchableViaBSTAndAVL()
    {
        await _service.InitializeAsync();

        await _service.AddPatientAsync("Ali", "Yılmaz", "123", "555", new DateTime(1990, 1, 1));

        _service.SearchBST("Ali", "Yılmaz").Should().NotBeNull();
        _service.SearchAVL("Ali", "Yılmaz").Should().NotBeNull();
    }

    // ============ GET BY ID ============

    [Fact]
    public async Task GetPatientByIdAsync_Existing_ShouldReturnCorrectPatient()
    {
        await _service.InitializeAsync();
        var patient = await _service.AddPatientAsync("Ayşe", "Demir", "999", "555", DateTime.Today);

        var found = await _service.GetPatientByIdAsync(patient.Id);
        found.Should().NotBeNull();
        found!.FirstName.Should().Be("Ayşe");
    }

    [Fact]
    public async Task GetPatientByIdAsync_NonExisting_ShouldReturnNull()
    {
        await _service.InitializeAsync();
        var result = await _service.GetPatientByIdAsync(9999);
        result.Should().BeNull();
    }

    // ============ DELETE ============

    [Fact]
    public async Task DeletePatientAsync_ShouldRemoveFromCollectionAndCallDelete()
    {
        await _service.InitializeAsync();
        var patient = await _service.AddPatientAsync("Ali", "Yılmaz", "123", "555", DateTime.Today);

        await _service.DeletePatientAsync(patient.Id);

        var found = await _service.GetPatientByIdAsync(patient.Id);
        found.Should().BeNull();

        _mockDb.Verify(db => db.DeletePatientAsync(patient.Id), Times.Once);
    }

    [Fact]
    public async Task DeletePatientAsync_ShouldRemoveFromBSTAndAVL()
    {
        await _service.InitializeAsync();
        var patient = await _service.AddPatientAsync("Ali", "Yılmaz", "123", "555", DateTime.Today);

        await _service.DeletePatientAsync(patient.Id);

        _service.SearchBST("Ali", "Yılmaz").Should().BeNull();
        _service.SearchAVL("Ali", "Yılmaz").Should().BeNull();
    }

    // ============ SEARCH ============

    [Fact]
    public async Task SearchPatientsAsync_ByName_ShouldReturnMatches()
    {
        await _service.InitializeAsync();
        await _service.AddPatientAsync("Ali", "Yılmaz", "111", "555", DateTime.Today);
        await _service.AddPatientAsync("Veli", "Demir", "222", "555", DateTime.Today);

        var results = (await _service.SearchPatientsAsync("Ali")).ToList();
        results.Should().HaveCount(1);
        results[0].FirstName.Should().Be("Ali");
    }

    [Fact]
    public async Task SearchPatientsAsync_ByNationalId_ShouldReturnMatch()
    {
        await _service.InitializeAsync();
        await _service.AddPatientAsync("Ali", "Yılmaz", "12345678901", "555", DateTime.Today);

        var results = (await _service.SearchPatientsAsync("12345678901")).ToList();
        results.Should().HaveCount(1);
        results[0].NationalId.Should().Be("12345678901");
    }

    [Fact]
    public async Task SearchPatientsAsync_EmptyQuery_ShouldReturnAll()
    {
        await _service.InitializeAsync();
        await _service.AddPatientAsync("Ali", "Yılmaz", "111", "555", DateTime.Today);
        await _service.AddPatientAsync("Veli", "Demir", "222", "555", DateTime.Today);

        var results = (await _service.SearchPatientsAsync("")).ToList();
        results.Should().HaveCount(2);
    }

    // ============ INIT IDEMPOTENCY ============

    [Fact]
    public async Task InitializeAsync_CalledTwice_ShouldOnlyLoadOnce()
    {
        await _service.InitializeAsync();
        await _service.InitializeAsync();

        _mockDb.Verify(db => db.LoadPatientsAsync(), Times.Once);
    }
}

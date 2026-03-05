using FluentAssertions;
using HospitalManagementAvolonia.Models;
using HospitalManagementAvolonia.Services;
using HospitalManagementAvolonia.ViewModels;
using Moq;

namespace HospitalManagementAvolonia.Tests.ViewModels;

public class PatientViewModelTests
{
    private readonly Mock<IPatientService> _mockService;
    private readonly PatientViewModel _vm;

    public PatientViewModelTests()
    {
        _mockService = new Mock<IPatientService>();
        _mockService.Setup(s => s.SearchPatientsAsync(It.IsAny<string>()))
            .ReturnsAsync(new List<Patient>());

        _vm = new PatientViewModel(_mockService.Object);
    }

    // ============ REGISTER ============

    [Fact]
    public async Task RegisterPatientAsync_EmptyFirstName_ShouldNotCallService()
    {
        _vm.NewFirstName = "";
        _vm.NewLastName = "Yılmaz";

        await _vm.RegisterPatientAsync();

        _mockService.Verify(
            s => s.AddPatientAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>()),
            Times.Never);
    }

    [Fact]
    public async Task RegisterPatientAsync_EmptyLastName_ShouldNotCallService()
    {
        _vm.NewFirstName = "Ali";
        _vm.NewLastName = "";

        await _vm.RegisterPatientAsync();

        _mockService.Verify(
            s => s.AddPatientAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>()),
            Times.Never);
    }

    [Fact]
    public async Task RegisterPatientAsync_ValidData_ShouldCallAddPatientAsync()
    {
        _mockService.Setup(s => s.AddPatientAsync("Ali", "Yılmaz", "123", "555", It.IsAny<DateTime>()))
            .ReturnsAsync(TestHelpers.CreatePatient(1, "Ali", "Yılmaz", "123", "555"));

        _vm.NewFirstName = "Ali";
        _vm.NewLastName = "Yılmaz";
        _vm.NewNationalId = "123";
        _vm.NewPhone = "555";

        await _vm.RegisterPatientAsync();

        _mockService.Verify(
            s => s.AddPatientAsync("Ali", "Yılmaz", "123", "555", It.IsAny<DateTime>()),
            Times.Once);
    }

    // ============ DELETE ============

    [Fact]
    public async Task DeletePatientAsync_NoSelection_ShouldNotThrow()
    {
        _vm.SelectedPatient = null;

        var act = () => _vm.DeletePatientAsync();
        await act.Should().NotThrowAsync();

        _mockService.Verify(s => s.DeletePatientAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task DeletePatientAsync_WithSelection_ShouldCallService()
    {
        _vm.SelectedPatient = TestHelpers.CreatePatient(1);

        await _vm.DeletePatientAsync();

        _mockService.Verify(s => s.DeletePatientAsync(1), Times.Once);
    }
}

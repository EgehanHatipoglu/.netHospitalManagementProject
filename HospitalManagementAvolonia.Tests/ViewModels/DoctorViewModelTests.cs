using FluentAssertions;
using HospitalManagementAvolonia.Models;
using HospitalManagementAvolonia.Services;
using HospitalManagementAvolonia.ViewModels;
using Moq;

namespace HospitalManagementAvolonia.Tests.ViewModels;

public class DoctorViewModelTests
{
    private readonly Mock<IDoctorService> _mockService;
    private readonly DoctorViewModel _vm;

    public DoctorViewModelTests()
    {
        _mockService = new Mock<IDoctorService>();
        _mockService.Setup(s => s.SearchDoctorsAsync(It.IsAny<string>()))
            .ReturnsAsync(Enumerable.Empty<Doctor>());
        _mockService.Setup(s => s.GetAllDoctorsAsync())
            .ReturnsAsync(new List<Doctor>());
        _mockService.Setup(s => s.GetDepartmentsAsync())
            .ReturnsAsync(new List<Department>());

        _vm = new DoctorViewModel(_mockService.Object);
    }

    // ============ REGISTER ============

    [Fact]
    public async Task RegisterDoctorAsync_EmptyFirstName_ShouldNotCallService()
    {
        _vm.NewFirstName = "";
        _vm.NewLastName = "Öz";
        _vm.SelectedDepartmentForNew = TestHelpers.CreateDepartment();

        await _vm.RegisterDoctorAsync();

        _mockService.Verify(
            s => s.AddDoctorAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()),
            Times.Never);
        _vm.ValidationMessage.Should().Contain("Ad");
    }

    [Fact]
    public async Task RegisterDoctorAsync_EmptyLastName_ShouldNotCallService()
    {
        _vm.NewFirstName = "Mehmet";
        _vm.NewLastName = "";
        _vm.SelectedDepartmentForNew = TestHelpers.CreateDepartment();

        await _vm.RegisterDoctorAsync();

        _mockService.Verify(
            s => s.AddDoctorAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()),
            Times.Never);
        _vm.ValidationMessage.Should().Contain("Soyad");
    }

    [Fact]
    public async Task RegisterDoctorAsync_NoDepartment_ShouldNotCallService()
    {
        _vm.NewFirstName = "Mehmet";
        _vm.NewLastName = "Öz";
        _vm.SelectedDepartmentForNew = null;

        await _vm.RegisterDoctorAsync();

        _mockService.Verify(
            s => s.AddDoctorAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()),
            Times.Never);
        _vm.ValidationMessage.Should().Contain("Bölüm");
    }

    [Fact]
    public async Task RegisterDoctorAsync_ValidData_ShouldCallServiceAndResetForm()
    {
        var dept = TestHelpers.CreateDepartment(1, "Kardiyoloji");
        _mockService.Setup(s => s.AddDoctorAsync("Mehmet", "Öz", 1, "555"))
            .ReturnsAsync(TestHelpers.CreateDoctor(1, "Mehmet", "Öz", dept, "555"));

        _vm.NewFirstName = "Mehmet";
        _vm.NewLastName = "Öz";
        _vm.NewPhone = "555";
        _vm.SelectedDepartmentForNew = dept;

        await _vm.RegisterDoctorAsync();

        _mockService.Verify(s => s.AddDoctorAsync("Mehmet", "Öz", 1, "555"), Times.Once);
        _vm.NewFirstName.Should().BeEmpty();
        _vm.NewLastName.Should().BeEmpty();
        _vm.NewPhone.Should().BeEmpty();
    }

    // ============ DELETE ============

    [Fact]
    public async Task DeleteDoctorAsync_NoSelection_ShouldNotCallService()
    {
        _vm.SelectedDoctor = null;

        await _vm.DeleteDoctorAsync();

        _mockService.Verify(s => s.DeleteDoctorAsync(It.IsAny<int>()), Times.Never);
        _vm.ValidationMessage.Should().Contain("seçin");
    }

    [Fact]
    public async Task DeleteDoctorAsync_WithSelection_ShouldCallService()
    {
        _vm.SelectedDoctor = TestHelpers.CreateDoctor(5);

        await _vm.DeleteDoctorAsync();

        _mockService.Verify(s => s.DeleteDoctorAsync(5), Times.Once);
    }

    // ============ REFERRAL NETWORK ============

    [Fact]
    public void AddReferral_SameDoctorSelected_ShouldNotCallService()
    {
        var doc = TestHelpers.CreateDoctor(1);
        _vm.SelectedDoctor = doc;
        _vm.SelectedReferralDoctor = doc;

        _vm.AddReferral();

        _mockService.Verify(s => s.AddReferral(It.IsAny<Doctor>(), It.IsAny<Doctor>()), Times.Never);
    }

    [Fact]
    public void AddReferral_TwoDifferentDoctors_ShouldCallService()
    {
        var doc1 = TestHelpers.CreateDoctor(1, "Ali");
        var doc2 = TestHelpers.CreateDoctor(2, "Veli");
        _vm.SelectedDoctor = doc1;
        _vm.SelectedReferralDoctor = doc2;

        _vm.AddReferral();

        _mockService.Verify(s => s.AddReferral(doc1, doc2), Times.Once);
    }

    [Fact]
    public void AddReferral_NullSelection_ShouldNotCallService()
    {
        _vm.SelectedDoctor = null;
        _vm.SelectedReferralDoctor = TestHelpers.CreateDoctor(2);

        _vm.AddReferral();

        _mockService.Verify(s => s.AddReferral(It.IsAny<Doctor>(), It.IsAny<Doctor>()), Times.Never);
    }

    [Fact]
    public void FindReferralPath_ShouldPopulateNetworkPath()
    {
        var doc1 = TestHelpers.CreateDoctor(1);
        var doc2 = TestHelpers.CreateDoctor(2);
        _vm.SelectedDoctor = doc1;
        _vm.SelectedReferralDoctor = doc2;

        _mockService.Setup(s => s.GetReferralPathBFS(1, 2))
            .Returns(new List<string> { "Sevk Ağı Bulundu (Mesafe: 1 adım):", "Dr. Ali ➔ Dr. Veli" });

        _vm.FindReferralPath();

        _vm.NetworkPath.Should().HaveCount(2);
    }

    [Fact]
    public void ShowFullNetwork_ShouldPopulateNetworkPath()
    {
        var doc = TestHelpers.CreateDoctor(1);
        _vm.SelectedDoctor = doc;

        _mockService.Setup(s => s.GetReferralNetworkDFS(1))
            .Returns(new List<string> { "Dr. Ali A", "Dr. Veli B" });

        _vm.ShowFullNetwork();

        _vm.NetworkPath.Should().HaveCount(2);
    }

    [Fact]
    public void ShowFullNetwork_NullSelection_ShouldNotThrow()
    {
        _vm.SelectedDoctor = null;

        var act = () => _vm.ShowFullNetwork();
        act.Should().NotThrow();
    }
}

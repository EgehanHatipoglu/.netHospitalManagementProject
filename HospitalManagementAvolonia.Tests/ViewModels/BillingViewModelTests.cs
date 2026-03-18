using FluentAssertions;
using HospitalManagementAvolonia.Models;
using HospitalManagementAvolonia.Services;
using HospitalManagementAvolonia.ViewModels;
using Moq;

namespace HospitalManagementAvolonia.Tests.ViewModels;

public class BillingViewModelTests
{
    private readonly Mock<IBillingService> _mockBillingService;
    private readonly BillingViewModel _vm;

    public BillingViewModelTests()
    {
        _mockBillingService = new Mock<IBillingService>();
        _mockBillingService.Setup(s => s.GetAllAsync())
            .ReturnsAsync(new List<Invoice>());
        _vm = new BillingViewModel(_mockBillingService.Object);
    }

    // ============ VALIDATION — MISSING FIELDS ============

    [Fact]
    public async Task CreateInvoiceAsync_NoAppointmentId_ShouldSetValidation()
    {
        _vm.AppointmentId = null;

        await _vm.CreateInvoiceAsync();

        _vm.ValidationMessage.Should().Contain("randevu");
        _mockBillingService.Verify(s => s.CreateInvoiceAsync(
            It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<decimal>(), It.IsAny<decimal>()), Times.Never);
    }

    [Fact]
    public async Task CreateInvoiceAsync_EmptyPatientName_ShouldSetValidation()
    {
        _vm.AppointmentId = 1;
        _vm.PatientName = "";

        await _vm.CreateInvoiceAsync();

        _vm.ValidationMessage.Should().Contain("Hasta");
        _mockBillingService.Verify(s => s.CreateInvoiceAsync(
            It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<decimal>(), It.IsAny<decimal>()), Times.Never);
    }

    [Fact]
    public async Task CreateInvoiceAsync_EmptyDoctorName_ShouldSetValidation()
    {
        _vm.AppointmentId = 1;
        _vm.PatientName = "Ali";
        _vm.DoctorName = "";

        await _vm.CreateInvoiceAsync();

        _vm.ValidationMessage.Should().Contain("Doktor");
    }

    // ============ VALIDATION — AMOUNT ============

    [Fact]
    public async Task CreateInvoiceAsync_NegativeAmount_ShouldSetValidation()
    {
        _vm.AppointmentId = 1;
        _vm.PatientName = "Ali";
        _vm.DoctorName = "Dr. X";
        _vm.BaseAmount = -100;

        await _vm.CreateInvoiceAsync();

        _vm.ValidationMessage.Should().Contain("Tutar");
        _mockBillingService.Verify(s => s.CreateInvoiceAsync(
            It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<decimal>(), It.IsAny<decimal>()), Times.Never);
    }

    [Fact]
    public async Task CreateInvoiceAsync_ZeroAmount_ShouldSetValidation()
    {
        _vm.AppointmentId = 1;
        _vm.PatientName = "Ali";
        _vm.DoctorName = "Dr. X";
        _vm.BaseAmount = 0;

        await _vm.CreateInvoiceAsync();

        _vm.ValidationMessage.Should().Contain("Tutar");
    }

    // ============ VALIDATION — INSURANCE ============

    [Fact]
    public async Task CreateInvoiceAsync_InsuranceOver100_ShouldSetValidation()
    {
        _vm.AppointmentId = 1;
        _vm.PatientName = "Ali";
        _vm.DoctorName = "Dr. X";
        _vm.BaseAmount = 500;
        _vm.InsuranceCoveragePercent = 150;

        await _vm.CreateInvoiceAsync();

        _vm.ValidationMessage.Should().Contain("Sigorta");
        _mockBillingService.Verify(s => s.CreateInvoiceAsync(
            It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<decimal>(), It.IsAny<decimal>()), Times.Never);
    }

    [Fact]
    public async Task CreateInvoiceAsync_NegativeInsurance_ShouldSetValidation()
    {
        _vm.AppointmentId = 1;
        _vm.PatientName = "Ali";
        _vm.DoctorName = "Dr. X";
        _vm.BaseAmount = 500;
        _vm.InsuranceCoveragePercent = -10;

        await _vm.CreateInvoiceAsync();

        _vm.ValidationMessage.Should().Contain("Sigorta");
    }

    // ============ VALID CREATION ============

    [Fact]
    public async Task CreateInvoiceAsync_ValidData_ShouldCallServiceAndAddToCollection()
    {
        var expectedInvoice = new Invoice(1, 1, "Ali Yılmaz", "Dr. Mehmet", DateTime.Now, 500, 20);
        _mockBillingService.Setup(s => s.CreateInvoiceAsync(1, "Ali Yılmaz", "Dr. Mehmet", 500m, 20m))
            .ReturnsAsync(expectedInvoice);

        _vm.AppointmentId = 1;
        _vm.PatientName = "Ali Yılmaz";
        _vm.DoctorName = "Dr. Mehmet";
        _vm.BaseAmount = 500;
        _vm.InsuranceCoveragePercent = 20;

        await _vm.CreateInvoiceAsync();

        _vm.ValidationMessage.Should().BeEmpty();
        _vm.Invoices.Should().HaveCount(1);
        _vm.Invoices[0].PatientName.Should().Be("Ali Yılmaz");
        _mockBillingService.Verify(s => s.CreateInvoiceAsync(1, "Ali Yılmaz", "Dr. Mehmet", 500m, 20m), Times.Once);
    }

    // ============ MARK AS PAID ============

    [Fact]
    public async Task MarkAsPaidAsync_AlreadyPaid_ShouldSetValidation()
    {
        var invoice = new Invoice(1, 1, "Ali", "Dr. X", DateTime.Now, 500, 10) { Status = "Paid" };
        _vm.Invoices.Add(invoice);
        _vm.SelectedInvoice = invoice;

        await _vm.MarkAsPaidAsync();

        _vm.ValidationMessage.Should().Contain("zaten");
    }

    [Fact]
    public async Task MarkAsPaidAsync_NoSelection_ShouldSetValidation()
    {
        _vm.SelectedInvoice = null;

        await _vm.MarkAsPaidAsync();

        _vm.ValidationMessage.Should().Contain("seçilmedi");
    }

    [Fact]
    public async Task MarkAsPaidAsync_PendingInvoice_ShouldCallService()
    {
        var invoice = new Invoice(1, 1, "Ali", "Dr. X", DateTime.Now, 500, 10);
        _vm.Invoices.Add(invoice);
        _vm.SelectedInvoice = invoice;

        await _vm.MarkAsPaidAsync();

        _mockBillingService.Verify(s => s.MarkAsPaidAsync(1), Times.Once);
    }
}

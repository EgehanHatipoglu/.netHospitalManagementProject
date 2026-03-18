using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HospitalManagementAvolonia.Models;
using HospitalManagementAvolonia.Services;

namespace HospitalManagementAvolonia.ViewModels
{
    public partial class BillingViewModel : ViewModelBase
    {
        // ✅ FIX: Depends on IBillingService, NOT IDatabaseService
        private readonly IBillingService _billingService;

        public ObservableCollection<Invoice> Invoices { get; } = new();

        [ObservableProperty] private int? _appointmentId;
        [ObservableProperty] private string _patientName = "";
        [ObservableProperty] private string _doctorName = "";
        [ObservableProperty] private decimal _baseAmount = 500m;
        [ObservableProperty] private decimal _insuranceCoveragePercent = 0m;
        [ObservableProperty] private Invoice? _selectedInvoice;
        [ObservableProperty] private decimal _totalRevenue;
        [ObservableProperty] private int _pendingCount;
        [ObservableProperty] private int _paidCount;
        [ObservableProperty] private string _validationMessage = "";

        public BillingViewModel(IBillingService billingService)
        {
            _billingService = billingService;
        }

        [RelayCommand]
        public async Task RefreshDataAsync()
        {
            var all = await _billingService.GetAllAsync();
            Invoices.Clear();
            foreach (var inv in all) Invoices.Add(inv);
            RecalculateSummary();
        }

        [RelayCommand]
        public async Task CreateInvoiceAsync()
        {
            ValidationMessage = "";

            if (!AppointmentId.HasValue || AppointmentId <= 0)
            { ValidationMessage = "⚠ Geçerli randevu ID girin!"; return; }
            if (string.IsNullOrWhiteSpace(PatientName))
            { ValidationMessage = "⚠ Hasta adı boş olamaz!"; return; }
            if (string.IsNullOrWhiteSpace(DoctorName))
            { ValidationMessage = "⚠ Doktor adı boş olamaz!"; return; }
            if (BaseAmount <= 0)
            { ValidationMessage = "⚠ Tutar 0'dan büyük olmalı!"; return; }
            if (InsuranceCoveragePercent < 0 || InsuranceCoveragePercent > 100)
            { ValidationMessage = "⚠ Sigorta yüzdesi 0-100 arasında olmalı!"; return; }

            var inv = await _billingService.CreateInvoiceAsync(
                AppointmentId.Value, PatientName.Trim(), DoctorName.Trim(),
                BaseAmount, InsuranceCoveragePercent);

            Invoices.Insert(0, inv);
            RecalculateSummary();

            AppointmentId = null;
            PatientName = "";
            DoctorName = "";
            BaseAmount = 500m;
            InsuranceCoveragePercent = 0m;

            ToastService.Instance.Success(
                $"✓ Fatura #{inv.Id} oluşturuldu. Hasta payı: ₺{inv.PatientPayment:F2}");
        }

        [RelayCommand]
        public async Task MarkAsPaidAsync()
        {
            if (SelectedInvoice == null)
            { ValidationMessage = "⚠ Fatura seçilmedi!"; return; }
            if (SelectedInvoice.Status == "Paid")
            { ValidationMessage = "⚠ Bu fatura zaten ödendi."; return; }

            await _billingService.MarkAsPaidAsync(SelectedInvoice.Id);

            // Refresh the item in the collection so UI updates
            var idx = Invoices.IndexOf(SelectedInvoice);
            if (idx >= 0) { Invoices.Remove(SelectedInvoice); Invoices.Insert(idx, SelectedInvoice); }

            RecalculateSummary();
            ToastService.Instance.Success($"✓ Fatura #{SelectedInvoice.Id} ödendi olarak işaretlendi.");
        }

        [RelayCommand]
        public async Task CancelInvoiceAsync()
        {
            if (SelectedInvoice == null || SelectedInvoice.Status == "Cancelled") return;
            await _billingService.CancelAsync(SelectedInvoice.Id);
            RecalculateSummary();
            ToastService.Instance.Warning($"Fatura #{SelectedInvoice.Id} iptal edildi.");
        }

        private void RecalculateSummary()
        {
            TotalRevenue = Invoices.Where(i => i.Status == "Paid").Sum(i => i.PatientPayment);
            PendingCount = Invoices.Count(i => i.Status == "Pending");
            PaidCount    = Invoices.Count(i => i.Status == "Paid");
        }
    }
}

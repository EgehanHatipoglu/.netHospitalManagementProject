using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HospitalManagementAvolonia.Data;
using HospitalManagementAvolonia.Models;
using HospitalManagementAvolonia.Services;

namespace HospitalManagementAvolonia.ViewModels
{
    public partial class BillingViewModel : ViewModelBase
    {
        private readonly IDatabaseService _db;

        public ObservableCollection<Invoice> Invoices { get; } = new();

        // --- Yeni Fatura Alanları ---
        [ObservableProperty] private int? _appointmentId;
        [ObservableProperty] private string _patientName = "";
        [ObservableProperty] private string _doctorName = "";
        [ObservableProperty] private decimal _baseAmount = 500m;
        [ObservableProperty] private decimal _insuranceCoveragePercent = 0m;

        // --- Seçili Fatura ---
        [ObservableProperty] private Invoice? _selectedInvoice;

        // --- Hesaplanan Özetler ---
        [ObservableProperty] private decimal _totalRevenue;
        [ObservableProperty] private int _pendingCount;
        [ObservableProperty] private int _paidCount;

        [ObservableProperty] private string _validationMessage = "";

        private int _invoiceIdCounter = 0;

        public BillingViewModel(IDatabaseService db)
        {
            _db = db;
        }

        [RelayCommand]
        public async Task RefreshDataAsync()
        {
            var rows = await _db.LoadInvoicesAsync();
            Invoices.Clear();
            _invoiceIdCounter = 0;

            foreach (var (id, appId, pname, dname, date, baseAmt, insPct, status) in rows)
            {
                var inv = new Invoice(id, appId, pname, dname, DateTime.Parse(date),
                    (decimal)baseAmt, (decimal)insPct)
                {
                    Status = status
                };
                Invoices.Add(inv);
                if (id > _invoiceIdCounter) _invoiceIdCounter = id;
            }

            RecalculateSummary();
        }

        [RelayCommand]
        public async Task CreateInvoiceAsync()
        {
            ValidationMessage = "";

            if (!AppointmentId.HasValue || AppointmentId <= 0)
            {
                ValidationMessage = "⚠ Geçerli randevu ID girin!";
                return;
            }
            if (string.IsNullOrWhiteSpace(PatientName))
            {
                ValidationMessage = "⚠ Hasta adı boş olamaz!";
                return;
            }
            if (string.IsNullOrWhiteSpace(DoctorName))
            {
                ValidationMessage = "⚠ Doktor adı boş olamaz!";
                return;
            }
            if (BaseAmount <= 0)
            {
                ValidationMessage = "⚠ Tutar 0'dan büyük olmalı!";
                return;
            }
            if (InsuranceCoveragePercent < 0 || InsuranceCoveragePercent > 100)
            {
                ValidationMessage = "⚠ Sigorta yüzdesi 0-100 arasında olmalı!";
                return;
            }

            _invoiceIdCounter++;
            var inv = new Invoice(
                _invoiceIdCounter,
                AppointmentId.Value,
                PatientName.Trim(),
                DoctorName.Trim(),
                DateTime.Now,
                BaseAmount,
                InsuranceCoveragePercent
            );

            await _db.SaveInvoiceAsync(inv);
            Invoices.Insert(0, inv);
            RecalculateSummary();

            // Formu temizle
            AppointmentId = null;
            PatientName = "";
            DoctorName = "";
            BaseAmount = 500m;
            InsuranceCoveragePercent = 0m;

            ToastService.Instance.Success($"✓ Fatura #{inv.Id} oluşturuldu. Hasta payı: ₺{inv.PatientPayment:F2}");
        }

        [RelayCommand]
        public async Task MarkAsPaidAsync()
        {
            if (SelectedInvoice == null)
            {
                ValidationMessage = "⚠ Fatura seçilmedi!";
                return;
            }
            if (SelectedInvoice.Status == "Paid")
            {
                ValidationMessage = "⚠ Bu fatura zaten ödendi.";
                return;
            }

            SelectedInvoice.Status = "Paid";
            await _db.SaveInvoiceAsync(SelectedInvoice);
            RecalculateSummary();

            // ObservableCollection'ı yenile
            var idx = Invoices.IndexOf(SelectedInvoice);
            if (idx >= 0)
            {
                Invoices.Remove(SelectedInvoice);
                Invoices.Insert(idx, SelectedInvoice);
            }

            ToastService.Instance.Success($"✓ Fatura #{SelectedInvoice.Id} ödendi olarak işaretlendi.");
        }

        [RelayCommand]
        public async Task CancelInvoiceAsync()
        {
            if (SelectedInvoice == null || SelectedInvoice.Status == "Cancelled") return;

            SelectedInvoice.Status = "Cancelled";
            await _db.SaveInvoiceAsync(SelectedInvoice);
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

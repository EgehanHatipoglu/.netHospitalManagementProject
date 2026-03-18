using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HospitalManagementAvolonia.Models;
using HospitalManagementAvolonia.Services;

namespace HospitalManagementAvolonia.ViewModels
{
    public partial class PrescriptionViewModel : ViewModelBase
    {
        // ✅ FIX: Depends on IPrescriptionService, NOT IDatabaseService
        private readonly IPrescriptionService _prescriptionService;
        private readonly IPatientService      _patientService;
        private readonly IDoctorService       _doctorService;

        public ObservableCollection<Prescription>    Prescriptions { get; } = new();
        public ObservableCollection<Drug>            AvailableDrugs { get; } = new();
        public ObservableCollection<PrescriptionItem> CurrentItems { get; } = new();

        [ObservableProperty] private int?   _patientId;
        [ObservableProperty] private int?   _doctorId;
        [ObservableProperty] private string _patientName = "";
        [ObservableProperty] private string _doctorName  = "";

        [ObservableProperty] private string _newDrugName      = "";
        [ObservableProperty] private string _newDrugUnit      = "";
        [ObservableProperty] private int    _newDrugStock     = 100;
        [ObservableProperty] private int    _newDrugThreshold = 10;

        [ObservableProperty] private Drug?  _selectedDrug;
        [ObservableProperty] private int    _quantity = 1;
        [ObservableProperty] private string _dosage   = "";
        [ObservableProperty] private string _validationMessage = "";

        public PrescriptionViewModel(IPrescriptionService prescriptionService,
            IPatientService patientService, IDoctorService doctorService)
        {
            _prescriptionService = prescriptionService;
            _patientService      = patientService;
            _doctorService       = doctorService;
        }

        [RelayCommand]
        public async Task RefreshDataAsync()
        {
            var drugs = await _prescriptionService.GetAllDrugsAsync();
            AvailableDrugs.Clear();
            foreach (var d in drugs) AvailableDrugs.Add(d);

            var rxList = await _prescriptionService.GetAllPrescriptionsAsync();
            Prescriptions.Clear();
            foreach (var rx in rxList) Prescriptions.Add(rx);
        }

        partial void OnPatientIdChanged(int? value)
        {
            if (value.HasValue) _ = LookupPatientAsync(value.Value);
        }

        partial void OnDoctorIdChanged(int? value)
        {
            if (value.HasValue) _ = LookupDoctorAsync(value.Value);
        }

        private async Task LookupPatientAsync(int id)
        {
            var p = await _patientService.GetPatientByIdAsync(id);
            PatientName = p?.FullName ?? "Hasta bulunamadı";
        }

        private async Task LookupDoctorAsync(int id)
        {
            var d = await _doctorService.GetDoctorByIdAsync(id);
            DoctorName = d?.FullName ?? "Doktor bulunamadı";
        }

        [RelayCommand]
        public async Task AddDrugAsync()
        {
            if (string.IsNullOrWhiteSpace(NewDrugName) || string.IsNullOrWhiteSpace(NewDrugUnit))
            { ValidationMessage = "⚠ İlaç adı ve birimi zorunludur."; return; }

            var drug = await _prescriptionService.AddDrugAsync(
                NewDrugName, NewDrugUnit, NewDrugStock, NewDrugThreshold);

            AvailableDrugs.Add(drug);
            NewDrugName = ""; NewDrugUnit = ""; NewDrugStock = 100; NewDrugThreshold = 10;
            ToastService.Instance.Success($"✓ {drug.Name} kataloga eklendi.");
        }

        [RelayCommand]
        public void StartPrescription()
        {
            CurrentItems.Clear();
            PatientId = null; PatientName = "";
            DoctorId  = null; DoctorName  = "";
            ValidationMessage = "Yeni reçete başlatıldı.";
        }

        [RelayCommand]
        public void AddDrugToCurrentPrescription()
        {
            ValidationMessage = "";
            if (SelectedDrug == null)
            { ValidationMessage = "⚠ İlaç seçilmedi!"; return; }
            if (Quantity <= 0)
            { ValidationMessage = "⚠ Miktar 0'dan büyük olmalı!"; return; }
            if (SelectedDrug.Stock < Quantity)
            { ValidationMessage = $"⚠ Stok yetersiz! Mevcut: {SelectedDrug.Stock}"; return; }

            var existing = CurrentItems.FirstOrDefault(i => i.Drug.Id == SelectedDrug.Id);
            if (existing != null) CurrentItems.Remove(existing);

            CurrentItems.Add(new PrescriptionItem(SelectedDrug, Quantity, Dosage.Trim()));
            SelectedDrug = null; Quantity = 1; Dosage = "";
        }

        [RelayCommand]
        public async Task SavePrescriptionAsync()
        {
            ValidationMessage = "";
            if (!PatientId.HasValue || PatientName.Contains("bulunamadı"))
            { ValidationMessage = "⚠ Geçerli bir hasta ID girin!"; return; }
            if (!DoctorId.HasValue || DoctorName.Contains("bulunamadı"))
            { ValidationMessage = "⚠ Geçerli bir doktor ID girin!"; return; }
            if (!CurrentItems.Any())
            { ValidationMessage = "⚠ En az bir ilaç ekleyin!"; return; }

            var rx = await _prescriptionService.SavePrescriptionAsync(
                PatientId.Value, PatientName, DoctorId.Value, DoctorName, CurrentItems);

            Prescriptions.Insert(0, rx);
            CurrentItems.Clear();
            PatientId = null; PatientName = "";
            DoctorId  = null; DoctorName  = "";

            ToastService.Instance.Success($"✓ Reçete #{rx.Id} kaydedildi.");
        }
    }
}

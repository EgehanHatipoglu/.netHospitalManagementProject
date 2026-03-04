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
    public partial class PrescriptionViewModel : ViewModelBase
    {
        private readonly IDatabaseService _db;
        private readonly IPatientService _patientService;
        private readonly IDoctorService _doctorService;

        // --- Listeler ---
        public ObservableCollection<Prescription> Prescriptions { get; } = new();
        public ObservableCollection<Drug> AvailableDrugs { get; } = new();

        // Aktif reçeteye eklenen ilaçlar (geçici liste)
        public ObservableCollection<PrescriptionItem> CurrentItems { get; } = new();

        // --- Yeni Reçete Alanları ---
        [ObservableProperty] private int? _patientId;
        [ObservableProperty] private int? _doctorId;
        [ObservableProperty] private string _patientName = "";
        [ObservableProperty] private string _doctorName = "";

        // --- Yeni İlaç Alanları ---
        [ObservableProperty] private string _newDrugName = "";
        [ObservableProperty] private string _newDrugUnit = "";
        [ObservableProperty] private int _newDrugStock = 100;
        [ObservableProperty] private int _newDrugThreshold = 10;

        // --- İlaç Ekleme Alanları ---
        [ObservableProperty] private Drug? _selectedDrug;
        [ObservableProperty] private int _quantity = 1;
        [ObservableProperty] private string _dosage = "";

        [ObservableProperty] private string _validationMessage = "";

        private int _rxIdCounter = 0;

        public PrescriptionViewModel(IDatabaseService db, IPatientService ps, IDoctorService ds)
        {
            _db = db;
            _patientService = ps;
            _doctorService = ds;
        }

        [RelayCommand]
        public async Task RefreshDataAsync()
        {
            // İlaçları yükle
            var drugs = await _db.LoadDrugsAsync();
            AvailableDrugs.Clear();
            foreach (var (id, name, unit, stock, threshold) in drugs)
                AvailableDrugs.Add(new Drug(id, name, unit, stock, threshold));

            // Reçeteleri yükle
            var rxList = await _db.LoadPrescriptionsAsync();
            Prescriptions.Clear();
            _rxIdCounter = 0;
            foreach (var (id, pid, pname, did, dname, date) in rxList)
            {
                var rx = new Prescription(id, pid, pname, did, dname, DateTime.Parse(date));
                Prescriptions.Add(rx);
                if (id > _rxIdCounter) _rxIdCounter = id;
            }
        }

        // Hasta ID girilince otomatik isim doldurur
        partial void OnPatientIdChanged(int? value)
        {
            if (value.HasValue)
                _ = LookupPatientNameAsync(value.Value);
        }

        // Doktor ID girilince otomatik isim doldurur
        partial void OnDoctorIdChanged(int? value)
        {
            if (value.HasValue)
                _ = LookupDoctorNameAsync(value.Value);
        }

        private async Task LookupPatientNameAsync(int id)
        {
            var p = await _patientService.GetPatientByIdAsync(id);
            PatientName = p?.FullName ?? "Hasta bulunamadı";
        }

        private async Task LookupDoctorNameAsync(int id)
        {
            var d = await _doctorService.GetDoctorByIdAsync(id);
            DoctorName = d?.FullName ?? "Doktor bulunamadı";
        }

        [RelayCommand]
        public async Task AddDrugAsync()
        {
            if (string.IsNullOrWhiteSpace(NewDrugName) || string.IsNullOrWhiteSpace(NewDrugUnit))
            {
                ValidationMessage = "⚠ İlaç adı ve birimi zorunludur.";
                return;
            }

            var md = new Drug(0, NewDrugName, NewDrugUnit, NewDrugStock, NewDrugThreshold);
            await _db.SaveDrugAsync(md);
            
            NewDrugName = "";
            NewDrugUnit = "";
            NewDrugStock = 100;
            NewDrugThreshold = 10;
            
            await RefreshDataAsync();
            ToastService.Instance.Success($"✓ {md.Name} kataloga eklendi.");
        }

        [RelayCommand]
        public void StartPrescription()
        {
            CurrentItems.Clear();
            PatientId = null;
            PatientName = "";
            DoctorId = null;
            DoctorName = "";
            ValidationMessage = "Yeni reçete başlatıldı.";
        }

        [RelayCommand]
        public void AddDrugToCurrentPrescription()
        {
            ValidationMessage = "";

            if (SelectedDrug == null)
            {
                ValidationMessage = "⚠ İlaç seçilmedi!";
                return;
            }
            if (Quantity <= 0)
            {
                ValidationMessage = "⚠ Miktar 0'dan büyük olmalı!";
                return;
            }
            if (SelectedDrug.Stock < Quantity)
            {
                ValidationMessage = $"⚠ Stok yetersiz! Mevcut: {SelectedDrug.Stock} {SelectedDrug.Unit}";
                return;
            }

            // Aynı ilaç zaten eklendiyse miktarını güncelle
            var existing = CurrentItems.FirstOrDefault(i => i.Drug.Id == SelectedDrug.Id);
            if (existing != null)
            {
                CurrentItems.Remove(existing);
            }

            CurrentItems.Add(new PrescriptionItem(SelectedDrug, Quantity, Dosage.Trim()));

            // Formu temizle
            SelectedDrug = null;
            Quantity = 1;
            Dosage = "";
        }

        [RelayCommand]
        public void RemoveDrugFromCurrent(PrescriptionItem? item)
        {
            if (item != null) CurrentItems.Remove(item);
        }

        [RelayCommand]
        public async Task SavePrescriptionAsync()
        {
            ValidationMessage = "";

            if (!PatientId.HasValue || string.IsNullOrWhiteSpace(PatientName) || PatientName.Contains("bulunamadı"))
            {
                ValidationMessage = "⚠ Geçerli bir hasta ID girin!";
                return;
            }
            if (!DoctorId.HasValue || string.IsNullOrWhiteSpace(DoctorName) || DoctorName.Contains("bulunamadı"))
            {
                ValidationMessage = "⚠ Geçerli bir doktor ID girin!";
                return;
            }
            if (!CurrentItems.Any())
            {
                ValidationMessage = "⚠ En az bir ilaç ekleyin!";
                return;
            }

            _rxIdCounter++;
            var rx = new Prescription(_rxIdCounter, PatientId.Value, PatientName, DoctorId.Value, DoctorName, DateTime.Now);
            foreach (var item in CurrentItems)
            {
                rx.Items.Add(item);
                // Stoktan düş
                item.Drug.Stock -= item.Quantity;
                await _db.SaveDrugAsync(item.Drug);
            }

            await _db.SavePrescriptionAsync(rx);
            Prescriptions.Insert(0, rx);
            CurrentItems.Clear();

            PatientId = null;
            DoctorId = null;
            PatientName = "";
            DoctorName = "";

            ToastService.Instance.Success($"✓ Reçete #{rx.Id} kaydedildi.");
        }
    }
}

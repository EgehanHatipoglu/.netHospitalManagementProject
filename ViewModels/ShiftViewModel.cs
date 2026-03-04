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
    public partial class ShiftViewModel : ViewModelBase
    {
        private readonly IDatabaseService _db;
        private readonly IDoctorService _doctorService;

        public ObservableCollection<DoctorShift> Shifts { get; } = new();
        public ObservableCollection<Doctor> Doctors { get; } = new();

        // Günler için ComboBox kaynağı
        public Array ShiftDays { get; } = Enum.GetValues(typeof(ShiftDay));
        public ObservableCollection<object> WeeklyGroups { get; } = new();

        // --- Yeni Vardiya Alanları ---
        [ObservableProperty] private Doctor? _selectedDoctor;
        [ObservableProperty] private ShiftDay _selectedDay = ShiftDay.Pazartesi;
        [ObservableProperty] private int _startHour = 8;
        [ObservableProperty] private int _endHour = 16;
        
        [ObservableProperty] private int? _manualDoctorId;
        [ObservableProperty] private string _manualDoctorName = "";

        [ObservableProperty] private DoctorShift? _selectedShift;
        [ObservableProperty] private string _validationMessage = "";
        [ObservableProperty] private string _statusMessage = "";
        
        [ObservableProperty] private string _todayOnDutyText = "Bugün için kayıtlı nöbetçi yok.";
        [ObservableProperty] private string _autoRotationStatus = "";

        // Saat seçenekleri (8-22)
        public ObservableCollection<int> HourOptions { get; } = new(
            Enumerable.Range(0, 24).ToList()
        );

        private int _shiftIdCounter = 0;

        public ShiftViewModel(IDatabaseService db, IDoctorService ds)
        {
            _db = db;
            _doctorService = ds;
        }

        partial void OnManualDoctorIdChanged(int? value)
        {
            if (value.HasValue)
            {
                _ = LookupDoctorNameAsync(value.Value);
            }
        }

        private async Task LookupDoctorNameAsync(int id)
        {
            var d = await _doctorService.GetDoctorByIdAsync(id);
            if (d != null)
            {
                ManualDoctorName = d.FullName;
                SelectedDoctor = d;
            }
            else
            {
                ManualDoctorName = "Doktor bulunamadı";
                SelectedDoctor = null;
            }
        }

        [RelayCommand]
        public void AutoGenerateRotation()
        {
            AutoRotationStatus = "Otomatik planlama özelliği hazırlanıyor...";
        }

        [RelayCommand]
        public async Task RefreshDataAsync()
        {
            // Doktorları yükle
            var docs = await _doctorService.GetAllDoctorsAsync();
            Doctors.Clear();
            foreach (var d in docs) Doctors.Add(d);

            // Vardiyaları yükle
            var rows = await _db.LoadShiftsAsync();
            Shifts.Clear();
            _shiftIdCounter = 0;

            foreach (var (id, docId, docName, day, startH, endH) in rows)
            {
                Shifts.Add(new DoctorShift(id, docId, docName, (ShiftDay)day, startH, endH));
                if (id > _shiftIdCounter) _shiftIdCounter = id;
            }

            // Populate WeeklyGroups for UI
            WeeklyGroups.Clear();
            foreach (ShiftDay day in Enum.GetValues(typeof(ShiftDay)))
            {
                var dayShifts = Shifts.Where(s => s.Day == day).ToList();
                var summary = dayShifts.Any() ? string.Join(", ", dayShifts.Select(s => $"{s.DoctorName} ({s.StartHour:00}:00-{s.EndHour:00}:00)")) : "Nöbetçi Yok";
                WeeklyGroups.Add(new { DayName = day.ToString(), Summary = summary });
            }

            // Update TodayOnDutyText
            var today = DateTime.Today.DayOfWeek;
            ShiftDay mapping = today switch {
                DayOfWeek.Monday => ShiftDay.Pazartesi,
                DayOfWeek.Tuesday => ShiftDay.Salı,
                DayOfWeek.Wednesday => ShiftDay.Çarşamba,
                DayOfWeek.Thursday => ShiftDay.Perşembe,
                DayOfWeek.Friday => ShiftDay.Cuma,
                DayOfWeek.Saturday => ShiftDay.Cumartesi,
                DayOfWeek.Sunday => ShiftDay.Pazar,
                _ => ShiftDay.Pazartesi
            };

            var todayShifts = Shifts.Where(s => s.Day == mapping).ToList();
            TodayOnDutyText = todayShifts.Any() 
                ? string.Join("\n", todayShifts.Select(s => $"• {s.DoctorName} ({s.StartHour:00}:00-{s.EndHour:00}:00)")) 
                : "Bugün için kayıtlı nöbetçi yok.";
        }

        [RelayCommand]
        public async Task AddShiftAsync()
        {
            StatusMessage = "";

            if (SelectedDoctor == null)
            {
                StatusMessage = "⚠ Doktor seçilmedi!";
                return;
            }
            if (StartHour >= EndHour)
            {
                StatusMessage = "⚠ Bitiş saati başlangıçtan sonra olmalı!";
                return;
            }

            // Çakışma kontrolü
            var newShift = new DoctorShift(0, SelectedDoctor.Id, SelectedDoctor.FullName, SelectedDay, StartHour, EndHour);
            var conflict = Shifts.FirstOrDefault(s => s.ConflictsWith(newShift));
            if (conflict != null)
            {
                StatusMessage = $"⚠ Çakışma var: {conflict.Day} {conflict.StartHour:00}:00-{conflict.EndHour:00}:00";
                return;
            }

            _shiftIdCounter++;
            var shift = new DoctorShift(
                _shiftIdCounter,
                SelectedDoctor.Id,
                SelectedDoctor.FullName,
                SelectedDay,
                StartHour,
                EndHour
            );

            await _db.SaveShiftAsync(shift);
            Shifts.Add(shift);
            
            ManualDoctorId = null;
            ManualDoctorName = "";
            SelectedDoctor = null;

            await RefreshDataAsync();
            ToastService.Instance.Success($"✓ {shift.DoctorName} — {shift.Day} vardiyası eklendi.");
        }

        [RelayCommand]
        public async Task DeleteShiftAsync()
        {
            if (SelectedShift == null)
            {
                StatusMessage = "⚠ Silmek için vardiya seçin!";
                return;
            }

            await _db.DeleteShiftAsync(SelectedShift.Id);
            ToastService.Instance.Warning($"Vardiya silindi: {SelectedShift}");
            Shifts.Remove(SelectedShift);
            SelectedShift = null;
        }

        // Belirli bir doktora ait vardiyaları filtreler
        [RelayCommand]
        public void FilterByDoctor(Doctor? doctor)
        {
            // Bu metod XAML'dan çağrılabilir, ya da SearchQuery ile genişletilebilir
            // Şimdilik Shifts koleksiyonu tüm vardiyaları tutuyor
            // Gelişmiş filtreleme için PaginatedList eklenebilir
        }

        // Haftalık özet: her güne kaç vardiya düştüğünü döner (Dashboard widget için)
        public int GetShiftCountForDay(ShiftDay day)
            => Shifts.Count(s => s.Day == day);
    }
}

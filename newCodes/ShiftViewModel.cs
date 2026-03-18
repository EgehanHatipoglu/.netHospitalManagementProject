using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HospitalManagementAvolonia.Models;
using HospitalManagementAvolonia.Services;

namespace HospitalManagementAvolonia.ViewModels
{
    public partial class ShiftViewModel : ViewModelBase
    {
        // ✅ FIX: Depends on IShiftService, NOT IDatabaseService
        private readonly IShiftService _shiftService;
        private readonly IDoctorService _doctorService;

        public ObservableCollection<DoctorShift> Shifts  { get; } = new();
        public ObservableCollection<Doctor>      Doctors { get; } = new();
        public ObservableCollection<object>      WeeklyGroups { get; } = new();
        public Array ShiftDays { get; } = Enum.GetValues(typeof(ShiftDay));

        [ObservableProperty] private Doctor?    _selectedDoctor;
        [ObservableProperty] private ShiftDay   _selectedDay = ShiftDay.Pazartesi;
        [ObservableProperty] private int        _startHour = 8;
        [ObservableProperty] private int        _endHour   = 16;
        [ObservableProperty] private int?       _manualDoctorId;
        [ObservableProperty] private string     _manualDoctorName = "";
        [ObservableProperty] private DoctorShift? _selectedShift;
        [ObservableProperty] private string     _statusMessage = "";
        [ObservableProperty] private string     _todayOnDutyText = "Bugün için nöbetçi yok.";
        [ObservableProperty] private string     _autoRotationStatus = "";

        public ShiftViewModel(IShiftService shiftService, IDoctorService doctorService)
        {
            _shiftService  = shiftService;
            _doctorService = doctorService;
        }

        partial void OnManualDoctorIdChanged(int? value)
        {
            if (value.HasValue) _ = LookupDoctorAsync(value.Value);
        }

        private async Task LookupDoctorAsync(int id)
        {
            var d = await _doctorService.GetDoctorByIdAsync(id);
            ManualDoctorName = d?.FullName ?? "Doktor bulunamadı";
            SelectedDoctor   = d;
        }

        [RelayCommand]
        public async Task RefreshDataAsync()
        {
            var docs = await _doctorService.GetAllDoctorsAsync();
            Doctors.Clear();
            foreach (var d in docs) Doctors.Add(d);

            var all = await _shiftService.GetAllAsync();
            Shifts.Clear();
            foreach (var s in all) Shifts.Add(s);

            RebuildWeeklyGroups();
            UpdateTodayBanner();
        }

        [RelayCommand]
        public async Task AddShiftAsync()
        {
            StatusMessage = "";

            if (SelectedDoctor == null)
            { StatusMessage = "⚠ Doktor seçilmedi!"; return; }
            if (StartHour >= EndHour)
            { StatusMessage = "⚠ Bitiş saati başlangıçtan sonra olmalı!"; return; }

            try
            {
                var shift = await _shiftService.AddShiftAsync(
                    SelectedDoctor.Id, SelectedDoctor.FullName, SelectedDay, StartHour, EndHour);

                Shifts.Add(shift);
                RebuildWeeklyGroups();
                UpdateTodayBanner();

                ManualDoctorId = null;
                ManualDoctorName = "";
                SelectedDoctor = null;

                ToastService.Instance.Success(
                    $"✓ {shift.DoctorName} — {shift.Day} vardiyası eklendi.");
            }
            catch (InvalidOperationException ex)
            {
                StatusMessage = $"⚠ {ex.Message}";
            }
        }

        [RelayCommand]
        public async Task DeleteShiftAsync()
        {
            if (SelectedShift == null)
            { StatusMessage = "⚠ Silmek için vardiya seçin!"; return; }

            await _shiftService.DeleteShiftAsync(SelectedShift.Id);
            ToastService.Instance.Warning($"Vardiya silindi: {SelectedShift}");
            Shifts.Remove(SelectedShift);
            SelectedShift = null;

            RebuildWeeklyGroups();
            UpdateTodayBanner();
        }

        [RelayCommand]
        public void AutoGenerateRotation() =>
            AutoRotationStatus = "Otomatik planlama özelliği hazırlanıyor...";

        // ─── Helpers ──────────────────────────────────────────────────────────

        private void RebuildWeeklyGroups()
        {
            WeeklyGroups.Clear();
            foreach (ShiftDay day in Enum.GetValues(typeof(ShiftDay)))
            {
                var dayShifts = Shifts.Where(s => s.Day == day).ToList();
                var summary = dayShifts.Any()
                    ? string.Join(", ", dayShifts.Select(s =>
                        $"{s.DoctorName} ({s.StartHour:00}:00-{s.EndHour:00}:00)"))
                    : "Nöbetçi Yok";
                WeeklyGroups.Add(new { DayName = day.ToString(), Summary = summary });
            }
        }

        private void UpdateTodayBanner()
        {
            ShiftDay today = DateTime.Today.DayOfWeek switch
            {
                DayOfWeek.Monday    => ShiftDay.Pazartesi,
                DayOfWeek.Tuesday   => ShiftDay.Salı,
                DayOfWeek.Wednesday => ShiftDay.Çarşamba,
                DayOfWeek.Thursday  => ShiftDay.Perşembe,
                DayOfWeek.Friday    => ShiftDay.Cuma,
                DayOfWeek.Saturday  => ShiftDay.Cumartesi,
                _                   => ShiftDay.Pazar
            };

            var todayShifts = Shifts.Where(s => s.Day == today).ToList();
            TodayOnDutyText = todayShifts.Any()
                ? string.Join("\n", todayShifts.Select(s =>
                    $"• {s.DoctorName} ({s.StartHour:00}:00-{s.EndHour:00}:00)"))
                : "Bugün için kayıtlı nöbetçi yok.";
        }
    }
}

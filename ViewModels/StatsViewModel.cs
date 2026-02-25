using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HospitalManagementAvolonia.Models;
using HospitalManagementAvolonia.Services;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace HospitalManagementAvolonia.ViewModels
{
    public partial class StatsViewModel : ViewModelBase
    {
        private readonly IPatientService _patientService;
        private readonly IDoctorService _doctorService;
        private readonly IAppointmentService _appointmentService;

        // ── Summary cards ──────────────────────────────────────────
        [ObservableProperty] private int _totalPatients;
        [ObservableProperty] private int _totalDoctors;
        [ObservableProperty] private int _totalAppointments;

        // ── Line Chart: Daily appointment flow (last 7 days) ───────
        public ObservableCollection<ISeries> LineChartSeries { get; } = new();
        public Axis[] LineXAxes { get; set; } = Array.Empty<Axis>();
        public Axis[] LineYAxes { get; set; } = { new Axis { MinLimit = 0 } };

        // ── Pie Chart: Appointments by department ─────────────────
        public ObservableCollection<ISeries> PieChartSeries { get; } = new();

        // ── Column Chart: Appointments by weekday ─────────────────
        public ObservableCollection<ISeries> WeeklyChartSeries { get; } = new();
        public Axis[] WeeklyXAxes { get; set; } = Array.Empty<Axis>();
        public Axis[] WeeklyYAxes { get; set; } = { new Axis { MinLimit = 0 } };

        public StatsViewModel(IPatientService ps, IDoctorService ds, IAppointmentService aps)
        {
            _patientService = ps;
            _doctorService = ds;
            _appointmentService = aps;
        }

        [RelayCommand]
        public async Task RefreshDataAsync()
        {
            var patients = await _patientService.GetAllPatientsAsync();
            var doctors  = await _doctorService.GetAllDoctorsAsync();
            var apps     = await _appointmentService.GetAllAppointmentsAsync();

            TotalPatients     = patients.Count;
            TotalDoctors      = doctors.Count;
            TotalAppointments = apps.Count;

            BuildLineChart(apps);
            BuildPieChart(apps, doctors);
            BuildWeeklyChart(apps);
        }

        // ── Line Chart ─────────────────────────────────────────────
        private void BuildLineChart(List<Appointment> apps)
        {
            var labels = new List<string>();
            var data   = new List<double>();
            var today  = DateTime.Today;

            for (int i = 6; i >= 0; i--)
            {
                var day   = today.AddDays(-i);
                labels.Add(day.ToString("ddd dd", new CultureInfo("tr-TR")));
                data.Add(apps.Count(a => a.Start.Date == day));
            }

            LineXAxes = new[]
            {
                new Axis
                {
                    Labels           = labels,
                    LabelsPaint      = new SolidColorPaint(SKColors.LightGray),
                    TextSize         = 12,
                    LabelsRotation   = -30,
                    SeparatorsPaint  = new SolidColorPaint(new SKColor(60, 60, 80))
                }
            };
            LineYAxes = new[] { new Axis { MinLimit = 0, LabelsPaint = new SolidColorPaint(SKColors.LightGray), TextSize = 12 } };

            LineChartSeries.Clear();
            LineChartSeries.Add(new LineSeries<double>
            {
                Values          = data,
                Name            = "Randevular",
                Stroke          = new SolidColorPaint(SKColor.Parse("#3B82F6"), 3),
                GeometryStroke  = new SolidColorPaint(SKColor.Parse("#3B82F6"), 3),
                GeometryFill    = new SolidColorPaint(SKColors.White),
                GeometrySize    = 10,
                Fill            = new SolidColorPaint(new SKColor(29, 78, 216, 25)),
            });
        }

        // ── Pie Chart ──────────────────────────────────────────────
        private void BuildPieChart(List<Appointment> apps, List<Doctor> doctors)
        {
            var palette = new[] { "#3B82F6", "#10B981", "#F59E0B", "#EF4444", "#8B5CF6", "#EC4899" };
            var depts   = doctors
                .Where(d => d.Department != null)
                .GroupBy(d => d.Department!.Name)
                .ToDictionary(g => g.Key, g => g.Select(d => d.Id).ToHashSet());

            PieChartSeries.Clear();
            int ci = 0;
            foreach (var (deptName, docIds) in depts)
            {
                int count = apps.Count(a => docIds.Contains(a.Doctor.Id));
                if (count == 0) continue;
                PieChartSeries.Add(new PieSeries<double>
                {
                    Values    = new[] { (double)count },
                    Name      = deptName,
                    Fill      = new SolidColorPaint(SKColor.Parse(palette[ci % palette.Length])),
                    DataLabelsPaint = new SolidColorPaint(SKColors.White),
                    DataLabelsSize  = 13,
                    DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Middle,
                    DataLabelsFormatter = p => $"{deptName}\n{count}"
                });
                ci++;
            }

            // Fallback if no dept data
            if (PieChartSeries.Count == 0 && apps.Count > 0)
            {
                PieChartSeries.Add(new PieSeries<double>
                {
                    Values = new[] { (double)apps.Count },
                    Name   = "Tüm Randevular",
                    Fill   = new SolidColorPaint(SKColor.Parse("#3B82F6"))
                });
            }
        }

        // ── Column Chart ───────────────────────────────────────────
        private void BuildWeeklyChart(List<Appointment> apps)
        {
            var dayNames = new[] { "Pzt", "Sal", "Çar", "Per", "Cum", "Cmt", "Paz" };
            var counts   = new double[7];
            foreach (var a in apps)
            {
                // DayOfWeek: Sunday=0, Monday=1... map to index 0=Pzt
                int idx = ((int)a.Start.DayOfWeek + 6) % 7;
                counts[idx]++;
            }

            WeeklyXAxes = new[]
            {
                new Axis
                {
                    Labels          = dayNames,
                    LabelsPaint     = new SolidColorPaint(SKColors.LightGray),
                    TextSize        = 13,
                    SeparatorsPaint = new SolidColorPaint(new SKColor(60, 60, 80))
                }
            };
            WeeklyYAxes = new[] { new Axis { MinLimit = 0, LabelsPaint = new SolidColorPaint(SKColors.LightGray), TextSize = 12 } };

            WeeklyChartSeries.Clear();
            WeeklyChartSeries.Add(new ColumnSeries<double>
            {
                Values    = counts,
                Name      = "Randevu Sayısı",
                Fill      = new SolidColorPaint(SKColor.Parse("#6366F1")),
                Rx        = 6,
                Ry        = 6
            });
        }
    }
}

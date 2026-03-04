using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HospitalManagementAvolonia.DataStructures;
using HospitalManagementAvolonia.Models;
using HospitalManagementAvolonia.Services;

namespace HospitalManagementAvolonia.ViewModels
{
    public partial class EmergencyViewModel : ViewModelBase
    {
        private readonly IEmergencyService _emergencyService;
        private readonly Services.IPatientService _patientService; // Add patient service

        public ObservableCollection<ERPriorityQueue.ERPatient> ERPatients { get; } = new();

        [ObservableProperty] private int? _newPatientId;
        [ObservableProperty] private string _newComplaint = "Acil Durum";
        [ObservableProperty] private int _severity = 5;

        public EmergencyViewModel(IEmergencyService emergencyService, Services.IPatientService patientService)
        {
            _emergencyService = emergencyService;
            _patientService = patientService;
            RefreshData();
        }

        [RelayCommand]
        public async System.Threading.Tasks.Task AddToERAsync()
        {
            if (NewPatientId.HasValue)
            {
                var p = await _patientService.GetPatientByIdAsync(NewPatientId.Value);
                if (p != null)
                {
                    _emergencyService.AddPatientToER(p, Severity, NewComplaint);
                    RefreshData();
                    NewPatientId = null;
                }
                else
                {
                    HospitalManagementAvolonia.Services.ToastService.Instance.Warning("Hasta bulunamadı.");
                }
            }
        }

        [RelayCommand]
        public void ProcessNext()
        {
            var p = _emergencyService.TreatHighestPriorityPatient();
            if (p != null)
            {
                RefreshData();
            }
        }

        [RelayCommand]
        public void RefreshData()
        {
            var items = _emergencyService.GetQueue();
            ERPatients.Clear();
            foreach (var r in items) ERPatients.Add(r);
        }
    }
}

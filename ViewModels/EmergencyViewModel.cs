using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HospitalManagementAvolonia.DataStructures;
using HospitalManagementAvolonia.Models;

namespace HospitalManagementAvolonia.ViewModels
{
    public partial class EmergencyViewModel : ViewModelBase
    {
        private readonly ERPriorityQueue _erQueue = new();

        public ObservableCollection<ERPriorityQueue.ERPatient> ERPatients { get; } = new();

        [ObservableProperty] private Patient? _selectedPatient;
        [ObservableProperty] private int _severity = 1;

        public EmergencyViewModel()
        {
        }

        [RelayCommand]
        public void AddToER()
        {
            if (SelectedPatient != null)
            {
                var patient = new ERPriorityQueue.ERPatient(SelectedPatient, Severity, "Acil Durum");
                _erQueue.AddPatient(patient);
                RefreshData();
            }
        }

        [RelayCommand]
        public void ProcessNext()
        {
            var p = _erQueue.RemoveHighestPriority();
            if (p != null)
            {
                RefreshData();
            }
        }

        [RelayCommand]
        public void RefreshData()
        {
            var items = _erQueue.GetAllSorted();
            ERPatients.Clear();
            foreach (var r in items) ERPatients.Add(r);
        }
    }
}

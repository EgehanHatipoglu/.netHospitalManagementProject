using System.Collections.Generic;
using HospitalManagementAvolonia.DataStructures;
using HospitalManagementAvolonia.Models;

namespace HospitalManagementAvolonia.Services
{
    public class EmergencyService : IEmergencyService
    {
        private readonly ERPriorityQueue _erQueue = new();

        public void AddPatientToER(Patient patient, int severity, string complaint)
        {
            var erPatient = new ERPriorityQueue.ERPatient(patient, severity, complaint);
            _erQueue.AddPatient(erPatient);
        }

        public ERPriorityQueue.ERPatient? TreatHighestPriorityPatient()
        {
            return _erQueue.RemoveHighestPriority();
        }

        public List<ERPriorityQueue.ERPatient> GetQueue()
        {
            return _erQueue.GetAllSorted();
        }
    }
}

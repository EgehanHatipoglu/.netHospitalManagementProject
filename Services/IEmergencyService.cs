using System.Collections.Generic;
using HospitalManagementAvolonia.DataStructures;
using HospitalManagementAvolonia.Models;

namespace HospitalManagementAvolonia.Services
{
    public interface IEmergencyService
    {
        void AddPatientToER(Patient patient, int severity, string complaint);
        ERPriorityQueue.ERPatient? TreatHighestPriorityPatient();
        List<ERPriorityQueue.ERPatient> GetQueue();
    }
}

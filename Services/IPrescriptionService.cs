using System.Collections.Generic;
using System.Threading.Tasks;
using HospitalManagementAvolonia.Models;

namespace HospitalManagementAvolonia.Services
{
    public interface IPrescriptionService
    {
        Task InitializeAsync();
        Task<List<Drug>> GetAllDrugsAsync();
        Task<Drug> AddDrugAsync(string name, string unit, int stock, int threshold);
        Task<List<Prescription>> GetAllPrescriptionsAsync();
        Task<Prescription> SavePrescriptionAsync(int patientId, string patientName,
            int doctorId, string doctorName, IEnumerable<PrescriptionItem> items);
    }
}

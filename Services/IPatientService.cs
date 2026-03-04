using System.Collections.Generic;
using System.Threading.Tasks;
using HospitalManagementAvolonia.Models;

namespace HospitalManagementAvolonia.Services
{
    public interface IPatientService
    {
        Task InitializeAsync();
        Task<List<Patient>> GetAllPatientsAsync();
        Task<Patient?> GetPatientByIdAsync(int id);
        Task<IEnumerable<Patient>> SearchPatientsAsync(string searchString);
        Task<Patient> AddPatientAsync(string firstName, string lastName, string nationalId, string phone, System.DateTime birthDate);
        Task UpdatePatientAsync(Patient patient);
        Task DeletePatientAsync(int id);

        // Data Structures Integration
        Patient? SearchBST(string firstName, string lastName);
        List<Patient> GetAllFromBST();
        
        Patient? SearchAVL(string firstName, string lastName);
        List<Patient> GetAllFromAVL();

        void RecordPatientView(Patient patient);
        List<Patient> GetRecentPatients();
    }
}

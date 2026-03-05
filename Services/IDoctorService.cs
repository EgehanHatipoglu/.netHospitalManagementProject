using System.Collections.Generic;
using System.Threading.Tasks;
using HospitalManagementAvolonia.Models;

namespace HospitalManagementAvolonia.Services
{
    public interface IDoctorService
    {
        Task InitializeAsync();
        Task<List<Doctor>> GetAllDoctorsAsync();
        Task<Doctor?> GetDoctorByIdAsync(int id);
        Task<IEnumerable<Doctor>> SearchDoctorsAsync(string searchString);
        Task<Doctor> AddDoctorAsync(string firstName, string lastName, int departmentId, string phone);
        Task UpdateDoctorAsync(Doctor doctor);
        Task DeleteDoctorAsync(int id);
        Task<List<Department>> GetDepartmentsAsync();

        // DoctorGraph methods
        void AddReferral(Doctor from, Doctor to);
        List<Doctor> GetReferrals(int doctorId);
        List<string> GetReferralPathBFS(int startId, int targetId);
        List<string> GetReferralNetworkDFS(int startId);
    }
}

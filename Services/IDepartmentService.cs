using System.Collections.Generic;
using System.Threading.Tasks;
using HospitalManagementAvolonia.DataStructures;
using HospitalManagementAvolonia.Models;

namespace HospitalManagementAvolonia.Services
{
    public interface IDepartmentService
    {
        Task InitializeAsync();
        Task<List<Department>> GetAllDepartmentsAsync();
        Task<Department?> GetDepartmentByIdAsync(int id);
        Task<Department> AddDepartmentAsync(string name, int capacity);
        List<(string name, int level, int doctorCount)> GetHierarchy();
    }
}

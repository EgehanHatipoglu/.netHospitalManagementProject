using System.Collections.Generic;
using System.Threading.Tasks;
using HospitalManagementAvolonia.Models;

namespace HospitalManagementAvolonia.Services
{
    public interface IShiftService
    {
        Task InitializeAsync();
        Task<List<DoctorShift>> GetAllAsync();
        Task<DoctorShift> AddShiftAsync(int doctorId, string doctorName,
            ShiftDay day, int startHour, int endHour);
        Task DeleteShiftAsync(int shiftId);
        bool HasConflict(DoctorShift newShift);
    }
}

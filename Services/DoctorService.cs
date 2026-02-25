using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HospitalManagementAvolonia.Data;
using HospitalManagementAvolonia.Models;

namespace HospitalManagementAvolonia.Services
{
    public class DoctorService : IDoctorService
    {
        private readonly IDatabaseService _db;
        private readonly Dictionary<int, Doctor> _doctors = new();
        private int _doctorIdCounter = 0;

        public DoctorService(IDatabaseService db)
        {
            _db = db;
        }

        public async Task<List<Doctor>> GetAllDoctorsAsync()
        {
            if (!_doctors.Any())
            {
                var dtos = await _db.LoadDoctorsAsync();
                foreach (var dto in dtos)
                {
                    if (dto.id > _doctorIdCounter) _doctorIdCounter = dto.id;
                    var d = new Doctor(dto.id, dto.firstName, dto.lastName, null, dto.phone)
                    {
                        DepartmentId = dto.departmentId
                    };
                    _doctors[d.Id] = d;
                }
            }
            return _doctors.Values.OrderBy(d => d.Id).ToList();
        }

        public async Task<Doctor?> GetDoctorByIdAsync(int id)
        {
            if (!_doctors.Any()) await GetAllDoctorsAsync();
            _doctors.TryGetValue(id, out var doc);
            return doc;
        }

        public async Task<IEnumerable<Doctor>> SearchDoctorsAsync(string searchString)
        {
            var all = await GetAllDoctorsAsync();
            if (string.IsNullOrWhiteSpace(searchString)) return all;
            
            var q = searchString.Trim().ToLowerInvariant();
            return all.Where(d => d.FirstName.ToLowerInvariant().Contains(q) ||
                                  d.LastName.ToLowerInvariant().Contains(q) ||
                                  (d.DeptName?.ToLowerInvariant().Contains(q) ?? false) ||
                                  d.Phone.Contains(q));
        }

        public async Task<Doctor> AddDoctorAsync(string firstName, string lastName, int departmentId, string phone)
        {
            await GetAllDoctorsAsync();
            _doctorIdCounter++;
            var d = new Doctor(_doctorIdCounter, firstName, lastName, null, phone)
            {
                DepartmentId = departmentId
            };
            _doctors[d.Id] = d;
            
            await _db.SaveDoctorAsync(d);
            return d;
        }

        public async Task UpdateDoctorAsync(Doctor doctor)
        {
            _doctors[doctor.Id] = doctor;
            await _db.SaveDoctorAsync(doctor);
        }

        public async Task DeleteDoctorAsync(int id)
        {
            if (_doctors.Remove(id))
            {
                await _db.DeleteDoctorAsync(id);
            }
        }

        public async Task<List<Department>> GetDepartmentsAsync()
        {
            var dtos = await _db.LoadDepartmentsAsync();
            var list = new List<Department>();
            foreach (var dto in dtos)
            {
                list.Add(new Department(dto.id, dto.name, dto.capacity));
            }
            return list;
        }
    }
}

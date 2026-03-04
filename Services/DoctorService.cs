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
        private bool _isInitialized = false;

        private readonly HospitalManagementAvolonia.DataStructures.DoctorGraph _doctorGraph = new();

        public DoctorService(IDatabaseService db)
        {
            _db = db;
        }

        public async Task InitializeAsync()
        {
            if (_isInitialized) return;
            await LoadFromDbAsync();
            _isInitialized = true;
        }

        public async Task<List<Doctor>> GetAllDoctorsAsync()
        {
            if (!_isInitialized) await InitializeAsync();
            return _doctors.Values.OrderBy(d => d.Id).ToList();
        }

        private async Task LoadFromDbAsync()
        {
            _doctors.Clear();
            var deptRows = await _db.LoadDepartmentsAsync();
            var deptMap = new Dictionary<int, Department>();
            foreach (var dept in deptRows)
                deptMap[dept.id] = new Department(dept.id, dept.name, dept.capacity);

            var dtos = await _db.LoadDoctorsAsync();
            foreach (var dto in dtos)
            {
                if (dto.id > _doctorIdCounter) _doctorIdCounter = dto.id;
                deptMap.TryGetValue(dto.departmentId, out var department);
                var d = new Doctor(dto.id, dto.firstName, dto.lastName, department, dto.phone)
                {
                    DepartmentId = dto.departmentId
                };
                _doctors[d.Id] = d;
                _doctorGraph.AddDoctor(d);
            }
        }

        public async Task ForceReloadAsync()
        {
            _doctors.Clear();
            await LoadFromDbAsync();
            _isInitialized = true;
        }

        public async Task<Doctor?> GetDoctorByIdAsync(int id)
        {
            if (!_isInitialized) await InitializeAsync();
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

            // Load department to attach it
            var deptRows = await _db.LoadDepartmentsAsync();
            Department? dept = null;
            foreach (var row in deptRows)
            {
                if (row.id == departmentId) { dept = new Department(row.id, row.name, row.capacity); break; }
            }

            var d = new Doctor(_doctorIdCounter, firstName, lastName, dept, phone)
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
            if (!_isInitialized) await InitializeAsync();
            if (_doctors.Remove(id))
            {
                _doctorGraph.RemoveDoctor(id);
                await _db.DeleteDoctorAsync(id);
            }
        }

        public async Task<List<Department>> GetDepartmentsAsync()
        {
            var rows = await _db.LoadDepartmentsAsync();
            var list = new List<Department>();
            foreach (var r in rows) list.Add(new Department(r.id, r.name, r.capacity));
            return list;
        }

        // DoctorGraph methods
        public void AddReferral(Doctor from, Doctor to) => _doctorGraph.AddReferral(from, to);
        public List<Doctor> GetReferrals(int doctorId) => _doctorGraph.GetReferrals(doctorId);
        public List<string> GetReferralPathBFS(int startId, int targetId) => _doctorGraph.BFS(startId, targetId);
        public List<string> GetReferralNetworkDFS(int startId) => _doctorGraph.DFS(startId);
    }
}

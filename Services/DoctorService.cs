using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HospitalManagementAvolonia.Data;
using HospitalManagementAvolonia.Models;

namespace HospitalManagementAvolonia.Services
{
    public class DoctorService : ServiceBase, IDoctorService
    {
        private readonly IDatabaseService _db;
        private readonly IDepartmentService _departmentService;
        private readonly Dictionary<int, Doctor> _doctors = new();
        private int _doctorIdCounter = 0;

        private readonly HospitalManagementAvolonia.DataStructures.DoctorGraph _doctorGraph = new();

        public DoctorService(IDatabaseService db, IDepartmentService departmentService)
        {
            _db = db;
            _departmentService = departmentService;
        }

        public async Task InitializeAsync()
        {
            await EnsureInitializedAsync(LoadFromDbAsync);
        }

        public async Task<List<Doctor>> GetAllDoctorsAsync()
        {
            if (!IsInitialized) await InitializeAsync();
            return _doctors.Values.OrderBy(d => d.Id).ToList();
        }

        private async Task LoadFromDbAsync()
        {
            _doctors.Clear();
            var depts = await _departmentService.GetAllDepartmentsAsync();
            var deptMap = depts.ToDictionary(d => d.Id, d => d);

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
            SetInitialized(true);
        }

        public async Task<Doctor?> GetDoctorByIdAsync(int id)
        {
            if (!IsInitialized) await InitializeAsync();
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

            // Load department from service rather than direct database hit
            var dept = await _departmentService.GetDepartmentByIdAsync(departmentId);

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
            if (!IsInitialized) await InitializeAsync();
            if (_doctors.Remove(id))
            {
                _doctorGraph.RemoveDoctor(id);
                await _db.DeleteDoctorAsync(id);
            }
        }

        public async Task<List<Department>> GetDepartmentsAsync()
        {
            return await _departmentService.GetAllDepartmentsAsync();
        }

        // DoctorGraph methods
        public void AddReferral(Doctor from, Doctor to) => _doctorGraph.AddReferral(from, to);
        public List<Doctor> GetReferrals(int doctorId) => _doctorGraph.GetReferrals(doctorId);
        public List<string> GetReferralPathBFS(int startId, int targetId) => _doctorGraph.BFS(startId, targetId);
        public List<string> GetReferralNetworkDFS(int startId) => _doctorGraph.DFS(startId);
    }
}

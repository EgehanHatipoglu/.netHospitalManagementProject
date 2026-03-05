using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HospitalManagementAvolonia.Data;
using HospitalManagementAvolonia.DataStructures;
using HospitalManagementAvolonia.Models;

namespace HospitalManagementAvolonia.Services
{
    public class DepartmentService : ServiceBase, IDepartmentService
    {
        private readonly IDatabaseService _db;
        private readonly HashTable<int, Department> _departments = new();
        private readonly HospitalTree _hospitalTree = new("Manisa Celal Bayar University Hospital");
        private int _departmentIdCounter = 0;

        public DepartmentService(IDatabaseService db)
        {
            _db = db;
        }

        public async Task InitializeAsync()
        {
            await EnsureInitializedAsync(async () =>
            {
                var depts = await _db.LoadDepartmentsAsync();
                foreach (var (id, name, cap) in depts)
                {
                    var d = new Department(id, name, cap);
                    _departments.Put(id, d);
                    _hospitalTree.AddDepartmentToRoot(d);
                    if (id > _departmentIdCounter) _departmentIdCounter = id;
                }
            });
        }

        public async Task<List<Department>> GetAllDepartmentsAsync()
        {
            if (!IsInitialized) await InitializeAsync();
            return _departments.Values().OrderBy(d => d.Name).ToList();
        }

        public async Task<Department?> GetDepartmentByIdAsync(int id)
        {
            if (!IsInitialized) await InitializeAsync();
            return _departments.Get(id);
        }

        public async Task<Department> AddDepartmentAsync(string name, int capacity)
        {
            if (!IsInitialized) await InitializeAsync();

            _departmentIdCounter++;
            var dept = new Department(_departmentIdCounter, name, capacity);
            
            _departments.Put(dept.Id, dept);
            _hospitalTree.AddDepartmentToRoot(dept);
            
            await _db.SaveDepartmentAsync(dept);
            return dept;
        }

        public List<(string name, int level, int doctorCount)> GetHierarchy()
        {
            return _hospitalTree.GetHierarchy();
        }
    }
}

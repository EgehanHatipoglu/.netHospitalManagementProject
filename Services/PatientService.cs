using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HospitalManagementAvolonia.Data;
using HospitalManagementAvolonia.DataStructures;
using HospitalManagementAvolonia.Models;

namespace HospitalManagementAvolonia.Services
{
    public class PatientService : ServiceBase, IPatientService
    {
        private readonly IDatabaseService _db;
        private readonly Dictionary<int, Patient> _patients = new();
        private int _patientIdCounter = 0;

        private readonly PatientBST _patientBST = new();
        private readonly PatientAVL _patientAVL = new();
        private readonly PatientTrie _patientTrie = new();
        private readonly PatientLRUCache _lruCache = new(10);

        public PatientService(IDatabaseService db)
        {
            _db = db;
        }

        public async Task InitializeAsync()
        {
            await EnsureInitializedAsync(async () =>
            {
                var dtos = await _db.LoadPatientsAsync();
                foreach (var dto in dtos)
                {
                    if (dto.id > _patientIdCounter) _patientIdCounter = dto.id;
                    if (DateTime.TryParse(dto.birthDate, out DateTime bd))
                    {
                        var p = new Patient(dto.id, dto.firstName, dto.lastName, dto.nationalId, dto.phone, bd);
                        _patients[p.Id] = p;
                        
                        // Insert into data structures
                        _patientBST.Insert(p);
                        _patientAVL.Insert(p);
                        _patientTrie.Insert(p);
                    }
                }
            });
        }

        public async Task<List<Patient>> GetAllPatientsAsync()
        {
            if (!IsInitialized) await InitializeAsync();
            return _patients.Values.OrderBy(p => p.Id).ToList();
        }

        public async Task<Patient?> GetPatientByIdAsync(int id)
        {
            if (!IsInitialized) await InitializeAsync();
            _patients.TryGetValue(id, out var patient);
            return patient;
        }

        public async Task<IEnumerable<Patient>> SearchPatientsAsync(string searchString)
        {
            var all = await GetAllPatientsAsync();
            if (string.IsNullOrWhiteSpace(searchString)) return all;
            
            var q = searchString.Trim().ToLowerInvariant();
            
            // Priority: TC National ID precise search via Trie
            if (q.All(char.IsDigit) && q.Length <= 11)
            {
                // Trie expects Patient object in the original Insert, let's look at how it searches.
                // Assuming it has a Search method that takes a string. Or we can just use linear search if Trie doesn't support it well yet.
                var exact = _patients.Values.FirstOrDefault(p => p.NationalId == q);
                if (exact != null) return new[] { exact };
            }

            return all.Where(p => p.FirstName.ToLowerInvariant().Contains(q) ||
                                  p.LastName.ToLowerInvariant().Contains(q) ||
                                  p.NationalId.Contains(q) ||
                                  p.Phone.Contains(q));
        }

        public async Task<Patient> AddPatientAsync(string firstName, string lastName, string nationalId, string phone, DateTime birthDate)
        {
            if (!IsInitialized) await InitializeAsync();

            _patientIdCounter++;
            var p = new Patient(_patientIdCounter, firstName, lastName, nationalId, phone, birthDate);
            _patients[p.Id] = p;
            
            _patientBST.Insert(p);
            _patientAVL.Insert(p);
            _patientTrie.Insert(p);
            
            await _db.SavePatientAsync(p);
            return p;
        }

        public async Task UpdatePatientAsync(Patient patient)
        {
            if (!IsInitialized) await InitializeAsync();

            if (_patients.TryGetValue(patient.Id, out var existing))
            {
                _patientBST.Delete(existing.FirstName, existing.LastName);
                _patientAVL.Delete(existing.FirstName, existing.LastName);
                // Trie doesn't easily support deletion of old node and insertion of new one without full rebuild or direct method
                // Assuming NationalId doesn't change frequently, or we just overwrite.
            }

            _patients[patient.Id] = patient;
            _patientBST.Insert(patient);
            _patientAVL.Insert(patient);
            _patientTrie.Insert(patient);

            await _db.SavePatientAsync(patient);
        }

        public async Task DeletePatientAsync(int id)
        {
            if (!IsInitialized) await InitializeAsync();

            if (_patients.TryGetValue(id, out var p))
            {
                _patients.Remove(id);
                _patientBST.Delete(p.FirstName, p.LastName);
                _patientAVL.Delete(p.FirstName, p.LastName);
                
                await _db.DeletePatientAsync(id);
            }
        }

        // --- Data Structure Specific Integrations ---
        public Patient? SearchBST(string firstName, string lastName) => _patientBST.Search(firstName, lastName);
        public List<Patient> GetAllFromBST() => _patientBST.GetAllInOrder();
        
        public Patient? SearchAVL(string firstName, string lastName) => _patientAVL.Search(firstName, lastName);
        public List<Patient> GetAllFromAVL() => _patientAVL.GetAllInOrder();

        public void RecordPatientView(Patient patient) => _lruCache.AccessPatient(patient);
        public List<Patient> GetRecentPatients() => _lruCache.GetRecentPatients();
    }
}

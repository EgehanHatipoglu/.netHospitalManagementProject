using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HospitalManagementAvolonia.Data;
using HospitalManagementAvolonia.Models;

namespace HospitalManagementAvolonia.Services
{
    public class PatientService : IPatientService
    {
        private readonly IDatabaseService _db;
        private readonly Dictionary<int, Patient> _patients = new();
        private int _patientIdCounter = 0;

        public PatientService(IDatabaseService db)
        {
            _db = db;
        }

        public async Task<List<Patient>> GetAllPatientsAsync()
        {
            if (!_patients.Any())
            {
                var dtos = await _db.LoadPatientsAsync();
                foreach (var dto in dtos)
                {
                    if (dto.id > _patientIdCounter) _patientIdCounter = dto.id;
                    if (DateTime.TryParse(dto.birthDate, out DateTime bd))
                    {
                        var p = new Patient(dto.id, dto.firstName, dto.lastName, dto.nationalId, dto.phone, bd);
                        _patients[p.Id] = p;
                    }
                }
            }
            return _patients.Values.OrderBy(p => p.Id).ToList();
        }

        public async Task<Patient?> GetPatientByIdAsync(int id)
        {
            if (!_patients.Any()) await GetAllPatientsAsync();
            _patients.TryGetValue(id, out var patient);
            return patient;
        }

        public async Task<IEnumerable<Patient>> SearchPatientsAsync(string searchString)
        {
            var all = await GetAllPatientsAsync();
            if (string.IsNullOrWhiteSpace(searchString)) return all;
            
            var q = searchString.Trim().ToLowerInvariant();
            return all.Where(p => p.FirstName.ToLowerInvariant().Contains(q) ||
                                  p.LastName.ToLowerInvariant().Contains(q) ||
                                  p.NationalId.Contains(q) ||
                                  p.Phone.Contains(q));
        }

        public async Task<Patient> AddPatientAsync(string firstName, string lastName, string nationalId, string phone, DateTime birthDate)
        {
            await GetAllPatientsAsync(); // Ensure loaded
            _patientIdCounter++;
            var p = new Patient(_patientIdCounter, firstName, lastName, nationalId, phone, birthDate);
            _patients[p.Id] = p;
            
            await _db.SavePatientAsync(p);
            return p;
        }

        public async Task UpdatePatientAsync(Patient patient)
        {
            _patients[patient.Id] = patient;
            await _db.SavePatientAsync(patient);
        }

        public async Task DeletePatientAsync(int id)
        {
            if (_patients.Remove(id))
            {
                await _db.DeletePatientAsync(id);
            }
        }
    }
}

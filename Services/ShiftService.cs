using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HospitalManagementAvolonia.Data;
using HospitalManagementAvolonia.Models;

namespace HospitalManagementAvolonia.Services
{
    public sealed class ShiftService : ServiceBase, IShiftService
    {
        private readonly IDatabaseService _db;
        private readonly List<DoctorShift> _shifts = new();
        private int _idCounter;

        public ShiftService(IDatabaseService db) => _db = db;

        public async Task InitializeAsync()
        {
            await EnsureInitializedAsync(async () =>
            {
                var rows = await _db.LoadShiftsAsync();
                foreach (var (id, docId, docName, day, startH, endH) in rows)
                {
                    _shifts.Add(new DoctorShift(id, docId, docName, (ShiftDay)day, startH, endH));
                    if (id > _idCounter) _idCounter = id;
                }
            });
        }

        public async Task<List<DoctorShift>> GetAllAsync()
        {
            if (!IsInitialized) await InitializeAsync();
            return _shifts.ToList();
        }

        public async Task<DoctorShift> AddShiftAsync(int doctorId, string doctorName,
            ShiftDay day, int startHour, int endHour)
        {
            if (!IsInitialized) await InitializeAsync();

            _idCounter++;
            var shift = new DoctorShift(_idCounter, doctorId, doctorName, day, startHour, endHour);

            if (HasConflict(shift))
                throw new InvalidOperationException(
                    $"Çakışma: {doctorName} — {day} {startHour:00}:00-{endHour:00}:00 zaten dolu.");

            _shifts.Add(shift);
            await _db.SaveShiftAsync(shift);
            return shift;
        }

        public async Task DeleteShiftAsync(int shiftId)
        {
            if (!IsInitialized) await InitializeAsync();
            var shift = _shifts.FirstOrDefault(s => s.Id == shiftId);
            if (shift == null) return;
            _shifts.Remove(shift);
            await _db.DeleteShiftAsync(shiftId);
        }

        public bool HasConflict(DoctorShift newShift) =>
            _shifts.Any(s => s.Id != newShift.Id && s.ConflictsWith(newShift));
    }
}

using System.Collections.Generic;
using System.Threading.Tasks;
using HospitalManagementAvolonia.Models;

namespace HospitalManagementAvolonia.Services
{
    public sealed class PrescriptionService : ServiceBase, IPrescriptionService
    {
        private readonly IDatabaseService _db;
        private readonly List<Drug> _drugs = new();
        private readonly List<Prescription> _prescriptions = new();
        private int _drugIdCounter;
        private int _rxIdCounter;

        public PrescriptionService(IDatabaseService db) => _db = db;

        public async Task InitializeAsync()
        {
            await EnsureInitializedAsync(async () =>
            {
                // Load drugs
                var drugs = await _db.LoadDrugsAsync();
                foreach (var (id, name, unit, stock, threshold) in drugs)
                {
                    _drugs.Add(new Drug(id, name, unit, stock, threshold));
                    if (id > _drugIdCounter) _drugIdCounter = id;
                }

                // Load prescriptions (items not loaded here — extend if needed)
                var rxList = await _db.LoadPrescriptionsAsync();
                foreach (var (id, pid, pname, did, dname, date) in rxList)
                {
                    _prescriptions.Add(new Prescription(id, pid, pname, did, dname,
                        System.DateTime.Parse(date)));
                    if (id > _rxIdCounter) _rxIdCounter = id;
                }
            });
        }

        public async Task<List<Drug>> GetAllDrugsAsync()
        {
            if (!IsInitialized) await InitializeAsync();
            return _drugs.ToList();
        }

        public async Task<Drug> AddDrugAsync(string name, string unit, int stock, int threshold)
        {
            if (!IsInitialized) await InitializeAsync();

            _drugIdCounter++;
            var drug = new Drug(_drugIdCounter, name, unit, stock, threshold);
            _drugs.Add(drug);
            await _db.SaveDrugAsync(drug);
            return drug;
        }

        public async Task<List<Prescription>> GetAllPrescriptionsAsync()
        {
            if (!IsInitialized) await InitializeAsync();
            return _prescriptions.ToList();
        }

        public async Task<Prescription> SavePrescriptionAsync(int patientId, string patientName,
            int doctorId, string doctorName, IEnumerable<PrescriptionItem> items)
        {
            if (!IsInitialized) await InitializeAsync();

            _rxIdCounter++;
            var rx = new Prescription(_rxIdCounter, patientId, patientName,
                doctorId, doctorName, System.DateTime.Now);

            foreach (var item in items)
            {
                rx.Items.Add(item);
                // Deduct stock
                item.Drug.Stock -= item.Quantity;
                await _db.SaveDrugAsync(item.Drug);
            }

            await _db.SavePrescriptionAsync(rx);
            _prescriptions.Insert(0, rx);
            return rx;
        }
    }
}

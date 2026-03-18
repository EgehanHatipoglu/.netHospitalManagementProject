using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HospitalManagementAvolonia.Data;
using HospitalManagementAvolonia.Models;

namespace HospitalManagementAvolonia.Services
{
    public sealed class BillingService : ServiceBase, IBillingService
    {
        private readonly IDatabaseService _db;
        private readonly List<Invoice> _invoices = new();
        private int _idCounter;

        public BillingService(IDatabaseService db) => _db = db;

        public async Task InitializeAsync()
        {
            await EnsureInitializedAsync(async () =>
            {
                var rows = await _db.LoadInvoicesAsync();
                foreach (var (id, appId, pname, dname, date, baseAmt, insPct, status) in rows)
                {
                    var inv = new Invoice(id, appId, pname, dname,
                        DateTime.Parse(date), (decimal)baseAmt, (decimal)insPct)
                    { Status = status };
                    _invoices.Add(inv);
                    if (id > _idCounter) _idCounter = id;
                }
            });
        }

        public async Task<List<Invoice>> GetAllAsync()
        {
            if (!IsInitialized) await InitializeAsync();
            return _invoices.ToList();
        }

        public async Task<Invoice> CreateInvoiceAsync(int appointmentId, string patientName,
            string doctorName, decimal baseAmount, decimal insurancePct)
        {
            if (!IsInitialized) await InitializeAsync();

            _idCounter++;
            var inv = new Invoice(_idCounter, appointmentId, patientName, doctorName,
                DateTime.Now, baseAmount, insurancePct);

            _invoices.Insert(0, inv);
            await _db.SaveInvoiceAsync(inv);
            return inv;
        }

        public async Task MarkAsPaidAsync(int invoiceId)
        {
            if (!IsInitialized) await InitializeAsync();
            var inv = _invoices.FirstOrDefault(i => i.Id == invoiceId)
                      ?? throw new InvalidOperationException($"Invoice {invoiceId} not found.");
            inv.Status = "Paid";
            await _db.SaveInvoiceAsync(inv);
        }

        public async Task CancelAsync(int invoiceId)
        {
            if (!IsInitialized) await InitializeAsync();
            var inv = _invoices.FirstOrDefault(i => i.Id == invoiceId)
                      ?? throw new InvalidOperationException($"Invoice {invoiceId} not found.");
            inv.Status = "Cancelled";
            await _db.SaveInvoiceAsync(inv);
        }
    }
}

using System.Collections.Generic;
using System.Threading.Tasks;
using HospitalManagementAvolonia.Models;

namespace HospitalManagementAvolonia.Services
{
    public interface IBillingService
    {
        Task InitializeAsync();
        Task<List<Invoice>> GetAllAsync();
        Task<Invoice> CreateInvoiceAsync(int appointmentId, string patientName,
            string doctorName, decimal baseAmount, decimal insurancePct);
        Task MarkAsPaidAsync(int invoiceId);
        Task CancelAsync(int invoiceId);
    }
}

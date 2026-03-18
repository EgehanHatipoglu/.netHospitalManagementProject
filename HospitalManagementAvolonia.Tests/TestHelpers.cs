using HospitalManagementAvolonia.Models;

namespace HospitalManagementAvolonia.Tests;

/// <summary>
/// Helper factory methods for creating test objects without repeating boilerplate.
/// </summary>
public static class TestHelpers
{
    public static Patient CreatePatient(int id = 1, string firstName = "Ali", string lastName = "Yılmaz",
        string nationalId = "12345678901", string phone = "5551234567", DateTime? birthDate = null)
    {
        return new Patient(id, firstName, lastName, nationalId, phone, birthDate ?? new DateTime(1990, 1, 1));
    }

    public static Doctor CreateDoctor(int id = 1, string firstName = "Mehmet", string lastName = "Öz",
        Department? department = null, string phone = "5559876543")
    {
        return new Doctor(id, firstName, lastName, department ?? new Department(1, "Kardiyoloji", 20), phone);
    }

    public static Appointment CreateAppointment(int id = 1, Patient? patient = null, Doctor? doctor = null,
        DateTime? start = null)
    {
        return new Appointment(id, patient ?? CreatePatient(), doctor ?? CreateDoctor(),
            start ?? new DateTime(2026, 3, 5, 10, 0, 0));
    }

    public static Department CreateDepartment(int id = 1, string name = "Kardiyoloji", int capacity = 20)
    {
        return new Department(id, name, capacity);
    }
}

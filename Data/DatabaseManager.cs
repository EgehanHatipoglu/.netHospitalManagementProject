using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using HospitalManagementAvolonia.Models;

namespace HospitalManagementAvolonia.Data
{
    /// <summary>
    /// SQLite Database Manager for persistent storage.
    /// Handles schema creation and CRUD operations asynchronously.
    /// </summary>
    public class DatabaseManager : IDatabaseService
    {
        private readonly string _connectionString;

        public DatabaseManager(string dbPath = "hospital.db")
        {
            _connectionString = $"Data Source={dbPath}";
        }

        private SqliteConnection GetConnection() => new SqliteConnection(_connectionString);

        public async Task InitializeDatabaseAsync()
        {
            await using var connection = GetConnection();
            await connection.OpenAsync();
            await using var cmd = connection.CreateCommand();

            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS Departments (
                    Id INTEGER PRIMARY KEY,
                    Name TEXT NOT NULL,
                    Capacity INTEGER NOT NULL
                );

                CREATE TABLE IF NOT EXISTS Doctors (
                    Id INTEGER PRIMARY KEY,
                    FirstName TEXT NOT NULL,
                    LastName TEXT NOT NULL,
                    DepartmentId INTEGER,
                    Phone TEXT,
                    FOREIGN KEY (DepartmentId) REFERENCES Departments(Id)
                );

                CREATE TABLE IF NOT EXISTS Patients (
                    Id INTEGER PRIMARY KEY,
                    FirstName TEXT NOT NULL,
                    LastName TEXT NOT NULL,
                    NationalId TEXT NOT NULL,
                    Phone TEXT,
                    BirthDate TEXT NOT NULL
                );

                CREATE TABLE IF NOT EXISTS Appointments (
                    Id INTEGER PRIMARY KEY,
                    PatientId INTEGER NOT NULL,
                    DoctorId INTEGER NOT NULL,
                    StartTime TEXT NOT NULL,
                    EndTime TEXT NOT NULL,
                    Status TEXT DEFAULT 'Waiting',
                    FOREIGN KEY (PatientId) REFERENCES Patients(Id),
                    FOREIGN KEY (DoctorId) REFERENCES Doctors(Id)
                );

                CREATE TABLE IF NOT EXISTS Visits (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    PatientId INTEGER NOT NULL,
                    DoctorId INTEGER NOT NULL,
                    VisitDate TEXT NOT NULL,
                    Notes TEXT,
                    FOREIGN KEY (PatientId) REFERENCES Patients(Id),
                    FOREIGN KEY (DoctorId) REFERENCES Doctors(Id)
                );

                CREATE TABLE IF NOT EXISTS Drugs (
                    Id INTEGER PRIMARY KEY,
                    Name TEXT NOT NULL,
                    Unit TEXT NOT NULL,
                    Stock INTEGER NOT NULL DEFAULT 0,
                    LowStockThreshold INTEGER NOT NULL DEFAULT 10
                );

                CREATE TABLE IF NOT EXISTS Prescriptions (
                    Id INTEGER PRIMARY KEY,
                    PatientId INTEGER NOT NULL,
                    PatientName TEXT NOT NULL,
                    DoctorId INTEGER NOT NULL,
                    DoctorName TEXT NOT NULL,
                    Date TEXT NOT NULL
                );

                CREATE TABLE IF NOT EXISTS PrescriptionItems (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    PrescriptionId INTEGER NOT NULL,
                    DrugId INTEGER NOT NULL,
                    DrugName TEXT NOT NULL,
                    Quantity INTEGER NOT NULL,
                    Dosage TEXT,
                    FOREIGN KEY (PrescriptionId) REFERENCES Prescriptions(Id)
                );

                CREATE TABLE IF NOT EXISTS Invoices (
                    Id INTEGER PRIMARY KEY,
                    AppointmentId INTEGER NOT NULL,
                    PatientName TEXT NOT NULL,
                    DoctorName TEXT NOT NULL,
                    Date TEXT NOT NULL,
                    BaseAmount REAL NOT NULL,
                    InsuranceCoveragePercent REAL NOT NULL DEFAULT 0,
                    Status TEXT NOT NULL DEFAULT 'Pending'
                );

                CREATE TABLE IF NOT EXISTS DoctorShifts (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    DoctorId INTEGER NOT NULL,
                    DoctorName TEXT NOT NULL,
                    Day INTEGER NOT NULL,
                    StartHour INTEGER NOT NULL,
                    EndHour INTEGER NOT NULL
                );
            ";
            await cmd.ExecuteNonQueryAsync();
        }

        // ============================================
        // DEPARTMENT CRUD
        // ============================================
        public async Task SaveDepartmentAsync(Department dept)
        {
            await using var conn = GetConnection();
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"INSERT OR REPLACE INTO Departments (Id, Name, Capacity) VALUES (@id, @name, @cap)";
            cmd.Parameters.AddWithValue("@id", dept.Id);
            cmd.Parameters.AddWithValue("@name", dept.Name);
            cmd.Parameters.AddWithValue("@cap", dept.Capacity);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<List<(int id, string name, int capacity)>> LoadDepartmentsAsync()
        {
            var result = new List<(int, string, int)>();
            await using var conn = GetConnection();
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, Name, Capacity FROM Departments";
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add((reader.GetInt32(0), reader.GetString(1), reader.GetInt32(2)));
            }
            return result;
        }

        // ============================================
        // PATIENT CRUD
        // ============================================
        public async Task SavePatientAsync(Patient p)
        {
            await using var conn = GetConnection();
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"INSERT OR REPLACE INTO Patients (Id, FirstName, LastName, NationalId, Phone, BirthDate)
                                VALUES (@id, @fn, @ln, @nid, @phone, @bd)";
            cmd.Parameters.AddWithValue("@id", p.Id);
            cmd.Parameters.AddWithValue("@fn", p.FirstName);
            cmd.Parameters.AddWithValue("@ln", p.LastName);
            cmd.Parameters.AddWithValue("@nid", p.NationalId);
            cmd.Parameters.AddWithValue("@phone", p.Phone);
            cmd.Parameters.AddWithValue("@bd", p.BirthDate.ToString("yyyy-MM-dd"));
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task DeletePatientAsync(int patientId)
        {
            await using var conn = GetConnection();
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM Patients WHERE Id = @id";
            cmd.Parameters.AddWithValue("@id", patientId);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<List<(int id, string firstName, string lastName, string nationalId, string phone, string birthDate)>> LoadPatientsAsync()
        {
            var result = new List<(int, string, string, string, string, string)>();
            await using var conn = GetConnection();
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, FirstName, LastName, NationalId, Phone, BirthDate FROM Patients";
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add((
                    reader.GetInt32(0),
                    reader.GetString(1),
                    reader.GetString(2),
                    reader.GetString(3),
                    reader.IsDBNull(4) ? "" : reader.GetString(4),
                    reader.GetString(5)
                ));
            }
            return result;
        }

        // ============================================
        // DOCTOR CRUD
        // ============================================
        public async Task SaveDoctorAsync(Doctor d)
        {
            await using var conn = GetConnection();
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"INSERT OR REPLACE INTO Doctors (Id, FirstName, LastName, DepartmentId, Phone)
                                VALUES (@id, @fn, @ln, @did, @phone)";
            cmd.Parameters.AddWithValue("@id", d.Id);
            cmd.Parameters.AddWithValue("@fn", d.FirstName);
            cmd.Parameters.AddWithValue("@ln", d.LastName);
            cmd.Parameters.AddWithValue("@did", d.DepartmentId);
            cmd.Parameters.AddWithValue("@phone", d.Phone);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task DeleteDoctorAsync(int doctorId)
        {
            await using var conn = GetConnection();
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM Doctors WHERE Id = @id";
            cmd.Parameters.AddWithValue("@id", doctorId);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<List<(int id, string firstName, string lastName, int departmentId, string phone)>> LoadDoctorsAsync()
        {
            var result = new List<(int, string, string, int, string)>();
            await using var conn = GetConnection();
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, FirstName, LastName, DepartmentId, Phone FROM Doctors";
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add((
                    reader.GetInt32(0),
                    reader.GetString(1),
                    reader.GetString(2),
                    reader.IsDBNull(3) ? 0 : reader.GetInt32(3),
                    reader.IsDBNull(4) ? "" : reader.GetString(4)
                ));
            }
            return result;
        }

        // ============================================
        // APPOINTMENT CRUD
        // ============================================
        public async Task SaveAppointmentAsync(Appointment app)
        {
            await using var conn = GetConnection();
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"INSERT OR REPLACE INTO Appointments (Id, PatientId, DoctorId, StartTime, EndTime, Status)
                                VALUES (@id, @pid, @did, @st, @et, @stt)";
            cmd.Parameters.AddWithValue("@id", app.Id);
            cmd.Parameters.AddWithValue("@pid", app.Patient.Id);
            cmd.Parameters.AddWithValue("@did", app.Doctor.Id);
            cmd.Parameters.AddWithValue("@st", app.Start.ToString("yyyy-MM-dd HH:mm"));
            cmd.Parameters.AddWithValue("@et", app.Start.AddMinutes(30).ToString("yyyy-MM-dd HH:mm"));
            cmd.Parameters.AddWithValue("@stt", app.Status);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task DeleteAppointmentAsync(int appointmentId)
        {
            await using var conn = GetConnection();
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM Appointments WHERE Id = @id";
            cmd.Parameters.AddWithValue("@id", appointmentId);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<List<(int id, int patientId, int doctorId, string startTime, string endTime, string status)>> LoadAppointmentsAsync()
        {
            var result = new List<(int, int, int, string, string, string)>();
            await using var conn = GetConnection();
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, PatientId, DoctorId, StartTime, EndTime, Status FROM Appointments";
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add((
                    reader.GetInt32(0),
                    reader.GetInt32(1),
                    reader.GetInt32(2),
                    reader.GetString(3),
                    reader.GetString(4),
                    reader.GetString(5)
                ));
            }
            return result;
        }

        // ============================================
        // VISITS CRUD
        // ============================================
        public async Task SaveVisitAsync(int patientId, int doctorId, DateTime visitDate, string notes)
        {
            await using var conn = GetConnection();
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT INTO Visits (PatientId, DoctorId, VisitDate, Notes) VALUES (@pid, @did, @dt, @notes)";
            cmd.Parameters.AddWithValue("@pid", patientId);
            cmd.Parameters.AddWithValue("@did", doctorId);
            cmd.Parameters.AddWithValue("@dt", visitDate.ToString("yyyy-MM-dd HH:mm"));
            cmd.Parameters.AddWithValue("@notes", notes ?? "");
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<List<(int patientId, int doctorId, string visitDate, string notes)>> LoadVisitsAsync()
        {
            var result = new List<(int, int, string, string)>();
            await using var conn = GetConnection();
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT PatientId, DoctorId, VisitDate, Notes FROM Visits";
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add((
                    reader.GetInt32(0),
                    reader.GetInt32(1),
                    reader.GetString(2),
                    reader.IsDBNull(3) ? "" : reader.GetString(3)
                ));
            }
            return result;
        }

        // ============================================
        // DRUG CRUD
        // ============================================
        public async Task SaveDrugAsync(Drug drug)
        {
            await using var conn = GetConnection();
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"INSERT OR REPLACE INTO Drugs (Id, Name, Unit, Stock, LowStockThreshold)
                                VALUES (@id, @name, @unit, @stock, @threshold)";
            cmd.Parameters.AddWithValue("@id", drug.Id);
            cmd.Parameters.AddWithValue("@name", drug.Name);
            cmd.Parameters.AddWithValue("@unit", drug.Unit);
            cmd.Parameters.AddWithValue("@stock", drug.Stock);
            cmd.Parameters.AddWithValue("@threshold", drug.LowStockThreshold);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<List<(int id, string name, string unit, int stock, int threshold)>> LoadDrugsAsync()
        {
            var result = new List<(int, string, string, int, int)>();
            await using var conn = GetConnection();
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, Name, Unit, Stock, LowStockThreshold FROM Drugs";
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                result.Add((reader.GetInt32(0), reader.GetString(1), reader.GetString(2), reader.GetInt32(3), reader.GetInt32(4)));
            return result;
        }

        // ============================================
        // PRESCRIPTION CRUD
        // ============================================
        public async Task SavePrescriptionAsync(Prescription rx)
        {
            await using var conn = GetConnection();
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"INSERT OR REPLACE INTO Prescriptions (Id, PatientId, PatientName, DoctorId, DoctorName, Date)
                                VALUES (@id, @pid, @pname, @did, @dname, @date)";
            cmd.Parameters.AddWithValue("@id", rx.Id);
            cmd.Parameters.AddWithValue("@pid", rx.PatientId);
            cmd.Parameters.AddWithValue("@pname", rx.PatientName);
            cmd.Parameters.AddWithValue("@did", rx.DoctorId);
            cmd.Parameters.AddWithValue("@dname", rx.DoctorName);
            cmd.Parameters.AddWithValue("@date", rx.Date.ToString("yyyy-MM-dd HH:mm"));
            await cmd.ExecuteNonQueryAsync();

            // Save items
            foreach (var item in rx.Items)
            {
                await using var cmd2 = conn.CreateCommand();
                cmd2.CommandText = @"INSERT INTO PrescriptionItems (PrescriptionId, DrugId, DrugName, Quantity, Dosage)
                                     VALUES (@rxid, @did, @dname, @qty, @dosage)";
                cmd2.Parameters.AddWithValue("@rxid", rx.Id);
                cmd2.Parameters.AddWithValue("@did", item.Drug.Id);
                cmd2.Parameters.AddWithValue("@dname", item.Drug.Name);
                cmd2.Parameters.AddWithValue("@qty", item.Quantity);
                cmd2.Parameters.AddWithValue("@dosage", item.Dosage ?? "");
                await cmd2.ExecuteNonQueryAsync();
            }
        }

        public async Task<List<(int id, int patientId, string patientName, int doctorId, string doctorName, string date)>> LoadPrescriptionsAsync()
        {
            var result = new List<(int, int, string, int, string, string)>();
            await using var conn = GetConnection();
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, PatientId, PatientName, DoctorId, DoctorName, Date FROM Prescriptions ORDER BY Date DESC";
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                result.Add((reader.GetInt32(0), reader.GetInt32(1), reader.GetString(2), reader.GetInt32(3), reader.GetString(4), reader.GetString(5)));
            return result;
        }

        // ============================================
        // INVOICE CRUD
        // ============================================
        public async Task SaveInvoiceAsync(Invoice inv)
        {
            await using var conn = GetConnection();
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"INSERT OR REPLACE INTO Invoices (Id, AppointmentId, PatientName, DoctorName, Date, BaseAmount, InsuranceCoveragePercent, Status)
                                VALUES (@id, @appid, @pname, @dname, @date, @base, @ins, @status)";
            cmd.Parameters.AddWithValue("@id", inv.Id);
            cmd.Parameters.AddWithValue("@appid", inv.AppointmentId);
            cmd.Parameters.AddWithValue("@pname", inv.PatientName);
            cmd.Parameters.AddWithValue("@dname", inv.DoctorName);
            cmd.Parameters.AddWithValue("@date", inv.Date.ToString("yyyy-MM-dd HH:mm"));
            cmd.Parameters.AddWithValue("@base", (double)inv.BaseAmount);
            cmd.Parameters.AddWithValue("@ins", (double)inv.InsuranceCoveragePercent);
            cmd.Parameters.AddWithValue("@status", inv.Status);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<List<(int id, int appId, string patientName, string doctorName, string date, double baseAmount, double insurancePct, string status)>> LoadInvoicesAsync()
        {
            var result = new List<(int, int, string, string, string, double, double, string)>();
            await using var conn = GetConnection();
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, AppointmentId, PatientName, DoctorName, Date, BaseAmount, InsuranceCoveragePercent, Status FROM Invoices ORDER BY Date DESC";
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                result.Add((reader.GetInt32(0), reader.GetInt32(1), reader.GetString(2), reader.GetString(3), reader.GetString(4), reader.GetDouble(5), reader.GetDouble(6), reader.GetString(7)));
            return result;
        }

        // ============================================
        // DOCTOR SHIFT CRUD
        // ============================================
        public async Task SaveShiftAsync(DoctorShift shift)
        {
            await using var conn = GetConnection();
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"INSERT OR REPLACE INTO DoctorShifts (Id, DoctorId, DoctorName, Day, StartHour, EndHour)
                                VALUES (@id, @did, @dname, @day, @start, @end)";
            cmd.Parameters.AddWithValue("@id", shift.Id);
            cmd.Parameters.AddWithValue("@did", shift.DoctorId);
            cmd.Parameters.AddWithValue("@dname", shift.DoctorName);
            cmd.Parameters.AddWithValue("@day", (int)shift.Day);
            cmd.Parameters.AddWithValue("@start", shift.StartHour);
            cmd.Parameters.AddWithValue("@end", shift.EndHour);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task DeleteShiftAsync(int shiftId)
        {
            await using var conn = GetConnection();
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM DoctorShifts WHERE Id = @id";
            cmd.Parameters.AddWithValue("@id", shiftId);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<List<(int id, int doctorId, string doctorName, int day, int startHour, int endHour)>> LoadShiftsAsync()
        {
            var result = new List<(int, int, string, int, int, int)>();
            await using var conn = GetConnection();
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, DoctorId, DoctorName, Day, StartHour, EndHour FROM DoctorShifts ORDER BY Day, StartHour";
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                result.Add((reader.GetInt32(0), reader.GetInt32(1), reader.GetString(2), reader.GetInt32(3), reader.GetInt32(4), reader.GetInt32(5)));
            return result;
        }
    }
}

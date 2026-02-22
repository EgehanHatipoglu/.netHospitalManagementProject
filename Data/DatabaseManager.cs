using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using HospitalManagementWPF.Models;

namespace HospitalManagementWPF.Data
{
    /// <summary>
    /// SQLite Database Manager for persistent storage.
    /// Handles schema creation and CRUD operations for all entities.
    /// </summary>
    public class DatabaseManager
    {
        private readonly string _connectionString;

        public DatabaseManager(string dbPath = "hospital.db")
        {
            _connectionString = $"Data Source={dbPath}";
            InitializeDatabase();
        }

        private SqliteConnection GetConnection()
        {
            var connection = new SqliteConnection(_connectionString);
            connection.Open();
            return connection;
        }

        private void InitializeDatabase()
        {
            using var connection = GetConnection();
            using var cmd = connection.CreateCommand();

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
            ";
            cmd.ExecuteNonQuery();
        }

        // ============================================
        // DEPARTMENT CRUD
        // ============================================
        public void SaveDepartment(Department dept)
        {
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"INSERT OR REPLACE INTO Departments (Id, Name, Capacity) VALUES (@id, @name, @cap)";
            cmd.Parameters.AddWithValue("@id", dept.Id);
            cmd.Parameters.AddWithValue("@name", dept.Name);
            cmd.Parameters.AddWithValue("@cap", dept.Capacity);
            cmd.ExecuteNonQuery();
        }

        public List<(int id, string name, int capacity)> LoadDepartments()
        {
            var result = new List<(int, string, int)>();
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, Name, Capacity FROM Departments";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                result.Add((reader.GetInt32(0), reader.GetString(1), reader.GetInt32(2)));
            }
            return result;
        }

        // ============================================
        // PATIENT CRUD
        // ============================================
        public void SavePatient(Patient p)
        {
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"INSERT OR REPLACE INTO Patients (Id, FirstName, LastName, NationalId, Phone, BirthDate)
                                VALUES (@id, @fn, @ln, @nid, @phone, @bd)";
            cmd.Parameters.AddWithValue("@id", p.Id);
            cmd.Parameters.AddWithValue("@fn", p.FirstName);
            cmd.Parameters.AddWithValue("@ln", p.LastName);
            cmd.Parameters.AddWithValue("@nid", p.NationalId);
            cmd.Parameters.AddWithValue("@phone", p.Phone);
            cmd.Parameters.AddWithValue("@bd", p.BirthDate.ToString("yyyy-MM-dd"));
            cmd.ExecuteNonQuery();
        }

        public void DeletePatient(int patientId)
        {
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM Patients WHERE Id = @id";
            cmd.Parameters.AddWithValue("@id", patientId);
            cmd.ExecuteNonQuery();
        }

        public List<(int id, string firstName, string lastName, string nationalId, string phone, string birthDate)> LoadPatients()
        {
            var result = new List<(int, string, string, string, string, string)>();
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, FirstName, LastName, NationalId, Phone, BirthDate FROM Patients";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                result.Add((reader.GetInt32(0), reader.GetString(1), reader.GetString(2),
                            reader.GetString(3), reader.GetString(4), reader.GetString(5)));
            }
            return result;
        }

        // ============================================
        // DOCTOR CRUD
        // ============================================
        public void SaveDoctor(Doctor d)
        {
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"INSERT OR REPLACE INTO Doctors (Id, FirstName, LastName, DepartmentId, Phone)
                                VALUES (@id, @fn, @ln, @did, @phone)";
            cmd.Parameters.AddWithValue("@id", d.Id);
            cmd.Parameters.AddWithValue("@fn", d.FirstName);
            cmd.Parameters.AddWithValue("@ln", d.LastName);
            cmd.Parameters.AddWithValue("@did", d.Department?.Id ?? 0);
            cmd.Parameters.AddWithValue("@phone", d.Phone);
            cmd.ExecuteNonQuery();
        }

        public void DeleteDoctor(int doctorId)
        {
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM Doctors WHERE Id = @id";
            cmd.Parameters.AddWithValue("@id", doctorId);
            cmd.ExecuteNonQuery();
        }

        public List<(int id, string firstName, string lastName, int departmentId, string phone)> LoadDoctors()
        {
            var result = new List<(int, string, string, int, string)>();
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, FirstName, LastName, DepartmentId, Phone FROM Doctors";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                result.Add((reader.GetInt32(0), reader.GetString(1), reader.GetString(2),
                            reader.GetInt32(3), reader.GetString(4)));
            }
            return result;
        }

        // ============================================
        // APPOINTMENT CRUD
        // ============================================
        public void SaveAppointment(Appointment a)
        {
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"INSERT OR REPLACE INTO Appointments (Id, PatientId, DoctorId, StartTime, EndTime, Status)
                                VALUES (@id, @pid, @did, @start, @end, @status)";
            cmd.Parameters.AddWithValue("@id", a.Id);
            cmd.Parameters.AddWithValue("@pid", a.Patient.Id);
            cmd.Parameters.AddWithValue("@did", a.Doctor.Id);
            cmd.Parameters.AddWithValue("@start", a.Start.ToString("yyyy-MM-dd HH:mm"));
            cmd.Parameters.AddWithValue("@end", a.End.ToString("yyyy-MM-dd HH:mm"));
            cmd.Parameters.AddWithValue("@status", a.Status);
            cmd.ExecuteNonQuery();
        }

        public void DeleteAppointment(int appointmentId)
        {
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM Appointments WHERE Id = @id";
            cmd.Parameters.AddWithValue("@id", appointmentId);
            cmd.ExecuteNonQuery();
        }

        public List<(int id, int patientId, int doctorId, string startTime, string endTime, string status)> LoadAppointments()
        {
            var result = new List<(int, int, int, string, string, string)>();
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, PatientId, DoctorId, StartTime, EndTime, Status FROM Appointments";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                result.Add((reader.GetInt32(0), reader.GetInt32(1), reader.GetInt32(2),
                            reader.GetString(3), reader.GetString(4), reader.GetString(5)));
            }
            return result;
        }

        // ============================================
        // VISIT CRUD
        // ============================================
        public void SaveVisit(int patientId, int doctorId, DateTime visitDate, string notes)
        {
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"INSERT INTO Visits (PatientId, DoctorId, VisitDate, Notes)
                                VALUES (@pid, @did, @date, @notes)";
            cmd.Parameters.AddWithValue("@pid", patientId);
            cmd.Parameters.AddWithValue("@did", doctorId);
            cmd.Parameters.AddWithValue("@date", visitDate.ToString("yyyy-MM-dd HH:mm"));
            cmd.Parameters.AddWithValue("@notes", notes);
            cmd.ExecuteNonQuery();
        }

        public List<(int patientId, int doctorId, string visitDate, string notes)> LoadVisits()
        {
            var result = new List<(int, int, string, string)>();
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT PatientId, DoctorId, VisitDate, Notes FROM Visits";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                result.Add((reader.GetInt32(0), reader.GetInt32(1), reader.GetString(2), reader.GetString(3)));
            }
            return result;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using HospitalManagementWPF.Data;
using HospitalManagementWPF.DataStructures;
using HospitalManagementWPF.Models;

namespace HospitalManagementWPF;

public partial class MainWindow : Window
{
    private const int STUDENT_ID = 230316064;
    private readonly int MAX_PATIENT_NUMBER;
    private readonly int MAX_DOCTOR_NUMBER;

    private HashTable<int, Patient> _patients;
    private HashTable<int, Doctor> _doctors;
    private HashTable<int, Appointment> _appointments;
    private HashTable<int, Department> _departments;

    private PatientBST _patientBST;
    private PatientAVL _patientAVL;
    private HospitalTree _hospitalTree;
    private ERPriorityQueue _erQueue;
    private UndoStack _undoStack;
    private DatabaseManager _db;

    private int _patientIdCounter, _doctorIdCounter, _appointmentIdCounter, _departmentIdCounter;

    private StackPanel[] _allPanels = null!;
    private List<Department> _departmentList = new();

    // Fix 4: Safe in-place edit tracking
    private int? _editingPatientId = null;

    // Fix 5: Delete confirmation
    private Action? _pendingDeleteAction = null;

    // Feature 4: Calendar week state
    private DateTime _currentWeekStart;

    public MainWindow()
    {
        InitializeComponent();

        MAX_PATIENT_NUMBER = STUDENT_ID % 10 + 3;
        MAX_DOCTOR_NUMBER = STUDENT_ID % 10 + 8;

        _patients = new HashTable<int, Patient>(STUDENT_ID);
        _doctors = new HashTable<int, Doctor>(STUDENT_ID);
        _appointments = new HashTable<int, Appointment>(STUDENT_ID);
        _departments = new HashTable<int, Department>(STUDENT_ID);

        _patientBST = new PatientBST();
        _patientAVL = new PatientAVL();
        _hospitalTree = new HospitalTree("Manisa Celal Bayar University Hospital");
        _erQueue = new ERPriorityQueue();
        _undoStack = new UndoStack();
        _db = new DatabaseManager();

        _allPanels = new[] { PanelDashboard, PanelPatients, PanelDoctors, PanelAppointments, PanelEmergency,
                             PanelBST, PanelAVL, PanelDepartments, PanelUndo, PanelStats };

        SliderSeverity.PropertyChanged += (s, e) =>
        {
            if (e.Property.Name == "Value")
                TxtSeverityValue.Text = ((int)SliderSeverity.Value).ToString();
        };

        LoadSampleData();
        LoadFromDatabase();
        RefreshAllViews();
        RefreshDashboard();

        // Initialize calendar to current week (Monday)
        int dow = (int)DateTime.Today.DayOfWeek;
        _currentWeekStart = DateTime.Today.AddDays(dow == 0 ? -6 : 1 - dow);
    }

    // ============ NAVIGATION ============
    private void Nav_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string tag)
        {
            foreach (var p in _allPanels) p.IsVisible = false;
            switch (tag)
            {
                case "Dashboard": PanelDashboard.IsVisible = true; RefreshDashboard(); break;
                case "Patients": PanelPatients.IsVisible = true; break;
                case "Doctors": PanelDoctors.IsVisible = true; break;
                case "Appointments": PanelAppointments.IsVisible = true; break;
                case "Emergency": PanelEmergency.IsVisible = true; RefreshERQueue(); break;
                case "BST": PanelBST.IsVisible = true; break;
                case "AVL": PanelAVL.IsVisible = true; break;
                case "Departments": PanelDepartments.IsVisible = true; break;
                case "Undo": PanelUndo.IsVisible = true; UpdateUndoPeek(); break;
                case "Stats": PanelStats.IsVisible = true; RefreshStats(); break;
            }
        }
    }

    // ============ PATIENT ============
    private void RegisterPatient_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            string fn = TxtPatientFirstName.Text?.Trim() ?? "";
            string ln = TxtPatientLastName.Text?.Trim() ?? "";
            string nid = TxtPatientNationalId.Text?.Trim() ?? "";
            string phone = TxtPatientPhone.Text?.Trim() ?? "";

            if (string.IsNullOrEmpty(fn) || string.IsNullOrEmpty(ln)) { ShowValidation(TxtPatientValidation, "Ad ve Soyad boş olamaz!"); return; }
            if (nid.Length != 11 || !nid.All(char.IsDigit)) { ShowValidation(TxtPatientValidation, "TC 11 hane olmalı!"); return; }
            TxtPatientValidation.Text = "";

            DateTime bd = DpPatientBirthDate.SelectedDate?.DateTime ?? DateTime.Today;

            // Fix 4: In-place update mode
            if (_editingPatientId.HasValue)
            {
                int editId = _editingPatientId.Value;
                var existing = _patients.Get(editId);
                if (existing != null)
                {
                    _patientBST.Delete(existing.FirstName, existing.LastName);
                    _patientAVL.Delete(existing.FirstName, existing.LastName);

                    existing.FirstName = fn;
                    existing.LastName = ln;
                    existing.NationalId = nid;
                    existing.Phone = phone;
                    existing.BirthDate = bd;

                    _patientBST.Insert(existing);
                    _patientAVL.Insert(existing);
                    _db.SavePatient(existing);

                    _editingPatientId = null;
                    BtnRegisterPatient.Content = "✓ Hasta Kaydet";
                    SetStatus($"✓ Hasta güncellendi! ID: {editId}");
                }
            }
            else
            {
                if (_patients.Size >= MAX_PATIENT_NUMBER) { ShowMsg($"Maks hasta kapasitesi: {MAX_PATIENT_NUMBER}"); return; }
                if (_patientBST.Search(fn, ln) != null) { ShowValidation(TxtPatientValidation, "Bu isimde hasta mevcut!"); return; }

                int id = 1;
                while (_patients.Get(id) != null) id++;
                if (id > _patientIdCounter) _patientIdCounter = id;
                
                var p = new Patient(id, fn, ln, nid, phone, bd);
                _patients.Put(id, p); _patientBST.Insert(p); _patientAVL.Insert(p);
                _undoStack.Push("PATIENT_ADD:" + id); _db.SavePatient(p);
                SetStatus($"✓ Hasta kaydedildi! ID: {id}");
            }

            TxtPatientFirstName.Text = ""; TxtPatientLastName.Text = "";
            TxtPatientNationalId.Text = ""; TxtPatientPhone.Text = "";

            RefreshPatientList();
        }
        catch (Exception ex) { ShowMsg(ex.Message); }
    }

    private void ShowHistory_Click(object? sender, RoutedEventArgs e)
    {
        if (int.TryParse(TxtHistoryPatientId.Text, out int pid))
        {
            var p = _patients.Get(pid);
            LbHistory.ItemsSource = p?.GetHistory() ?? new List<string> { "Hasta bulunamadı!" };
        }
    }

    // ============ DOCTOR ============
    private void RegisterDoctor_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (_doctors.Size >= MAX_DOCTOR_NUMBER) { ShowMsg($"Maks doktor kapasitesi: {MAX_DOCTOR_NUMBER}"); return; }

            string fn = TxtDoctorFirstName.Text?.Trim() ?? "";
            string ln = TxtDoctorLastName.Text?.Trim() ?? "";
            string phone = TxtDoctorPhone.Text?.Trim() ?? "";

            if (string.IsNullOrEmpty(fn) || string.IsNullOrEmpty(ln)) { ShowMsg("Ad/Soyad boş olamaz!"); return; }

            int si = CbDoctorDepartment.SelectedIndex;
            if (si < 0 || si >= _departmentList.Count) { ShowMsg("Bölüm seçiniz!"); return; }

            var dept = _departmentList[si];
            int id = 1;
            while (_doctors.Get(id) != null) id++;
            if (id > _doctorIdCounter) _doctorIdCounter = id;
            
            var d = new Doctor(id, fn, ln, dept, phone);

            if (dept.AddDoctor(d))
            {
                _doctors.Put(id, d); _undoStack.Push("DOCTOR_ADD:" + id); _db.SaveDoctor(d);
                TxtDoctorFirstName.Text = ""; TxtDoctorLastName.Text = ""; TxtDoctorPhone.Text = "";
                RefreshDoctorList(); RefreshDepartmentList();
                SetStatus($"✓ Doktor kaydedildi! ID: {id}");
            }
            else { if (id == _doctorIdCounter) _doctorIdCounter--; ShowMsg($"Bölüm dolu: {dept.DoctorCount}/{dept.Capacity}"); }
        }
        catch (Exception ex) { ShowMsg(ex.Message); }
    }

    // ============ APPOINTMENT ============
    private void CreateAppointment_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (!int.TryParse(TxtAppPatientId.Text, out int pid)) { ShowMsg("Geçersiz Hasta ID!"); return; }
            if (!int.TryParse(TxtAppDoctorId.Text, out int did)) { ShowMsg("Geçersiz Doktor ID!"); return; }

            var patient = _patients.Get(pid); if (patient == null) { ShowMsg("Hasta bulunamadı!"); return; }
            var doctor = _doctors.Get(did); if (doctor == null) { ShowMsg("Doktor bulunamadı!"); return; }

            if (!TimeSpan.TryParse(TxtAppTime.Text?.Trim(), out var time)) { ShowMsg("Saat formatı: 14:30"); return; }

            DateTime dt = (DpAppDate.SelectedDate?.DateTime ?? DateTime.Today).Date + time;
            if (dt < DateTime.Now) { ShowMsg("Geçmiş tarih!"); return; }
            if (DoctorHasConflict(doctor, dt)) { ShowMsg("Doktor müsait değil!"); return; }
            if (PatientHasConflict(patient, dt)) { ShowMsg("Hastanın randevusu var!"); return; }

            int id = ++_appointmentIdCounter;
            var app = new Appointment(id, patient, doctor, dt);
            _appointments.Put(id, app); doctor.DailyQueue.Enqueue(app);
            _undoStack.Push("APPOINTMENT_ADD:" + id); _db.SaveAppointment(app);

            TxtAppPatientId.Text = ""; TxtAppDoctorId.Text = ""; TxtAppTime.Text = "";
            RefreshAppointmentList();
            SetStatus($"✓ Randevu oluşturuldu! ID: {id}");
        }
        catch (Exception ex) { ShowMsg(ex.Message); }
    }

    private void ShowDoctorQueue_Click(object? sender, RoutedEventArgs e)
    {
        if (int.TryParse(TxtQueueDoctorId.Text, out int did))
        {
            var d = _doctors.Get(did);
            if (d != null)
            {
                var items = d.DailyQueue.GetAll().Select((a, i) => $"{i + 1}. {a}").ToList();
                LbDoctorQueue.ItemsSource = items.Count > 0 ? items : new[] { "Kuyruk boş." };
            }
            else LbDoctorQueue.ItemsSource = new[] { "Doktor bulunamadı!" };
        }
    }

    private void ExaminePatient_Click(object? sender, RoutedEventArgs e)
    {
        if (int.TryParse(TxtQueueDoctorId.Text, out int did))
        {
            var d = _doctors.Get(did); if (d == null) { ShowMsg("Doktor bulunamadı!"); return; }
            var app = d.DailyQueue.Dequeue(); if (app == null) { ShowMsg("Kuyruk boş."); return; }
            app.Patient.AddVisit(DateTime.Now, d, "Muayene tamamlandı");
            app.Status = "Completed";
            _db.SaveVisit(app.Patient.Id, d.Id, DateTime.Now, "Muayene tamamlandı");
            _db.SaveAppointment(app); // Fix 3: Persist status change to DB
            ShowDoctorQueue_Click(sender, e); RefreshAppointmentList();
            SetStatus($"✓ Muayene: {app.Patient.FullName}");
        }
    }

    // ============ EMERGENCY ============
    private void AddERPatient_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (!int.TryParse(TxtERPatientId.Text, out int pid)) { ShowMsg("Geçersiz ID!"); return; }
            var p = _patients.Get(pid); if (p == null) { ShowMsg("Hasta bulunamadı!"); return; }

            int sev = (int)SliderSeverity.Value;
            string complaint = TxtERComplaint.Text?.Trim() ?? "";
            if (string.IsNullOrEmpty(complaint)) { ShowMsg("Şikayet boş olamaz!"); return; }

            var erp = new ERPriorityQueue.ERPatient(p, sev, complaint);
            _erQueue.AddPatient(erp); _undoStack.Push("ER_ADD", erp);

            TxtERPatientId.Text = ""; TxtERComplaint.Text = ""; SliderSeverity.Value = 5;
            RefreshERQueue();
            SetStatus($"✓ Acil kabul! Severity: {sev}/10");
        }
        catch (Exception ex) { ShowMsg(ex.Message); }
    }

    private void TreatHighest_Click(object? sender, RoutedEventArgs e)
    {
        var erp = _erQueue.RemoveHighestPriority();
        if (erp == null) { ShowMsg("Acil serviste hasta yok."); return; }
        SetStatus($"✓ Tedavi: {erp.Patient.FullName} (Severity: {erp.Severity}/10)");
        RefreshERQueue();
    }

    // ============ BST & AVL ============
    private void SearchBST_Click(object? sender, RoutedEventArgs e)
    {
        var found = _patientBST.Search(TxtBSTFirstName.Text?.Trim() ?? "", TxtBSTLastName.Text?.Trim() ?? "");
        TxtBSTResult.Text = found != null ? $"✓ Bulundu! {found}" : "✗ Bulunamadı.";
        TxtBSTResult.Foreground = new SolidColorBrush(found != null ? Color.Parse("#3FB950") : Color.Parse("#F85149"));
    }

    private void ListBST_Click(object? sender, RoutedEventArgs e)
    {
        var patients = _patientBST.GetAllInOrder();
        LbBST.ItemsSource = patients.Count > 0
            ? patients.Select((p, i) => $"{i + 1}. {p.FullName} (ID: {p.Id})").ToList()
            : new List<string> { "BST boş." };
    }

    private void SearchAVL_Click(object? sender, RoutedEventArgs e)
    {
        var found = _patientAVL.Search(TxtAVLFirstName.Text?.Trim() ?? "", TxtAVLLastName.Text?.Trim() ?? "");
        TxtAVLResult.Text = found != null ? $"✓ Bulundu! {found}" : "✗ Bulunamadı.";
        TxtAVLResult.Foreground = new SolidColorBrush(found != null ? Color.Parse("#3FB950") : Color.Parse("#F85149"));
    }

    private void ListAVL_Click(object? sender, RoutedEventArgs e)
    {
        var patients = _patientAVL.GetAllInOrder();
        LbAVL.ItemsSource = patients.Count > 0
            ? patients.Select((p, i) => $"{i + 1}. {p.FullName} (ID: {p.Id})").ToList()
            : new List<string> { "AVL boş." };
    }

    // ============ DEPARTMENTS ============
    private void CreateDepartment_Click(object? sender, RoutedEventArgs e)
    {
        string name = TxtDeptName.Text?.Trim() ?? "";
        if (string.IsNullOrEmpty(name)) { ShowMsg("Bölüm adı boş!"); return; }
        if (!int.TryParse(TxtDeptCapacity.Text, out int cap) || cap <= 0) { ShowMsg("Geçersiz kapasite!"); return; }

        var dept = new Department(++_departmentIdCounter, name, cap);
        _departments.Put(_departmentIdCounter, dept); _hospitalTree.AddDepartmentToRoot(dept); _db.SaveDepartment(dept);
        TxtDeptName.Text = ""; TxtDeptCapacity.Text = "";
        RefreshDepartmentList();
        SetStatus($"✓ Bölüm oluşturuldu! ID: {_departmentIdCounter}");
    }

    private void ShowHierarchy_Click(object? sender, RoutedEventArgs e)
    {
        TvHierarchy.Items.Clear();
        var hierarchy = _hospitalTree.GetHierarchy();
        if (hierarchy.Count == 0) return;

        var root = new TreeViewItem { Header = $"🏥 {hierarchy[0].name} ({hierarchy[0].doctorCount} doctors)", IsExpanded = true };
        for (int i = 1; i < hierarchy.Count; i++)
            root.Items.Add(new TreeViewItem { Header = $"🏛️ {hierarchy[i].name} ({hierarchy[i].doctorCount} doctors)" });
        TvHierarchy.Items.Add(root);
    }

    // ============ UNDO ============
    private void Undo_Click(object? sender, RoutedEventArgs e)
    {
        if (_undoStack.IsEmpty)
        {
            TxtUndoResult.Text = "Geri alınacak işlem yok.";
            TxtUndoResult.Foreground = new SolidColorBrush(Color.Parse("#D29922"));
            return;
        }

        var node = _undoStack.PopWithData(); if (node == null) return;
        string[] parts = node.Operation.Split(':');

        if (parts[0] == "PATIENT_ADD")
        {
            int id = int.Parse(parts[1]);
            var p = _patients.Get(id);
            if (p != null)
            {
                foreach (int appId in _appointments.Values().Where(a => a.Patient.Id == id).Select(a => a.Id).ToList())
                { _appointments.Remove(appId); _db.DeleteAppointment(appId); }
                _patients.Remove(id); _patientBST.Delete(p.FirstName, p.LastName); _patientAVL.Delete(p.FirstName, p.LastName);
                _db.DeletePatient(id); if (id == _patientIdCounter) _patientIdCounter--;
                TxtUndoResult.Text = "✓ Hasta ve randevuları silindi.";
            }
        }
        else if (parts[0] == "DOCTOR_ADD")
        {
            int id = int.Parse(parts[1]);
            var d = _doctors.Get(id);
            if (d != null) { d.Department?.Doctors.Remove(d); _doctors.Remove(id); _db.DeleteDoctor(id);
                if (id == _doctorIdCounter) _doctorIdCounter--; TxtUndoResult.Text = "✓ Doktor silindi."; }
        }
        else if (parts[0] == "APPOINTMENT_ADD")
        {
            int id = int.Parse(parts[1]); _appointments.Remove(id); _db.DeleteAppointment(id);
            if (id == _appointmentIdCounter) _appointmentIdCounter--;
            TxtUndoResult.Text = "✓ Randevu silindi.";
        }
        else if (parts[0] == "ER_ADD")
        {
            if (node.Data is ERPriorityQueue.ERPatient erp)
            { _erQueue.RemovePatientById(erp.Patient.Id); TxtUndoResult.Text = "✓ Acil servis hastası silindi."; }
        }

        TxtUndoResult.Foreground = new SolidColorBrush(Color.Parse("#3FB950"));
        UpdateUndoPeek(); RefreshAllViews();
    }

    // ============ HELPERS ============
    private bool DoctorHasConflict(Doctor doc, DateTime dt)
    {
        var end = dt.AddMinutes(Appointment.AppointmentDuration);
        return _appointments.Values().Any(a => a.Doctor.Id == doc.Id && dt < a.End && a.Start < end);
    }

    private bool PatientHasConflict(Patient pat, DateTime dt)
    {
        var end = dt.AddMinutes(Appointment.AppointmentDuration);
        return _appointments.Values().Any(a => a.Patient.Id == pat.Id && dt < a.End && a.Start < end);
    }

    private void RefreshPatientList() { DgPatients.ItemsSource = _patients.Values().OrderBy(p => p.Id).ToList(); }

    private void RefreshDoctorList()
    {
        DgDoctors.ItemsSource = _doctors.Values().OrderBy(d => d.Id).Select(d => new
        { d.Id, d.FirstName, d.LastName, DeptName = d.Department?.Name ?? "N/A", d.Phone }).ToList();
    }

    private void RefreshAppointmentList()
    {
        DgAppointments.ItemsSource = _appointments.Values().OrderBy(a => a.Id).Select(a => new
        { a.Id, PatientName = a.Patient.FullName, DoctorName = a.Doctor.FullName,
          DateTime = a.Start.ToString("dd/MM/yyyy HH:mm"), a.Status }).ToList();
    }

    private void RefreshDepartmentList()
    {
        _departmentList = _departments.Values().OrderBy(d => d.Name).ToList();
        CbDoctorDepartment.ItemsSource = _departmentList.Select(d => d.Name).ToList();
        DgDepartments.ItemsSource = _departmentList;
    }

    private void RefreshERQueue()
    {
        IcERQueue.ItemsSource = _erQueue.GetAllSorted().Select((p, i) =>
        {
            SolidColorBrush bgBrush, borderBrush, sevBrush;
            if (p.Severity >= 8)
            {
                bgBrush = new SolidColorBrush(Color.Parse("#2D1518"));
                borderBrush = new SolidColorBrush(Color.Parse("#F85149"));
                sevBrush = new SolidColorBrush(Color.Parse("#F85149"));
            }
            else if (p.Severity >= 5)
            {
                bgBrush = new SolidColorBrush(Color.Parse("#2D2518"));
                borderBrush = new SolidColorBrush(Color.Parse("#D29922"));
                sevBrush = new SolidColorBrush(Color.Parse("#D29922"));
            }
            else
            {
                bgBrush = new SolidColorBrush(Color.Parse("#152D18"));
                borderBrush = new SolidColorBrush(Color.Parse("#3FB950"));
                sevBrush = new SolidColorBrush(Color.Parse("#3FB950"));
            }
            return new
            {
                RankStr = $"#{i+1}",
                PatientName = p.Patient.FullName,
                SeverityBadge = $"{p.Severity}/10",
                SevBrush = (IBrush)sevBrush,
                p.Complaint,
                ArrivalTime = p.ArrivalTime.ToString("HH:mm"),
                BgBrush = (IBrush)bgBrush,
                BorderBrush = (IBrush)borderBrush
            };
        }).ToList();
    }

    private void RefreshStats()
    {
        TxtStatPatients.Text = _patients.Size.ToString();
        TxtStatDoctors.Text = _doctors.Size.ToString();
        TxtStatAppointments.Text = _appointments.Size.ToString();
        TxtStatER.Text = _erQueue.Size.ToString();

        var s = _patientAVL.GetStats();
        TxtAVLStats.Text = $"Toplam: {s.totalNodes} | Yükseklik: {s.treeHeight} | Dengeli: {(s.isBalanced ? "✓" : "✗")}";
        TxtHashStats.Text = $"Hasta: {_patients.Size}/{_patients.Capacity} | Doktor: {_doctors.Size}/{_doctors.Capacity} | Randevu: {_appointments.Size}/{_appointments.Capacity}";
    }

    private void RefreshAllViews() { RefreshPatientList(); RefreshDoctorList(); RefreshAppointmentList(); RefreshDepartmentList(); UpdateNotifications(); }
    private void UpdateUndoPeek() { TxtUndoPeek.Text = _undoStack.PeekOperation() is string op ? $"Sıradaki: {op}" : "Stack boş."; }
    private void ShowMsg(string msg) { SetStatus("⚠ " + msg); }
    private void SetStatus(string msg) { TxtStatusBar.Text = msg; }
    private void ShowValidation(TextBlock tb, string msg) { tb.Text = "⚠ " + msg; SetStatus("⚠ " + msg); }

    // ============ PATIENT PROFILE ============
    private void ShowProfile_Click(object? sender, RoutedEventArgs e)
    {
        if (DgPatients.SelectedItem == null) { ShowMsg("Profil için bir hasta seçin!"); return; }
        if (DgPatients.SelectedItem is not Patient pSel) { ShowMsg("Profil için bir hasta seçin!"); return; }
        int id = pSel.Id;
        var p = _patients.Get(id);
        if (p == null) return;

        TxtProfileName.Text = $"{p.FullName} (ID: {p.Id})";
        TxtProfileDetails.Text = $"TC: {p.NationalId} | Telefon: {p.Phone} | Doğum: {p.BirthDate:dd/MM/yyyy}";

        var apps = _appointments.Values().Where(a => a.Patient.Id == id)
            .OrderByDescending(a => a.Start)
            .Select(a => $"{a.Start:dd/MM HH:mm} — {a.Doctor.FullName} [{a.Status}]").ToList();
        LbProfileAppointments.ItemsSource = apps.Count > 0 ? apps : new[] { "Randevu yok." };

        LbProfileHistory.ItemsSource = p.GetHistory().Count > 0
            ? p.GetHistory()
            : new List<string> { "Geçmiş yok." };

        // ★ Feature 5: ER visits
        var erVisits = _erQueue.GetAllSorted()
            .Where(erp => erp.Patient.Id == id)
            .Select(erp => $"🚨 {erp.ArrivalTime:dd/MM HH:mm} — {erp.Complaint} (Şiddet: {erp.Severity}/10)")
            .ToList();
        LbProfileER.ItemsSource = erVisits.Count > 0 ? erVisits : new[] { "Acil servis ziyareti yok." };

        PatientProfileCard.IsVisible = true;
    }

    private void CloseProfile_Click(object? sender, RoutedEventArgs e) { PatientProfileCard.IsVisible = false; }

    // ============ DOCTOR AVAILABILITY ============
    private void ShowAvailability_Click(object? sender, RoutedEventArgs e)
    {
        if (!int.TryParse(TxtAvailDoctorId.Text, out int did)) { ShowMsg("Geçersiz Doktor ID!"); return; }
        var doc = _doctors.Get(did);
        if (doc == null) { ShowMsg("Doktor bulunamadı!"); return; }

        DateTime day = DpAvailDate.SelectedDate?.DateTime.Date ?? DateTime.Today;
        var slots = new List<object>();
        var docApps = _appointments.Values()
            .Where(a => a.Doctor.Id == did && a.Start.Date == day).OrderBy(a => a.Start).ToList();

        const double maxBarWidth = 180.0; // px, fits the Grid column

        for (int h = 9; h < 18; h++)
        {
            var slotStart = day.AddHours(h);
            var slotEnd = slotStart.AddHours(1);
            var conflict = docApps.FirstOrDefault(a => a.Start < slotEnd && a.End > slotStart);

            bool isSoon = slotStart > DateTime.Now && slotStart < DateTime.Now.AddMinutes(30);

            if (conflict != null)
            {
                double fillRatio = Math.Min(1.0, (double)Appointment.AppointmentDuration / 60.0);
                slots.Add(new
                {
                    Icon = "🔴",
                    TimeSlot = $"{h:00}:00-{h+1:00}:00",
                    StatusText = $"Dolu — {conflict.Patient.FullName}",
                    BgBrush = (IBrush)new SolidColorBrush(Color.Parse("#2D1518")),
                    BorderBrush = (IBrush)new SolidColorBrush(Color.Parse("#F85149")),
                    BarWidth = maxBarWidth * fillRatio,
                    BarBrush = (IBrush)new SolidColorBrush(Color.Parse("#F85149"))
                });
            }
            else if (isSoon)
            {
                slots.Add(new
                {
                    Icon = "🟡",
                    TimeSlot = $"{h:00}:00-{h+1:00}:00",
                    StatusText = "Yaklaşan randevu",
                    BgBrush = (IBrush)new SolidColorBrush(Color.Parse("#2D2518")),
                    BorderBrush = (IBrush)new SolidColorBrush(Color.Parse("#D29922")),
                    BarWidth = maxBarWidth * 0.3,
                    BarBrush = (IBrush)new SolidColorBrush(Color.Parse("#D29922"))
                });
            }
            else
            {
                slots.Add(new
                {
                    Icon = "🟢",
                    TimeSlot = $"{h:00}:00-{h+1:00}:00",
                    StatusText = "Müsait",
                    BgBrush = (IBrush)new SolidColorBrush(Color.Parse("#152D18")),
                    BorderBrush = (IBrush)new SolidColorBrush(Color.Parse("#3FB950")),
                    BarWidth = maxBarWidth * 1.0,
                    BarBrush = (IBrush)new SolidColorBrush(Color.Parse("#3FB950"))
                });
            }
        }
        IcAvailability.ItemsSource = slots;
        SetStatus($"✓ {doc.FullName} — {day:dd/MM/yyyy} müsaitlik gösteriliyor");
    }

    // ============ CALENDAR VIEW (Feature 4) ============
    private void SwitchToTableView_Click(object? sender, RoutedEventArgs e)
    {
        AppTableView.IsVisible = true;
        AppCalendarView.IsVisible = false;
        BtnViewTable.Background = new SolidColorBrush(Color.Parse("#58A6FF"));
        BtnViewTable.Foreground = new SolidColorBrush(Colors.White);
        BtnViewCalendar.Background = new SolidColorBrush(Color.Parse("#1C2128"));
        BtnViewCalendar.Foreground = new SolidColorBrush(Color.Parse("#8B949E"));
    }

    private void SwitchToCalendarView_Click(object? sender, RoutedEventArgs e)
    {
        AppTableView.IsVisible = false;
        AppCalendarView.IsVisible = true;
        BtnViewCalendar.Background = new SolidColorBrush(Color.Parse("#58A6FF"));
        BtnViewCalendar.Foreground = new SolidColorBrush(Colors.White);
        BtnViewTable.Background = new SolidColorBrush(Color.Parse("#1C2128"));
        BtnViewTable.Foreground = new SolidColorBrush(Color.Parse("#8B949E"));
        BuildCalendarGrid();
    }

    private void PrevWeek_Click(object? sender, RoutedEventArgs e)
    { _currentWeekStart = _currentWeekStart.AddDays(-7); BuildCalendarGrid(); }

    private void NextWeek_Click(object? sender, RoutedEventArgs e)
    { _currentWeekStart = _currentWeekStart.AddDays(7); BuildCalendarGrid(); }

    private void GoToday_Click(object? sender, RoutedEventArgs e)
    {
        int dow = (int)DateTime.Today.DayOfWeek;
        _currentWeekStart = DateTime.Today.AddDays(dow == 0 ? -6 : 1 - dow);
        BuildCalendarGrid();
    }

    private void BuildCalendarGrid()
    {
        var weekEnd = _currentWeekStart.AddDays(6);
        TxtWeekLabel.Text = $"{_currentWeekStart:dd MMM} – {weekEnd:dd MMM yyyy}";

        // 8 columns: time label + 7 days
        // 10 rows: header + 09:00-17:00 (9 slots)
        var grid = new Grid { Background = new SolidColorBrush(Color.Parse("#0D1117")) };

        for (int c = 0; c < 8; c++)
            grid.ColumnDefinitions.Add(new ColumnDefinition(c == 0
                ? GridLength.Parse("60")
                : GridLength.Parse("*")));

        int totalRows = 10; // header + 9 hour slots
        for (int r = 0; r < totalRows; r++)
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Parse(r == 0 ? "32" : "52")));

        string[] dayNames = { "Pzt", "Sal", "Çar", "Per", "Cum", "Cmt", "Paz" };

        // Header row: day names
        // Time column header
        var timeHeader = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#161B22")),
            BorderBrush = new SolidColorBrush(Color.Parse("#30363D")),
            BorderThickness = new Thickness(0, 0, 1, 1)
        };
        Grid.SetRow(timeHeader, 0); Grid.SetColumn(timeHeader, 0);
        grid.Children.Add(timeHeader);

        for (int d = 0; d < 7; d++)
        {
            var date = _currentWeekStart.AddDays(d);
            bool isToday = date.Date == DateTime.Today;
            var hdr = new Border
            {
                Background = new SolidColorBrush(Color.Parse(isToday ? "#1C2F4A" : "#161B22")),
                BorderBrush = new SolidColorBrush(Color.Parse("#30363D")),
                BorderThickness = new Thickness(0, 0, 1, 1),
                Child = new TextBlock
                {
                    Text = $"{dayNames[d]}\n{date:dd/MM}",
                    FontSize = 11, FontWeight = FontWeight.SemiBold,
                    Foreground = new SolidColorBrush(Color.Parse(isToday ? "#58A6FF" : "#8B949E")),
                    TextAlignment = TextAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                }
            };
            Grid.SetRow(hdr, 0); Grid.SetColumn(hdr, d + 1);
            grid.Children.Add(hdr);
        }

        // Hour rows
        for (int h = 0; h < 9; h++)
        {
            int hour = 9 + h;
            int row = h + 1;

            // Time label
            var timeLbl = new Border
            {
                Background = new SolidColorBrush(Color.Parse("#161B22")),
                BorderBrush = new SolidColorBrush(Color.Parse("#30363D")),
                BorderThickness = new Thickness(0, 0, 1, 1),
                Child = new TextBlock
                {
                    Text = $"{hour:00}:00",
                    FontSize = 10,
                    Foreground = new SolidColorBrush(Color.Parse("#8B949E")),
                    VerticalAlignment = VerticalAlignment.Top,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 4, 0, 0)
                }
            };
            Grid.SetRow(timeLbl, row); Grid.SetColumn(timeLbl, 0);
            grid.Children.Add(timeLbl);

            // Day cells
            for (int d = 0; d < 7; d++)
            {
                var date = _currentWeekStart.AddDays(d);
                bool isToday = date.Date == DateTime.Today;
                var slotStart = date.AddHours(hour);
                var slotEnd = slotStart.AddHours(1);

                var cellApps = _appointments.Values()
                    .Where(a => a.Start >= slotStart && a.Start < slotEnd)
                    .OrderBy(a => a.Start).ToList();

                Control cellContent;
                string cellBg = isToday ? "#1C2128" : "#0D1117";

                if (cellApps.Count > 0)
                {
                    var app = cellApps[0];
                    bool isDone = app.Status == "Completed";
                    string appBg = isDone ? "#152D18" : "#1C2F4A";
                    string appFg = isDone ? "#3FB950" : "#58A6FF";
                    string extraBadge = cellApps.Count > 1 ? $" +{cellApps.Count - 1}" : "";

                    var appBlock = new Border
                    {
                        Background = new SolidColorBrush(Color.Parse(appBg)),
                        CornerRadius = new CornerRadius(4),
                        Margin = new Thickness(2),
                        Padding = new Thickness(4, 2),
                        Child = new StackPanel
                        {
                            Children =
                            {
                                new TextBlock
                                {
                                    Text = $"{app.Start:HH:mm} {app.Patient.FullName}{extraBadge}",
                                    FontSize = 10, FontWeight = FontWeight.SemiBold,
                                    Foreground = new SolidColorBrush(Color.Parse(appFg)),
                                    TextTrimming = TextTrimming.CharacterEllipsis
                                },
                                new TextBlock
                                {
                                    Text = app.Doctor.FullName,
                                    FontSize = 9,
                                    Foreground = new SolidColorBrush(Color.Parse("#8B949E")),
                                    TextTrimming = TextTrimming.CharacterEllipsis
                                }
                            }
                        }
                    };
                    cellContent = appBlock;
                }
                else
                {
                    cellContent = new Border(); // empty cell
                }

                var cell = new Border
                {
                    Background = new SolidColorBrush(Color.Parse(cellBg)),
                    BorderBrush = new SolidColorBrush(Color.Parse("#21262D")),
                    BorderThickness = new Thickness(0, 0, 1, 1),
                    Child = cellContent
                };
                Grid.SetRow(cell, row); Grid.SetColumn(cell, d + 1);
                grid.Children.Add(cell);
            }
        }

        CalendarContent.Content = grid;
    }

    // ============ THEME TOGGLE ============
    private bool _isDark = true;
    private void ToggleTheme_Click(object? sender, RoutedEventArgs e)
    {
        _isDark = !_isDark;
        if (Avalonia.Application.Current != null)
        {
            Avalonia.Application.Current.RequestedThemeVariant =
                _isDark ? Avalonia.Styling.ThemeVariant.Dark : Avalonia.Styling.ThemeVariant.Light;
        }
        BtnThemeToggle.Content = _isDark ? "🌙  Koyu Tema" : "☀️  Açık Tema";
        SetStatus(_isDark ? "Koyu tema aktif" : "Açık tema aktif");
    }

    // ============ DASHBOARD ============
    private void RefreshDashboard()
    {
        TxtDashPatients.Text = _patients.Size.ToString();
        TxtDashDoctors.Text = _doctors.Size.ToString();
        TxtDashER.Text = _erQueue.Size.ToString();
        var today = _appointments.Values().Where(a => a.Start.Date == DateTime.Today).ToList();
        TxtDashTodayApps.Text = today.Count.ToString();
        DgDashTodayApps.ItemsSource = today.OrderBy(a => a.Start).Select(a => new
        { Time = a.Start.ToString("HH:mm"), PatientName = a.Patient.FullName,
          DoctorName = a.Doctor.FullName, a.Status }).ToList();
    }

    // ============ SEARCH / FILTER ============
    private void PatientSearch_Changed(object? sender, TextChangedEventArgs e)
    {
        string q = TxtPatientSearch.Text?.Trim().ToLowerInvariant() ?? "";
        if (string.IsNullOrEmpty(q)) { RefreshPatientList(); return; }
        DgPatients.ItemsSource = _patients.Values()
            .Where(p => p.FirstName.ToLowerInvariant().Contains(q) ||
                        p.LastName.ToLowerInvariant().Contains(q) ||
                        p.NationalId.Contains(q) ||
                        p.Phone.Contains(q)).ToList();
    }

    private void DoctorSearch_Changed(object? sender, TextChangedEventArgs e)
    {
        string q = TxtDoctorSearch.Text?.Trim().ToLowerInvariant() ?? "";
        if (string.IsNullOrEmpty(q)) { RefreshDoctorList(); return; }
        DgDoctors.ItemsSource = _doctors.Values()
            .Where(d => d.FirstName.ToLowerInvariant().Contains(q) ||
                        d.LastName.ToLowerInvariant().Contains(q) ||
                        (d.Department?.Name ?? "").ToLowerInvariant().Contains(q))
            .Select(d => new { d.Id, d.FirstName, d.LastName, DeptName = d.Department?.Name ?? "N/A", d.Phone }).ToList();
    }

    // ============ DELETE / EDIT ============
    private void DeletePatient_Click(object? sender, RoutedEventArgs e)
    {
        if (DgPatients.SelectedItem == null) { ShowMsg("Silmek için bir hasta seçin!"); return; }
        dynamic sel = DgPatients.SelectedItem;
        int id = (int)sel.Id;
        var p = _patients.Get(id);
        if (p == null) return;

        // Fix 5: Confirmation dialog
        TxtDeleteConfirmMsg.Text = $"⚠️ '{p.FullName}' (ID: {id}) hastasını silmek istediğinize emin misiniz?";
        _pendingDeleteAction = () =>
        {
            foreach (int appId in _appointments.Values().Where(a => a.Patient.Id == id).Select(a => a.Id).ToList())
            { _appointments.Remove(appId); _db.DeleteAppointment(appId); }

            _patients.Remove(id); _patientBST.Delete(p.FirstName, p.LastName); _patientAVL.Delete(p.FirstName, p.LastName);
            _db.DeletePatient(id);
            RefreshAllViews();
            SetStatus($"✓ Hasta silindi: {p.FullName}");
        };
        PanelDeleteConfirm.IsVisible = true;
    }

    // Fix 4: Safe in-place edit
    private void EditPatient_Click(object? sender, RoutedEventArgs e)
    {
        if (DgPatients.SelectedItem == null) { ShowMsg("Düzenlemek için bir hasta seçin!"); return; }
        dynamic sel = DgPatients.SelectedItem;
        int id = (int)sel.Id;
        var p = _patients.Get(id);
        if (p == null) return;

        // Just populate form — no deletion
        _editingPatientId = id;
        TxtPatientFirstName.Text = p.FirstName;
        TxtPatientLastName.Text = p.LastName;
        TxtPatientNationalId.Text = p.NationalId;
        TxtPatientPhone.Text = p.Phone;
        DpPatientBirthDate.SelectedDate = new DateTimeOffset(p.BirthDate);

        BtnRegisterPatient.Content = "✓ Güncelle";
        SetStatus($"✏️ Düzenleniyor: {p.FullName} — Değişiklikleri yapıp 'Güncelle' butonuna basın.");
    }

    private void DeleteDoctor_Click(object? sender, RoutedEventArgs e)
    {
        if (DgDoctors.SelectedItem == null) { ShowMsg("Silmek için bir doktor seçin!"); return; }
        dynamic sel = DgDoctors.SelectedItem;
        int id = (int)sel.Id;
        var d = _doctors.Get(id);
        if (d == null) return;

        // Fix 5: Confirmation dialog
        TxtDeleteConfirmMsg.Text = $"⚠️ '{d.FullName}' (ID: {id}) doktoru silmek istediğinize emin misiniz?";
        _pendingDeleteAction = () =>
        {
            d.Department?.Doctors.Remove(d);
            _doctors.Remove(id); _db.DeleteDoctor(id);
            RefreshDoctorList(); RefreshDepartmentList();
            SetStatus($"✓ Doktor silindi: {d.FullName}");
        };
        PanelDeleteConfirm.IsVisible = true;
    }

    // Fix 5: Confirmation handlers
    private void ConfirmDelete_Click(object? sender, RoutedEventArgs e)
    {
        _pendingDeleteAction?.Invoke();
        _pendingDeleteAction = null;
        PanelDeleteConfirm.IsVisible = false;
    }

    private void CancelDelete_Click(object? sender, RoutedEventArgs e)
    {
        _pendingDeleteAction = null;
        PanelDeleteConfirm.IsVisible = false;
        SetStatus("✓ Silme işlemi iptal edildi.");
    }

    // ============ SAMPLE DATA ============
    private void LoadSampleData()
    {
        if (_db.LoadDepartments().Count > 0) return;

        var c = new Department(++_departmentIdCounter, "Cardiology", 5);
        _departments.Put(_departmentIdCounter, c); _hospitalTree.AddDepartmentToRoot(c); _db.SaveDepartment(c);
        var n = new Department(++_departmentIdCounter, "Neurology", 4);
        _departments.Put(_departmentIdCounter, n); _hospitalTree.AddDepartmentToRoot(n); _db.SaveDepartment(n);
        var em = new Department(++_departmentIdCounter, "Emergency Medicine", 8);
        _departments.Put(_departmentIdCounter, em); _hospitalTree.AddDepartmentToRoot(em); _db.SaveDepartment(em);

        var d1 = new Doctor(++_doctorIdCounter, "Mehmet", "Yilmaz", c, "0532-111-2233");
        c.AddDoctor(d1); _doctors.Put(_doctorIdCounter, d1); _db.SaveDoctor(d1);
        var d2 = new Doctor(++_doctorIdCounter, "Semih", "Arslan", em, "0535-444-5566");
        em.AddDoctor(d2); _doctors.Put(_doctorIdCounter, d2); _db.SaveDoctor(d2);

        var p1 = new Patient(++_patientIdCounter, "Ahmet", "Oz", "12345678901", "0535-444-5566", new DateTime(1985, 5, 15));
        _patients.Put(_patientIdCounter, p1); _patientBST.Insert(p1); _patientAVL.Insert(p1); _db.SavePatient(p1);
        p1.AddVisit(new DateTime(2024, 10, 15, 10, 30, 0), d1, "Annual checkup");
        _db.SaveVisit(p1.Id, d1.Id, new DateTime(2024, 10, 15, 10, 30, 0), "Annual checkup");
    }

    private void LoadFromDatabase()
    {
        if (_departments.Size > 0) return;

        foreach (var (id, name, cap) in _db.LoadDepartments())
        { var d = new Department(id, name, cap); _departments.Put(id, d); _hospitalTree.AddDepartmentToRoot(d);
          if (id > _departmentIdCounter) _departmentIdCounter = id; }

        foreach (var (id, fn, ln, did, phone) in _db.LoadDoctors())
        { var dept = _departments.Get(did); var doc = new Doctor(id, fn, ln, dept, phone);
          dept?.AddDoctor(doc); _doctors.Put(id, doc);
          if (id > _doctorIdCounter) _doctorIdCounter = id; }

        foreach (var (id, fn, ln, nid, phone, bd) in _db.LoadPatients())
        { var p = new Patient(id, fn, ln, nid, phone, DateTime.Parse(bd));
          _patients.Put(id, p); _patientBST.Insert(p); _patientAVL.Insert(p);
          if (id > _patientIdCounter) _patientIdCounter = id; }

        foreach (var (pid, did, vd, notes) in _db.LoadVisits())
        { var p = _patients.Get(pid); var d = _doctors.Get(did);
          if (p != null && d != null) p.AddVisit(DateTime.Parse(vd), d, notes); }

        foreach (var (id, pid, did, start, end, status) in _db.LoadAppointments())
        { var p = _patients.Get(pid); var d = _doctors.Get(did);
          if (p != null && d != null) { var app = new Appointment(id, p, d, DateTime.Parse(start));
            app.Status = status; _appointments.Put(id, app); d.DailyQueue.Enqueue(app);
            if (id > _appointmentIdCounter) _appointmentIdCounter = id; } }
    }

    // ============ REPORT EXPORT ============
    private void ExportPatientReport_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            var html = "<!DOCTYPE html><html><head><meta charset='utf-8'/><title>Hasta Raporu</title>" +
                "<style>body{font-family:Inter,sans-serif;background:#0D1117;color:#E6EDF3;padding:40px}" +
                "h1{color:#58A6FF}table{width:100%;border-collapse:collapse;margin-top:20px}" +
                "th{background:#21262D;color:#58A6FF;padding:12px;text-align:left;border-bottom:2px solid #30363D}" +
                "td{padding:10px;border-bottom:1px solid #30363D}" +
                "tr:hover{background:#1C2128}.footer{margin-top:30px;color:#8B949E;font-size:12px}</style></head><body>" +
                $"<h1>🏥 Hasta Raporu</h1><p>Toplam: {_patients.Size} hasta | Tarih: {DateTime.Now:dd/MM/yyyy HH:mm}</p><table>" +
                "<tr><th>ID</th><th>Ad</th><th>Soyad</th><th>TC</th><th>Telefon</th></tr>";

            foreach (var p in _patients.Values())
                html += $"<tr><td>{p.Id}</td><td>{p.FirstName}</td><td>{p.LastName}</td><td>{p.NationalId}</td><td>{p.Phone}</td></tr>";

            html += "</table><p class='footer'>Student ID: 230316064 | Hastane Yönetim Sistemi</p></body></html>";

            var path = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "hasta_raporu.html");
            System.IO.File.WriteAllText(path, html);
            TxtReportStatus.Text = $"✓ Rapor kaydedildi: {path}";
            SetStatus($"✓ Hasta raporu: {path}");
        }
        catch (Exception ex) { ShowMsg(ex.Message); }
    }

    private void ExportAppointmentReport_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            var html = "<!DOCTYPE html><html><head><meta charset='utf-8'/><title>Randevu Raporu</title>" +
                "<style>body{font-family:Inter,sans-serif;background:#0D1117;color:#E6EDF3;padding:40px}" +
                "h1{color:#3FB950}table{width:100%;border-collapse:collapse;margin-top:20px}" +
                "th{background:#21262D;color:#3FB950;padding:12px;text-align:left;border-bottom:2px solid #30363D}" +
                "td{padding:10px;border-bottom:1px solid #30363D}" +
                "tr:hover{background:#1C2128}.completed{color:#3FB950}.pending{color:#D29922}" +
                ".footer{margin-top:30px;color:#8B949E;font-size:12px}</style></head><body>" +
                $"<h1>📋 Randevu Raporu</h1><p>Toplam: {_appointments.Size} | Tarih: {DateTime.Now:dd/MM/yyyy HH:mm}</p><table>" +
                "<tr><th>ID</th><th>Hasta</th><th>Doktor</th><th>Tarih</th><th>Durum</th></tr>";

            foreach (var a in _appointments.Values().OrderBy(a => a.Start))
            {
                var cls = a.Status == "Completed" ? "completed" : "pending";
                html += $"<tr><td>{a.Id}</td><td>{a.Patient.FullName}</td><td>{a.Doctor.FullName}</td>" +
                        $"<td>{a.Start:dd/MM/yyyy HH:mm}</td><td class='{cls}'>{a.Status}</td></tr>";
            }

            html += "</table><p class='footer'>Student ID: 230316064 | Hastane Yönetim Sistemi</p></body></html>";

            var path = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "randevu_raporu.html");
            System.IO.File.WriteAllText(path, html);
            TxtReportStatus.Text = $"✓ Rapor kaydedildi: {path}";
            SetStatus($"✓ Randevu raporu: {path}");
        }
        catch (Exception ex) { ShowMsg(ex.Message); }
    }

    // ============ NOTIFICATIONS ============
    private void UpdateNotifications()
    {
        var soon = _appointments.Values()
            .Where(a => a.Status == "Waiting" && a.Start > DateTime.Now && a.Start < DateTime.Now.AddMinutes(30)).ToList();
        int count = soon.Count + _erQueue.Size;
        TxtNotifBadge.Text = $"🔔 {count}";
    }

    // ============ ROLE SYSTEM ============
    private void RoleChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (CbRole == null) return;
        // Guard: buttons may not be initialized yet during startup
        if (BtnRegisterPatient == null) return;

        int role = CbRole.SelectedIndex; // 0=Admin, 1=Doktor, 2=Hemşire
        bool isAdmin = role == 0;
        bool isDoctor = role == 1;
        bool isNurse = role == 2;

        // Fix 1: Actually enable/disable buttons per role
        // Admin = full access
        // Doktor = can register patients, create appointments, examine; NO delete, dept, undo
        // Hemşire = view-only, all write operations disabled

        BtnRegisterPatient.IsEnabled = isAdmin || isDoctor;
        BtnEditPatient.IsEnabled = isAdmin || isDoctor;
        BtnDeletePatient.IsEnabled = isAdmin;
        BtnRegisterDoctor.IsEnabled = isAdmin;
        BtnDeleteDoctor.IsEnabled = isAdmin;
        BtnCreateAppointment.IsEnabled = isAdmin || isDoctor;
        BtnExaminePatient.IsEnabled = isAdmin || isDoctor;
        BtnAddER.IsEnabled = isAdmin || isDoctor;
        BtnTreatHighest.IsEnabled = isAdmin || isDoctor;
        BtnCreateDept.IsEnabled = isAdmin;
        BtnUndo.IsEnabled = isAdmin;

        SetStatus(role switch
        {
            0 => "👑 Admin modu: Tüm yetkiler aktif",
            1 => "🩺 Doktor modu: Hasta ve randevu işlemleri",
            2 => "🏥 Hemşire modu: Görüntüleme yetkisi",
            _ => ""
        });
    }
}

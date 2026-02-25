using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using HospitalManagementAvolonia.Data;
using HospitalManagementAvolonia.DataStructures;
using HospitalManagementAvolonia.Models;

namespace HospitalManagementAvolonia;

public partial class MainWindow : Window
{
    private HashTable<int, Patient> _patients;
    private HashTable<int, Doctor> _doctors;
    private HashTable<int, Appointment> _appointments;
    private HashTable<int, Department> _departments;

    private PatientBST _patientBST;
    private PatientAVL _patientAVL;
    private HospitalTree _hospitalTree;
    private ERPriorityQueue _erQueue;
    private UndoStack _undoStack;
    
    // Phase 2 Data Structures
    private PatientTrie _patientTrie;
    private DoctorGraph _doctorGraph;
    private PatientLRUCache _lruCache;
    private AppointmentSegmentTree _segmentTree;

    private IDatabaseService _db;

    private int _patientIdCounter, _doctorIdCounter, _appointmentIdCounter, _departmentIdCounter;

    private StackPanel[] _allPanels = null!;
    private List<Department> _departmentList = new();

    // Fix 4: Safe in-place edit tracking
    private int? _editingPatientId = null;

    // Fix 5: Delete confirmation
    private Action? _pendingDeleteAction = null;

    // Feature 4: Calendar week state
    private DateTime _currentWeekStart;

    private HashSet<int> _notifiedAppointmentIds = new();

    public MainWindow()
    {
        InitializeComponent();
        
        // Resolve the entire MVVM data context tree from DI and bind it to the view
        DataContext = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<ViewModels.MainViewModel>(App.Services!);

        _patients = new HashTable<int, Patient>();
        _doctors = new HashTable<int, Doctor>();
        _appointments = new HashTable<int, Appointment>();
        _departments = new HashTable<int, Department>();

        _patientBST = new PatientBST();
        _patientAVL = new PatientAVL();
        _hospitalTree = new HospitalTree("Manisa Celal Bayar University Hospital");
        _erQueue = new ERPriorityQueue();
        _undoStack = new UndoStack();

        // Phase 2 Instantiations
        _patientTrie = new PatientTrie();
        _doctorGraph = new DoctorGraph();
        _lruCache = new PatientLRUCache(10);
        // Track appointments for the next 30 days and previous 30 days (60 days total capacity roughly)
        _segmentTree = new AppointmentSegmentTree(DateTime.Today.AddDays(-30), 100);

        _db = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<IDatabaseService>(App.Services!);

        _allPanels = new[] { PanelDashboard, PanelPatients, PanelDoctors, PanelAppointments, PanelEmergency,
                             PanelBST, PanelAVL, PanelDepartments, PanelUndo, PanelStats };

        SliderSeverity.PropertyChanged += (s, e) =>
        {
            if (e.Property.Name == "Value")
                TxtSeverityValue.Text = ((int)SliderSeverity.Value).ToString();
        };

        // Initialize calendar to current week (Monday)
        int dow = (int)DateTime.Today.DayOfWeek;
        _currentWeekStart = DateTime.Today.AddDays(dow == 0 ? -6 : 1 - dow);
    }

    public async System.Threading.Tasks.Task InitializeAsync()
    {
        await LoadSampleDataAsync();
        await LoadFromDatabaseAsync();
        RefreshAllViews();
        // Dashboard loaded via ViewModel
    }

    // ============ NAVIGATION ============
    private void Nav_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string tag)
        {
            foreach (var p in _allPanels) p.IsVisible = false;
            switch (tag)
            {
                case "Dashboard": PanelDashboard.IsVisible = true; break;
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
                    _ = System.Threading.Tasks.Task.Run(() => _db.SavePatientAsync(existing));

                    _editingPatientId = null;
                    BtnRegisterPatient.Content = "✓ Hasta Kaydet";
                    SetStatus($"✓ Hasta güncellendi! ID: {editId}");
                }
            }
            else
            {
                if (_patientBST.Search(fn, ln) != null) { ShowValidation(TxtPatientValidation, "Bu isimde hasta mevcut!"); return; }

                int id = 1;
                while (_patients.Get(id) != null) id++;
                if (id > _patientIdCounter) _patientIdCounter = id;
                
                var p = new Patient(id, fn, ln, nid, phone, bd);
                _patients.Put(id, p); _patientBST.Insert(p); _patientAVL.Insert(p);
                _patientTrie.Insert(p); // Phase 2: Trie
                _undoStack.Push("PATIENT_ADD:" + id); 
                _ = System.Threading.Tasks.Task.Run(() => _db.SavePatientAsync(p));
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

    // Doctor registration is now handled by DoctorViewModel


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
            _segmentTree.AddAppointment(dt); // Phase 2: Segment Tree
            _undoStack.Push("APPOINTMENT_ADD:" + id); 
            _ = System.Threading.Tasks.Task.Run(() => _db.SaveAppointmentAsync(app));

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
            _ = System.Threading.Tasks.Task.Run(() => _db.SaveVisitAsync(app.Patient.Id, d.Id, DateTime.Now, "Muayene tamamlandı"));
            _ = System.Threading.Tasks.Task.Run(() => _db.SaveAppointmentAsync(app)); // Fix 3: Persist status change to DB
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
        TxtBSTResult.Foreground = new SolidColorBrush(found != null ? Color.Parse("#16A34A") : Color.Parse("#DC2626"));
        BtnListSearchedBST.IsVisible = found != null;
    }

    private void ListSearchedBST_Click(object? sender, RoutedEventArgs e)
    {
        var found = _patientBST.Search(TxtBSTFirstName.Text?.Trim() ?? "", TxtBSTLastName.Text?.Trim() ?? "");
        if (found != null)
        {
            LbBST.ItemsSource = new List<string> { $"1. {found.FullName} (ID: {found.Id})" };
        }
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
        TxtAVLResult.Foreground = new SolidColorBrush(found != null ? Color.Parse("#16A34A") : Color.Parse("#DC2626"));
        BtnListSearchedAVL.IsVisible = found != null;
    }

    private void ListSearchedAVL_Click(object? sender, RoutedEventArgs e)
    {
        var found = _patientAVL.Search(TxtAVLFirstName.Text?.Trim() ?? "", TxtAVLLastName.Text?.Trim() ?? "");
        if (found != null)
        {
            LbAVL.ItemsSource = new List<string> { $"1. {found.FullName} (ID: {found.Id})" };
        }
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
        _departments.Put(_departmentIdCounter, dept); _hospitalTree.AddDepartmentToRoot(dept); 
        _ = System.Threading.Tasks.Task.Run(() => _db.SaveDepartmentAsync(dept));
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
            TxtUndoResult.Foreground = new SolidColorBrush(Color.Parse("#D97706"));
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
                { 
                    var app = _appointments.Get(appId);
                    if (app != null) _segmentTree.RemoveAppointment(app.Start); // Phase 2
                    _appointments.Remove(appId); _ = System.Threading.Tasks.Task.Run(() => _db.DeleteAppointmentAsync(appId)); 
                }
                _patients.Remove(id); _patientBST.Delete(p.FirstName, p.LastName); _patientAVL.Delete(p.FirstName, p.LastName);
                _patientTrie.Remove(p); // Phase 2
                _lruCache.RemovePatient(id); // Phase 2
                _ = System.Threading.Tasks.Task.Run(() => _db.DeletePatientAsync(id)); if (id == _patientIdCounter) _patientIdCounter--;
                TxtUndoResult.Text = "✓ Hasta ve randevuları silindi.";
            }
        }
        else if (parts[0] == "DOCTOR_ADD")
        {
            int id = int.Parse(parts[1]);
            var d = _doctors.Get(id);
            if (d != null) { d.Department?.Doctors.Remove(d); _doctors.Remove(id); _doctorGraph.RemoveDoctor(id); _ = System.Threading.Tasks.Task.Run(() => _db.DeleteDoctorAsync(id));
                if (id == _doctorIdCounter) _doctorIdCounter--; TxtUndoResult.Text = "✓ Doktor silindi."; }
        }
        else if (parts[0] == "APPOINTMENT_ADD")
        {
            int id = int.Parse(parts[1]); 
            var app = _appointments.Get(id);
            if (app != null) _segmentTree.RemoveAppointment(app.Start);
            _appointments.Remove(id); _ = System.Threading.Tasks.Task.Run(() => _db.DeleteAppointmentAsync(id));
            if (id == _appointmentIdCounter) _appointmentIdCounter--;
            TxtUndoResult.Text = "✓ Randevu silindi.";
        }
        else if (parts[0] == "ER_ADD")
        {
            if (node.Data is ERPriorityQueue.ERPatient erp)
            { _erQueue.RemovePatientById(erp.Patient.Id); TxtUndoResult.Text = "✓ Acil servis hastası silindi."; }
        }

        TxtUndoResult.Foreground = new SolidColorBrush(Color.Parse("#16A34A"));
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

    private void RefreshDoctorList() { /* Handled by DoctorViewModel */ }

    private void RefreshAppointmentList()
    {
        DgAppointments.ItemsSource = _appointments.Values().OrderBy(a => a.Id).Select(a => new
        { a.Id, PatientName = a.Patient.FullName, DoctorName = a.Doctor.FullName,
          DateTime = a.Start.ToString("dd/MM/yyyy HH:mm"), a.Status }).ToList();
    }

    private void RefreshDepartmentList()
    {
        _departmentList = _departments.Values().OrderBy(d => d.Name).ToList();
        DgDepartments.ItemsSource = _departmentList;
    }

    private void RefreshERQueue()
    {
        IcERQueue.ItemsSource = _erQueue.GetAllSorted().Select((p, i) =>
        {
            SolidColorBrush bgBrush, borderBrush, sevBrush;
            if (p.Severity >= 8)
            {
                bgBrush = new SolidColorBrush(Color.Parse("#FEF2F2"));
                borderBrush = new SolidColorBrush(Color.Parse("#DC2626"));
                sevBrush = new SolidColorBrush(Color.Parse("#DC2626"));
            }
            else if (p.Severity >= 5)
            {
                bgBrush = new SolidColorBrush(Color.Parse("#FFFBEB"));
                borderBrush = new SolidColorBrush(Color.Parse("#D97706"));
                sevBrush = new SolidColorBrush(Color.Parse("#D97706"));
            }
            else
            {
                bgBrush = new SolidColorBrush(Color.Parse("#F0FDF4"));
                borderBrush = new SolidColorBrush(Color.Parse("#16A34A"));
                sevBrush = new SolidColorBrush(Color.Parse("#16A34A"));
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
        // Summary stats now handled by StatsViewModel
        // Only update the performance TextBlocks that remain in code-behind
        var s = _patientAVL.GetStats();
        TxtAVLStats.Text = $"Toplam: {s.totalNodes} | Yükseklik: {s.treeHeight} | Dengeli: {(s.isBalanced ? "✓" : "✗")}";
        TxtHashStats.Text = $"Hasta: {_patients.Size}/{_patients.Capacity} | Doktor: {_doctors.Size}/{_doctors.Capacity} | Randevu: {_appointments.Size}/{_appointments.Capacity}";
    }

    private void RefreshAllViews() { RefreshPatientList(); RefreshDoctorList(); RefreshAppointmentList(); RefreshDepartmentList(); UpdateNotifications(); }
    private void UpdateUndoPeek() { TxtUndoPeek.Text = _undoStack.PeekOperation() is string op ? $"Sıradaki: {op}" : "Geri alınacak işlem yok."; }
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

        // Phase 2: Add patient to LRU Cache on view
        _lruCache.AccessPatient(p);

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
                    BgBrush = (IBrush)new SolidColorBrush(Color.Parse("#FEF2F2")),
                    BorderBrush = (IBrush)new SolidColorBrush(Color.Parse("#DC2626")),
                    BarWidth = maxBarWidth * fillRatio,
                    BarBrush = (IBrush)new SolidColorBrush(Color.Parse("#DC2626"))
                });
            }
            else if (isSoon)
            {
                slots.Add(new
                {
                    Icon = "🟡",
                    TimeSlot = $"{h:00}:00-{h+1:00}:00",
                    StatusText = "Yaklaşan randevu",
                    BgBrush = (IBrush)new SolidColorBrush(Color.Parse("#FFFBEB")),
                    BorderBrush = (IBrush)new SolidColorBrush(Color.Parse("#D97706")),
                    BarWidth = maxBarWidth * 0.3,
                    BarBrush = (IBrush)new SolidColorBrush(Color.Parse("#D97706"))
                });
            }
            else
            {
                slots.Add(new
                {
                    Icon = "🟢",
                    TimeSlot = $"{h:00}:00-{h+1:00}:00",
                    StatusText = "Müsait",
                    BgBrush = (IBrush)new SolidColorBrush(Color.Parse("#F0FDF4")),
                    BorderBrush = (IBrush)new SolidColorBrush(Color.Parse("#16A34A")),
                    BarWidth = maxBarWidth * 1.0,
                    BarBrush = (IBrush)new SolidColorBrush(Color.Parse("#16A34A"))
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
        BtnViewTable.Background = new SolidColorBrush(Color.Parse("#2563EB"));
        BtnViewTable.Foreground = new SolidColorBrush(Colors.White);
        BtnViewCalendar.Background = new SolidColorBrush(Color.Parse("#F9FAFB"));
        BtnViewCalendar.Foreground = new SolidColorBrush(Color.Parse("#4B5563"));
    }

    private void SwitchToCalendarView_Click(object? sender, RoutedEventArgs e)
    {
        AppTableView.IsVisible = false;
        AppCalendarView.IsVisible = true;
        BtnViewCalendar.Background = new SolidColorBrush(Color.Parse("#2563EB"));
        BtnViewCalendar.Foreground = new SolidColorBrush(Colors.White);
        BtnViewTable.Background = new SolidColorBrush(Color.Parse("#F9FAFB"));
        BtnViewTable.Foreground = new SolidColorBrush(Color.Parse("#4B5563"));
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
        var grid = new Grid { Background = new SolidColorBrush(Color.Parse("#F3F4F6")) };

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
            Background = new SolidColorBrush(Color.Parse("#FFFFFF")),
            BorderBrush = new SolidColorBrush(Color.Parse("#E5E7EB")),
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
                Background = new SolidColorBrush(Color.Parse(isToday ? "#E0F2FE" : "#FFFFFF")),
                BorderBrush = new SolidColorBrush(Color.Parse("#E5E7EB")),
                BorderThickness = new Thickness(0, 0, 1, 1),
                Child = new TextBlock
                {
                    Text = $"{dayNames[d]}\n{date:dd/MM}",
                    FontSize = 11, FontWeight = FontWeight.SemiBold,
                    Foreground = new SolidColorBrush(Color.Parse(isToday ? "#2563EB" : "#4B5563")),
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
                Background = new SolidColorBrush(Color.Parse("#FFFFFF")),
                BorderBrush = new SolidColorBrush(Color.Parse("#E5E7EB")),
                BorderThickness = new Thickness(0, 0, 1, 1),
                Child = new TextBlock
                {
                    Text = $"{hour:00}:00",
                    FontSize = 10,
                    Foreground = new SolidColorBrush(Color.Parse("#4B5563")),
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
                string cellBg = isToday ? "#F9FAFB" : "#F3F4F6";

                if (cellApps.Count > 0)
                {
                    var app = cellApps[0];
                    bool isDone = app.Status == "Completed";
                    string appBg = isDone ? "#F0FDF4" : "#E0F2FE";
                    string appFg = isDone ? "#16A34A" : "#2563EB";
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
                                    Foreground = new SolidColorBrush(Color.Parse("#4B5563")),
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
                    BorderBrush = new SolidColorBrush(Color.Parse("#FFFFFF")),
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
    private bool _isDark = false;
    private void ToggleTheme_Click(object? sender, RoutedEventArgs e)
    {
        _isDark = !_isDark;
        if (Avalonia.Application.Current != null)
        {
            Avalonia.Application.Current.RequestedThemeVariant =
                _isDark ? Avalonia.Styling.ThemeVariant.Dark : Avalonia.Styling.ThemeVariant.Light;
        }
        BtnThemeToggle.Content = _isDark ? "☀️  Açık Tema" : "☀️  Açık Tema";
        SetStatus(_isDark ? "Açık tema aktif" : "Açık tema aktif");
    }

    // ============ DASHBOARD ============
    private void RefreshDashboard()
    {
        // Dashboard panel uses DashboardViewModel via DataContext="{Binding Dashboard}"
        if (DataContext is ViewModels.MainViewModel mvm)
        {
            mvm.Dashboard.TotalPatients = _patients.Size;
            mvm.Dashboard.TotalDoctors = _doctors.Size;
            mvm.Dashboard.EmergencyPatients = _erQueue.Size;
            var today = _appointments.Values().Where(a => a.Start.Date == DateTime.Today).ToList();
            mvm.Dashboard.TodayAppointments = today.Count;
            mvm.Dashboard.TodayAppointmentList.Clear();
            foreach (var a in today.OrderBy(x => x.Start)) mvm.Dashboard.TodayAppointmentList.Add(a);
        }

        // Phase 3: LRU Cache widget (x:Name="LbDashRecentPatients" still exists in XAML)
        if (LbDashRecentPatients != null)
        {
            var recent = _lruCache.GetRecentPatients();
            if (recent.Count > 0)
                LbDashRecentPatients.ItemsSource = recent.Select((p, i) => $"{i + 1}. {p.FullName} (ID: {p.Id}, {DateTime.Today.Year - p.BirthDate.Year} yaş)").ToList();
            else
                LbDashRecentPatients.ItemsSource = new List<string> { "Henüz profil görüntülenmedi." };
        }
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

    // Doctor search is now handled by DoctorViewModel


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
            { 
               var app = _appointments.Get(appId);
               if (app != null) _segmentTree.RemoveAppointment(app.Start);
               _appointments.Remove(appId); _ = System.Threading.Tasks.Task.Run(() => _db.DeleteAppointmentAsync(appId)); 
            }

            _patients.Remove(id); _patientBST.Delete(p.FirstName, p.LastName); _patientAVL.Delete(p.FirstName, p.LastName);
            _patientTrie.Remove(p); _lruCache.RemovePatient(id);
            _ = System.Threading.Tasks.Task.Run(() => _db.DeletePatientAsync(id));
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

    // DeleteDoctor is now handled by DoctorViewModel


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
    private async Task LoadSampleDataAsync()
    {
        var depts = await _db.LoadDepartmentsAsync();
        if (depts.Count > 0) return;

        var c = new Department(++_departmentIdCounter, "Cardiology", 5);
        _departments.Put(_departmentIdCounter, c); _hospitalTree.AddDepartmentToRoot(c); await _db.SaveDepartmentAsync(c);
        var n = new Department(++_departmentIdCounter, "Neurology", 4);
        _departments.Put(_departmentIdCounter, n); _hospitalTree.AddDepartmentToRoot(n); await _db.SaveDepartmentAsync(n);
        var em = new Department(++_departmentIdCounter, "Emergency Medicine", 8);
        _departments.Put(_departmentIdCounter, em); _hospitalTree.AddDepartmentToRoot(em); await _db.SaveDepartmentAsync(em);

        var d1 = new Doctor(++_doctorIdCounter, "Mehmet", "Yilmaz", c, "0532-111-2233");
        c.AddDoctor(d1); _doctors.Put(_doctorIdCounter, d1); await _db.SaveDoctorAsync(d1);
        var d2 = new Doctor(++_doctorIdCounter, "Semih", "Arslan", em, "0535-444-5566");
        em.AddDoctor(d2); _doctors.Put(_doctorIdCounter, d2); await _db.SaveDoctorAsync(d2);

        var p1 = new Patient(++_patientIdCounter, "Ahmet", "Oz", "12345678901", "0535-444-5566", new DateTime(1985, 5, 15));
        _patients.Put(_patientIdCounter, p1); _patientBST.Insert(p1); _patientAVL.Insert(p1); await _db.SavePatientAsync(p1);
        p1.AddVisit(new DateTime(2024, 10, 15, 10, 30, 0), d1, "Annual checkup");
        await _db.SaveVisitAsync(p1.Id, d1.Id, new DateTime(2024, 10, 15, 10, 30, 0), "Annual checkup");
    }

    private async Task LoadFromDatabaseAsync()
    {
        if (_departments.Size > 0) return;

        foreach (var (id, name, cap) in await _db.LoadDepartmentsAsync())
        { var d = new Department(id, name, cap); _departments.Put(id, d); _hospitalTree.AddDepartmentToRoot(d);
          if (id > _departmentIdCounter) _departmentIdCounter = id; }

        foreach (var (id, fn, ln, did, phone) in await _db.LoadDoctorsAsync())
        { var dept = _departments.Get(did); var doc = new Doctor(id, fn, ln, dept, phone);
          dept?.AddDoctor(doc); _doctors.Put(id, doc); _doctorGraph.AddDoctor(doc);
          if (id > _doctorIdCounter) _doctorIdCounter = id; }

        foreach (var (id, fn, ln, nid, phone, bd) in await _db.LoadPatientsAsync())
        { var p = new Patient(id, fn, ln, nid, phone, DateTime.Parse(bd));
          _patients.Put(id, p); _patientBST.Insert(p); _patientAVL.Insert(p); _patientTrie.Insert(p);
          if (id > _patientIdCounter) _patientIdCounter = id; }

        foreach (var (pid, did, vd, notes) in await _db.LoadVisitsAsync())
        { var p = _patients.Get(pid); var d = _doctors.Get(did);
          if (p != null && d != null) p.AddVisit(DateTime.Parse(vd), d, notes); }

        foreach (var (id, pid, did, start, end, status) in await _db.LoadAppointmentsAsync())
        { var p = _patients.Get(pid); var d = _doctors.Get(did);
          if (p != null && d != null) { var app = new Appointment(id, p, d, DateTime.Parse(start));
            app.Status = status; _appointments.Put(id, app); d.DailyQueue.Enqueue(app);
            _segmentTree.AddAppointment(app.Start);
            if (id > _appointmentIdCounter) _appointmentIdCounter = id; } }

        // Phase 2: Create a random referral network for testing if doctors exist
        var docs = _doctors.Values();
        if (docs.Count >= 2)
        {
            _doctorGraph.AddReferral(docs[0], docs[1]);
            if (docs.Count >= 3) _doctorGraph.AddReferral(docs[1], docs[2]);
            if (docs.Count >= 4) {
                _doctorGraph.AddReferral(docs[0], docs[3]);
                _doctorGraph.AddReferral(docs[3], docs[2]);
            }
        }

        RefreshPatientList(); RefreshDoctorList(); RefreshDepartmentList(); RefreshAppointmentList(); UpdateDashboard();

    }

    private void UpdateDashboard()
    {
        // Now handled by ViewModel
    }

    // ============ REPORT EXPORT ============
    private void ExportPatientPdf_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            var path = Services.ReportService.ExportPatientsPdf(_patients.Values());
            TxtReportStatus.Text = $"✓ PDF kaydedildi: {System.IO.Path.GetFileName(path)}";
            SetStatus($"✓ Hasta PDF raporu: {path}");
        }
        catch (Exception ex) { TxtReportStatus.Text = $"✗ Hata: {ex.Message}"; }
    }

    private void ExportAppointmentPdf_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            var path = Services.ReportService.ExportAppointmentsPdf(_appointments.Values());
            TxtReportStatus.Text = $"✓ PDF kaydedildi: {System.IO.Path.GetFileName(path)}";
            SetStatus($"✓ Randevu PDF raporu: {path}");
        }
        catch (Exception ex) { TxtReportStatus.Text = $"✗ Hata: {ex.Message}"; }
    }

    private void ExportPatientsExcel_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            var path = Services.ReportService.ExportPatientsExcel(_patients.Values());
            TxtReportStatus.Text = $"✓ Excel kaydedildi: {System.IO.Path.GetFileName(path)}";
            SetStatus($"✓ Hasta Excel listesi: {path}");
        }
        catch (Exception ex) { TxtReportStatus.Text = $"✗ Hata: {ex.Message}"; }
    }

    private void ExportAppointmentsExcel_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            var path = Services.ReportService.ExportAppointmentsExcel(_appointments.Values());
            TxtReportStatus.Text = $"✓ Excel kaydedildi: {System.IO.Path.GetFileName(path)}";
            SetStatus($"✓ Randevu Excel listesi: {path}");
        }
        catch (Exception ex) { TxtReportStatus.Text = $"✗ Hata: {ex.Message}"; }
    }

    // Legacy HTML handlers (kept for backward compat — buttons removed from XAML)
    private void ExportPatientReport_Click(object? sender, RoutedEventArgs e) => ExportPatientPdf_Click(sender, e);
    private void ExportAppointmentReport_Click(object? sender, RoutedEventArgs e) => ExportAppointmentPdf_Click(sender, e);


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

import re

file_path = "MainWindow.axaml.cs"
with open(file_path, "r") as f:
    text = f.read()

# Instead of setting MainContentControl.Content, we just toggle IsVisible for the panels
# Original: MainContentControl.Content = PanelDashboard;
# Fix: Hide all panels, then set PanelDashboard.IsVisible = true;

text = text.replace(
"""        // Initial Panel View (Animation requires it to be set through ContentControl)
        MainContentControl.Content = PanelDashboard;""",
"""        // Initial Panel View
        ShowPanel(PanelDashboard);"""
)

nav_click_old = """            switch (tag)
            {
                case "Dashboard": MainContentControl.Content = PanelDashboard; break;
                case "Patients": MainContentControl.Content = PanelPatients; break;
                case "Doctors": MainContentControl.Content = PanelDoctors; break;
                case "Appointments": MainContentControl.Content = PanelAppointments; break;
                case "Emergency": MainContentControl.Content = PanelEmergency; RefreshERQueue(); break;
                case "BST": MainContentControl.Content = PanelBST; break;
                case "AVL": MainContentControl.Content = PanelAVL; break;
                case "Departments": MainContentControl.Content = PanelDepartments; break;
                case "Undo": MainContentControl.Content = PanelUndo; UpdateUndoPeek(); break;
                case "Stats": MainContentControl.Content = PanelStats; RefreshStats(); break;
            }"""

nav_click_new = """            switch (tag)
            {
                case "Dashboard": ShowPanel(PanelDashboard); break;
                case "Patients": ShowPanel(PanelPatients); break;
                case "Doctors": ShowPanel(PanelDoctors); break;
                case "Appointments": ShowPanel(PanelAppointments); break;
                case "Emergency": ShowPanel(PanelEmergency); RefreshERQueue(); break;
                case "BST": ShowPanel(PanelBST); break;
                case "AVL": ShowPanel(PanelAVL); break;
                case "Departments": ShowPanel(PanelDepartments); break;
                case "Undo": ShowPanel(PanelUndo); UpdateUndoPeek(); break;
                case "Stats": ShowPanel(PanelStats); RefreshStats(); break;
            }"""

text = text.replace(nav_click_old, nav_click_new)

# Add ShowPanel method
show_panel_method = """
    private void ShowPanel(StackPanel targetPanel)
    {
        foreach (var p in _allPanels)
        {
            if (p != null) p.IsVisible = (p == targetPanel);
        }
    }
"""

# Insert ShowPanel before the Navigation comment
text = text.replace("    // ============ NAVIGATION ============", show_panel_method + "    // ============ NAVIGATION ============")


with open(file_path, "w") as f:
    f.write(text)

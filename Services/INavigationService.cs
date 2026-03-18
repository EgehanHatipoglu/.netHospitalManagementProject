using System;
using HospitalManagementAvolonia.ViewModels;

namespace HospitalManagementAvolonia.Services
{
    /// <summary>
    /// Decouples panel/page navigation from MainWindow code-behind.
    /// MainWindow listens to the Navigated event and swaps panels accordingly.
    /// </summary>
    public interface INavigationService
    {
        /// <summary>Key of the currently active panel (e.g. "Dashboard", "Patients").</summary>
        string CurrentPanel { get; }

        /// <summary>Fired whenever the active panel changes.</summary>
        event Action<string> Navigated;

        /// <summary>Navigate to a named panel and optionally refresh its ViewModel.</summary>
        void NavigateTo(string panelName);
    }
}

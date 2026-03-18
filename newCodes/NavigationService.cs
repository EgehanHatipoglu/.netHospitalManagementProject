using System;
using System.Collections.Generic;
using HospitalManagementAvolonia.ViewModels;

namespace HospitalManagementAvolonia.Services
{
    /// <summary>
    /// Singleton navigation service.  MainWindow subscribes to <see cref="Navigated"/>
    /// and calls ShowPanel() — keeping all navigation logic OUT of code-behind.
    /// </summary>
    public sealed class NavigationService : INavigationService
    {
        public string CurrentPanel { get; private set; } = "Dashboard";

        public event Action<string>? Navigated;

        // Optional: map panel name → refresh action so the service can trigger a refresh
        private readonly Dictionary<string, Action> _refreshActions = new();

        /// <summary>Register a refresh callback for a panel (called by MainViewModel on init).</summary>
        public void RegisterRefresh(string panelName, Action refreshAction)
        {
            _refreshActions[panelName] = refreshAction;
        }

        public void NavigateTo(string panelName)
        {
            if (string.IsNullOrWhiteSpace(panelName)) return;

            CurrentPanel = panelName;

            // Fire registered refresh callback if any
            if (_refreshActions.TryGetValue(panelName, out var refresh))
                refresh();

            Navigated?.Invoke(panelName);
        }
    }
}

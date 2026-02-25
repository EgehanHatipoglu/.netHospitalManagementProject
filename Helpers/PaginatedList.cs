using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace HospitalManagementAvolonia.Helpers
{
    /// <summary>
    /// Generic, observable paginated list.
    /// Bind ItemsSource to <see cref="PagedItems"/>, and expose
    /// <see cref="PageLabel"/>, <see cref="NextPageCommand"/>, <see cref="PrevPageCommand"/>.
    /// </summary>
    public partial class PaginatedList<T> : ObservableObject
    {
        private List<T> _allItems = new();

        [ObservableProperty] private int _currentPage = 1;
        [ObservableProperty] private int _totalPages  = 1;
        [ObservableProperty] private string _pageLabel = "Sayfa 1 / 1";
        [ObservableProperty] private bool _hasPrev;
        [ObservableProperty] private bool _hasNext;

        public int PageSize { get; }
        public ObservableCollection<T> PagedItems { get; } = new();

        public PaginatedList(int pageSize = 15)
        {
            PageSize = pageSize;
        }

        public void Load(IEnumerable<T> items)
        {
            _allItems   = items.ToList();
            CurrentPage = 1;
            TotalPages  = Math.Max(1, (int)Math.Ceiling(_allItems.Count / (double)PageSize));
            Refresh();
        }

        private void Refresh()
        {
            PagedItems.Clear();
            foreach (var item in _allItems.Skip((CurrentPage - 1) * PageSize).Take(PageSize))
                PagedItems.Add(item);

            PageLabel = $"Sayfa {CurrentPage} / {TotalPages}";
            HasPrev   = CurrentPage > 1;
            HasNext   = CurrentPage < TotalPages;
        }

        [RelayCommand]
        private void NextPage()
        {
            if (CurrentPage < TotalPages) { CurrentPage++; Refresh(); }
        }

        [RelayCommand]
        private void PrevPage()
        {
            if (CurrentPage > 1) { CurrentPage--; Refresh(); }
        }
    }
}

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HospitalManagementAvolonia.Services;

namespace HospitalManagementAvolonia.ViewModels
{
    public partial class UndoViewModel : ViewModelBase
    {
        private readonly IUndoService _undoService;

        [ObservableProperty] private string _undoPeek = "Geri alınacak işlem yok.";
        [ObservableProperty] private string _undoResult = "";

        public UndoViewModel(IUndoService undoService)
        {
            _undoService = undoService;
        }

        [RelayCommand]
        public void RefreshData()
        {
            UpdateUndoPeek();
        }

        private void UpdateUndoPeek()
        {
            var op = _undoService.Peek();
            UndoPeek = string.IsNullOrEmpty(op) ? "Geri alınacak işlem yok." : $"Sıradaki: {op}";
        }

        [RelayCommand]
        public void UndoOperation()
        {
            var op = _undoService.UndoLastOperation();
            if (op == null)
            {
                UndoResult = "Geri alınacak işlem yok.";
                return;
            }

            UndoResult = $"✓ İşlem geri alındı: {op}";
            UpdateUndoPeek();
        }
    }
}

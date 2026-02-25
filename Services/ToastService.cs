using System;
using System.Collections.ObjectModel;
using Avalonia.Threading;

namespace HospitalManagementAvolonia.Services
{
    public enum ToastType { Success, Warning, Error, Info }

    public class ToastItem
    {
        public string Message  { get; }
        public ToastType Type  { get; }
        public string Icon     => Type switch
        {
            ToastType.Success => "✓",
            ToastType.Warning => "⚠",
            ToastType.Error   => "✕",
            _                 => "ℹ"
        };
        public string Color    => Type switch
        {
            ToastType.Success => "#16A34A",
            ToastType.Warning => "#D97706",
            ToastType.Error   => "#DC2626",
            _                 => "#2563EB"
        };

        public ToastItem(string message, ToastType type)
        {
            Message = message;
            Type    = type;
        }
    }

    /// <summary>
    /// Singleton service that exposes a queue of toasts for XAML binding.
    /// </summary>
    public class ToastService
    {
        public static ToastService Instance { get; } = new();

        public ObservableCollection<ToastItem> Toasts { get; } = new();

        private ToastService() { }

        public void Show(string message, ToastType type = ToastType.Info)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                var item = new ToastItem(message, type);
                Toasts.Add(item);

                // Auto-dismiss after 3.5 seconds
                var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3.5) };
                timer.Tick += (_, _) =>
                {
                    Toasts.Remove(item);
                    timer.Stop();
                };
                timer.Start();
            });
        }

        public void Success(string msg) => Show(msg, ToastType.Success);
        public void Warning(string msg) => Show(msg, ToastType.Warning);
        public void Error(string msg)   => Show(msg, ToastType.Error);
        public void Info(string msg)    => Show(msg, ToastType.Info);
    }
}

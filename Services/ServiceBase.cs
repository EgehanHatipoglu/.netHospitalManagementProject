using System;
using System.Threading;
using System.Threading.Tasks;

namespace HospitalManagementAvolonia.Services
{
    public abstract class ServiceBase : IAsyncDisposable
    {
        private readonly SemaphoreSlim _initSemaphore = new(1, 1);
        private volatile bool _isInitialized = false;

        protected bool IsInitialized => _isInitialized;

        protected async Task EnsureInitializedAsync(Func<Task> initAction)
        {
            if (_isInitialized) return;

            await _initSemaphore.WaitAsync();
            try
            {
                if (_isInitialized) return;

                await initAction();

                _isInitialized = true;
            }
            finally
            {
                _initSemaphore.Release();
            }
        }
        
        protected void SetInitialized(bool status)
        {
            _isInitialized = status;
        }

        public virtual ValueTask DisposeAsync()
        {
            _initSemaphore.Dispose();
            GC.SuppressFinalize(this);
            return ValueTask.CompletedTask;
        }
    }
}

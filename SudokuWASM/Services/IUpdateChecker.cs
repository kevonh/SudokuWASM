using System;
using System.Threading.Tasks;

namespace Sudoku.Services
{
    public interface IUpdateChecker : IAsyncDisposable
    {
        event Action? OnUpdateAvailable;
        ValueTask CheckForUpdateAsync();
        ValueTask RegisterServiceWorkerAsync();
        Task SkipWaitingAsync();
    }
}

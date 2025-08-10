using System;
using System.Threading.Tasks;

namespace Sudoku.Services
{
    public interface IUpdateChecker
    {
        event Action? OnUpdateAvailable;
        ValueTask CheckForUpdateAsync();
        ValueTask RegisterServiceWorkerAsync();
    }
}

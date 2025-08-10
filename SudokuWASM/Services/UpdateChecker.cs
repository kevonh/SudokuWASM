using Microsoft.JSInterop;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Components;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace Sudoku.Services
{
    public class UpdateChecker : IUpdateChecker, IAsyncDisposable
    {
        private readonly HttpClient httpClient;
        private readonly NavigationManager navigationManager;
        private readonly IJSRuntime jsRuntime;
        private readonly ILogger<UpdateChecker> logger;
        private DotNetObjectReference<UpdateChecker>? dotNetRef;

        public event Action? OnUpdateAvailable;

        public UpdateChecker(HttpClient httpClient, NavigationManager navigationManager, IJSRuntime jsRuntime, ILogger<UpdateChecker> logger)
        {
            this.httpClient = httpClient;
            this.navigationManager = navigationManager;
            this.jsRuntime = jsRuntime;
            this.logger = logger;
        }

        public async ValueTask RegisterServiceWorkerAsync()
        {
            dotNetRef = DotNetObjectReference.Create(this);
            try
            {
                await jsRuntime.InvokeVoidAsync("sudokuRegisterServiceWorker", dotNetRef);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to register service worker");
            }
        }

        [JSInvokable]
        public void NotifyUpdateAvailable()
        {
            logger.LogInformation("Service worker reports an update is available");
            OnUpdateAvailable?.Invoke();
        }

        public async ValueTask CheckForUpdateAsync()
        {
            try
            {
                // Load local version
                var localVersion = await GetVersionAsync("version.json");
                // Force bypass caching on remote fetch by appending timestamp
                var remoteVersion = await GetVersionAsync($"version.json?ts={DateTimeOffset.UtcNow.ToUnixTimeSeconds()}");
                if (string.Compare(remoteVersion, localVersion, StringComparison.OrdinalIgnoreCase) > 0)
                {
                    logger.LogInformation($"Update available: {localVersion} ? {remoteVersion}");
                    OnUpdateAvailable?.Invoke();
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error checking for updates");
            }
        }

        private async Task<string> GetVersionAsync(string path)
        {
            var response = await httpClient.GetAsync(navigationManager.BaseUri + path);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadFromJsonAsync<VersionFile>();
            return json?.Version ?? "0.0.0";
        }

        private class VersionFile
        {
            public string Version { get; set; } = "0.0.0";
        }

        public async ValueTask DisposeAsync()
        {
            if (dotNetRef is not null)
            {
                await jsRuntime.InvokeVoidAsync("sudokuUnregisterServiceWorker", dotNetRef);
                dotNetRef.Dispose();
            }
        }
    }
}

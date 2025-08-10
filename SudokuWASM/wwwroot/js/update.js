window.sudokuRegisterServiceWorker = async function (dotNetRef) {
    if ('serviceWorker' in navigator) {
        try {
            const registration = await navigator.serviceWorker.register('/service-worker.js');
            // Listen for new service worker installation
            registration.onupdatefound = () => {
                const newWorker = registration.installing;
                newWorker.onstatechange = () => {
                    if (newWorker.state === 'installed' && navigator.serviceWorker.controller) {
                        // A new version is available
                        dotNetRef.invokeMethodAsync('NotifyUpdateAvailable');
                    }
                };
            };
        } catch (err) {
            console.error('Service worker registration failed:', err);
        }
    }
};

window.sudokuUnregisterServiceWorker = async function (dotNetRef) {
    // no op; kept for potential clean?up logic in the future
};

// Handle skip waiting messages from Blazor
window.skipWaiting = function () {
    if (navigator.serviceWorker?.controller) {
        navigator.serviceWorker.controller.postMessage({ type: 'SKIP_WAITING' });
    }
};

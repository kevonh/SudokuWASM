// Development service worker with basic offline support
// This provides a fallback for offline scenarios during development

const CACHE_VERSION = 'sudoku-cache-v1';
const CACHE_NAME = CACHE_VERSION;
const STATIC_CACHE = 'sudoku-static-v1';

// Essential files to cache for offline functionality
const ESSENTIAL_FILES = [
    '/',
    '/index.html',
    '/css/app.css',
    '/tailwind.css',
    '/manifest.webmanifest',
    '/icon-192.png',
    '/icon-512.png',
    '/favicon.png'
];

// Install event - cache essential files
self.addEventListener('install', event => {
    console.log('Service Worker: Installing');
    event.waitUntil(
        caches.open(STATIC_CACHE)
            .then(cache => {
                console.log('Service Worker: Caching essential files');
                return cache.addAll(ESSENTIAL_FILES);
            })
            .then(() => {
                console.log('Service Worker: Installation complete');
                return self.skipWaiting();
            })
            .catch(error => {
                console.error('Service Worker: Installation failed', error);
            })
    );
});

// Activate event - clean up old caches
self.addEventListener('activate', event => {
    console.log('Service Worker: Activating');
    event.waitUntil(
        caches.keys()
            .then(cacheNames => {
                return Promise.all(
                    cacheNames
                        .filter(cacheName => 
                            cacheName !== CACHE_NAME && 
                            cacheName !== STATIC_CACHE
                        )
                        .map(cacheName => {
                            console.log('Service Worker: Deleting old cache', cacheName);
                            return caches.delete(cacheName);
                        })
                );
            })
            .then(() => {
                console.log('Service Worker: Activation complete');
                return self.clients.claim();
            })
    );
});

// Fetch event - serve from cache when offline
self.addEventListener('fetch', event => {
    // Skip non-GET requests
    if (event.request.method !== 'GET') {
        return;
    }

    // Skip chrome extensions and other protocols
    if (!event.request.url.startsWith('http')) {
        return;
    }

    event.respondWith(
        fetch(event.request)
            .then(response => {
                // If online, cache important responses and return
                if (response.status === 200) {
                    const responseClone = response.clone();
                    
                    // Cache static assets
                    if (shouldCache(event.request.url)) {
                        caches.open(CACHE_NAME)
                            .then(cache => cache.put(event.request, responseClone))
                            .catch(error => console.log('Cache put failed:', error));
                    }
                }
                return response;
            })
            .catch(error => {
                console.log('Fetch failed, trying cache:', event.request.url);
                
                // If offline, try to serve from cache
                return caches.match(event.request)
                    .then(cachedResponse => {
                        if (cachedResponse) {
                            console.log('Served from cache:', event.request.url);
                            return cachedResponse;
                        }
                        
                        // For navigation requests, serve index.html from cache
                        if (event.request.mode === 'navigate') {
                            console.log('Serving offline page for navigation');
                            return caches.match('/index.html') || caches.match('/');
                        }
                        
                        // For other requests, throw the original error
                        throw error;
                    });
            })
    );
});

// Helper function to determine if a request should be cached
function shouldCache(url) {
    const cacheableExtensions = ['.css', '.js', '.png', '.jpg', '.jpeg', '.gif', '.ico', '.woff', '.woff2'];
    const excludePatterns = [
        'service-worker',
        'hot-reload',
        'aspnetcore-browser-refresh',
        'chrome-extension'
    ];
    
    // Don't cache if URL contains excluded patterns
    if (excludePatterns.some(pattern => url.includes(pattern))) {
        return false;
    }
    
    // Cache if URL has cacheable extension or is the root/index
    return cacheableExtensions.some(ext => url.includes(ext)) || 
           url.endsWith('/') || 
           url.includes('index.html');
}

// Listen for SKIP_WAITING message from client
self.addEventListener('message', (event) => {
    if (event.data && event.data.type === 'SKIP_WAITING') {
        self.skipWaiting();
    }
});

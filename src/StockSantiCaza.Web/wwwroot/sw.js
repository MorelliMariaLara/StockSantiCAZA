/* StockSantiCAZA service worker — cache de assets estáticos para uso tipo app */
const CACHE_VERSION = 'santicaza-v4';
const PRECACHE = [
  '/manifest.webmanifest',
  '/css/app.css?v=19',
  '/js/app.js?v=16',
  '/js/api.js?v=12',
  '/js/pwa.js?v=1',
  '/js/dialogs.js?v=10',
  '/js/download.js?v=10',
  '/img/logo-login.png',
  '/img/logo-santicaza.png',
  '/img/logo-santicaza.webp',
  '/img/logo-login.webp',
  '/img/logo-santicaza-mark.png',
  '/img/logo-santicaza-mark.webp',
  '/img/icon-192.png',
  '/img/icon-512.png',
  '/img/apple-touch-icon.png',
  '/login'
];

self.addEventListener('install', (event) => {
  event.waitUntil(
    caches.open(CACHE_VERSION)
      .then((cache) => cache.addAll(PRECACHE.map((url) => new Request(url, { cache: 'reload' }))))
      .then(() => self.skipWaiting())
      .catch(() => self.skipWaiting())
  );
});

self.addEventListener('activate', (event) => {
  event.waitUntil(
    caches.keys().then((keys) =>
      Promise.all(keys.filter((key) => key !== CACHE_VERSION).map((key) => caches.delete(key)))
    ).then(() => self.clients.claim())
  );
});

function isApiRequest(url) {
  return url.pathname.startsWith('/api/');
}

function isNavigation(request) {
  return request.mode === 'navigate' || (request.headers.get('accept') || '').includes('text/html');
}

self.addEventListener('fetch', (event) => {
  const request = event.request;
  if (request.method !== 'GET') return;

  const url = new URL(request.url);
  if (url.origin !== self.location.origin) return;

  // API y auth siempre red (cookies / datos vivos)
  if (isApiRequest(url)) return;

  if (isNavigation(request)) {
    event.respondWith(
      fetch(request)
        .then((response) => {
          const copy = response.clone();
          caches.open(CACHE_VERSION).then((cache) => cache.put(request, copy)).catch(() => {});
          return response;
        })
        .catch(async () => {
          const cached = await caches.match(request);
          if (cached) return cached;
          return caches.match('/login');
        })
    );
    return;
  }

  // Assets estáticos: cache-first con actualización en segundo plano
  event.respondWith(
    caches.match(request).then((cached) => {
      const network = fetch(request)
        .then((response) => {
          if (response && response.ok) {
            const copy = response.clone();
            caches.open(CACHE_VERSION).then((cache) => cache.put(request, copy)).catch(() => {});
          }
          return response;
        })
        .catch(() => cached);

      return cached || network;
    })
  );
});

/**
 * Service Worker cho MultiGuide Heritage PWA
 * Hỗ trợ lưu cache tài nguyên tĩnh, font chữ và chạy offline khi mất sóng di động
 */
const CACHE_NAME = 'multiguide-v2.3';

const STATIC_ASSETS = [
  './',
  './index.html',
  './manifest.json',
  './css/variables.css',
  './css/base.css',
  './css/components.css',
  './css/map.css',
  './style.css',
  './js/app.js?v=2.2',
  './js/config.js',
  './js/state.js',
  './js/api.js',
  './js/geo.js',
  './js/map.js',
  './js/audio.js',
  './js/ui.js',
  './assets/icons/logo-mark.svg',
  './assets/icons/compass-n.svg',
  './assets/icons/recenter.svg',
  './assets/icons/filter-sliders.svg',
  './assets/icons/heritage-temple.svg',
  './assets/icons/tab-explore.svg',
  './assets/icons/tab-stops.svg',
  './assets/icons/tab-pass.svg',
  './assets/icons/lock.svg',
  './assets/khue_van_cac.png'
];

self.addEventListener('install', (event) => {
  console.log('[MultiGuide SW] Đang cài đặt Service Worker và lưu cache tĩnh...');
  event.waitUntil(
    caches.open(CACHE_NAME).then((cache) => {
      return cache.addAll(STATIC_ASSETS);
    }).then(() => self.skipWaiting())
  );
});

self.addEventListener('activate', (event) => {
  console.log('[MultiGuide SW] Kích hoạt phiên bản mới...');
  event.waitUntil(
    caches.keys().then((keys) => {
      return Promise.all(
        keys.filter((key) => key !== CACHE_NAME).map((key) => caches.delete(key))
      );
    }).then(() => self.clients.claim())
  );
});

self.addEventListener('fetch', (event) => {
  // Chỉ xử lý các request GET cục bộ
  if (event.request.method !== 'GET') return;
  const url = new URL(event.request.url);

  // Không cache API Backend động (để luôn lấy dữ liệu mới nhất)
  if (url.pathname.includes('/api/') || url.hostname.includes('render.com')) {
    return;
  }

  event.respondWith(
    caches.match(event.request).then((cachedResponse) => {
      if (cachedResponse) {
        return cachedResponse;
      }
      return fetch(event.request).then((networkResponse) => {
        // Lưu tạm vào cache nếu request hợp lệ
        if (networkResponse && networkResponse.status === 200 && networkResponse.type === 'basic') {
          const responseToCache = networkResponse.clone();
          caches.open(CACHE_NAME).then((cache) => {
            cache.put(event.request, responseToCache);
          });
        }
        return networkResponse;
      }).catch(() => {
        // Nếu offline và yêu cầu file HTML -> trả về cache trang chính
        if (event.request.headers.get('accept').includes('text/html')) {
          return caches.match('./index.html');
        }
      });
    })
  );
});

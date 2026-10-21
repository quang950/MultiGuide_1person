/**
 * Service Worker cho MultiGuide Heritage PWA
 * Hỗ trợ lưu cache tài nguyên tĩnh, font chữ và chạy offline khi mất sóng di động
 */
const CACHE_NAME = 'multiguide-v3.7';

const STATIC_ASSETS = [
  './',
  './index.html',
  './admin.html',
  './manifest.json',
  './css/variables.css?v=2.9',
  './css/base.css?v=2.9',
  './css/components.css?v=2.9',
  './css/map.css?v=2.9',
  './css/admin.css?v=3.2',
  './js/app.js?v=2.9',
  './js/admin.js?v=3.2',
  './js/i18n.js',
  './js/config.js',
  './js/state.js',
  './js/api.js',
  './js/geo.js',
  './js/map.js',
  './js/audio.js',
  './js/ui.js',
  './assets/icons/flag-vn.svg',
  './assets/icons/flag-en.svg',
  './assets/icons/flag-zh.svg',
  './assets/icons/flag-ja.svg',
  './assets/icons/logo-mark.svg',
  './assets/icons/compass-n.svg',
  './assets/icons/recenter.svg',
  './assets/icons/filter-sliders.svg',
  './assets/icons/heritage-temple.svg',
  './assets/icons/tab-explore.svg',
  './assets/icons/tab-stops.svg',
  './assets/icons/tab-pass.svg',
  './assets/icons/ticket-watermark.svg',
  './assets/icons/lock.svg',
  './assets/hero-khue-van-cac.jpg'
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
  
  // Bỏ qua chrome-extension và các giao thức không phải http/https
  if (!event.request.url.startsWith('http://') && !event.request.url.startsWith('https://')) {
    return;
  }
  
  const url = new URL(event.request.url);

  // Khi phát triển trên localhost / 127.0.0.1: luôn lấy trực tiếp từ network để không bị dính cache file JS/CSS cũ
  if (url.hostname === 'localhost' || url.hostname === '127.0.0.1') {
    return;
  }

  // Để trình duyệt tự xử lý điều hướng trang (navigate) nguyên bản, tránh lỗi xung đột redirect trên Vercel
  if (event.request.mode === 'navigate') {
    return;
  }

  // Không cache API Backend động (để luôn lấy dữ liệu mới nhất)
  if (url.pathname.includes('/api/') || url.hostname.includes('render.com') || url.hostname.includes('supabase.co')) {
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

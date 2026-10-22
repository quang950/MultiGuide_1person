/**
 * Service Worker cho MultiGuide Heritage PWA
 * Chiến lược: Network-First cho HTML/JS/CSS (luôn lấy bản mới nhất trên Vercel),
 * Fallback sang Cache Storage khi mất sóng / offline ngoài thực địa di tích.
 */
const CACHE_NAME = 'multiguide-v4.2';

const STATIC_ASSETS = [
  './',
  './index.html',
  './admin.html',
  './manifest.json',
  './css/variables.css?v=2.9',
  './css/base.css?v=2.9',
  './css/components.css?v=2.9',
  './css/map.css?v=4.2',
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
  console.log('[MultiGuide SW v4.0] Cài đặt Service Worker mới và lưu cache tĩnh...');
  event.waitUntil(
    caches.open(CACHE_NAME).then((cache) => {
      return cache.addAll(STATIC_ASSETS);
    }).then(() => self.skipWaiting())
  );
});

self.addEventListener('activate', (event) => {
  console.log('[MultiGuide SW v4.0] Kích hoạt và dọn dẹp toàn bộ cache cũ...');
  event.waitUntil(
    caches.keys().then((keys) => {
      return Promise.all(
        keys.filter((key) => key !== CACHE_NAME).map((key) => {
          console.log('[MultiGuide SW] Xóa bỏ cache cũ:', key);
          return caches.delete(key);
        })
      );
    }).then(() => self.clients.claim())
  );
});

self.addEventListener('fetch', (event) => {
  // Chỉ xử lý các request GET
  if (event.request.method !== 'GET') return;
  
  // Bỏ qua chrome-extension và các giao thức không phải http/https
  if (!event.request.url.startsWith('http://') && !event.request.url.startsWith('https://')) {
    return;
  }
  
  const url = new URL(event.request.url);

  // Khi phát triển trên localhost / 127.0.0.1: luôn lấy trực tiếp từ network
  if (url.hostname === 'localhost' || url.hostname === '127.0.0.1') {
    return;
  }

  // Không cache API Backend động (Render, Supabase)
  if (url.pathname.includes('/api/') || url.hostname.includes('render.com') || url.hostname.includes('supabase.co')) {
    return;
  }

  // 1. Với các tài nguyên Code/Nội dung (HTML, JS, CSS, JSON, Navigations):
  // ÁP DỤNG NETWORK-FIRST: Luôn hỏi Vercel trước để lấy bản mới nhất ngay khi vừa deploy!
  const isCodeOrDoc = 
    event.request.mode === 'navigate' ||
    event.request.destination === 'script' ||
    event.request.destination === 'style' ||
    event.request.destination === 'document' ||
    url.pathname.endsWith('.html') ||
    url.pathname.endsWith('.js') ||
    url.pathname.endsWith('.css') ||
    url.pathname.endsWith('.json') ||
    url.pathname === '/' ||
    url.pathname.endsWith('/');

  if (isCodeOrDoc) {
    event.respondWith(
      fetch(event.request)
        .then((networkResponse) => {
          // Khi lấy được bản mới nhất từ mạng thành công, cập nhật đè vào Cache
          if (networkResponse && networkResponse.status === 200) {
            const responseClone = networkResponse.clone();
            caches.open(CACHE_NAME).then((cache) => {
              cache.put(event.request, responseClone);
            });
          }
          return networkResponse;
        })
        .catch(() => {
          // MẤT MẠNG / OFFLINE NGOÀI THỰC ĐỊA: Lấy từ Cache Storage dự phòng
          return caches.match(event.request).then((cached) => {
            if (cached) return cached;
            if (event.request.headers.get('accept')?.includes('text/html') || event.request.mode === 'navigate') {
              return caches.match('./index.html');
            }
          });
        })
    );
    return;
  }

  // 2. Với các tài nguyên tĩnh Media/Images/Icons: Áp dụng Stale-While-Revalidate
  event.respondWith(
    caches.match(event.request).then((cachedResponse) => {
      const fetchPromise = fetch(event.request).then((networkResponse) => {
        if (networkResponse && networkResponse.status === 200) {
          const responseToCache = networkResponse.clone();
          caches.open(CACHE_NAME).then((cache) => cache.put(event.request, responseToCache));
        }
        return networkResponse;
      }).catch(() => {});

      return cachedResponse || fetchPromise;
    })
  );
});

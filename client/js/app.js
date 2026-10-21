import { CONFIG } from './config.js?v=3.6';
import { store } from './state.js';
import { apiService } from './api.js';
import { initLanguageSwitcher, initPassCheckout, initAdminLogin } from './ui.js';
import { initI18n } from './i18n.js';

console.log('[MultiGuide PWA] Khởi tạo ứng dụng di sản...');

document.addEventListener('DOMContentLoaded', () => {
  // 1. Khởi tạo bộ máy Đa Ngôn Ngữ tập trung (i18n)
  initI18n();

  // 2. Gửi ngầm Wake-up Ping kiểm tra Backend API ngay khi tải trang
  apiService.checkHealth().then(isAwake => {
    console.log(`[MultiGuide Backend] API Target (${CONFIG.API_BASE_URL}):`, isAwake ? 'READY (Online)' : 'OFFLINE / STARTING');
  });

  // 3. Khởi tạo Language Switcher Pill (FE-2.2)
  initLanguageSwitcher();

  // 4. Khởi tạo Thẻ vé số & Nút kích hoạt 1-chạm (FE-2.3 + FE-2.4)
  initPassCheckout();

  // 5. Khởi tạo Nút nổi Admin & Modal đăng nhập
  initAdminLogin();

  // 6. Quản lý Service Worker PWA (FE-7.1)
  const isLocal = typeof window !== 'undefined' && 
    (window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1');

  if ('serviceWorker' in navigator) {
    if (isLocal) {
      // Khi dev local: Hủy đăng ký SW và xóa cache để không bị kẹt JS cũ
      navigator.serviceWorker.getRegistrations().then(regs => {
        for (const reg of regs) reg.unregister();
      });
      if (window.caches) {
        caches.keys().then(keys => keys.forEach(k => caches.delete(k)));
      }
    } else {
      window.addEventListener('load', () => {
        navigator.serviceWorker.register('./sw.js')
          .then(reg => {
            console.log('[MultiGuide PWA] Service Worker đăng ký thành công với scope:', reg.scope);
          })
          .catch(err => {
            console.warn('[MultiGuide PWA] Service Worker đăng ký thất bại:', err);
          });
      });
    }
  }

  console.log('[MultiGuide PWA] Giao diện đã sẵn sàng. Ngôn ngữ hiện tại:', store.get().selectedLang);
});


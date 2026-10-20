import { CONFIG } from './config.js';
import { store } from './state.js';
import { apiService } from './api.js';
import { initLanguageSwitcher, initPassCheckout, initAdminLogin } from './ui.js';
import { initI18n } from './i18n.js';

console.log('[MultiGuide PWA] Khởi tạo ứng dụng di sản...');

document.addEventListener('DOMContentLoaded', () => {
  // 1. Khởi tạo bộ máy Đa Ngôn Ngữ tập trung (i18n)
  initI18n();

  // 2. Gửi ngầm Wake-up Ping đánh thức Render server ngay khi tải trang (CONSTRAINTS.md)
  apiService.checkHealth().then(isAwake => {
    console.log('[MultiGuide Backend] Render Server Status:', isAwake ? 'READY (Online)' : 'WAKING UP (Cold Start)');
  });

  // 3. Khởi tạo Language Switcher Pill (FE-2.2)
  initLanguageSwitcher();

  // 4. Khởi tạo Thẻ vé số & Nút kích hoạt 1-chạm (FE-2.3 + FE-2.4)
  initPassCheckout();

  // 5. Khởi tạo Nút nổi Admin & Modal đăng nhập
  initAdminLogin();

  // 4. Đăng ký Service Worker PWA (FE-7.1)
  if ('serviceWorker' in navigator) {
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

  console.log('[MultiGuide PWA] Giao diện đã sẵn sàng. Ngôn ngữ hiện tại:', store.get().selectedLang);
});


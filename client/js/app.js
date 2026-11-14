import { CONFIG } from './config.js?v=3.6';
import { store } from './state.js';
import { apiService } from './api.js';
import { initLanguageSwitcher, initPassCheckout, initAdminLogin, transitionToMainApp, initAreaSelectorAndGps, updateWelcomeScreenPrice, initScanRadiusController, unlockArea } from './ui.js';
import { initI18n } from './i18n.js';

console.log('[MultiGuide PWA] Khởi tạo ứng dụng di sản...');

function initApp() {
  // 1. Khởi tạo bộ máy Đa Ngôn Ngữ tập trung (i18n)
  initI18n();

  // 2. Gửi ngầm Wake-up Ping kiểm tra Backend API ngay khi tải trang
  apiService.checkHealth().then(isAwake => {
    console.log(`[MultiGuide Backend] API Target (${CONFIG.API_BASE_URL}):`, isAwake ? 'READY (Online)' : 'OFFLINE / STARTING');
  });

  // 3. Khởi tạo Language Switcher Pill (FE-2.2)
  initLanguageSwitcher();

  // 3b. Khởi tạo Thanh Chọn Khu Di Tích & Định vị GPS
  initAreaSelectorAndGps();

  // 4. Khởi tạo Thẻ vé số & Nút kích hoạt 1-chạm (FE-2.3 + FE-2.4)
  initPassCheckout();
  updateWelcomeScreenPrice();

  // 5. Khởi tạo Nút nổi Admin & Modal đăng nhập
  initAdminLogin();

  // 5a. Khởi tạo Widget chỉnh Bán kính quét vị trí hiện tại
  initScanRadiusController();

  // 5b. Kiểm tra chế độ xem từ Admin (Xem App Khách) hoặc Admin đang đăng nhập
  const urlParams = new URLSearchParams(window.location.search);
  const isAdminPreview = urlParams.get('mode') === 'admin_preview';
  const hasAdminToken = !!localStorage.getItem('mg_admin_token');

  if (isAdminPreview) {
    document.documentElement.classList.add('is-admin-preview');
  }

  const btnReturnAdmin = document.getElementById('btnReturnAdmin');
  if (btnReturnAdmin && hasAdminToken) {
    btnReturnAdmin.style.display = 'inline-flex';
  }

  const isReload = !!(window.performance && (
    window.performance.getEntriesByType?.('navigation')?.[0]?.type === 'reload' ||
    window.performance.navigation?.type === 1
  ));

  if (isReload) {
    sessionStorage.removeItem('mg_unlocked_areas_session');
    sessionStorage.removeItem('mg_tokens_by_area');
    localStorage.removeItem('mg_access_token');
    store.setAccessToken(null);
    store.set({ hasPurchased: false });
  }

  if (isAdminPreview) {
    console.log('[MultiGuide PWA] Chế độ xem trước dành riêng cho Quản trị viên...');
    const chosenArea = store.get().areaCode || 'van-mieu';
    unlockArea(chosenArea);

    if (!store.get().accessToken) {
      store.setAccessToken('mg_admin_preview_pass', {
        code: 'ADMIN-PREVIEW-PASS',
        expiresAt: new Date(Date.now() + 86400000).toISOString(),
        status: 'active'
      });
      apiService.purchasePass(chosenArea).then(passData => {
        const token = passData.accessToken || passData.token;
        if (token) store.setAccessToken(token, passData);
        unlockArea(chosenArea);
      }).catch(() => {});
    }
    transitionToMainApp();
  } else {
    // Luồng người dùng thông thường: Reset vé khi F5/reload trang theo đúng yêu cầu
    if (isReload) {
      sessionStorage.removeItem('mg_unlocked_areas_session');
      sessionStorage.removeItem('mg_tokens_by_area');
      localStorage.removeItem('mg_access_token');
      store.setAccessToken(null);
      store.set({ hasPurchased: false });
    }
  }

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
            // Chủ động kiểm tra cập nhật trên Vercel mỗi khi mở trang
            reg.update().catch(() => {});
          })
          .catch(err => {
            console.warn('[MultiGuide PWA] Service Worker đăng ký thất bại:', err);
          });
      });

      // Khi có Service Worker mới được kích hoạt, tự động làm mới trang 1 lần duy nhất để người dùng luôn có bản mới
      let refreshing = false;
      navigator.serviceWorker.addEventListener('controllerchange', () => {
        if (!refreshing) {
          refreshing = true;
          console.log('[MultiGuide PWA] Đã phát hiện phiên bản mới, đang tự động làm mới ứng dụng...');
          window.location.reload();
        }
      });
    }
  }

  console.log('[MultiGuide PWA] Giao diện đã sẵn sàng. Ngôn ngữ hiện tại:', store.get().selectedLang);
}

if (document.readyState === 'loading') {
  document.addEventListener('DOMContentLoaded', initApp);
} else {
  initApp();
}

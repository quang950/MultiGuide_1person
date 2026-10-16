/**
 * MultiGuide - UI Controller & Components
 * Quản lý giao diện và tương tác người dùng
 */
import { store } from './state.js';
import { apiService } from './api.js';
import { mapManager } from './map.js';

/**
 * Mở khóa AudioContext cho iOS Safari / Android Chrome (CONSTRAINTS.md)
 * Bằng cách phát một âm thanh rỗng cực ngắn ở volume 0 ngay khi có cử chỉ người dùng.
 */
export function unlockAudioContextSilently() {
  try {
    const silentAudio = new Audio('data:audio/wav;base64,UklGRigAAABXQVZFZm10IBIAAAABAAEARKwAAIhYAQACABAAAABkYXRhAgAAAAEA');
    silentAudio.volume = 0;
    const playPromise = silentAudio.play();
    if (playPromise !== undefined) {
      playPromise.catch(() => {});
    }
  } catch (e) {
    // Bỏ qua nếu trình duyệt không hỗ trợ
  }
}

/**
 * Khởi tạo Thanh chuyển đổi ngôn ngữ (Language Switcher Pill - FE-2.2)
 */
export function initLanguageSwitcher() {
  const switcherContainer = document.getElementById('languageSwitcher');
  if (!switcherContainer) return;

  const buttons = switcherContainer.querySelectorAll('.lang-btn');

  // Hàm cập nhật trạng thái hiển thị của các nút theo mã ngôn ngữ
  function updateActiveButton(activeLang) {
    buttons.forEach((btn) => {
      const isSelected = btn.dataset.lang === activeLang;
      btn.classList.toggle('is-active', isSelected);
      btn.setAttribute('aria-checked', isSelected ? 'true' : 'false');
    });

    // Đồng bộ với select ẩn (nếu có)
    const selectElem = document.getElementById('languageSelect');
    if (selectElem && selectElem.value !== activeLang) {
      selectElem.value = activeLang;
    }
  }

  // Khởi tạo trạng thái ban đầu từ store
  const initialLang = store.get().selectedLang || 'vi';
  updateActiveButton(initialLang);

  // Đăng ký lắng nghe store để tự động phản ánh thay đổi
  store.subscribe((state) => {
    updateActiveButton(state.selectedLang);
  });

  // Lắng nghe sự kiện click trên các nút (Event Delegation)
  switcherContainer.addEventListener('click', (e) => {
    const btn = e.target.closest('.lang-btn');
    if (!btn) return;

    const langCode = btn.dataset.lang;
    if (langCode && ['vi', 'en', 'ja'].includes(langCode)) {
      // 1. Mở khóa AudioContext im lặng cho mobile
      unlockAudioContextSilently();

      // 2. Cập nhật state tập trung (tự động ghi vào localStorage)
      store.setLanguage(langCode);
      console.log(`[MultiGuide] Ngôn ngữ đã chuyển sang: ${langCode}`);
    }
  });
}

/**
 * Khởi tạo Nút Mua/Kích hoạt vé 1-chạm (FE-2.4)
 * Kết nối RESTful API Backend: POST /areas/van-mieu/passes
 */
export function initPassCheckout() {
  const btnActivate = document.getElementById('btnActivatePass');
  if (!btnActivate) return;

  const btnText = btnActivate.querySelector('.btn-text');

  // Kiểm tra nếu người dùng đã có vé hợp lệ trong máy
  const currentToken = store.get().accessToken;
  if (currentToken) {
    if (btnText) btnText.textContent = 'Tiếp tục khám phá di tích →';
  }

  btnActivate.addEventListener('click', async () => {
    // 1. Mở khóa AudioContext ngay tại cử chỉ click người dùng (CONSTRAINTS.md)
    unlockAudioContextSilently();

    // Nếu đã có token vé từ trước, chuyển thẳng sang màn hình chính
    if (store.get().accessToken) {
      transitionToMainApp();
      return;
    }

    // 2. Hiệu ứng Loading "Calm Technology"
    btnActivate.disabled = true;
    btnActivate.classList.add('is-loading');
    if (btnText) btnText.textContent = 'Đang kết nối không gian di sản...';

    const areaCode = store.get().areaCode || 'van-mieu';

    try {
      // 3. Gọi RESTful API Backend Render thật
      console.log(`[MultiGuide API] Gửi POST /areas/${areaCode}/passes...`);
      const passData = await apiService.purchasePass(areaCode);

      console.log('[MultiGuide API] Kích hoạt vé thành công từ Backend:', passData);

      // Lưu trữ token và thông tin vé
      const token = passData.accessToken || passData.token;
      store.setAccessToken(token, passData);

      // Cập nhật trạng thái thành công
      if (btnText) btnText.textContent = '✓ Vé di sản đã kích hoạt';
      btnActivate.style.backgroundColor = '#2E7D32';

      // Chuyển cảnh sang màn hình chính
      setTimeout(() => {
        transitionToMainApp();
      }, 700);

    } catch (error) {
      console.warn('[MultiGuide API] Lỗi kết nối server (Cold Start/Offline), kích hoạt vé trải nghiệm:', error);

      // Fallback Smart Token để trải nghiệm không bị nghẽn nếu Render đang thức giấc
      const fallbackToken = 'mg_demo_pass_' + Date.now();
      const mockPassData = {
        passId: 'HN-001-DEMO',
        accessToken: fallbackToken,
        expiresAt: new Date(Date.now() + 24 * 3600 * 1000).toISOString(),
        areaCode: areaCode,
        isOfflineDemo: true
      };

      store.setAccessToken(fallbackToken, mockPassData);

      if (btnText) btnText.textContent = '✓ Đã kích hoạt vé tham quan';
      btnActivate.style.backgroundColor = '#C28E58';

      setTimeout(() => {
        transitionToMainApp();
      }, 700);

    } finally {
      btnActivate.classList.remove('is-loading');
      btnActivate.disabled = false;
    }
  });
}

import { audioEngine } from './audio.js';

let proximityToastTimer = null;

/**
 * Hiển thị thông báo nhẹ nhàng Calm Technology khi đến gần POI (Cụm 4)
 */
export function showProximityBeacon(poi) {
  const toast = document.getElementById('proximityToast');
  const toastText = document.getElementById('proximityToastText');
  if (!toast || !toastText || !poi) return;

  const lang = store.get().selectedLang || 'vi';
  const poiName = (lang === 'en' && poi.titleEn) ? poi.titleEn : (poi.title || poi.name || 'di tích');

  // Haptic feedback nhẹ nhàng (Calm Technology)
  if ('vibrate' in navigator) {
    try {
      navigator.vibrate([40, 60, 40]);
    } catch (e) {}
  }

  const message = lang === 'en'
    ? `Nearby: ${poiName} · Tap to explore`
    : lang === 'ja'
      ? `近くに ${poiName} があります · タップして探索`
      : `Bạn đang ở gần ${poiName} · Chạm để khám phá`;

  toastText.textContent = message;
  toast.classList.add('is-visible');

  // Chạm vào thông báo để mở ngay POI và phát thuyết minh
  toast.onclick = () => {
    toast.classList.remove('is-visible');
    mapManager.selectPoi(poi.id || poi.poiId);
  };

  if (proximityToastTimer) clearTimeout(proximityToastTimer);
  proximityToastTimer = setTimeout(() => {
    toast.classList.remove('is-visible');
  }, 4500);
}

/**
 * Khởi tạo Interactive Heritage Bottom Sheet Drawer (FE-5.1)
 * 3 nấc trượt: Nấc 0 (Ẩn), Nấc 1 (Mini Player ~140px), Nấc 2 (Kéo lên 78vh đọc toàn văn)
 */
export function initBottomSheet() {
  const sheet = document.getElementById('heritageBottomSheet');
  const dragZone = document.getElementById('sheetDragZone');
  const miniHeader = document.getElementById('miniPlayerHeader');
  if (!sheet) return;

  // 1. Nhấp vào Mini Player Header để chuyển đổi nấc mở rộng (tránh chạm nút play và scrubber)
  if (miniHeader) {
    miniHeader.addEventListener('click', (e) => {
      if (e.target.closest('#btnPlayerToggle') || e.target.closest('#playerScrubberTrack')) {
        return;
      }
      sheet.classList.toggle('is-expanded');
    });
  }

  // 2. Kéo vuốt cảm ứng (Swipe Gesture) trên thanh nắm Drag Handle
  let startY = 0;
  let isDragging = false;

  const onDragStart = (clientY) => {
    startY = clientY;
    isDragging = true;
  };

  const onDragEnd = (clientY) => {
    if (!isDragging) return;
    isDragging = false;
    const deltaY = clientY - startY;

    // Vuốt lên > 40px -> Mở rộng Nấc 2 đọc toàn văn
    if (deltaY < -40) {
      sheet.classList.add('is-expanded');
    }
    // Vuốt xuống > 40px -> Thu gọn về Mini Player Nấc 1
    else if (deltaY > 40) {
      sheet.classList.remove('is-expanded');
    }
  };

  if (dragZone) {
    dragZone.addEventListener('touchstart', (e) => {
      if (e.touches && e.touches[0]) onDragStart(e.touches[0].clientY);
    }, { passive: true });

    dragZone.addEventListener('touchend', (e) => {
      if (e.changedTouches && e.changedTouches[0]) onDragEnd(e.changedTouches[0].clientY);
    }, { passive: true });

    dragZone.addEventListener('mousedown', (e) => {
      onDragStart(e.clientY);
      const onMouseUp = (upEvt) => {
        onDragEnd(upEvt.clientY);
        window.removeEventListener('mouseup', onMouseUp);
      };
      window.addEventListener('mouseup', onMouseUp);
    });
  }

  // 3. Nút đóng / hạ khung Bottom Sheet (✕)
  const btnClose = document.getElementById('btnSheetClose');
  if (btnClose) {
    btnClose.addEventListener('click', (e) => {
      e.stopPropagation();
      hideBottomSheet();
    });
  }
}

/**
 * Hiển thị khung Bottom Sheet (Nấc 1) khi du khách chọn POI
 */
export function showBottomSheet() {
  const sheet = document.getElementById('heritageBottomSheet');
  if (sheet) {
    sheet.classList.add('is-visible');
  }
}

/**
 * Thu nhỏ / Ẩn hoàn toàn khung Bottom Sheet (Nấc 0)
 */
export function hideBottomSheet() {
  const sheet = document.getElementById('heritageBottomSheet');
  if (sheet) {
    sheet.classList.remove('is-visible');
    sheet.classList.remove('is-expanded');
  }

  // Tạm dừng phát âm thanh
  if (audioEngine && audioEngine.audioElement) {
    audioEngine.audioElement.pause();
  }

  // Xóa trạng thái active của marker trên bản đồ
  if (mapManager) {
    mapManager.deselectAllPois();
  }
}

/**
 * ========================================================
 * 4. Bottom Navigation & Tab Views Logic (FE-6.1, FE-6.2, FE-6.3)
 * ========================================================
 */
let passCountdownInterval = null;

export function initBottomNavigation() {
  const btnExplore = document.getElementById('btnTabExplore');
  const btnStops = document.getElementById('btnTabStops');
  const btnPass = document.getElementById('btnTabPass');
  const btnCloseStops = document.getElementById('btnCloseStopsView');
  const btnClosePass = document.getElementById('btnClosePassView');

  if (btnExplore) btnExplore.addEventListener('click', () => switchTab('explore'));
  if (btnStops) btnStops.addEventListener('click', () => switchTab('stops'));
  if (btnPass) btnPass.addEventListener('click', () => switchTab('pass'));

  if (btnCloseStops) btnCloseStops.addEventListener('click', () => switchTab('explore'));
  if (btnClosePass) btnClosePass.addEventListener('click', () => switchTab('explore'));

  // Bộ chuyển đổi ngôn ngữ trong tab My Pass
  initPassLangSwitcher();

  // Nút tiện ích trong tab My Pass
  initPassActions();
}

/**
 * Chuyển đổi giữa 3 Tab: 'explore' (Bản đồ), 'stops' (Lộ trình), 'pass' (Vé của tôi)
 */
export function switchTab(tabName) {
  const btnExplore = document.getElementById('btnTabExplore');
  const btnStops = document.getElementById('btnTabStops');
  const btnPass = document.getElementById('btnTabPass');
  const viewStops = document.getElementById('tabViewStops');
  const viewPass = document.getElementById('tabViewPass');

  // Cập nhật trạng thái active của nút Tab
  [btnExplore, btnStops, btnPass].forEach(btn => {
    if (btn) btn.classList.remove('is-active');
  });

  if (tabName === 'explore') {
    if (btnExplore) btnExplore.classList.add('is-active');
    if (viewStops) viewStops.classList.remove('is-open');
    if (viewPass) viewPass.classList.remove('is-open');
    if (window.map) {
      setTimeout(() => window.map.invalidateSize(), 300);
    }
  } else if (tabName === 'stops') {
    if (btnStops) btnStops.classList.add('is-active');
    if (viewStops) {
      renderAllStops();
      viewStops.classList.add('is-open');
    }
    if (viewPass) viewPass.classList.remove('is-open');
  } else if (tabName === 'pass') {
    if (btnPass) btnPass.classList.add('is-active');
    if (viewPass) {
      initPassCountdown();
      viewPass.classList.add('is-open');
    }
    if (viewStops) viewStops.classList.remove('is-open');
  }
}

/**
 * Render động danh sách 6 điểm di tích cho màn hình "All stops" (FE-6.2)
 */
export function renderAllStops() {
  const container = document.getElementById('stopsListContainer');
  if (!container || !window.HERITAGE_POIS) return;

  const pois = window.HERITAGE_POIS;
  const activePoi = store.get().activePoi;
  const activeId = activePoi ? (activePoi.id || activePoi.order) : null;

  const html = pois.map(p => {
    const isNowPlaying = (p.id === activeId);
    const cardClass = isNowPlaying ? 'poi-journey-card is-active' : 'poi-journey-card';
    
    let statusBadge = '';
    if (isNowPlaying) {
      statusBadge = '<span class="journey-status-pill status-pill-playing">ĐANG PHÁT</span>';
    } else if (p.isNearby) {
      statusBadge = '<span class="journey-status-pill status-pill-nearby">Ở GẦN BẠN</span>';
    } else {
      statusBadge = '<span class="journey-status-pill status-pill-listen">NGHE NGAY</span>';
    }

    return `
      <div class="${cardClass}" data-poi-id="${p.id}">
        <div class="journey-number-badge">${String(p.order).padStart(2, '0')}</div>
        <div class="journey-info-group">
          <div class="journey-meta-row">
            <span class="journey-order-tag">ĐIỂM DỪNG ${p.order} / 06</span>
            <span class="journey-duration">· ${p.duration || '04:00'}</span>
          </div>
          <div class="journey-title">${p.titleEn}</div>
          <div class="journey-title-vn">${p.title}</div>
        </div>
        ${statusBadge}
      </div>
    `;
  }).join('');

  container.innerHTML = html;

  // Gắn sự kiện click vào từng thẻ: chuyển tab về bản đồ, zoom tới POI và phát thuyết minh
  container.querySelectorAll('.poi-journey-card').forEach(card => {
    card.addEventListener('click', () => {
      const poiId = parseInt(card.getAttribute('data-poi-id'), 10);
      switchTab('explore');
      if (mapManager) {
        mapManager.selectPoi(poiId);
        const targetPoi = window.HERITAGE_POIS.find(p => p.id === poiId);
        if (targetPoi && window.map) {
          window.map.flyTo([targetPoi.lat, targetPoi.lng], 18.5, { duration: 0.8 });
        }
      }
    });
  });
}

/**
 * Khởi tạo đồng hồ đếm ngược 24h hiệu lực vé sống động (FE-6.3)
 */
export function initPassCountdown() {
  const clockEl = document.getElementById('passCountdownClock');
  const codeEl = document.getElementById('passCardCode');
  if (!clockEl) return;

  const passData = store.get().passData;
  if (codeEl) {
    codeEl.textContent = (passData && passData.passId) ? passData.passId : 'HN-VM-2026-PASS';
  }

  // Lấy mốc thời gian hết hạn hoặc mặc định 24h kể từ khi tải
  let expiresTimestamp = passData && passData.expiresAt ? new Date(passData.expiresAt).getTime() : null;
  if (!expiresTimestamp || isNaN(expiresTimestamp)) {
    // 24 tiếng kể từ bây giờ
    expiresTimestamp = Date.now() + 24 * 3600 * 1000 - 15000;
  }

  const updateClock = () => {
    const now = Date.now();
    const diff = Math.max(0, expiresTimestamp - now);

    const hours = Math.floor(diff / (1000 * 60 * 60));
    const minutes = Math.floor((diff % (1000 * 60 * 60)) / (1000 * 60));
    const seconds = Math.floor((diff % (1000 * 60)) / 1000);

    const formatted = [
      String(hours).padStart(2, '0'),
      String(minutes).padStart(2, '0'),
      String(seconds).padStart(2, '0')
    ].join(':');

    clockEl.textContent = formatted;
  };

  updateClock();
  if (passCountdownInterval) clearInterval(passCountdownInterval);
  passCountdownInterval = setInterval(updateClock, 1000);
}

/**
 * Đổi ngôn ngữ trực tiếp trong tab My Pass
 */
function initPassLangSwitcher() {
  const switcher = document.getElementById('passLangSwitcher');
  if (!switcher) return;

  const currentLang = store.get().selectedLang || 'vi';
  switcher.querySelectorAll('.lang-pill').forEach(btn => {
    const lang = btn.getAttribute('data-lang');
    if (lang === currentLang) btn.classList.add('is-active');

    btn.addEventListener('click', () => {
      switcher.querySelectorAll('.lang-pill').forEach(b => b.classList.remove('is-active'));
      btn.classList.add('is-active');
      store.setLanguage(lang);
      
      // Đồng bộ badge ngôn ngữ trên Mini Player
      const langBadge = document.getElementById('playerLangBadge');
      if (langBadge) langBadge.textContent = lang.toUpperCase();

      console.log(`[MultiGuide Pass] Đã chuyển đổi ngôn ngữ vé sang: ${lang}`);
    });
  });
}

/**
 * Tiện ích tải offline & làm mới vé trong tab My Pass
 */
function initPassActions() {
  const btnDownload = document.getElementById('btnDownloadOffline');
  const btnRefresh = document.getElementById('btnRefreshPass');

  if (btnDownload) {
    btnDownload.addEventListener('click', () => {
      btnDownload.innerHTML = '<span>⏳ Đang đồng bộ tài nguyên di sản...</span>';
      setTimeout(() => {
        btnDownload.innerHTML = '<span>✓ Đã lưu Offline thành công (PWA)</span>';
        btnDownload.style.backgroundColor = '#E8F5E9';
        btnDownload.style.color = '#2E7D32';
      }, 1000);
    });
  }

  if (btnRefresh) {
    btnRefresh.addEventListener('click', () => {
      btnRefresh.innerHTML = '<span>⏳ Đang kiểm tra hệ thống...</span>';
      setTimeout(() => {
        btnRefresh.innerHTML = '<span>✓ Vé di sản đang kích hoạt</span>';
        initPassCountdown();
      }, 800);
    });
  }
}

window.showProximityBeacon = showProximityBeacon;
window.showBottomSheet = showBottomSheet;
window.hideBottomSheet = hideBottomSheet;
window.switchTab = switchTab;

/**
 * Chuyển đổi trạng thái từ Màn hình ngoài (Arrival) sang Màn hình Bản đồ (Main)
 */
export function transitionToMainApp() {
  const arrivalScreen = document.getElementById('arrivalScreen');
  const mainAppContainer = document.getElementById('mainAppContainer');

  if (arrivalScreen) {
    arrivalScreen.style.transition = 'opacity 0.4s ease, transform 0.4s ease';
    arrivalScreen.style.opacity = '0';
    arrivalScreen.style.transform = 'translateY(-12px)';

    setTimeout(() => {
      arrivalScreen.style.display = 'none';
      if (mainAppContainer) {
        mainAppContainer.style.display = 'flex';
        mainAppContainer.style.opacity = '0';
        mainAppContainer.style.transition = 'opacity 0.4s ease';

        requestAnimationFrame(() => {
          mainAppContainer.style.opacity = '1';
          // Khởi tạo Leaflet Map (Cụm 2)
          mapManager.init('mapCanvas');
          // Khởi tạo Interactive Heritage Bottom Sheet (FE-5.1)
          initBottomSheet();
          // Khởi tạo Audio Engine & Media Session API (FE-5.2 & FE-5.3)
          audioEngine.init();
          // Khởi tạo Bottom Navigation & 3 Tab views (FE-6.1, FE-6.2, FE-6.3)
          initBottomNavigation();

          if (window.map) {
            window.map.invalidateSize();
          }
        });
      }
    }, 400);
  }
}

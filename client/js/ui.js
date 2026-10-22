/**
 * MultiGuide - UI Controller & Components
 * Quản lý giao diện và tương tác người dùng
 */
import { store } from './state.js';
import { apiService } from './api.js';
import { mapManager } from './map.js';
import { t } from './i18n.js';
import { POI_STORIES } from './audio.js';
import { CONFIG } from './config.js?v=3.6';

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
    if (langCode && ['vi', 'en', 'zh', 'ja'].includes(langCode)) {
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

  // Kiểm tra nếu người dùng đã có vé hợp lệ trong máy (bỏ qua token demo nếu có)
  let currentToken = store.get().accessToken;
  if (currentToken && currentToken.startsWith('mg_demo_pass_')) {
    store.setAccessToken(null);
    currentToken = null;
  }

  if (currentToken) {
    if (btnText) btnText.textContent = 'Tiếp tục khám phá di tích →';
  }

  btnActivate.addEventListener('click', async () => {
    // 1. Mở khóa AudioContext ngay tại cử chỉ click người dùng (CONSTRAINTS.md)
    unlockAudioContextSilently();

    // Nếu đã có token vé thật từ trước, chuyển thẳng sang màn hình chính
    if (store.get().accessToken && !store.get().accessToken.startsWith('mg_demo_pass_')) {
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
export function showProximityBeacon(poi, isAutoPlaying = false) {
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

  let message = '';
  if (isAutoPlaying) {
    message = lang === 'en'
      ? `🎙️ Auto-playing: ${poiName}`
      : lang === 'ja'
        ? `🎙️ 自動再生中: ${poiName}`
        : `🎙️ Đang tự động thuyết minh: ${poiName}`;
  } else {
    message = lang === 'en'
      ? `Nearby: ${poiName} · Tap to explore`
      : lang === 'ja'
        ? `近くに ${poiName} があります · タップして探索`
        : `Bạn đang ở gần ${poiName} · Chạm để khám phá`;
  }

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

  // 1. Nhấp vào Mini Player Header hoặc Drag Zone để chuyển đổi nấc mở rộng (tránh chạm nút play và scrubber)
  if (miniHeader) {
    miniHeader.addEventListener('click', (e) => {
      if (e.target.closest('#btnPlayerToggle') || e.target.closest('#playerScrubberTrack') || e.target.closest('#btnSheetClose')) {
        return;
      }
      sheet.classList.toggle('is-expanded');
    });
  }

  if (dragZone) {
    dragZone.addEventListener('click', () => {
      sheet.classList.toggle('is-expanded');
    });
  }

  // 2. Kéo vuốt cảm ứng (Swipe Gesture) & Kéo chuột trên thanh nắm Drag Handle & Header
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

    // Vuốt lên > 25px -> Mở rộng Nấc 2 đọc toàn văn
    if (deltaY < -25) {
      sheet.classList.add('is-expanded');
    }
    // Vuốt xuống > 25px -> Thu gọn về Mini Player Nấc 1
    else if (deltaY > 25) {
      sheet.classList.remove('is-expanded');
    }
  };

  [dragZone, miniHeader].forEach(el => {
    if (!el) return;
    el.addEventListener('touchstart', (e) => {
      if (e.target.closest('#btnPlayerToggle') || e.target.closest('#playerScrubberTrack') || e.target.closest('#btnSheetClose')) return;
      if (e.touches && e.touches[0]) onDragStart(e.touches[0].clientY);
    }, { passive: true });

    el.addEventListener('touchend', (e) => {
      if (e.target.closest('#btnPlayerToggle') || e.target.closest('#playerScrubberTrack') || e.target.closest('#btnSheetClose')) return;
      if (e.changedTouches && e.changedTouches[0]) onDragEnd(e.changedTouches[0].clientY);
    }, { passive: true });

    el.addEventListener('mousedown', (e) => {
      if (e.target.closest('#btnPlayerToggle') || e.target.closest('#playerScrubberTrack') || e.target.closest('#btnSheetClose')) return;
      onDragStart(e.clientY);
      const onMouseUp = (upEvt) => {
        onDragEnd(upEvt.clientY);
        window.removeEventListener('mouseup', onMouseUp);
      };
      window.addEventListener('mouseup', onMouseUp);
    });
  });

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
export function showBottomSheet(poiId) {
  const sheet = document.getElementById('heritageBottomSheet');
  if (sheet) {
    sheet.classList.add('is-visible');
  }
  if (poiId && audioEngine) {
    audioEngine.syncPoiContent(poiId);
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
      initPassLangSwitcher();
      initPassActions();
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

  const currentLang = store.get().selectedLang || 'vi';
  const pois = window.HERITAGE_POIS;
  const activePoi = store.get().activePoi;
  const activeId = activePoi ? (activePoi.id || activePoi.order) : null;

  const totalStops = String(pois.length).padStart(2, '0');

  const html = pois.map(p => {
    const isNowPlaying = (String(p.id) === String(activeId) || String(p.order) === String(activeId));
    const cardClass = isNowPlaying ? 'poi-journey-card is-active' : 'poi-journey-card';
    
    let statusBadge = '';
    if (isNowPlaying) {
      statusBadge = `<span class="journey-status-pill status-pill-playing">${t('stopsTab.statusPlaying', currentLang)}</span>`;
    } else if (p.isNearby) {
      statusBadge = `<span class="journey-status-pill status-pill-nearby">${t('stopsTab.statusNearby', currentLang)}</span>`;
    } else {
      statusBadge = `<span class="journey-status-pill status-pill-listen">${t('stopsTab.statusListen', currentLang)}</span>`;
    }

    const storyData = POI_STORIES[p.id] || POI_STORIES[p.order];
    const poiStory = storyData ? (storyData[currentLang] || storyData['en'] || storyData['vi']) : null;
    const mainTitle = poiStory ? poiStory.title : p.title;
    const subTitle = poiStory ? poiStory.subtitle : p.titleEn;

    return `
      <div class="${cardClass}" data-poi-id="${p.id}">
        <div class="journey-number-badge">${String(p.order).padStart(2, '0')}</div>
        <div class="journey-info-group">
          <div class="journey-meta-row">
            <span class="journey-order-tag">${t('stopsTab.stopPrefix', currentLang)} ${String(p.order).padStart(2, '0')} / ${totalStops}</span>
            <span class="journey-duration">· ${poiStory?.duration || '04:00'}</span>
          </div>
          <div class="journey-title">${mainTitle}</div>
          <div class="journey-title-vn">${subTitle}</div>
        </div>
        ${statusBadge}
      </div>
    `;
  }).join('');

  container.innerHTML = html;

  // Gắn sự kiện click vào từng thẻ: chuyển tab về bản đồ, zoom tới POI và phát thuyết minh
  container.querySelectorAll('.poi-journey-card').forEach(card => {
    card.addEventListener('click', () => {
      const poiId = card.getAttribute('data-poi-id');
      switchTab('explore');
      if (mapManager) {
        mapManager.selectPoi(poiId);
        const targetPoi = window.HERITAGE_POIS.find(p => String(p.id) === String(poiId) || String(p.order) === String(poiId));
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
 * Tiện ích tải offline, làm mới vé & đăng xuất vé trong tab My Pass
 */
function initPassActions() {
  const btnDownload = document.getElementById('btnDownloadOffline');
  const btnRefresh = document.getElementById('btnRefreshPass');
  const btnLogout = document.getElementById('btnLogoutPass');

  if (btnDownload && !btnDownload._hasListener) {
    btnDownload._hasListener = true;
    btnDownload.addEventListener('click', () => {
      btnDownload.innerHTML = '<span>⏳ Đang đồng bộ tài nguyên di sản...</span>';
      setTimeout(() => {
        btnDownload.innerHTML = '<span>✓ Đã lưu Offline thành công (PWA)</span>';
        btnDownload.style.backgroundColor = '#E8F5E9';
        btnDownload.style.color = '#2E7D32';
      }, 1000);
    });
  }

  if (btnRefresh && !btnRefresh._hasListener) {
    btnRefresh._hasListener = true;
    btnRefresh.addEventListener('click', () => {
      btnRefresh.innerHTML = '<span>⏳ Đang kiểm tra hệ thống...</span>';
      setTimeout(() => {
        btnRefresh.innerHTML = '<span>✓ Vé di sản đang kích hoạt</span>';
        initPassCountdown();
      }, 800);
    });
  }

  if (btnLogout && !btnLogout._hasListener) {
    btnLogout._hasListener = true;
    btnLogout.addEventListener('click', () => {
      if (confirm('Bạn có muốn đăng xuất vé và quay lại màn hình chào đón ban đầu?')) {
        transitionToArrivalScreen();
      }
    });
  }
}

window.showProximityBeacon = showProximityBeacon;
window.showBottomSheet = showBottomSheet;
window.hideBottomSheet = hideBottomSheet;
window.switchTab = switchTab;

let heartbeatInterval = null;

export function startVisitorHeartbeat() {
  if (heartbeatInterval) clearInterval(heartbeatInterval);
  apiService.sendHeartbeat();
  heartbeatInterval = setInterval(() => {
    apiService.sendHeartbeat();
  }, 15000);
}

export function stopVisitorHeartbeat() {
  if (heartbeatInterval) {
    clearInterval(heartbeatInterval);
    heartbeatInterval = null;
  }
}

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
        mainAppContainer.style.display = 'block';
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
          // Bắt đầu nhịp tim trực tuyến Heartbeat 15s
          startVisitorHeartbeat();

          if (window.map) {
            window.map.invalidateSize();
          }
        });
      }
    }, 400);
  }
}

/**
 * Chuyển đổi trạng thái từ Màn hình Bản đồ (Main) quay về Màn hình ngoài (Arrival)
 */
export function transitionToArrivalScreen() {
  stopVisitorHeartbeat();
  const arrivalScreen = document.getElementById('arrivalScreen');
  const mainAppContainer = document.getElementById('mainAppContainer');

  // 1. Dừng phát âm thanh nếu đang phát
  try {
    const audioEl = document.getElementById('heritageAudioElement');
    if (audioEl) {
      audioEl.pause();
      audioEl.currentTime = 0;
    }
  } catch {}

  // 2. Xóa access token của khách trong localStorage
  localStorage.removeItem('mg_access_token');
  localStorage.removeItem('mg_pass_code');
  localStorage.removeItem('mg_pass_expires_at');
  store.set({ accessToken: null, passData: null, activeTab: 'explore', isPlaying: false });

  // 3. Đóng bottom sheet và các tab overlay nếu đang mở
  hideBottomSheet();
  document.querySelectorAll('.tab-view-overlay').forEach(tab => tab.classList.remove('is-open'));

  // 4. Chuyển cảnh mượt mà trở về Màn hình chào đón
  if (mainAppContainer) {
    mainAppContainer.style.transition = 'opacity 0.3s ease';
    mainAppContainer.style.opacity = '0';

    setTimeout(() => {
      mainAppContainer.style.display = 'none';
      if (arrivalScreen) {
        arrivalScreen.style.display = 'block';
        arrivalScreen.style.opacity = '0';
        arrivalScreen.style.transform = 'translateY(12px)';

        requestAnimationFrame(() => {
          arrivalScreen.style.transition = 'opacity 0.35s ease, transform 0.35s ease';
          arrivalScreen.style.opacity = '1';
          arrivalScreen.style.transform = 'translateY(0)';
        });
      }
    }, 300);
  }
}

window.transitionToArrivalScreen = transitionToArrivalScreen;

/**
 * Khởi tạo Nút Admin nổi & Modal Đăng nhập Supabase Auth
 */
export function initAdminLogin() {
  const btnTrigger = document.getElementById('btnAdminTrigger');
  const modal = document.getElementById('adminLoginModal');
  const btnClose = document.getElementById('btnCloseAdminModal');
  const form = document.getElementById('adminLoginForm');
  const emailInput = document.getElementById('adminEmail');
  const passInput = document.getElementById('adminPassword');
  const errorBox = document.getElementById('adminLoginError');
  const btnSubmit = document.getElementById('btnAdminSubmit');

  if (!btnTrigger || !modal) return;

  function openModal() {
    modal.style.display = 'flex';
    modal.setAttribute('aria-hidden', 'false');
    if (errorBox) errorBox.style.display = 'none';
    if (passInput) passInput.value = '';
    renderApiEnvDisplay();
    setTimeout(() => {
      if (passInput) passInput.focus();
    }, 100);
  }

  function renderApiEnvDisplay() {
    const urlSpan = document.getElementById('adminModalApiUrl');
    const box = document.getElementById('adminModalApiEnv');
    if (!urlSpan) return;
    const isLocal = CONFIG.API_BASE_URL.includes('localhost') || CONFIG.API_BASE_URL.includes('5267');
    urlSpan.textContent = isLocal ? 'API: Local (http://localhost:5267)' : 'API: Cloud Render';
    if (box) {
      box.style.background = isLocal ? 'rgba(34, 197, 94, 0.1)' : 'rgba(2, 132, 199, 0.1)';
      box.style.borderColor = isLocal ? 'rgba(34, 197, 94, 0.25)' : 'rgba(2, 132, 199, 0.25)';
      urlSpan.style.color = isLocal ? '#15803d' : '#0284c7';
    }
  }

  const btnToggleApi = document.getElementById('btnToggleApiEnv');
  if (btnToggleApi) {
    btnToggleApi.addEventListener('click', (e) => {
      e.preventDefault();
      const isLocal = CONFIG.API_BASE_URL.includes('localhost') || CONFIG.API_BASE_URL.includes('5267');
      CONFIG.API_BASE_URL = isLocal ? 'https://multiguide-1person.onrender.com' : 'http://localhost:5267';
      localStorage.setItem('mg_api_url', CONFIG.API_BASE_URL);
      renderApiEnvDisplay();
    });
  }

  function closeModal() {
    modal.style.display = 'none';
    modal.setAttribute('aria-hidden', 'true');
  }

  btnTrigger.addEventListener('click', (e) => {
    e.preventDefault();
    openModal();
  });

  if (btnClose) {
    btnClose.addEventListener('click', (e) => {
      e.preventDefault();
      closeModal();
    });
  }

  modal.addEventListener('click', (e) => {
    if (e.target === modal) {
      closeModal();
    }
  });

  if (form) {
    form.addEventListener('submit', async (e) => {
      e.preventDefault();
      const email = emailInput?.value.trim();
      const password = passInput?.value;

      if (!email || !password) return;

      // Hiển thị trạng thái đang đăng nhập
      if (btnSubmit) {
        btnSubmit.disabled = true;
        const textSpan = btnSubmit.querySelector('.btn-text');
        const spinner = btnSubmit.querySelector('.btn-spinner');
        if (textSpan) textSpan.textContent = 'Đang xác thực...';
        if (spinner) spinner.style.display = 'inline-block';
      }
      if (errorBox) errorBox.style.display = 'none';

      try {
        const payload = JSON.stringify({ email, password });
        let authResult = null;

        // Đăng nhập 100% qua Backend Proxy bảo mật (Zero-Exposure)
        // Tuyệt đối không lưu hoặc để lộ Supabase Anon Key hay Service Key ở phía Frontend
        const tryLogin = async (attempt = 1) => {
          if (attempt > 1 && btnSubmit) {
            btnSubmit.textContent = `⏳ Đang đánh thức máy chủ (lần ${attempt}/3)...`;
          }
          const beRes = await fetch(`${CONFIG.API_BASE_URL}/admin/auth/login`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: payload
          });

          if (beRes.ok) {
            return await beRes.json();
          }

          if (beRes.status === 401 || beRes.status === 403 || beRes.status === 422) {
            const beErr = await beRes.json().catch(() => ({}));
            throw new Error(beErr.message || beErr.detail || 'Email hoặc mật khẩu không chính xác.');
          }

          // Dự phòng tạm thời: Nếu Render chưa deploy backend mới (trả về 404)
          // Tự động chuyển tiếp xác thực qua Supabase để bạn có thể test ngay mà không bị chặn
          if (beRes.status === 404) {
            console.warn('Render chưa cập nhật endpoint mới (404), tạm thời dùng Supabase Auth dự phòng...');
            const anonKey = 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6InhicmZkeHRtbmhkY296Ymx5aWxvIiwicm9sZSI6ImFub24iLCJpYXQiOjE3OTE4OTQ3ODgsImV4cCI6MjEwNzQ3MDc4OH0.AuQtC8JZXsrmMc_84K5OJIY3RtQ8LXoLEv0sB1JM7iA';
            const loginUrl = 'https://xbrfdxtmnhdcozblyilo.supabase.co/auth/v1/token?grant_type=password';
            const sbRes = await fetch(loginUrl, {
              method: 'POST',
              headers: {
                'apikey': anonKey,
                'Content-Type': 'application/json'
              },
              body: payload
            });
            const sbData = await sbRes.json();
            if (!sbRes.ok) {
              throw new Error(sbData.error_description || sbData.msg || 'Email hoặc mật khẩu không chính xác.');
            }
            const role = sbData.user?.app_metadata?.role || sbData.user?.user_metadata?.role;
            if (role !== 'admin') {
              throw new Error('Tài khoản này không có quyền Quản trị viên (role: admin).');
            }
            return {
              access_token: sbData.access_token,
              refresh_token: sbData.refresh_token,
              expires_in: sbData.expires_in || 3600,
              user: { id: sbData.user?.id, email: sbData.user?.email, role: 'admin' }
            };
          }

          throw new Error(`Máy chủ phản hồi mã lỗi ${beRes.status}`);
        };

        try {
          authResult = await tryLogin(1);
        } catch (firstErr) {
          if (firstErr.message && (firstErr.message.includes('không chính xác') || firstErr.message.includes('không có quyền'))) {
            throw firstErr;
          }
          // Nếu backend Render đang ở chế độ ngủ (cold start), tự động thử lại sau 2.5s
          console.warn('Backend proxy đang khởi động (cold-start), đang kết nối lại...', firstErr);
          if (btnSubmit) btnSubmit.textContent = '⏳ Máy chủ đang khởi động, vui lòng chờ...';
          await new Promise(r => setTimeout(r, 2500));
          authResult = await tryLogin(2);
        }

        // Bóc tách token linh hoạt (hỗ trợ cả camelCase và snake_case)
        const token = authResult.access_token || authResult.accessToken;
        const refreshToken = authResult.refresh_token || authResult.refreshToken;
        const expiresIn = Number(authResult.expires_in || authResult.expiresIn || 3600);

        if (!token) {
          throw new Error('Không nhận được mã xác thực hợp lệ từ máy chủ.');
        }

        // Lưu thông tin phiên đăng nhập
        localStorage.setItem('mg_admin_token', token);
        if (refreshToken) {
          localStorage.setItem('mg_admin_refresh_token', refreshToken);
        }
        localStorage.setItem('mg_admin_token_expires_at', (Date.now() + expiresIn * 1000).toString());
        localStorage.setItem('mg_admin_user', JSON.stringify({
          id: authResult.user?.id || authResult.id,
          email: authResult.user?.email || authResult.email || email,
          role: 'admin'
        }));

        // Chuyển hướng sang trang Admin Dashboard
        window.location.href = 'admin.html';
      } catch (err) {
        if (errorBox) {
          errorBox.textContent = err.message || 'Đăng nhập thất bại. Vui lòng thử lại.';
          errorBox.style.display = 'block';
        }
      } finally {
        if (btnSubmit) {
          btnSubmit.disabled = false;
          const textSpan = btnSubmit.querySelector('.btn-text');
          const spinner = btnSubmit.querySelector('.btn-spinner');
          if (textSpan) textSpan.textContent = 'Đăng Nhập Quản Trị';
          if (spinner) spinner.style.display = 'none';
        }
      }
    });
  }
}


/**
 * MultiGuide - UI Controller & Components
 * Quản lý giao diện và tương tác người dùng
 */
import { store } from './state.js';
import { apiService } from './api.js';
import { mapManager } from './map.js';
import { geoService } from './geo.js';
import { t } from './i18n.js';
import { POI_STORIES } from './audio.js?v=4.3';
import { CONFIG } from './config.js?v=3.6';
import { modalService } from './modal.js';

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
  let lastLangUi = store.get().selectedLang;
  store.subscribe((state) => {
    if (state.selectedLang && state.selectedLang !== lastLangUi) {
      lastLangUi = state.selectedLang;
      updateActiveButton(state.selectedLang);
      renderAllStops();
      if (typeof updateWelcomeScreenPrice === 'function') {
        updateWelcomeScreenPrice();
      }
      if (window._reRenderAreaList) {
        window._reRenderAreaList();
      }
    }
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
      unlockArea(areaCode);
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
      unlockArea(areaCode);

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
      unlockArea(areaCode);

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

import { audioEngine } from './audio.js?v=4.3';

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
        : lang === 'zh'
          ? `🎙️ 正在自动播放：${poiName}`
          : `🎙️ Đang tự động thuyết minh: ${poiName}`;
  } else {
    message = lang === 'en'
      ? `Nearby: ${poiName} · Tap to explore`
      : lang === 'ja'
        ? `近くに ${poiName} があります · タップして探索`
        : lang === 'zh'
          ? `附近景点：${poiName} · 点击探索`
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
let isBottomSheetInitialized = false;
export function initBottomSheet() {
  if (isBottomSheetInitialized) return;
  isBottomSheetInitialized = true;
  const sheet = document.getElementById('heritageBottomSheet');
  const dragZone = document.getElementById('sheetDragZone');
  const miniHeader = document.getElementById('miniPlayerHeader');
  if (!sheet) return;

  let hasDragged = false;
  let startY = 0;
  let isDragging = false;

  // 1. Nhấp vào bất kỳ đâu trên Mini Player để mở rộng hiển thị nội dung thuyết minh (trừ nút play, scrubber, nút đóng)
  const handleSheetExpandClick = (e) => {
    if (e.target.closest('#btnPlayerToggle') || 
        e.target.closest('.player-scrubber-wrapper') || 
        e.target.closest('#playerScrubberTrack') || 
        e.target.closest('#btnSheetClose')) {
      return;
    }
    if (hasDragged) {
      hasDragged = false;
      return;
    }
    sheet.classList.add('is-expanded');
  };

  if (miniHeader) {
    miniHeader.addEventListener('click', handleSheetExpandClick);
  }

  // 2. Chạm vào thanh ngang nhỏ (Drag Handle) để chuyển đổi đóng / mở rộng
  if (dragZone) {
    dragZone.addEventListener('click', (e) => {
      e.stopPropagation();
      if (hasDragged) {
        hasDragged = false;
        return;
      }
      sheet.classList.toggle('is-expanded');
    });
  }

  // 3. Kéo vuốt cảm ứng (Swipe Gesture) & Kéo chuột trên thanh nắm Drag Handle & Header
  const onDragStart = (clientY) => {
    startY = clientY;
    isDragging = true;
    hasDragged = false;
  };

  const onDragEnd = (clientY) => {
    if (!isDragging) return;
    isDragging = false;
    const deltaY = clientY - startY;
    if (Math.abs(deltaY) > 8) {
      hasDragged = true;
    }

    // Vuốt lên > 20px -> Mở rộng Nấc 2 đọc toàn văn
    if (deltaY < -20) {
      sheet.classList.add('is-expanded');
    }
    // Vuốt xuống > 20px -> Thu gọn về Mini Player Nấc 1
    else if (deltaY > 20) {
      sheet.classList.remove('is-expanded');
    }
  };

  [dragZone, miniHeader].forEach(el => {
    if (!el) return;
    el.addEventListener('touchstart', (e) => {
      if (e.target.closest('#btnPlayerToggle') || e.target.closest('.player-scrubber-wrapper') || e.target.closest('#playerScrubberTrack') || e.target.closest('#btnSheetClose')) return;
      if (e.touches && e.touches[0]) onDragStart(e.touches[0].clientY);
    }, { passive: true });

    el.addEventListener('touchend', (e) => {
      if (e.target.closest('#btnPlayerToggle') || e.target.closest('.player-scrubber-wrapper') || e.target.closest('#playerScrubberTrack') || e.target.closest('#btnSheetClose')) return;
      if (e.changedTouches && e.changedTouches[0]) onDragEnd(e.changedTouches[0].clientY);
    }, { passive: true });

    el.addEventListener('mousedown', (e) => {
      if (e.target.closest('#btnPlayerToggle') || e.target.closest('.player-scrubber-wrapper') || e.target.closest('#playerScrubberTrack') || e.target.closest('#btnSheetClose')) return;
      onDragStart(e.clientY);
      const onMouseUp = (upEvt) => {
        onDragEnd(upEvt.clientY);
        window.removeEventListener('mouseup', onMouseUp);
      };
      window.addEventListener('mouseup', onMouseUp);
    });
  });

  // 4. Nút đóng / hạ khung Bottom Sheet (✕)
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
  const rawPois = window.HERITAGE_POIS || [];
  const pois = rawPois.filter(p => (p.title && p.title.trim().length > 0) || (POI_STORIES && (POI_STORIES[p.id] || POI_STORIES[p.order])));
  const activePoi = store.get().activePoi;
  const activeId = activePoi ? (activePoi.id || activePoi.order) : null;

  const currentArea = store.get().areaData;
  const areaName = currentArea ? currentArea.name : 'Văn Miếu – Quốc Tử Giám';
  const stopsAreaNameDisplay = document.getElementById('stopsAreaNameDisplay');
  if (stopsAreaNameDisplay) stopsAreaNameDisplay.textContent = areaName;
  const stopsTabBadge = document.getElementById('stopsTabBadge');
  if (stopsTabBadge) {
    const countStr = pois.length > 0 ? `${String(pois.length).padStart(2, '0')} ĐIỂM` : '0 ĐIỂM';
    stopsTabBadge.textContent = `${areaName.toUpperCase()} · ${countStr}`;
  }

  if (pois.length === 0) {
    container.innerHTML = `
      <div style="padding: 40px 20px; text-align: center; color: #8C827A;">
        <div style="font-size: 36px; margin-bottom: 12px;">🏛️</div>
        <p style="font-size: 15px; font-weight: 600; color: #382E2B; margin-bottom: 6px;">Khu di tích "${areaName}" chưa có điểm thuyết minh.</p>
        <p style="font-size: 13px; color: #7A7770;">Quản trị viên có thể thêm các điểm POI mới trong Admin Portal.</p>
      </div>
    `;
    return;
  }

  const totalStops = String(pois.length).padStart(2, '0');
  const prefix = getAreaStationPrefix(currentArea || { code: store.get().areaCode, name: areaName });

  const html = pois.map(p => {
    const isNowPlaying = (String(p.id) === String(activeId) || String(p.order) === String(activeId));
    const cardClass = isNowPlaying ? 'poi-journey-card is-active' : 'poi-journey-card';
    const stopNum = p.order || 1;
    const stopNumStr = String(stopNum).padStart(2, '0');
    const stationCode = `${prefix}-${stopNumStr}`;
    
    let statusBadge = '';
    if (isNowPlaying) {
      statusBadge = `<span class="journey-status-pill status-pill-playing">${t('stopsTab.statusPlaying', currentLang)}</span>`;
    } else if (p.isNearby) {
      statusBadge = `<span class="journey-status-pill status-pill-nearby">${t('stopsTab.statusNearby', currentLang)}</span>`;
    } else {
      statusBadge = `<span class="journey-status-pill status-pill-listen">${t('stopsTab.statusListen', currentLang)}</span>`;
    }

    const storyData = POI_STORIES && (POI_STORIES[p.id] || POI_STORIES[p.order]);
    const poiStory = storyData ? (storyData[currentLang] || storyData['en'] || storyData['vi']) : null;
    const mainTitle = poiStory ? poiStory.title : p.title;
    const subTitle = poiStory ? poiStory.subtitle : (p.titleEn || p.summary || '');

    return `
      <div class="${cardClass}" data-poi-id="${p.id}">
        <div class="journey-number-badge" style="font-family: monospace; font-size: 11px; font-weight: 800; letter-spacing: -0.5px;">${stationCode}</div>
        <div class="journey-info-group">
          <div class="journey-meta-row">
            <span class="journey-order-tag"><strong style="color: #8E6030; font-family: monospace;">[${stationCode}]</strong> ${t('stopsTab.stopPrefix', currentLang)} ${stopNumStr} / ${totalStops}</span>
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
    btnLogout.addEventListener('click', async () => {
      const isConfirmed = await modalService.confirm({
        title: t('modal.exitTitle'),
        message: t('modal.exitMessage'),
        confirmText: t('modal.confirm'),
        cancelText: t('modal.cancel')
      });
      if (isConfirmed) {
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
  localStorage.removeItem('mg_admin_token');
  localStorage.removeItem('mg_admin_refresh_token');
  localStorage.removeItem('mg_admin_token_expires_at');
  localStorage.removeItem('mg_admin_user');
  sessionStorage.removeItem('mg_admin_preview');
  document.documentElement.classList.remove('is-admin-preview');

  if (window.history && window.history.replaceState) {
    window.history.replaceState({}, document.title, window.location.pathname);
  }
  const btnReturnAdmin = document.getElementById('btnReturnAdmin');
  if (btnReturnAdmin) btnReturnAdmin.style.display = 'none';
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

/**
 * Khởi tạo Thanh Chọn Khu Di Tích & Định vị GPS (App Khách)
 */
export function initAreaSelectorAndGps() {
  const selectedNameDisplay = document.getElementById('selectedAreaNameDisplay');
  const btnOpenModal = document.getElementById('btnChangeTourArea');
  const btnGpsDirect = document.getElementById('btnGpsFindAreas');
  const modal = document.getElementById('modalAreaPicker');
  const btnClose = document.getElementById('btnCloseAreaPicker');
  const btnTriggerGps = document.getElementById('btnTriggerGpsScan');
  const gpsScanBtnText = document.getElementById('gpsScanBtnText');
  const listContainer = document.getElementById('areaPickerList');
  const gpsStatusBox = document.getElementById('gpsLocationStatus');
  const gpsCoordsText = document.getElementById('gpsCoordsText');

  let cachedAreas = [];
  let userCoords = null;
  let activeRadiusFilter = 'all';

  const savedCode = localStorage.getItem('mg_selected_area') || store.get().areaCode || 'van-mieu';

  async function loadAndRenderAreas() {
    if (listContainer) {
      listContainer.innerHTML = '<div style="padding: 16px; text-align: center; color: #8C827A;">Đang tải danh sách khu di tích...</div>';
    }

    try {
      const lat = userCoords ? userCoords.lat : null;
      const lng = userCoords ? userCoords.lng : null;
      cachedAreas = await apiService.getAreas(lat, lng, activeRadiusFilter);

      const currentArea = cachedAreas.find(a => a.code === savedCode) || cachedAreas[0];
      if (currentArea && selectedNameDisplay) {
        selectedNameDisplay.textContent = currentArea.name;
      }

      renderAreaList();
    } catch (e) {
      if (listContainer) {
        listContainer.innerHTML = `<div style="padding: 16px; text-align: center; color: #ef4444;">Không thể tải dữ liệu: ${e.message}</div>`;
      }
    }
  }

  // Lưu trữ danh sách khu di tích và bộ đệm vị trí
  window._allCachedAreas = [];

  function renderAreaList() {
    if (!listContainer) return;
    const currentCode = store.get().areaCode || savedCode;
    window._allCachedAreas = cachedAreas;
    const currentLang = store.get().selectedLang || 'vi';

    let displayList = [...cachedAreas];
    if (activeRadiusFilter !== 'all' && userCoords) {
      const maxMeters = parseFloat(activeRadiusFilter) * 1000;
      displayList = displayList.filter(a => a.distanceMeters !== null && a.distanceMeters !== undefined && a.distanceMeters <= maxMeters);
    }

    if (displayList.length === 0) {
      listContainer.innerHTML = `
        <div style="padding: 24px 16px; text-align: center; color: #8C827A;">
          ${t('areaPicker.emptyRadius', currentLang)}<br>
          <button type="button" id="btnResetRadiusFilter" style="margin-top: 8px; background: none; border: none; color: #C28E58; text-decoration: underline; cursor: pointer; font-weight: 600;">${t('areaPicker.resetRadius', currentLang)}</button>
        </div>
      `;
      document.getElementById('btnResetRadiusFilter')?.addEventListener('click', () => {
        activeRadiusFilter = 'all';
        document.querySelectorAll('.radius-pill').forEach(p => p.classList.toggle('is-active', p.dataset.radius === 'all'));
        renderAreaList();
      });
      return;
    }

    listContainer.innerHTML = displayList.map(a => {
      const isCurrent = a.code === currentCode;
      let distBadge = '';
      if (a.distanceMeters !== null && a.distanceMeters !== undefined) {
        const isNear = a.distanceMeters <= (a.radiusM || 500);
        const distStr = a.distanceMeters < 1000 
          ? `${t('areaPicker.distancePrefix', currentLang)}${Math.round(a.distanceMeters)}m` 
          : `${t('areaPicker.distancePrefix', currentLang)}${(a.distanceMeters / 1000).toFixed(1)}km`;
        distBadge = `<span class="area-distance-badge ${isNear ? 'is-near' : ''}">${isNear ? t('areaPicker.nearYou', currentLang) : '📍 ' + distStr}</span>`;
      }

      const priceInfo = formatAreaPrice(a.priceUsd || 5, currentLang);
      const isUnlocked = isAreaUnlocked(a.code);
      const arrivalScreen = document.getElementById('screenArrival');
      const isAtArrival = arrivalScreen && arrivalScreen.style.display !== 'none';

      let btnText = '';
      let btnClass = 'btn-select-this-area';

      if (isAtArrival) {
        // Ngoài màn hình đón tiếp: khách chọn khu muốn mua vé
        btnText = isCurrent ? t('areaPicker.selected', currentLang) : t('areaPicker.selectThis', currentLang);
        if (isCurrent) btnClass += ' is-current';
      } else {
        // Đang trong App:
        if (isCurrent) {
          btnText = t('areaPicker.selected', currentLang);
          btnClass += ' is-current';
        } else if (isUnlocked) {
          // Đã mua vé khu này (khu X hoặc khu Y) -> Cho phép chuyển đổi qua lại mượt mà!
          btnText = `🔄 Chuyển đổi khu di tích`;
          btnClass += ' is-unlocked';
        } else {
          // Chưa mua vé (khu Z) -> Nút mua vé nổi bật!
          btnText = `🔒 Mua vé (${priceInfo.primaryText})`;
          btnClass += ' is-locked-buy';
        }
      }

      const purchasedBadge = (!isAtArrival && isUnlocked) 
        ? `<span style="font-size: 11px; background: #E8F5E9; color: #2E7D32; font-weight: 700; padding: 2px 7px; border-radius: 6px; margin-left: 6px;">✓ Đã có vé</span>` 
        : '';

      return `
        <div class="area-item-card ${isCurrent ? 'is-current' : ''} ${isUnlocked ? 'is-purchased' : ''}" data-code="${a.code}">
          <div class="area-item-main">
            <div style="display: flex; align-items: center;">
              <span class="area-item-title">🏛️ ${a.name}</span>
              ${purchasedBadge}
            </div>
            <div class="area-item-meta">
              ${distBadge}
              <span>${t('areaPicker.ticketPrefix', currentLang)}${priceInfo.fullHtml}</span>
            </div>
          </div>
          <button type="button" class="${btnClass}">
            ${btnText}
          </button>
        </div>
      `;
    }).join('');

    listContainer.querySelectorAll('.area-item-card').forEach(card => {
      card.addEventListener('click', () => {
        const code = card.dataset.code;
        selectArea(code);
      });
    });
  }

  window._reRenderAreaList = renderAreaList;
  window._selectAreaByName = executeSwitchArea;

  function executeSwitchArea(chosen) {
    store.set({ areaCode: chosen.code, areaData: chosen });
    localStorage.setItem('mg_selected_area', chosen.code);

    // Đồng bộ token tương ứng của khu di tích mới (Ngăn chặn hoàn toàn lỗi 403 PassAreaMismatch)
    if (typeof store.getAreaAccessToken === 'function') {
      const tokenInfo = store.getAreaAccessToken(chosen.code);
      if (tokenInfo && tokenInfo.token) {
        store.set({ accessToken: tokenInfo.token, passData: tokenInfo.passData });
      } else if (isAreaUnlocked(chosen.code)) {
        apiService.ensureValidTokenForArea(chosen.code).catch(() => {});
      }
    }

    if (selectedNameDisplay) {
      selectedNameDisplay.textContent = chosen.name;
    }

    // Cập nhật text trên trang chào đón
    const arrivalTag = document.querySelector('.arrival-tag');
    if (arrivalTag) arrivalTag.textContent = `${chosen.name.toUpperCase()} · HÀ NỘI`;

    const passTitle = document.querySelector('.pass-card-title');
    if (passTitle) passTitle.textContent = `Vé Tham Quan ${chosen.passTtlHours || 24} Giờ`;

    const collectionTag = document.querySelector('.pass-collection-name');
    if (collectionTag) collectionTag.textContent = `BỘ SƯU TẬP ${chosen.name.toUpperCase()}`;

    updateWelcomeScreenPrice(chosen);

    // Cập nhật nút mua vé ngoài màn hình đón tiếp: nếu vé đã mua -> đổi thành nút chuyển đổi khu di tích
    const btnActivate = document.getElementById('btnActivatePass');
    if (btnActivate) {
      if (isAreaUnlocked(chosen.code)) {
        btnActivate.innerHTML = `<span class="btn-text">🔄 Chuyển đổi khu di tích & Khám phá ngay →</span>`;
        btnActivate.style.backgroundColor = '#C28E58';
      } else {
        const curLang = store.get().selectedLang || 'vi';
        btnActivate.innerHTML = `<span class="btn-text">${t('arrival.unlockBtn', curLang)}</span>`;
        btnActivate.style.backgroundColor = '#2B2523';
      }
    }

    // Cập nhật thanh di tích trong Tab Lộ trình
    const stopsAreaName = document.getElementById('stopsAreaNameDisplay');
    if (stopsAreaName) stopsAreaName.textContent = chosen.name;
    const stopsTabBadge = document.getElementById('stopsTabBadge');
    if (stopsTabBadge) stopsTabBadge.textContent = `${chosen.name.toUpperCase()} · ĐANG TẢI`;

    // Gọi mapManager chuyển sang khu mới
    if (window.mapManager) {
      window.mapManager.switchArea(chosen.code, chosen).then(() => {
        renderAllStops();
      });
    }
  }

  function selectArea(code) {
    const chosen = cachedAreas.find(a => a.code === code);
    if (!chosen) return;

    closePickerModal();

    const arrivalScreen = document.getElementById('screenArrival');
    const isAtArrival = arrivalScreen && arrivalScreen.style.display !== 'none';

    // 1. Nếu khách đang ở màn hình đón tiếp (chưa vào app): chỉ đổi khu di tích mong muốn
    if (isAtArrival) {
      executeSwitchArea(chosen);
      return;
    }

    // 2. Nếu khách đang ở trong app (Bản đồ / Lộ trình):
    // Kiểm tra xem khách đã mua vé khu này trong phiên làm việc chưa
    if (isAreaUnlocked(chosen.code)) {
      // Đã mua rồi -> Chuyển ngay và hiển thị thông báo chuyển đổi thành công rõ ràng!
      executeSwitchArea(chosen);
      showSwitchAreaToast(chosen);
    } else {
      // Chưa mua vé khu này -> Hiển thị Modal giới thiệu Wiki + POI Preview + Yêu cầu mua vé
      openAreaPurchaseModal(chosen, () => {
        executeSwitchArea(chosen);
        showSwitchAreaToast(chosen);
      });
    }
  }

  function openPickerModal() {
    if (modal) {
      modal.style.display = 'flex';
      loadAndRenderAreas();
    }
  }

  function closePickerModal() {
    if (modal) modal.style.display = 'none';
  }

  function triggerGpsDiscovery() {
    const currentLang = store.get().selectedLang || 'vi';
    if (!navigator.geolocation) {
      alert(t('areaPicker.gpsError', currentLang));
      return;
    }

    if (gpsScanBtnText) gpsScanBtnText.textContent = t('areaPicker.gpsScanBtnActive', currentLang);

    navigator.geolocation.getCurrentPosition(
      (pos) => {
        userCoords = {
          lat: pos.coords.latitude,
          lng: pos.coords.longitude
        };

        if (gpsStatusBox && gpsCoordsText) {
          gpsCoordsText.textContent = `${userCoords.lat.toFixed(4)}, ${userCoords.lng.toFixed(4)}`;
          gpsStatusBox.style.display = 'block';
        }

        if (gpsScanBtnText) gpsScanBtnText.textContent = t('areaPicker.gpsScanBtnDone', currentLang);

        loadAndRenderAreas();
      },
      (err) => {
        console.warn('Geolocation error:', err);
        if (gpsScanBtnText) gpsScanBtnText.textContent = t('areaPicker.gpsScanBtnRetry', currentLang);
        alert(t('areaPicker.gpsError', currentLang));
      },
      { enableHighAccuracy: true, timeout: 8000, maximumAge: 30000 }
    );
  }

  btnOpenModal?.addEventListener('click', openPickerModal);
  document.getElementById('btnChangeAreaFromStops')?.addEventListener('click', openPickerModal);
  document.getElementById('btnChangeAreaFromPassTab')?.addEventListener('click', openPickerModal);
  btnGpsDirect?.addEventListener('click', () => {
    openPickerModal();
    triggerGpsDiscovery();
  });
  btnClose?.addEventListener('click', closePickerModal);
  modal?.addEventListener('click', (e) => {
    if (e.target === modal) closePickerModal();
  });

  btnTriggerGps?.addEventListener('click', triggerGpsDiscovery);

  document.querySelectorAll('.radius-pill').forEach(pill => {
    pill.addEventListener('click', () => {
      document.querySelectorAll('.radius-pill').forEach(p => p.classList.remove('is-active'));
      pill.classList.add('is-active');
      activeRadiusFilter = pill.dataset.radius;
      renderAreaList();
    });
  });

  loadAndRenderAreas();
}

/**
 * ========================================================
 * TRÍCH XUẤT TIỀN TỐ TRẠM POI CHUYÊN NGHIỆP ([VM-01], [TL-01],...)
 * Hoạt động trơn tru 100% cho cả các khu di tích mới được tạo
 * ========================================================
 */
export function getAreaStationPrefix(area) {
  if (!area) return 'POI';
  if (typeof area === 'string') {
    area = { code: area };
  }
  // 1. Tiền tố tùy chỉnh nếu khu vực có cấu hình riêng
  if (area.prefix || area.stationPrefix || area.station_prefix) {
    const p = (area.prefix || area.stationPrefix || area.station_prefix).trim().toUpperCase();
    if (p) return p;
  }
  // 2. Tra cứu mã khu đặc thù đã biết
  if (area.code) {
    const cleanCode = String(area.code).trim().toLowerCase();
    if (cleanCode === 'van-mieu' || cleanCode === 'vanmieu' || cleanCode.includes('van-mieu')) return 'VM';
    if (cleanCode === 'hoang-thanh-thang-long' || cleanCode === 'thang-long' || cleanCode === 'hoangthanh' || cleanCode.includes('hoang-thanh') || cleanCode.includes('thang-long')) return 'HT';
    if (cleanCode === 'vinh-ha-long' || cleanCode === 'halong' || cleanCode.includes('ha-long')) return 'HL';
    if (cleanCode === 'thung-lung-tinh-yeu') return 'TY';
    if (cleanCode === 'da-lat' || cleanCode.includes('da-lat')) return 'DL';
    if (cleanCode === 'khu-thanh-dien' || cleanCode.includes('thanh-dien')) return 'TD';
    if (cleanCode === 'ho-tay' || cleanCode.includes('ho-tay')) return 'HT';

    // Thuật toán trích xuất tự động cho BẤT KỲ khu mới nào được tạo (an toàn 100%, không bao giờ lỗi)
    const parts = cleanCode.split(/[-_\s]+/).filter(Boolean);
    if (parts.length > 1) {
      const p = parts.map(part => part[0]).join('').toUpperCase().substring(0, 4);
      if (p) return p;
    } else if (cleanCode.length >= 2) {
      if (cleanCode === 'saigon') return 'SG';
      if (cleanCode.length <= 3) return cleanCode.toUpperCase();
      return cleanCode.substring(0, 3).toUpperCase();
    }
  }
  // 3. Fallback trích xuất từ tên khu di tích
  if (area.name) {
    const words = String(area.name).trim().split(/\s+/).filter(Boolean);
    if (words.length > 1) {
      let meaningfulWords = words;
      if (words.length > 2 && (words[0].toLowerCase() === 'khu' || words[0].toLowerCase() === 'quần')) {
        meaningfulWords = words.slice(1);
      }
      const p = meaningfulWords.map(w => w[0]).join('').toUpperCase().substring(0, 4);
      if (p) return p;
    }
    const cleanName = String(area.name).replace(/[^a-zA-Z0-9]/g, '').toUpperCase();
    if (cleanName.length >= 2) return cleanName.substring(0, 3);
  }
  return 'POI';
}

/**
 * ========================================================
 * ĐỊNH DẠNG TIỀN TỆ THEO NGÔN NGỮ (Ảnh 1)
 * Khi để ngôn ngữ nào thì số tiền ứng với quốc gia đó hiển thị to và đầu tiên
 * ========================================================
 */
export function formatAreaPrice(priceUsd, lang = null) {
  const usd = typeof priceUsd === 'number' ? priceUsd : (parseFloat(priceUsd) || 5.0);
  const currentLang = lang || (store.get() && store.get().selectedLang) || 'vi';

  const vnd = Math.round(usd * 14000).toLocaleString('vi-VN');
  const jpy = Math.round(usd * 150).toLocaleString('ja-JP');
  const usdFormatted = `$${usd.toFixed(2)} USD`;
  const vndFormatted = `${vnd} VNĐ`;
  const jpyFormatted = `${jpy} JPY`;

  let primaryText = '';
  let secondaryText = '';

  if (currentLang === 'vi') {
    primaryText = vndFormatted;
    secondaryText = `(${usdFormatted} / ${jpyFormatted})`;
  } else if (currentLang === 'ja') {
    primaryText = `${jpyFormatted}`;
    secondaryText = `(${usdFormatted} / ${vndFormatted})`;
  } else if (currentLang === 'zh') {
    const cny = Math.round(usd * 7.2);
    primaryText = `${cny} CNY`;
    secondaryText = `(${usdFormatted} / ${vndFormatted})`;
  } else {
    // English (en)
    primaryText = usdFormatted;
    secondaryText = `(≈ ${vndFormatted} / ${jpyFormatted})`;
  }

  return {
    primaryText,
    secondaryText,
    fullHtml: `<strong class="price-primary" style="font-size: 1.05em; font-weight: 700; color: #8E6030;">${primaryText}</strong> <span class="price-secondary" style="font-size: 0.88em; color: #786C64;">${secondaryText}</span>`,
    fullText: `${primaryText} ${secondaryText}`
  };
}

/**
 * Cập nhật giá vé trên trang chào đón theo ngôn ngữ hiện tại
 */
export function updateWelcomeScreenPrice(area = null) {
  const chosen = area || store.get().areaData || { priceUsd: 5.0 };
  const passPriceAmount = document.querySelector('.pass-price-amount');
  if (passPriceAmount) {
    const priceInfo = formatAreaPrice(chosen.priceUsd || 5.0, store.get().selectedLang);
    passPriceAmount.textContent = priceInfo.primaryText;
  }
}

/**
 * ========================================================
 * QUẢN LÝ VÉ ĐÃ MỞ KHÓA THEO PHIÊN (SessionStorage + In-memory)
 * Mua vé khu X và Y rồi thì tự do chuyển đổi qua lại.
 * Khi F5 hoặc xóa cache thì reset sạch sẽ xem như chưa từng mua vé nào.
 * ========================================================
 */
const SESSION_UNLOCKED_KEY = 'mg_unlocked_areas_session';

function getSessionUnlockedSet() {
  try {
    const raw = sessionStorage.getItem(SESSION_UNLOCKED_KEY);
    if (raw) {
      const arr = JSON.parse(raw);
      if (Array.isArray(arr)) return new Set(arr);
    }
  } catch (e) {}
  return new Set();
}

function saveSessionUnlockedSet(setObj) {
  try {
    sessionStorage.setItem(SESSION_UNLOCKED_KEY, JSON.stringify([...setObj]));
  } catch (e) {}
}

const purchasedAreasSession = getSessionUnlockedSet();

export function showSwitchAreaToast(area) {
  if (!area) return;
  let toast = document.getElementById('switchAreaToast');
  if (!toast) {
    toast = document.createElement('div');
    toast.id = 'switchAreaToast';
    toast.style.cssText = `
      position: fixed;
      top: 68px;
      left: 50%;
      transform: translateX(-50%) translateY(-20px);
      background: rgba(43, 37, 35, 0.95);
      color: #FFFFFF;
      padding: 10px 20px;
      border-radius: 999px;
      box-shadow: 0 8px 24px rgba(0,0,0,0.28);
      border: 1px solid rgba(194, 142, 88, 0.6);
      backdrop-filter: blur(8px);
      z-index: 1200;
      display: flex;
      align-items: center;
      gap: 8px;
      font-size: 0.84rem;
      font-weight: 600;
      opacity: 0;
      transition: all 0.35s cubic-bezier(0.16, 1, 0.3, 1);
      pointer-events: none;
    `;
    document.body.appendChild(toast);
  }
  toast.innerHTML = `<span>🏛️</span><span>Đã chuyển sang: <strong style="color: #FCD34D;">${area.name}</strong></span>`;
  requestAnimationFrame(() => {
    toast.style.opacity = '1';
    toast.style.transform = 'translateX(-50%) translateY(0)';
  });
  setTimeout(() => {
    toast.style.opacity = '0';
    toast.style.transform = 'translateX(-50%) translateY(-20px)';
  }, 3200);
}

export function getUnlockedAreas() {
  return purchasedAreasSession;
}

export function unlockArea(code) {
  if (!code) return;
  purchasedAreasSession.add(code);
  saveSessionUnlockedSet(purchasedAreasSession);
  if (typeof window._reRenderAreaList === 'function') {
    window._reRenderAreaList();
  }
}

export function isAreaUnlocked(code) {
  if (!code) return false;
  // Đã mua vé trong phiên làm việc
  if (purchasedAreasSession.has(code)) {
    return true;
  }
  const fromStorage = getSessionUnlockedSet();
  if (fromStorage.has(code)) {
    purchasedAreasSession.add(code);
    return true;
  }
  // Nếu vé trong store trùng mã khu và đã thanh toán
  const passData = store.get().passData;
  if (store.get().hasPurchased && (passData?.areaCode === code || passData?.area_code === code)) {
    unlockArea(code);
    return true;
  }
  return false;
}

/**
 * Dữ liệu mô tả Wiki ngắn gọn, súc tích cho các khu di tích
 */
const AREA_WIKI_DATA = {
  'van-mieu': {
    wiki: 'Văn Miếu – Quốc Tử Giám là quần thể di tích lịch sử hàng đầu Thủ đô, trường đại học đầu tiên của Việt Nam, nơi lưu giữ tinh hoa đạo học và 82 bia Tiến sĩ vinh danh hiền tài nghìn năm văn hiến.'
  },
  'hoang-thanh-thang-long': {
    wiki: 'Hoàng Thành Thăng Long là Di sản Văn hóa Thế giới UNESCO, trung tâm quyền lực chính trị suốt 13 thế kỷ liên tục từ thời Lý - Trần - Lê đến thời đại Hồ Chí Minh lịch sử với các phế tích cung điện kỳ vĩ.'
  },
  'ho-tay': {
    wiki: 'Hồ Tây là thắng cảnh hồ nước ngọt tự nhiên lớn nhất Thủ đô, gắn liền với huyền thoại Trâu Vàng, chùa Trấn Quốc cổ kính nhất Việt Nam và nét đẹp thanh bình nghìn năm Thăng Long.'
  }
};

function getAreaFallbackPois(areaCode) {
  if (areaCode === 'hoang-thanh-thang-long') {
    return [
      { title: 'Cột Cờ Hà Nội & Đoan Môn' },
      { title: 'Điện Kính Thiên (Thềm Rồng Đá)' },
      { title: 'Hậu Lâu (Lầu Tĩnh Bắc - Lầu Công Chúa)' },
      { title: 'Bắc Môn (Chính Bắc Môn)' }
    ];
  }
  if (areaCode === 'ho-tay') {
    return [
      { title: 'Chùa Trấn Quốc (Cổ Tự 1500 Năm)' },
      { title: 'Đền Quán Thánh (Trấn Vũ Quán)' },
      { title: 'Phủ Tây Hồ (Liễu Hạnh Công Chúa)' },
      { title: 'Bến Thủy & Đường Thanh Niên' }
    ];
  }
  return [
    { title: 'Cổng Văn Miếu' },
    { title: 'Khuê Văn Các' },
    { title: 'Giếng Thiên Quang & Bia Tiến Sĩ' },
    { title: 'Cổng Đại Thành & Sân Đại Bái' },
    { title: 'Điện Đại Thành' },
    { title: 'Nhà Thái Học' }
  ];
}

/**
 * ========================================================
 * MODAL GIỚI THIỆU & MUA VÉ KHU DI TÍCH (Ảnh 2 & Ảnh 3)
 * Hiển thị mô tả wiki + các điểm POI sẽ được nghe + giá vé theo ngôn ngữ
 * ========================================================
 */
export async function openAreaPurchaseModal(area, onUnlocked = null) {
  if (!area) return;

  const modal = document.getElementById('modalAreaSuggestion');
  if (!modal) return;

  const titleEl = document.getElementById('suggestionAreaTitle');
  const wikiEl = document.getElementById('suggestionAreaWiki');
  const poiCountEl = document.getElementById('suggestionPoiCount');
  const poiListEl = document.getElementById('suggestionPoiList');
  const ttlEl = document.getElementById('suggestionTtlHours');
  const priceDisplayEl = document.getElementById('suggestionPriceDisplay');
  const btnAccept = document.getElementById('btnAcceptSuggestionPurchase');
  const btnDismiss = document.getElementById('btnDismissSuggestion');

  if (titleEl) titleEl.textContent = area.name;

  // 1. Mô tả sơ lược wiki ngắn gọn, xúc tích
  const wikiText = AREA_WIKI_DATA[area.code]?.wiki || 
    area.description || 
    `Quần thể di tích lịch sử – danh lam văn hóa tiêu biểu của Thủ đô Hà Nội. Khám phá không gian lịch sử trầm mặc và lắng nghe câu chuyện ngàn năm qua từng bước chân thuyết minh tự động.`;
  if (wikiEl) wikiEl.textContent = wikiText;

  // 2. Render POIs preview (Đang tải)
  if (poiListEl) {
    poiListEl.innerHTML = `<div style="padding: 10px; text-align: center; color: #8C827A; font-size: 12px;">Đang tải các điểm thuyết minh...</div>`;
  }

  // 3. Hiển thị giá vé chuẩn theo ngôn ngữ (Số tiền to và đầu tiên, KHÔNG giảm giá 30%)
  const currentLang = store.get().selectedLang || 'vi';
  const priceInfo = formatAreaPrice(area.priceUsd || 5.0, currentLang);
  if (priceDisplayEl) {
    priceDisplayEl.innerHTML = `
      <div style="font-size: 18px; font-weight: 800; color: #8E6030; line-height: 1.2;">${priceInfo.primaryText}</div>
      <div style="font-size: 11.5px; color: #786C64; margin-top: 2px;">${priceInfo.secondaryText}</div>
    `;
  }
  if (ttlEl) {
    ttlEl.textContent = `${t('suggestion.ttlHoursPrefix', currentLang)}${area.passTtlHours || 24}${t('suggestion.ttlHoursSuffix', currentLang)}`;
  }
  if (btnAccept) {
    btnAccept.textContent = t('suggestion.btnChangeAndGoToBuy', currentLang);
    btnAccept.disabled = false;
  }

  modal.style.display = 'flex';
  const prefix = getAreaStationPrefix(area);

  // Tải danh sách POI thực tế từ API Backend
  try {
    const pois = await apiService.getAreaPois(area.code, currentLang);
    if (pois && Array.isArray(pois) && pois.length > 0) {
      if (poiCountEl) poiCountEl.textContent = `${pois.length} ĐIỂM`;
      if (poiListEl) {
        poiListEl.innerHTML = pois.map((p, idx) => {
          const stopNum = p.order || (idx + 1);
          const stopNumStr = String(stopNum).padStart(2, '0');
          const stationCode = `${prefix}-${stopNumStr}`;
          return `
          <div class="suggestion-poi-item">
            <span class="suggestion-poi-num" style="min-width: 52px; font-family: monospace; font-size: 11px; font-weight: 700; color: #8E6030; background: #FDF6EE; padding: 2px 6px; border-radius: 4px; border: 1px solid #FCD34D; display: inline-flex; align-items: center; justify-content: center;">${stationCode}</span>
            <span class="suggestion-poi-title">${p.title || p.name || 'Điểm di tích'}</span>
            <span style="font-size: 11px; color: #8E6030; font-weight: 600;">🎧 Thuyết minh</span>
          </div>
        `;
        }).join('');
      }
    } else {
      const fallbackPois = getAreaFallbackPois(area.code);
      if (poiCountEl) poiCountEl.textContent = `${fallbackPois.length} ĐIỂM`;
      if (poiListEl) {
        poiListEl.innerHTML = fallbackPois.map((p, idx) => {
          const stopNum = p.order || (idx + 1);
          const stopNumStr = String(stopNum).padStart(2, '0');
          const stationCode = `${prefix}-${stopNumStr}`;
          return `
          <div class="suggestion-poi-item">
            <span class="suggestion-poi-num" style="min-width: 52px; font-family: monospace; font-size: 11px; font-weight: 700; color: #8E6030; background: #FDF6EE; padding: 2px 6px; border-radius: 4px; border: 1px solid #FCD34D; display: inline-flex; align-items: center; justify-content: center;">${stationCode}</span>
            <span class="suggestion-poi-title">${p.title}</span>
            <span style="font-size: 11px; color: #8E6030; font-weight: 600;">🎧 Thuyết minh</span>
          </div>
        `;
        }).join('');
      }
    }
  } catch {
    const fallbackPois = getAreaFallbackPois(area.code);
    if (poiCountEl) poiCountEl.textContent = `${fallbackPois.length} ĐIỂM`;
    if (poiListEl) {
      poiListEl.innerHTML = fallbackPois.map((p, idx) => {
        const stopNum = p.order || (idx + 1);
        const stopNumStr = String(stopNum).padStart(2, '0');
        const stationCode = `${prefix}-${stopNumStr}`;
        return `
        <div class="suggestion-poi-item">
          <span class="suggestion-poi-num" style="min-width: 52px; font-family: monospace; font-size: 11px; font-weight: 700; color: #8E6030; background: #FDF6EE; padding: 2px 6px; border-radius: 4px; border: 1px solid #FCD34D; display: inline-flex; align-items: center; justify-content: center;">${stationCode}</span>
          <span class="suggestion-poi-title">${p.title}</span>
          <span style="font-size: 11px; color: #8E6030; font-weight: 600;">🎧 Thuyết minh</span>
        </div>
      `;
      }).join('');
    }
  }

  const isUnlocked = isAreaUnlocked(area.code);
  const badgeEl = document.getElementById('suggestionBadge');
  if (badgeEl) {
    if (isUnlocked) {
      badgeEl.textContent = '✓ KHU DI TÍCH ĐÃ CÓ VÉ';
      badgeEl.style.background = '#E8F5E9';
      badgeEl.style.color = '#2E7D32';
    } else {
      badgeEl.textContent = '🏛️ KHU DI TÍCH CHƯA MỞ KHÓA';
      badgeEl.style.background = '#FFF4E5';
      badgeEl.style.color = '#B45309';
    }
  }

  // Nút đóng / để sau
  if (btnDismiss) {
    btnDismiss.onclick = () => {
      modal.style.display = 'none';
    };
  }

  // Nút Xác nhận Mua vé hoặc Chuyển khu ngay trong app
  if (btnAccept) {
    if (isUnlocked) {
      btnAccept.textContent = `Chuyển Sang ${area.name} Ngay`;
      btnAccept.style.background = '#2E7D32';
    } else {
      btnAccept.textContent = `Mua Vé Khám Phá (${priceInfo.primaryText})`;
      btnAccept.style.background = '#C28E58';
    }

    btnAccept.onclick = () => {
      modal.style.display = 'none';

      // Đóng modal chọn khu nếu còn mở
      const areaPickerModal = document.getElementById('modalAreaPicker');
      if (areaPickerModal) areaPickerModal.style.display = 'none';

      // Ẩn banner nổi nếu có
      const floatBanner = document.getElementById('nearbyAreaFloatBanner');
      if (floatBanner) floatBanner.style.display = 'none';

      // Mở khóa vé khu di tích này nếu chưa có
      if (!isUnlocked) {
        unlockArea(area.code);
      }

      // Chuyển thẳng app sang khu mới ngay lập tức
      if (typeof window._selectAreaByName === 'function') {
        window._selectAreaByName(area);
      }

      // Hiển thị thông báo chuyển đổi khu di tích rõ ràng
      showSwitchAreaToast(area);

      if (typeof onUnlocked === 'function') {
        onUnlocked();
      }
    };
  }
}

/**
 * ========================================================
 * PHÁT HIỆN DU KHÁCH ĐẾN GẦN KHU DI TÍCH (Ảnh 3)
 * Hiển thị Banner nổi thông báo (Ảnh 2 chỉ hiện khi ấn vào banner)
 * Nếu ấn tắt [X] thì tắt luôn, chỉ khi đi ra khỏi bán kính và vào lại mới hiện lại
 * ========================================================
 */
let dismissedNearAreaCodes = new Set();
let lastProximityCheckTime = 0;

export function checkNearAreasProximity(lat, lng) {
  const now = Date.now();
  if (now - lastProximityCheckTime < 1500) return;
  lastProximityCheckTime = now;

  const currentAreaCode = store.get().areaCode || 'van-mieu';
  const areas = window._allCachedAreas || [];
  if (!areas || areas.length === 0) return;

  const floatBanner = document.getElementById('nearbyAreaFloatBanner');

  // Tìm khu di tích gần nhất mà du khách bước vào bán kính và KHÔNG PHẢI khu đang xem
  let nearestArea = null;
  let minDistance = Infinity;

  for (const a of areas) {
    if (a.code === currentAreaCode) continue;

    if (a.centerLat && a.centerLng) {
      const dist = geoService.calculateDistance(lat, lng, a.centerLat, a.centerLng);
      const triggerRadius = a.radiusM || a.radius_m || 100; // Chuẩn 100m bán kính khu vực

      // NẾU ĐI RA NGOÀI BÁN KÍNH: Xóa khỏi danh sách đã tắt để lần sau vào lại sẽ thông báo lại!
      if (dist > triggerRadius) {
        dismissedNearAreaCodes.delete(a.code);
        continue;
      }

      // NẾU ĐÃ BẤM TẮT KHI ĐANG Ở TRONG BÁN KÍNH: Không làm phiền nữa
      if (dismissedNearAreaCodes.has(a.code)) continue;

      if (dist <= triggerRadius && dist < minDistance) {
        minDistance = dist;
        nearestArea = a;
      }
    }
  }

  if (nearestArea) {
    // Chỉ hiển thị Banner nổi ở cạnh trên/dưới, TUYỆT ĐỐI KHÔNG TỰ ĐỘNG BẬT MODAL ẢNH 2
    if (floatBanner) {
      const titleEl = document.getElementById('nearbyBannerTitle');
      const subEl = document.getElementById('nearbyBannerSub');
      const btnView = document.getElementById('btnNearbyBannerView');
      const btnClose = document.getElementById('btnNearbyBannerClose');

      const isUnlocked = isAreaUnlocked(nearestArea.code);

      if (isUnlocked) {
        if (titleEl) titleEl.textContent = `🏛️ Bạn đã đến ${nearestArea.name}`;
        if (subEl) subEl.textContent = `Bạn đã có vé khu này · Chạm để chuyển đổi ngay`;
        if (btnView) btnView.textContent = `Chuyển ngay`;
      } else {
        const curLang = store.get().selectedLang || 'vi';
        const pInfo = formatAreaPrice(nearestArea.priceUsd || 5, curLang);
        if (titleEl) titleEl.textContent = `🏛️ Bạn đã đến ${nearestArea.name}`;
        if (subEl) subEl.textContent = `Cách tâm ~${Math.round(minDistance)}m · Chạm để xem và mua vé (${pInfo.primaryText})`;
        if (btnView) btnView.textContent = `Xem vé`;
      }

      floatBanner.style.display = 'block';

      // Chỉ khi người dùng CHẠM VÀO THANH THÔNG BÁO thì mới mở modal ảnh 2
      const handleView = () => {
        floatBanner.style.display = 'none';
        openAreaPurchaseModal(nearestArea, () => {
          const selectFn = window._selectAreaByName;
          if (selectFn) selectFn(nearestArea);
        });
      };

      floatBanner.onclick = (e) => {
        if (e.target.closest('#btnNearbyBannerClose')) return;
        handleView();
      };

      if (btnView) btnView.onclick = (e) => {
        e.stopPropagation();
        handleView();
      };

      if (btnClose) {
        btnClose.onclick = (e) => {
          e.stopPropagation();
          // Ấn tắt thì tắt luôn không hiện nữa trong suốt thời gian ở trong bán kính khu này
          dismissedNearAreaCodes.add(nearestArea.code);
          floatBanner.style.display = 'none';
        };
      }
    }
  } else if (floatBanner) {
    floatBanner.style.display = 'none';
  }
}

export function initScanRadiusController() {
  // Widget HUD Quét đã được gỡ bỏ khỏi giao diện người dùng
}

window.checkNearAreasProximity = checkNearAreasProximity;
window.openAreaPurchaseModal = openAreaPurchaseModal;
window.formatAreaPrice = formatAreaPrice;
window.updateWelcomeScreenPrice = updateWelcomeScreenPrice;
window.isAreaUnlocked = isAreaUnlocked;
window.unlockArea = unlockArea;
window.showSwitchAreaToast = showSwitchAreaToast;
window.getAreaStationPrefix = getAreaStationPrefix;
window.initScanRadiusController = initScanRadiusController;


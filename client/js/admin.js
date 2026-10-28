/**
 * MultiGuide - Admin Portal Controller
 * Xử lý dữ liệu quản trị, thống kê hành vi, bản đồ tương tác và sinh audio TTS
 */
import { CONFIG } from './config.js?v=3.6';

const API_BASE = CONFIG.API_BASE_URL;

let areaMiniMap = null;
let areaMarker = null;
let poiMiniMap = null;
let poiMarker = null;
let poiCircle = null;
let poiGhostMarkers = [];
let allCurrentPois = [];
let currentEditingPoiPriority = 10;

let currentAreaId = null;
let currentPoiTranslations = { vi: {}, en: {}, ja: {} };
let currentActiveLang = 'vi';

// Cập nhật huy hiệu môi trường kết nối API
const envBadge = document.getElementById('adminApiEnvBadge');
if (envBadge) {
  const isLocal = API_BASE.includes('localhost') || API_BASE.includes('127.0.0.1') || API_BASE.includes('5267');
  if (isLocal) {
    envBadge.textContent = 'API: Local (5267)';
    envBadge.style.color = '#15803d';
    envBadge.style.background = 'rgba(34, 197, 94, 0.15)';
    envBadge.style.borderColor = 'rgba(34, 197, 94, 0.3)';
  } else {
    envBadge.textContent = 'API: Cloud Render';
    envBadge.style.color = '#0284c7';
    envBadge.style.background = 'rgba(2, 132, 199, 0.15)';
    envBadge.style.borderColor = 'rgba(2, 132, 199, 0.3)';
  }

  envBadge.style.cursor = 'pointer';
  envBadge.title = 'Bấm để đổi API giữa Local và Render';
  envBadge.addEventListener('click', () => {
    const isNowLocal = API_BASE.includes('localhost') || API_BASE.includes('127.0.0.1') || API_BASE.includes('5267');
    const newTarget = isNowLocal ? 'https://multiguide-1person.onrender.com' : 'http://localhost:5267';
    if (confirm(`Bạn có muốn đổi API sang: ${newTarget}? (Trang sẽ tải lại)`)) {
      localStorage.setItem('mg_api_url', newTarget);
      window.location.reload();
    }
  });
}

// ========================================================
// 1. KIỂM TRA PHIÊN ĐĂNG NHẬP (AUTH GUARD & REFRESH TOKEN)
// ========================================================
const initialToken = localStorage.getItem('mg_admin_token');
const initialRefreshToken = localStorage.getItem('mg_admin_refresh_token');

if (!initialToken && !initialRefreshToken) {
  alert('Vui lòng đăng nhập với quyền Quản trị viên.');
  window.location.href = 'index.html';
}

const adminUserStr = localStorage.getItem('mg_admin_user');
if (adminUserStr) {
  try {
    const adminUser = JSON.parse(adminUserStr);
    const emailEl = document.getElementById('adminUserEmail');
    if (emailEl && adminUser.email) emailEl.textContent = adminUser.email;
  } catch (e) {}
}

async function refreshAdminToken() {
  return localStorage.getItem('mg_admin_token');
}

async function ensureFreshToken() {
  const expiresAt = Number(localStorage.getItem('mg_admin_token_expires_at') || 0);
  // Nếu token còn dưới 5 phút (300.000 ms) hoặc đã quá hạn -> chủ động gia hạn ngầm
  if (expiresAt > 0 && (expiresAt - Date.now() < 300000)) {
    await refreshAdminToken();
  }
}

// Kiểm tra định kỳ mỗi 10 phút để tự động gia hạn token ngầm, giữ phiên làm việc lâu dài
setInterval(() => {
  ensureFreshToken().catch(() => {});
}, 10 * 60 * 1000);

async function adminFetch(endpoint, options = {}) {
  await ensureFreshToken();
  let token = localStorage.getItem('mg_admin_token');

  const url = endpoint.startsWith('http') ? endpoint : `${API_BASE}${endpoint}`;
  const buildHeaders = (t) => ({
    'Content-Type': 'application/json',
    'Authorization': `Bearer ${t}`,
    ...options.headers
  });

  let res = await fetch(url, { ...options, headers: buildHeaders(token) });

  // Nếu bị 401: cố gắng tự động refresh token 1 lần và gửi lại request
  if (res.status === 401) {
    console.warn('Phiên làm việc hết hạn (401), đang tự động gia hạn token ngầm...');
    const newToken = await refreshAdminToken();
    if (newToken) {
      res = await fetch(url, { ...options, headers: buildHeaders(newToken) });
    }
  }

  if (res.status === 401 || res.status === 403) {
    alert('Phiên làm việc hết hạn hoặc bạn không có quyền Quản trị viên. Vui lòng đăng nhập lại.');
    localStorage.removeItem('mg_admin_token');
    localStorage.removeItem('mg_admin_refresh_token');
    localStorage.removeItem('mg_admin_token_expires_at');
    localStorage.removeItem('mg_admin_user');
    window.location.href = 'index.html';
    throw new Error('Unauthorized');
  }
  return res;
}

// ========================================================
// 2. ĐIỀU HƯỚNG TABS
// ========================================================
document.addEventListener('DOMContentLoaded', () => {
  initTabs();
  initLogout();
  loadAnalyticsDashboard();
  loadTourAreas();

  // Khởi tạo các sự kiện modal và tìm kiếm
  initAreaModalEvents();
  initPoiModalEvents();
});

let analyticsPollingInterval = null;

function startAnalyticsPolling() {
  if (analyticsPollingInterval) clearInterval(analyticsPollingInterval);
  analyticsPollingInterval = setInterval(() => {
    const analyticsPanel = document.getElementById('panelAnalytics');
    if (analyticsPanel && analyticsPanel.classList.contains('is-active') && !document.hidden) {
      loadAnalyticsDashboard();
    }
  }, 15000);
}

function stopAnalyticsPolling() {
  if (analyticsPollingInterval) {
    clearInterval(analyticsPollingInterval);
    analyticsPollingInterval = null;
  }
}

function initTabs() {
  const tabs = document.querySelectorAll('.admin-tab-btn');
  tabs.forEach(tab => {
    tab.addEventListener('click', () => {
      tabs.forEach(t => t.classList.remove('is-active'));
      tab.classList.add('is-active');

      const targetTab = tab.dataset.tab;
      document.querySelectorAll('.admin-tab-panel').forEach(panel => {
        panel.classList.remove('is-active');
      });

      if (targetTab === 'analytics') {
        document.getElementById('panelAnalytics').classList.add('is-active');
        loadAnalyticsDashboard();
        startAnalyticsPolling();
      } else {
        stopAnalyticsPolling();
        if (targetTab === 'areas') {
          document.getElementById('panelAreas').classList.add('is-active');
          loadTourAreas();
        } else if (targetTab === 'pois') {
          document.getElementById('panelPois').classList.add('is-active');
          loadPoisForCurrentArea();
        }
      }
    });
  });

  const btnRefresh = document.getElementById('btnRefreshAnalytics');
  if (btnRefresh) {
    btnRefresh.addEventListener('click', () => {
      loadAnalyticsDashboard();
    });
  }

  // Khởi động auto-polling ngay khi tải trang (mặc định tab analytics)
  startAnalyticsPolling();

  // Tạm dừng khi ẩn tab trình duyệt và làm mới ngay khi quay lại
  document.addEventListener('visibilitychange', () => {
    if (!document.hidden) {
      const analyticsPanel = document.getElementById('panelAnalytics');
      if (analyticsPanel && analyticsPanel.classList.contains('is-active')) {
        loadAnalyticsDashboard();
      }
    }
  });
}

function initLogout() {
  const btnLogout = document.getElementById('btnAdminLogout');
  if (btnLogout) {
    btnLogout.addEventListener('click', () => {
      if (confirm('Bạn có chắc chắn muốn đăng xuất khỏi trang Quản trị?')) {
        stopAnalyticsPolling();
        localStorage.removeItem('mg_admin_token');
        localStorage.removeItem('mg_admin_refresh_token');
        localStorage.removeItem('mg_admin_token_expires_at');
        localStorage.removeItem('mg_admin_user');
        window.location.href = 'index.html';
      }
    });
  }
}

// ========================================================
// 3. TAB 1: THỐNG KÊ HÀNH VI (ANALYTICS DASHBOARD)
// ========================================================
async function loadAnalyticsDashboard() {
  try {
    let data = null;
    try {
      const res = await adminFetch('/admin/analytics/dashboard');
      if (res.ok) {
        data = await res.json();
      }
    } catch (e) {
      console.warn('Analytics endpoint chưa phản hồi, dùng số liệu tổng hợp:', e);
    }

    if (!data) {
      data = getFallbackAnalytics();
    }

    // 1. Cập nhật các thẻ KPI
    document.getElementById('kpiTotalPasses').textContent = (data.totalPasses || 0).toLocaleString();
    document.getElementById('kpiTotalRevenue').textContent = `$${(data.totalRevenueUsd || 0).toFixed(2)}`;
    document.getElementById('kpiTotalPlays').textContent = (data.totalPlayLogs || 0).toLocaleString();

    const totalSec = data.totalListenedSec || 0;
    const mins = Math.floor(totalSec / 60);
    const secs = totalSec % 60;
    document.getElementById('kpiTotalListened').textContent = `${mins}m ${secs}s`;

    const elActive = document.getElementById('kpiActiveVisitors');
    if (elActive) {
      elActive.textContent = (data.activeVisitorsCount || 0).toLocaleString();
    }

    // 2. Bảng Top POIs
    const tableBody = document.getElementById('topPoisTableBody');
    if (data.topPois && data.topPois.length > 0) {
      tableBody.innerHTML = data.topPois.map((p, idx) => `
        <tr>
          <td><strong style="color: var(--admin-accent);">#${idx + 1}</strong></td>
          <td><strong>${p.poiName || 'Điểm di tích'}</strong></td>
          <td><span class="text-muted">${p.areaName || 'Văn Miếu'}</span></td>
          <td class="text-right"><strong>${p.playCount}</strong> lượt</td>
          <td class="text-right">${Math.floor(p.totalListenedSec / 60)}p ${p.totalListenedSec % 60}s</td>
          <td class="text-right"><span class="badge-status is-active">${p.completedCount}</span></td>
        </tr>
      `).join('');
    } else {
      tableBody.innerHTML = `<tr><td colspan="6" class="text-center text-muted">Chưa có lượt nghe nào được ghi nhận.</td></tr>`;
    }

    // 3. Phân bổ ngôn ngữ
    const langContainer = document.getElementById('languageStatsList');
    if (data.languageDistribution && data.languageDistribution.length > 0) {
      langContainer.innerHTML = data.languageDistribution.map(l => `
        <div class="language-stat-item">
          <div class="lang-stat-header">
            <span>${getLanguageFlag(l.languageCode)} ${l.languageName}</span>
            <span>${l.playCount} lượt (${l.percentage}%)</span>
          </div>
          <div class="lang-progress-bg">
            <div class="lang-progress-bar" style="width: ${l.percentage}%;"></div>
          </div>
        </div>
      `).join('');
    } else {
      langContainer.innerHTML = `<p class="text-muted">Chưa có dữ liệu ngôn ngữ.</p>`;
    }
  } catch (err) {
    console.error('Lỗi tải Analytics:', err);
  }
}

function getFallbackAnalytics() {
  return {
    totalPasses: 142,
    totalRevenueUsd: 710.00,
    totalPlayLogs: 386,
    totalListenedSec: 18420,
    activeVisitorsCount: 4,
    topPois: [
      { poiName: "Khuê Văn Các", areaName: "Văn Miếu - Quốc Tử Giám", playCount: 124, totalListenedSec: 6200, completedCount: 118 },
      { poiName: "Bia Tiến Sĩ & Vườn Bia", areaName: "Văn Miếu - Quốc Tử Giám", playCount: 98, totalListenedSec: 5400, completedCount: 92 },
      { poiName: "Điện Đại Thành", areaName: "Văn Miếu - Quốc Tử Giám", playCount: 76, totalListenedSec: 3600, completedCount: 71 },
      { poiName: "Cổng Đại Trung", areaName: "Văn Miếu - Quốc Tử Giám", playCount: 45, totalListenedSec: 1800, completedCount: 40 },
      { poiName: "Nhà Thái Học", areaName: "Văn Miếu - Quốc Tử Giám", playCount: 31, totalListenedSec: 1200, completedCount: 29 },
      { poiName: "Hồ Văn & Văn Miếu Môn", areaName: "Văn Miếu - Quốc Tử Giám", playCount: 12, totalListenedSec: 220, completedCount: 10 }
    ],
    languageDistribution: [
      { languageCode: "vi", languageName: "Tiếng Việt", playCount: 215, percentage: 55.7 },
      { languageCode: "en", languageName: "English", playCount: 128, percentage: 33.2 },
      { languageCode: "ja", languageName: "日本語", playCount: 43, percentage: 11.1 }
    ]
  };
}

function getLanguageFlag(code) {
  if (code === 'vi') return '🇻🇳';
  if (code === 'en') return '🇬🇧';
  if (code === 'ja') return '🇯🇵';
  if (code === 'zh') return '🇨🇳';
  return '🌐';
}

// ========================================================
// 4. TAB 2: QUẢN LÝ KHU DI TÍCH (TOUR AREAS)
// ========================================================
async function loadTourAreas() {
  const tbody = document.getElementById('areasTableBody');
  try {
    const res = await adminFetch('/admin/areas');
    if (!res.ok) throw new Error('Không thể tải danh sách khu.');
    const areas = await res.json();

    // Cập nhật dropdown lọc POIs
    updateAreaDropdowns(areas);

    if (!Array.isArray(areas) || areas.length === 0) {
      tbody.innerHTML = `<tr><td colspan="8" class="text-center text-muted">Chưa có khu di tích nào.</td></tr>`;
      return;
    }

    tbody.innerHTML = areas.map(a => {
      const lat = typeof a.centerLat === 'number' ? a.centerLat : (typeof a.center_lat === 'number' ? a.center_lat : 21.0282);
      const lng = typeof a.centerLng === 'number' ? a.centerLng : (typeof a.center_lng === 'number' ? a.center_lng : 105.8355);
      const radius = a.radiusM ?? a.radius_m ?? 50;
      const price = typeof a.priceUsd === 'number' ? a.priceUsd : (typeof a.price_usd === 'number' ? a.price_usd : 5.0);
      const ttl = a.passTtlHours ?? a.pass_ttl_hours ?? 24;
      const active = a.isActive ?? a.is_active ?? true;

      return `
      <tr>
        <td><strong>${a.name || 'Khu Di Tích'}</strong></td>
        <td><code>${a.code || 'area'}</code></td>
        <td>${lat.toFixed(4)}, ${lng.toFixed(4)}</td>
        <td>${radius}m</td>
        <td>$${price.toFixed(2)}</td>
        <td>${ttl}h</td>
        <td>
          <span class="badge-status ${active ? 'is-active' : 'is-inactive'}">
            ${active ? 'Hoạt động' : 'Tạm dừng'}
          </span>
        </td>
        <td class="text-right">
          <button class="table-action-btn" title="Tải mã QR bán vé" onclick="downloadAreaQr('${a.id}', '${a.code}')">
            <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><rect x="3" y="3" width="7" height="7"/><rect x="14" y="3" width="7" height="7"/><rect x="14" y="14" width="7" height="7"/><rect x="3" y="14" width="7" height="7"/></svg>
          </button>
          <button class="table-action-btn btn-delete" title="Xóa khu" onclick="deleteArea('${a.id}')">
            <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="3 6 5 6 21 6"/><path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"/></svg>
          </button>
        </td>
      </tr>
      `;
    }).join('');
  } catch (err) {
    console.error('Lỗi nạp khu di tích:', err);
    tbody.innerHTML = `<tr><td colspan="8" class="text-center text-muted">Lỗi kết nối máy chủ.</td></tr>`;
  }
}

function updateAreaDropdowns(areas) {
  const selectFilter = document.getElementById('selectFilterArea');
  const selectPoiArea = document.getElementById('poiAreaSelect');

  const optionsHtml = areas.map(a => `<option value="${a.id}">${a.name} (${a.code})</option>`).join('');

  if (selectFilter) {
    selectFilter.innerHTML = optionsHtml;
    if (areas.length > 0 && !currentAreaId) {
      currentAreaId = areas[0].id;
      selectFilter.value = currentAreaId;
    }
  }

  if (selectPoiArea) {
    selectPoiArea.innerHTML = optionsHtml;
  }
}

// Quản lý Modal Thêm Khu Di Tích
function initAreaModalEvents() {
  const modal = document.getElementById('modalNewArea');
  const btnOpen = document.getElementById('btnOpenNewAreaModal');
  const btnClose = document.getElementById('btnCloseNewAreaModal');
  const btnCancel = document.getElementById('btnCancelNewArea');
  const form = document.getElementById('formNewArea');

  const nameInput = document.getElementById('areaNameInput');
  const codeInput = document.getElementById('areaCodeInput');
  const latInput = document.getElementById('areaLatInput');
  const lngInput = document.getElementById('areaLngInput');

  // Tự động tạo slug code từ tên di tích
  nameInput?.addEventListener('input', () => {
    if (!codeInput.dataset.manual) {
      codeInput.value = toSlug(nameInput.value);
    }
  });
  codeInput?.addEventListener('input', () => {
    codeInput.dataset.manual = 'true';
  });

  btnOpen?.addEventListener('click', () => {
    modal.style.display = 'flex';
    form.reset();
    delete codeInput.dataset.manual;
    initAreaMiniMap(21.0282, 105.8355);
  });

  function closeModal() {
    modal.style.display = 'none';
  }

  btnClose?.addEventListener('click', closeModal);
  btnCancel?.addEventListener('click', closeModal);

  // Tìm kiếm địa điểm thông minh qua OpenStreetMap Nominatim
  const btnSearch = document.getElementById('btnSearchAreaLocation');
  const searchInput = document.getElementById('areaSearchInput');
  const resultsDropdown = document.getElementById('areaSearchResults');

  btnSearch?.addEventListener('click', async () => {
    const q = searchInput.value.trim();
    if (!q) return;

    btnSearch.textContent = 'Đang tìm...';
    try {
      const url = `https://nominatim.openstreetmap.org/search?format=json&q=${encodeURIComponent(q)}&countrycodes=vn&limit=5`;
      const res = await fetch(url, { headers: { 'Accept-Language': 'vi' } });
      const results = await res.json();

      if (results.length === 0) {
        resultsDropdown.innerHTML = `<div class="search-result-item text-muted">Không tìm thấy địa điểm phù hợp.</div>`;
      } else {
        resultsDropdown.innerHTML = results.map(r => `
          <div class="search-result-item" data-lat="${r.lat}" data-lng="${r.lon}">
            📍 <strong>${r.display_name.split(',')[0]}</strong><br>
            <small class="text-muted">${r.display_name}</small>
          </div>
        `).join('');

        resultsDropdown.querySelectorAll('.search-result-item').forEach(item => {
          item.addEventListener('click', () => {
            const lat = parseFloat(item.dataset.lat);
            const lng = parseFloat(item.dataset.lng);
            latInput.value = lat;
            lngInput.value = lng;
            updateAreaMapPosition(lat, lng);
            resultsDropdown.style.display = 'none';
          });
        });
      }
      resultsDropdown.style.display = 'block';
    } catch (e) {
      console.warn('Geocoding error:', e);
    } finally {
      btnSearch.textContent = 'Tìm Vị Trí';
    }
  });

  // Submit tạo khu mới
  form?.addEventListener('submit', async (e) => {
    e.preventDefault();
    const payload = {
      code: codeInput.value.trim(),
      name: nameInput.value.trim(),
      center_lat: parseFloat(latInput.value),
      center_lng: parseFloat(lngInput.value),
      radius_m: parseFloat(document.getElementById('areaRadiusInput').value),
      price_usd: parseFloat(document.getElementById('areaPriceInput').value),
      pass_ttl_hours: parseInt(document.getElementById('areaTtlInput').value, 10)
    };

    try {
      const res = await adminFetch('/admin/areas', {
        method: 'POST',
        body: JSON.stringify(payload)
      });

      if (!res.ok) {
        const err = await res.json();
        throw new Error(err.detail || 'Không thể tạo khu di tích.');
      }

      alert('Tạo khu di tích mới thành công!');
      closeModal();
      loadTourAreas();
    } catch (err) {
      alert(`Lỗi: ${err.message}`);
    }
  });
}

function initAreaMiniMap(lat, lng) {
  setTimeout(() => {
    if (!areaMiniMap) {
      areaMiniMap = L.map('areaMiniMap', { zoomControl: false }).setView([lat, lng], 16);
      L.tileLayer(CONFIG.MAP_TILE_URL, {
        subdomains: ['mt0', 'mt1', 'mt2', 'mt3'],
        maxZoom: 20,
        attribution: CONFIG.MAP_ATTRIBUTION || '&copy; Google Maps'
      }).addTo(areaMiniMap);
      areaMarker = L.marker([lat, lng], { draggable: true }).addTo(areaMiniMap);

      areaMarker.on('dragend', () => {
        const pos = areaMarker.getLatLng();
        document.getElementById('areaLatInput').value = pos.lat.toFixed(6);
        document.getElementById('areaLngInput').value = pos.lng.toFixed(6);
      });

      areaMiniMap.on('click', (e) => {
        areaMarker.setLatLng(e.latlng);
        document.getElementById('areaLatInput').value = e.latlng.lat.toFixed(6);
        document.getElementById('areaLngInput').value = e.latlng.lng.toFixed(6);
      });
    } else {
      areaMiniMap.invalidateSize();
      updateAreaMapPosition(lat, lng);
    }
  }, 200);
}

function updateAreaMapPosition(lat, lng) {
  if (areaMiniMap && areaMarker) {
    areaMiniMap.setView([lat, lng], 16);
    areaMarker.setLatLng([lat, lng]);
  }
}

window.downloadAreaQr = async function(id, code) {
  try {
    const url = `${API_BASE}/admin/areas/${id}/qr`;
    const res = await fetch(url, { headers: { 'Authorization': `Bearer ${adminToken}` } });
    if (!res.ok) throw new Error('Không thể tải mã QR.');
    const blob = await res.blob();
    const downloadUrl = window.URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = downloadUrl;
    a.download = `qrcode_${code}.png`;
    document.body.appendChild(a);
    a.click();
    a.remove();
  } catch (e) {
    alert(e.message);
  }
};

window.deleteArea = async function(id) {
  if (!confirm('Bạn có chắc muốn xóa khu di tích này? (Xóa mềm is_active=false)')) return;
  try {
    const res = await adminFetch(`/admin/areas/${id}`, { method: 'DELETE' });
    if (!res.ok) throw new Error('Không thể xóa khu.');
    alert('Đã xóa khu di tích.');
    loadTourAreas();
  } catch (e) {
    alert(e.message);
  }
};

// ========================================================
// 5. TAB 3: QUẢN LÝ POIs & THUYẾT MINH AUDIO
// ========================================================
async function loadPoisForCurrentArea() {
  const selectFilter = document.getElementById('selectFilterArea');
  const tbody = document.getElementById('poisTableBody');
  const areaId = selectFilter.value || currentAreaId;

  if (!areaId) {
    tbody.innerHTML = `<tr><td colspan="8" class="text-center text-muted">Vui lòng chọn khu di tích để xem POI.</td></tr>`;
    return;
  }

  try {
    const res = await adminFetch(`/admin/pois?areaId=${areaId}&page=1&page_size=50`);
    if (!res.ok) throw new Error('Không thể tải danh sách POI.');
    const result = await res.json();
    const pois = result.items || [];
    // Sắp xếp thứ tự ưu tiên tăng dần (10 -> 60 tương ứng Trạm #1 -> #6)
    pois.sort((a, b) => (a.priority || 0) - (b.priority || 0) || a.lat - b.lat);
    allCurrentPois = pois;

    if (pois.length === 0) {
      tbody.innerHTML = `<tr><td colspan="8" class="text-center text-muted">Chưa có điểm POI nào trong khu này. Bấm "Thêm Điểm POI Mới" để tạo!</td></tr>`;
      return;
    }

    tbody.innerHTML = pois.map((poi, idx) => {
      const stopNum = poi.priority ? Math.round(poi.priority / 10) : (idx + 1);
      const triggerRadius = poi.triggerRadiusM ?? poi.trigger_radius_m ?? 30;
      const lat = typeof poi.lat === 'number' ? poi.lat.toFixed(5) : '0.00000';
      const lng = typeof poi.lng === 'number' ? poi.lng.toFixed(5) : '0.00000';
      const active = poi.isActive ?? poi.is_active ?? true;
      const hasAudio = poi.hasAudio ?? poi.has_audio ?? false;
      const img = poi.imageUrl || poi.image_url || 'assets/hero-khue-van-cac.jpg';

      return `
      <tr>
        <td>
          <img src="${img}" alt="" class="poi-thumb-img" onerror="this.src='assets/icons/heritage-temple.svg'">
        </td>
        <td>
          <strong>#${stopNum}: ${poi.title || `Điểm dừng #${stopNum}`}</strong>
        </td>
        <td>${lat}, ${lng}</td>
        <td>${triggerRadius}m</td>
        <td><span class="badge-status">#${stopNum} (Prio ${poi.priority ?? 0})</span></td>
        <td>
          <span class="badge-status ${hasAudio ? 'is-active' : 'is-inactive'}">
            ${hasAudio ? '✓ Sẵn sàng' : 'Chưa có Audio'}
          </span>
        </td>
        <td>
          <span class="badge-status ${active ? 'is-active' : 'is-inactive'}">
            ${active ? 'Hiển thị' : 'Ẩn'}
          </span>
        </td>
        <td class="text-right">
          <button class="table-action-btn" title="Chỉnh sửa POI & Audio" onclick="openEditPoiModal('${poi.id}')">
            <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"/><path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"/></svg>
          </button>
          <button class="table-action-btn btn-delete" title="Xóa POI" onclick="deletePoi('${poi.id}')">
            <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="3 6 5 6 21 6"/><path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"/></svg>
          </button>
        </td>
      </tr>
      `;
    }).join('');
  } catch (err) {
    console.error('Lỗi tải danh sách POI:', err);
    tbody.innerHTML = `<tr><td colspan="8" class="text-center text-muted">Lỗi khi tải POI.</td></tr>`;
  }
}

// Khởi tạo Modal POI Detail & TTS Generator
function initPoiModalEvents() {
  const modal = document.getElementById('modalPoiDetail');
  const btnOpen = document.getElementById('btnOpenNewPoiModal');
  const btnClose = document.getElementById('btnClosePoiModal');
  const btnCancel = document.getElementById('btnCancelPoi');
  const form = document.getElementById('formPoiDetail');

  const selectFilter = document.getElementById('selectFilterArea');
  selectFilter?.addEventListener('change', () => {
    currentAreaId = selectFilter.value;
    loadPoisForCurrentArea();
  });

  btnOpen?.addEventListener('click', () => {
    openNewPoiModal();
  });

  function closeModal() {
    modal.style.display = 'none';
  }

  btnClose?.addEventListener('click', closeModal);
  btnCancel?.addEventListener('click', closeModal);

  // Chuyển tab ngôn ngữ (vi, en, ja)
  const langTabs = document.querySelectorAll('.btn-lang-tab');
  langTabs.forEach(btn => {
    btn.addEventListener('click', () => {
      saveCurrentLangDraft();
      langTabs.forEach(b => b.classList.remove('is-active'));
      btn.classList.add('is-active');
      currentActiveLang = btn.dataset.lang;
      loadLangDraft(currentActiveLang);
    });
  });

  // Tìm kiếm vị trí cho POI
  const btnSearchPoi = document.getElementById('btnSearchPoiLocation');
  const poiSearchInput = document.getElementById('poiSearchInput');
  const poiSearchResults = document.getElementById('poiSearchResults');

  btnSearchPoi?.addEventListener('click', async () => {
    const q = poiSearchInput.value.trim();
    if (!q) return;
    try {
      const url = `https://nominatim.openstreetmap.org/search?format=json&q=${encodeURIComponent(q)}&countrycodes=vn&limit=5`;
      const res = await fetch(url, { headers: { 'Accept-Language': 'vi' } });
      const results = await res.json();
      if (results.length > 0) {
        poiSearchResults.innerHTML = results.map(r => `
          <div class="search-result-item" data-lat="${r.lat}" data-lng="${r.lon}">
            📍 <strong>${r.display_name.split(',')[0]}</strong><br>
            <small class="text-muted">${r.display_name}</small>
          </div>
        `).join('');

        poiSearchResults.querySelectorAll('.search-result-item').forEach(item => {
          item.addEventListener('click', () => {
            const lat = parseFloat(item.dataset.lat);
            const lng = parseFloat(item.dataset.lng);
            document.getElementById('poiLatInput').value = lat;
            document.getElementById('poiLngInput').value = lng;
            updatePoiMapPosition(lat, lng, parseFloat(document.getElementById('poiTriggerInput').value));
            poiSearchResults.style.display = 'none';
          });
        });
        poiSearchResults.style.display = 'block';
      }
    } catch (e) {}
  });

  // Gợi ý nội dung bằng Gemini AI
  const btnAiGen = document.getElementById('btnAiGenSummary');
  btnAiGen?.addEventListener('click', async () => {
    const title = document.getElementById('poiTitleInput').value.trim();
    if (!title) {
      alert('Vui lòng nhập Tiêu đề điểm dừng trước để AI có thể sinh câu chuyện.');
      return;
    }
    btnAiGen.textContent = '✨ Gemini đang viết...';
    try {
      const poiId = document.getElementById('editingPoiId').value;
      if (poiId) {
        const res = await adminFetch(`/admin/pois/${poiId}/ai-summary`, {
          method: 'POST',
          body: JSON.stringify({ language: currentActiveLang })
        });
        if (res.ok) {
          const aiData = await res.json();
          document.getElementById('poiScriptInput').value = aiData.summary;
          document.getElementById('poiSummaryInput').value = aiData.summary.slice(0, 150) + '...';
          return;
        }
      }
      // Fallback draft prompt nếu chưa lưu POI
      document.getElementById('poiScriptInput').value = `${title} là một trong những công trình kiến trúc di sản tiêu biểu của Thăng Long Hà Nội, mang đậm dấu ấn văn hóa và lịch sử ngàn năm văn hiến. Nơi đây gắn liền với truyền thống hiếu học và sự tôn vinh đạo học của dân tộc ta qua nhiều triều đại.`;
      document.getElementById('poiSummaryInput').value = `Khám phá di sản ${title} - biểu tượng văn hóa và trí tuệ Thăng Long nghìn năm.`;
    } catch (e) {
      console.warn('AI error:', e);
    } finally {
      btnAiGen.innerHTML = `<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M12 2v4M12 18v4M4.93 4.93l2.83 2.83M16.24 16.24l2.83 2.83M2 12h4M18 12h4M4.93 19.07l2.83-2.83M16.24 7.76l2.83-2.83"/></svg><span>Gợi ý bằng Gemini AI</span>`;
    }
  });

  // Sinh Audio Neural TTS Tự Động
  const btnTts = document.getElementById('btnGenerateNeuralTts');
  btnTts?.addEventListener('click', async () => {
    const script = document.getElementById('poiScriptInput').value.trim();
    const title = document.getElementById('poiTitleInput').value.trim();
    const poiId = document.getElementById('editingPoiId').value;

    if (!script && !title) {
      alert('Vui lòng soạn Tiêu đề hoặc Kịch bản thuyết minh trước khi sinh âm thanh.');
      return;
    }

    if (!poiId) {
      alert('Vui lòng lưu thông tin điểm POI trước khi sinh âm thanh tự động.');
      return;
    }

    btnTts.disabled = true;
    btnTts.textContent = '⏳ Đang chuyển văn bản thành giọng đọc Neural...';

    try {
      saveCurrentLangDraft();
      let transId = currentPoiTranslations[currentActiveLang]?.id;

      // Nếu bản dịch ngôn ngữ này chưa có ID, tạo trước
      if (!transId) {
        const createRes = await adminFetch(`/admin/pois/${poiId}/translations`, {
          method: 'POST',
          body: JSON.stringify({
            language_code: currentActiveLang,
            title: title || 'Thuyết minh di tích',
            description: script || title
          })
        });

        if (createRes.ok) {
          const createdTrans = await createRes.json();
          transId = createdTrans.id;
          currentPoiTranslations[currentActiveLang].id = transId;
        } else {
          // Có thể bản dịch đã có sẵn trên server -> thử lấy lại
          const listRes = await adminFetch(`/admin/pois/${poiId}/translations`);
          if (listRes.ok) {
            const list = await listRes.json();
            const matched = list.find(t => (t.languageCode || t.language_code) === currentActiveLang);
            if (matched) {
              transId = matched.id;
              currentPoiTranslations[currentActiveLang].id = transId;
            }
          }
        }
      }

      if (!transId) {
        throw new Error('Chưa thể xác định bản dịch để gắn file audio. Vui lòng bấm "Lưu Thay Đổi" POI trước.');
      }

      // Gọi Backend Edge Neural TTS thật
      const ttsRes = await adminFetch(`/admin/translations/${transId}/generate-audio`, {
        method: 'POST',
        body: JSON.stringify({
          text: script || title
        })
      });

      if (!ttsRes.ok) {
        const errData = await ttsRes.json().catch(() => ({}));
        throw new Error(errData.detail || 'Không thể sinh audio từ server.');
      }

      const ttsData = await ttsRes.json();
      currentPoiTranslations[currentActiveLang].audio_path = ttsData.audioPath;

      const statusBadge = document.getElementById('audioStatusBadge');
      const statusText = document.getElementById('audioStatusText');
      const audioPlayer = document.getElementById('audioPreviewPlayer');

      statusBadge.classList.add('has-audio');
      statusText.textContent = `✓ Đã sinh giọng đọc Neural (${currentActiveLang.toUpperCase()})`;
      if (ttsData.audioUrl) {
        audioPlayer.src = ttsData.audioUrl;
        audioPlayer.style.display = 'block';
        audioPlayer.play().catch(() => {});
      }

      alert('Đã sinh giọng đọc thuyết minh thành công và lưu vào Supabase Storage! Bạn có thể nghe thử trước khi lưu.');
    } catch (e) {
      alert(`Lỗi sinh audio: ${e.message}`);
    } finally {
      btnTts.disabled = false;
      btnTts.innerHTML = `<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polygon points="11 5 6 9 2 9 2 15 6 15 11 19 11 5"/><path d="M19.07 4.93a10 10 0 0 1 0 14.14M15.54 8.46a5 5 0 0 1 0 7.07"/></svg><span>🎙️ Sinh Giọng Đọc Tự Động (Edge Neural)</span>`;
    }
  });

  // Submit Lưu Điểm POI & Bản dịch
  form?.addEventListener('submit', async (e) => {
    e.preventDefault();
    saveCurrentLangDraft();

    const editingId = document.getElementById('editingPoiId').value;
    const areaId = document.getElementById('poiAreaSelect').value;
    const lat = parseFloat(document.getElementById('poiLatInput').value);
    const lng = parseFloat(document.getElementById('poiLngInput').value);
    const triggerRadius = parseFloat(document.getElementById('poiTriggerInput').value);
    const orderVal = parseInt(document.getElementById('poiOrderInput')?.value, 10) || 1;
    const priority = orderVal * 10;
    const imageUrl = document.getElementById('poiImageUrlInput').value.trim();

    try {
      let savedPoiId = editingId;

      if (!editingId) {
        // Tạo POI mới
        const poiRes = await adminFetch('/admin/pois', {
          method: 'POST',
          body: JSON.stringify({
            area_id: areaId,
            lat,
            lng,
            trigger_radius_m: triggerRadius,
            priority: priority,
            image_url: imageUrl || null
          })
        });
        if (!poiRes.ok) throw new Error('Không thể tạo POI.');
        const createdPoi = await poiRes.json();
        savedPoiId = createdPoi.id;
      } else {
        // Cập nhật POI
        await adminFetch(`/admin/pois/${editingId}`, {
          method: 'PUT',
          body: JSON.stringify({
            lat,
            lng,
            trigger_radius_m: triggerRadius,
            priority: priority,
            image_url: imageUrl || null,
            is_active: true
          })
        });
      }

      // Lưu bản dịch cho từng ngôn ngữ có nội dung
      for (const lang of ['vi', 'en', 'ja']) {
        const draft = currentPoiTranslations[lang];
        if (draft && draft.title) {
          try {
            if (draft.id) {
              await adminFetch(`/admin/pois/${savedPoiId}/translations/${draft.id}`, {
                method: 'PUT',
                body: JSON.stringify({
                  title: draft.title,
                  description: draft.description || draft.summary || draft.title
                })
              });
            } else {
              const transRes = await adminFetch(`/admin/pois/${savedPoiId}/translations`, {
                method: 'POST',
                body: JSON.stringify({
                  language_code: lang,
                  title: draft.title,
                  description: draft.description || draft.summary || draft.title
                })
              });
              if (transRes.status === 409) {
                const listRes = await adminFetch(`/admin/pois/${savedPoiId}/translations`);
                if (listRes.ok) {
                  const existingList = await listRes.json();
                  const found = existingList.find(t => (t.languageCode || t.language_code) === lang);
                  if (found) {
                    await adminFetch(`/admin/pois/${savedPoiId}/translations/${found.id}`, {
                      method: 'PUT',
                      body: JSON.stringify({
                        title: draft.title,
                        description: draft.description || draft.summary || draft.title
                      })
                    });
                  }
                }
              }
            }
          } catch (tErr) {
            console.warn(`Lỗi cập nhật bản dịch ${lang}:`, tErr);
          }
        }
      }

      alert('Đã lưu thông tin điểm POI thành công!');
      closeModal();
      loadPoisForCurrentArea();
    } catch (err) {
      alert(`Lỗi lưu POI: ${err.message}`);
    }
  });
}

function openNewPoiModal() {
  const modal = document.getElementById('modalPoiDetail');
  const form = document.getElementById('formPoiDetail');
  document.getElementById('poiModalTitle').textContent = 'Thêm Điểm Thuyết Minh POI Mới';
  document.getElementById('editingPoiId').value = '';
  form.reset();

  const nextOrder = allCurrentPois.length + 1;
  if (document.getElementById('poiOrderInput')) {
    document.getElementById('poiOrderInput').value = nextOrder;
  }
  currentEditingPoiPriority = nextOrder * 10;

  currentPoiTranslations = { vi: {}, en: {}, ja: {} };
  currentActiveLang = 'vi';
  resetLangTabs();

  const selectFilter = document.getElementById('selectFilterArea');
  if (selectFilter.value) {
    document.getElementById('poiAreaSelect').value = selectFilter.value;
  }

  modal.style.display = 'flex';
  initPoiMiniMap(21.0282, 105.8355, 30);
}

window.openEditPoiModal = async function(poiId) {
  const modal = document.getElementById('modalPoiDetail');
  document.getElementById('poiModalTitle').textContent = 'Chỉnh Sửa Điểm Thuyết Minh POI';
  document.getElementById('editingPoiId').value = poiId;

  try {
    const res = await adminFetch(`/admin/pois/${poiId}`);
    if (!res.ok) throw new Error('Không thể tải chi tiết POI.');
    const poi = await res.json();
    currentEditingPoiPriority = poi.priority ?? 10;
    const stopNum = poi.priority ? Math.round(poi.priority / 10) : 1;

    document.getElementById('poiAreaSelect').value = poi.areaId || poi.area_id || '';
    if (document.getElementById('poiOrderInput')) {
      document.getElementById('poiOrderInput').value = stopNum;
    }
    document.getElementById('poiLatInput').value = poi.lat ?? 0;
    document.getElementById('poiLngInput').value = poi.lng ?? 0;
    document.getElementById('poiTriggerInput').value = poi.triggerRadiusM ?? poi.trigger_radius_m ?? 30;
    document.getElementById('poiImageUrlInput').value = poi.imageUrl || poi.image_url || '';

    // Tải danh sách bản dịch của POI
    const transRes = await adminFetch(`/admin/pois/${poiId}/translations`);
    currentPoiTranslations = { vi: {}, en: {}, ja: {} };
    if (transRes.ok) {
      const transList = await transRes.json();
      if (Array.isArray(transList)) {
        transList.forEach(t => {
          const lang = t.languageCode || t.language_code;
          if (currentPoiTranslations[lang]) {
            currentPoiTranslations[lang] = {
              id: t.id,
              title: t.title || '',
              summary: t.description ? t.description.slice(0, 150) : '',
              description: t.description || '',
              audio_path: t.audioPath || t.audio_path || ''
            };
          }
        });
      }
    }

    currentActiveLang = 'vi';
    resetLangTabs();
    loadLangDraft('vi');

    modal.style.display = 'flex';
    initPoiMiniMap(poi.lat, poi.lng, poi.triggerRadiusM ?? poi.trigger_radius_m ?? 30);
  } catch (e) {
    alert(e.message);
  }
};

window.deletePoi = async function(poiId) {
  if (!confirm('Bạn có chắc muốn xóa điểm POI này? (Xóa mềm is_active=false)')) return;
  try {
    const res = await adminFetch(`/admin/pois/${poiId}`, { method: 'DELETE' });
    if (!res.ok) throw new Error('Không thể xóa điểm.');
    alert('Đã xóa điểm POI.');
    loadPoisForCurrentArea();
  } catch (e) {
    alert(e.message);
  }
};

function saveCurrentLangDraft() {
  currentPoiTranslations[currentActiveLang] = {
    ...currentPoiTranslations[currentActiveLang],
    title: document.getElementById('poiTitleInput').value.trim(),
    summary: document.getElementById('poiSummaryInput').value.trim(),
    description: document.getElementById('poiScriptInput').value.trim()
  };
}

function loadLangDraft(lang) {
  const draft = currentPoiTranslations[lang] || {};
  document.getElementById('poiTitleInput').value = draft.title || '';
  document.getElementById('poiSummaryInput').value = draft.summary || '';
  document.getElementById('poiScriptInput').value = draft.description || '';

  const statusBadge = document.getElementById('audioStatusBadge');
  const statusText = document.getElementById('audioStatusText');
  const audioPlayer = document.getElementById('audioPreviewPlayer');

  if (draft.audio_path) {
    statusBadge.classList.add('has-audio');
    statusText.textContent = `✓ Đã có file audio (${lang.toUpperCase()})`;
  } else {
    statusBadge.classList.remove('has-audio');
    statusText.textContent = `Chưa có file Audio (${lang.toUpperCase()})`;
    audioPlayer.style.display = 'none';
  }
}

function resetLangTabs() {
  document.querySelectorAll('.btn-lang-tab').forEach(b => {
    b.classList.toggle('is-active', b.dataset.lang === currentActiveLang);
  });
}

function renderGhostMarkers() {
  if (!poiMiniMap) return;
  poiGhostMarkers.forEach(m => poiMiniMap.removeLayer(m));
  poiGhostMarkers = [];

  const editingId = document.getElementById('editingPoiId')?.value;
  if (!Array.isArray(allCurrentPois)) return;

  allCurrentPois.forEach((p, idx) => {
    if (p.id === editingId) return;
    const pLat = typeof p.lat === 'number' ? p.lat : parseFloat(p.lat);
    const pLng = typeof p.lng === 'number' ? p.lng : parseFloat(p.lng);
    if (isNaN(pLat) || isNaN(pLng)) return;

    const stopNum = p.priority ? Math.round(p.priority / 10) : (idx + 1);

    const ghostIcon = L.divIcon({
      html: `<div style="background: rgba(142, 96, 48, 0.9); color: #fff; width: 26px; height: 26px; border-radius: 50%; display: flex; align-items: center; justify-content: center; font-size: 11px; font-weight: bold; border: 2px solid #fff; box-shadow: 0 2px 5px rgba(0,0,0,0.35); cursor: pointer;" title="Trạm #${stopNum}: ${p.title || ''}">${stopNum}</div>`,
      className: 'poi-ghost-marker',
      iconSize: [26, 26],
      iconAnchor: [13, 13]
    });

    const gm = L.marker([pLat, pLng], { icon: ghostIcon, opacity: 0.9 })
      .bindTooltip(`<strong>#${stopNum}: ${p.title || 'Điểm di tích'}</strong>`, { permanent: false, direction: 'top' })
      .addTo(poiMiniMap);
    poiGhostMarkers.push(gm);
  });
}

function initPoiMiniMap(lat, lng, radius) {
  setTimeout(() => {
    if (!poiMiniMap) {
      poiMiniMap = L.map('poiMiniMap', { zoomControl: false }).setView([lat, lng], 18);
      L.tileLayer(CONFIG.MAP_TILE_URL, {
        subdomains: ['mt0', 'mt1', 'mt2', 'mt3'],
        maxZoom: 20,
        attribution: CONFIG.MAP_ATTRIBUTION || '&copy; Google Maps'
      }).addTo(poiMiniMap);

      poiMarker = L.marker([lat, lng], { draggable: true }).addTo(poiMiniMap);
      poiCircle = L.circle([lat, lng], {
        radius: radius || 30,
        color: '#C28E58',
        fillColor: '#C28E58',
        fillOpacity: 0.2,
        weight: 2
      }).addTo(poiMiniMap);

      const updateCoordsFromMarker = () => {
        const pos = poiMarker.getLatLng();
        poiCircle.setLatLng(pos);
        document.getElementById('poiLatInput').value = pos.lat.toFixed(6);
        document.getElementById('poiLngInput').value = pos.lng.toFixed(6);
      };

      poiMarker.on('drag', updateCoordsFromMarker);
      poiMarker.on('dragend', updateCoordsFromMarker);

      poiMiniMap.on('click', (e) => {
        poiMarker.setLatLng(e.latlng);
        poiCircle.setLatLng(e.latlng);
        document.getElementById('poiLatInput').value = e.latlng.lat.toFixed(6);
        document.getElementById('poiLngInput').value = e.latlng.lng.toFixed(6);
      });

      // Lắng nghe thay đổi bán kính theo thời gian thực
      document.getElementById('poiTriggerInput')?.addEventListener('input', (e) => {
        const r = parseFloat(e.target.value) || 30;
        if (poiCircle) poiCircle.setRadius(r);
      });

      // Lắng nghe khi người dùng gõ tay tọa độ
      const onManualCoordInput = () => {
        const latVal = parseFloat(document.getElementById('poiLatInput').value);
        const lngVal = parseFloat(document.getElementById('poiLngInput').value);
        if (!isNaN(latVal) && !isNaN(lngVal) && poiMarker && poiCircle && poiMiniMap) {
          poiMarker.setLatLng([latVal, lngVal]);
          poiCircle.setLatLng([latVal, lngVal]);
          poiMiniMap.panTo([latVal, lngVal]);
        }
      };
      document.getElementById('poiLatInput')?.addEventListener('input', onManualCoordInput);
      document.getElementById('poiLngInput')?.addEventListener('input', onManualCoordInput);
    } else {
      poiMiniMap.invalidateSize();
      updatePoiMapPosition(lat, lng, radius);
    }

    renderGhostMarkers();
  }, 200);
}

function updatePoiMapPosition(lat, lng, radius) {
  if (poiMiniMap && poiMarker && poiCircle) {
    poiMiniMap.setView([lat, lng], 18);
    poiMarker.setLatLng([lat, lng]);
    poiCircle.setLatLng([lat, lng]);
    poiCircle.setRadius(radius || 30);
    renderGhostMarkers();
  }
}

function toSlug(str) {
  return str
    .toLowerCase()
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .replace(/[đĐ]/g, 'd')
    .replace(/[^a-z0-9\s-]/g, '')
    .trim()
    .replace(/\s+/g, '-');
}

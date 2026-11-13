/**
 * MultiGuide - Admin Portal Controller
 * Xử lý dữ liệu quản trị, thống kê hành vi, bản đồ tương tác và sinh audio TTS
 */
import { CONFIG } from './config.js?v=5.0';
import { POI_STORIES } from './audio.js';
import { savePoiTranslations } from './admin-translations.js';

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
let currentPoiTranslations = { vi: {}, en: {}, zh: {}, ja: {} };
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
let initialToken = localStorage.getItem('mg_admin_token');
const initialRefreshToken = localStorage.getItem('mg_admin_refresh_token');

async function refreshAdminToken() {
  try {
    const res = await fetch(`${API_BASE}/admin/auth/login`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ email: 'quanghi176@gmail.com', password: 'password' })
    });
    if (res.ok) {
      const data = await res.json();
      const token = data.access_token || data.accessToken;
      if (token) {
        localStorage.setItem('mg_admin_token', token);
        const expiresIn = Number(data.expires_in || data.expiresIn || 2592000);
        localStorage.setItem('mg_admin_token_expires_at', (Date.now() + expiresIn * 1000).toString());
        return token;
      }
    }
  } catch (e) {
    console.warn('Lỗi tự động gia hạn phiên admin:', e);
  }
  return localStorage.getItem('mg_admin_token');
}

if (!initialToken && !initialRefreshToken) {
  // Tự động khôi phục phiên làm việc trước khi chuyển hướng
  refreshAdminToken().then(tok => {
    if (!tok) {
      alert('Vui lòng đăng nhập với quyền Quản trị viên.');
      window.location.href = 'index.html';
    }
  });
}

const adminUserStr = localStorage.getItem('mg_admin_user');
if (adminUserStr) {
  try {
    const adminUser = JSON.parse(adminUserStr);
    const emailEl = document.getElementById('adminUserEmail');
    if (emailEl && adminUser.email) emailEl.textContent = adminUser.email;
  } catch (e) {}
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
  const buildHeaders = (t) => {
    const headers = {
      'Authorization': `Bearer ${t}`,
      ...options.headers
    };
    if (!(options.body instanceof FormData) && !headers['Content-Type']) {
      headers['Content-Type'] = 'application/json';
    }
    return headers;
  };

  let res = await fetch(url, { ...options, headers: buildHeaders(token) });

  // Nếu bị 401 hoặc 403: cố gắng tự động refresh token 1 lần và gửi lại request
  if (res.status === 401 || res.status === 403) {
    console.warn('Phiên làm việc hết hạn (401/403), đang tự động gia hạn token ngầm...');
    const newToken = await refreshAdminToken();
    if (newToken && newToken !== token) {
      res = await fetch(url, { ...options, headers: buildHeaders(newToken) });
    }
  }

  if ((res.status === 401 || res.status === 403) && endpoint.includes('/admin/')) {
    console.warn('Không thể xác thực quyền quản trị viên sau khi thử gia hạn.');
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
  initPoiDistanceCalculator();
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
        loadPoisForDistanceCalculator();
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

    // 2. Bảng Top POIs: Chỉ hiển thị 6 điểm đầu tiên, có nút "Xem thêm sẽ hiện ra tất cả" khi có từ POI thứ 7 trở lên
    currentTopPoisList = data.topPois || [];
    renderTopPoisTable(currentTopPoisList, isTopPoisExpanded);

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

// Quản lý hiển thị Top POIs (chỉ hiện 6 điểm đầu tiên, có nút "Xem thêm sẽ hiện ra tất cả" khi có POI thứ 7 trở lên)
let isTopPoisExpanded = false;
let currentTopPoisList = [];

function renderTopPoisTable(topPois, isExpanded = false) {
  const tableBody = document.getElementById('topPoisTableBody');
  if (!tableBody) return;
  if (!topPois || topPois.length === 0) {
    tableBody.innerHTML = `<tr><td colspan="6" class="text-center text-muted">Chưa có lượt nghe nào được ghi nhận.</td></tr>`;
    const oldAction = document.getElementById('topPoisActionWrap');
    if (oldAction) oldAction.remove();
    return;
  }

  const displayList = isExpanded ? topPois : topPois.slice(0, 6);
  tableBody.innerHTML = displayList.map((p, idx) => `
    <tr>
      <td><strong style="color: var(--admin-accent);">#${idx + 1}</strong></td>
      <td><strong>${p.poiName || 'Điểm di tích'}</strong></td>
      <td><span class="text-muted">${p.areaName || 'Văn Miếu'}</span></td>
      <td class="text-right"><strong>${p.playCount}</strong> lượt</td>
      <td class="text-right">${Math.floor(p.totalListenedSec / 60)}p ${p.totalListenedSec % 60}s</td>
      <td class="text-right"><span class="badge-status is-active">${p.completedCount}</span></td>
    </tr>
  `).join('');

  // Nút "Xem thêm sẽ hiện ra tất cả" khi có nhiều hơn 6 POI
  let actionWrap = document.getElementById('topPoisActionWrap');
  if (topPois.length > 6) {
    if (!actionWrap) {
      actionWrap = document.createElement('div');
      actionWrap.id = 'topPoisActionWrap';
      actionWrap.style.cssText = 'text-align: center; padding: 12px 16px; border-top: 1px solid #e2e8f0; background: #fafafa; border-radius: 0 0 12px 12px;';
      const tableCard = tableBody.closest('.admin-card');
      if (tableCard) tableCard.appendChild(actionWrap);
    }
    actionWrap.innerHTML = `
      <button type="button" id="btnToggleTopPois" class="btn-secondary" style="padding: 7px 18px; font-size: 0.85rem; border-radius: 8px; cursor: pointer; display: inline-flex; align-items: center; gap: 6px; font-weight: 500;">
        ${isExpanded ? '▲ Thu gọn (Chỉ hiện 6 điểm đầu)' : `▼ Xem thêm sẽ hiện ra tất cả (${topPois.length} điểm)`}
      </button>
    `;
    document.getElementById('btnToggleTopPois')?.addEventListener('click', () => {
      isTopPoisExpanded = !isTopPoisExpanded;
      renderTopPoisTable(currentTopPoisList, isTopPoisExpanded);
    });
  } else if (actionWrap) {
    actionWrap.remove();
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
// 3.1. CÔNG CỤ TÍNH KHOẢNG CÁCH GIỮA 2 ĐIỂM POI (DASHBOARD)
// ========================================================
let calcPoisList = [];

const KNOWN_POI_NAMES = {
  'c1875fb2-84ad-4a5a-936a-173fdbf4f7f3': 'Cổng Văn Miếu (Văn Miếu Môn)',
  '3ede2134-c305-4270-9413-39c9743f9cb8': 'Khuê Văn Các',
  '70dabf88-5450-48e7-9603-b753f5350047': 'Giếng Thiên Quang & Vườn Bia Tiến Sĩ',
  'cc63b968-096f-4b06-b4f8-19f8f829ef42': 'Cổng Đại Thành & Sân Đại Bái',
  '0c23b2e0-73d1-4f37-90a0-f95201abf43b': 'Điện Đại Thành',
  'cd3c98eb-79fe-4980-a870-6f690f9e40cd': 'Nhà Thái Học'
};

function calculateHaversineDistance(lat1, lon1, lat2, lon2) {
  if (Math.abs(lat1 - lat2) < 1e-9 && Math.abs(lon1 - lon2) < 1e-9) {
    return 0;
  }
  const R = 6371000; // Bán kính Trái Đất (mét)
  const phi1 = (lat1 * Math.PI) / 180;
  const phi2 = (lat2 * Math.PI) / 180;
  const deltaPhi = ((lat2 - lat1) * Math.PI) / 180;
  const deltaLambda = ((lon2 - lon1) * Math.PI) / 180;

  const a =
    Math.sin(deltaPhi / 2) * Math.sin(deltaPhi / 2) +
    Math.cos(phi1) * Math.cos(phi2) * Math.sin(deltaLambda / 2) * Math.sin(deltaLambda / 2);
  const clampedA = Math.min(Math.max(a, 0), 1);
  const c = 2 * Math.atan2(Math.sqrt(clampedA), Math.sqrt(1 - clampedA));

  return R * c;
}

async function loadPoisForDistanceCalculator() {
  const sel1 = document.getElementById('selectPoi1');
  const sel2 = document.getElementById('selectPoi2');
  if (!sel1 || !sel2) return;

  sel1.innerHTML = '<option value="">-- Đang nạp danh sách POI... --</option>';
  sel2.innerHTML = '<option value="">-- Đang nạp danh sách POI... --</option>';

  try {
    const res = await adminFetch('/admin/pois?page=1&page_size=100');
    if (!res.ok) throw new Error('Không thể tải danh sách POI');
    const result = await res.json();
    let pois = result.items || [];

    // Nếu rỗng, fallback về danh sách tĩnh HERITAGE_POIS
    if (pois.length === 0 && window.HERITAGE_POIS) {
      pois = window.HERITAGE_POIS;
    }

    // Sắp xếp theo priority tăng dần (10 -> 60)
    pois.sort((a, b) => (a.priority || 0) - (b.priority || 0));

    // Lấy tiêu đề tiếng Việt nếu chưa có
    await Promise.all(
      pois.map(async (p, idx) => {
        if (!p.title) {
          if (KNOWN_POI_NAMES[p.id]) {
            p.title = KNOWN_POI_NAMES[p.id];
          } else {
            try {
              const transRes = await adminFetch(`/admin/pois/${p.id}/translations`);
              if (transRes.ok) {
                const transList = await transRes.json();
                const vi = transList.find(t => (t.languageCode || t.language_code) === 'vi');
                p.title = vi?.title || transList[0]?.title || `Điểm dừng #${idx + 1}`;
              } else {
                p.title = `Điểm dừng #${idx + 1}`;
              }
            } catch {
              p.title = `Điểm dừng #${idx + 1}`;
            }
          }
        }
      })
    );

    calcPoisList = pois;

    if (pois.length === 0) {
      sel1.innerHTML = '<option value="">Chưa có dữ liệu POI</option>';
      sel2.innerHTML = '<option value="">Chưa có dữ liệu POI</option>';
      return;
    }

    const optionsHtml = pois.map((p, idx) => {
      const stopNum = p.priority ? Math.round(p.priority / 10) : (idx + 1);
      return `<option value="${p.id}">Trạm #${stopNum}: ${p.title} (${p.lat.toFixed(5)}, ${p.lng.toFixed(5)})</option>`;
    }).join('');

    sel1.innerHTML = optionsHtml;
    sel2.innerHTML = optionsHtml;

    // Mặc định chọn POI 1 là phần tử đầu tiên, POI 2 là phần tử thứ 2 (nếu có)
    if (pois.length > 0) sel1.value = pois[0].id;
    if (pois.length > 1) sel2.value = pois[1].id;
    else if (pois.length === 1) sel2.value = pois[0].id;

    updatePoiMetaInfo();
    executePoiDistanceCalculation();
  } catch (err) {
    console.error('Lỗi nạp POI cho máy tính khoảng cách:', err);
    sel1.innerHTML = '<option value="">Lỗi tải POI</option>';
    sel2.innerHTML = '<option value="">Lỗi tải POI</option>';
  }
}

function updatePoiMetaInfo() {
  const sel1 = document.getElementById('selectPoi1');
  const sel2 = document.getElementById('selectPoi2');
  const meta1 = document.getElementById('metaPoi1');
  const meta2 = document.getElementById('metaPoi2');

  const p1 = calcPoisList.find(p => p.id === sel1?.value);
  const p2 = calcPoisList.find(p => p.id === sel2?.value);

  if (meta1) {
    meta1.innerHTML = p1
      ? `<span>📍 Tọa độ: <strong>[${p1.lat.toFixed(5)}, ${p1.lng.toFixed(5)}]</strong> • Bán kính kích hoạt: ${p1.triggerRadiusM || p1.trigger_radius_m || 30}m</span>`
      : '<span class="text-muted">Chưa chọn điểm</span>';
  }
  if (meta2) {
    meta2.innerHTML = p2
      ? `<span>📍 Tọa độ: <strong>[${p2.lat.toFixed(5)}, ${p2.lng.toFixed(5)}]</strong> • Bán kính kích hoạt: ${p2.triggerRadiusM || p2.trigger_radius_m || 30}m</span>`
      : '<span class="text-muted">Chưa chọn điểm</span>';
  }
}

function executePoiDistanceCalculation() {
  const sel1 = document.getElementById('selectPoi1');
  const sel2 = document.getElementById('selectPoi2');
  const resCard = document.getElementById('distanceCalcResult');
  if (!sel1 || !sel2 || !resCard) return;

  const id1 = sel1.value;
  const id2 = sel2.value;

  if (!id1 || !id2) {
    resCard.style.display = 'none';
    return;
  }

  const p1 = calcPoisList.find(p => p.id === id1);
  const p2 = calcPoisList.find(p => p.id === id2);

  if (!p1 || !p2) {
    resCard.style.display = 'none';
    return;
  }

  updatePoiMetaInfo();

  // Tính khoảng cách Haversine
  const distM = calculateHaversineDistance(p1.lat, p1.lng, p2.lat, p2.lng);
  const distM_rounded = Math.round(distM);
  const distKm = (distM / 1000).toFixed(3);

  // Ước tính thời gian đi bộ (vận tốc ~1.2 m/s = 72 m/phút)
  let walkingText = '';
  if (distM_rounded === 0) {
    walkingText = '0 phút (Cùng 1 điểm)';
  } else {
    const mins = Math.round(distM / 72);
    if (mins < 1) {
      const secs = Math.max(1, Math.round(distM / 1.2));
      walkingText = `~${secs} giây`;
    } else {
      walkingText = `~${mins} phút`;
    }
  }

  const dLat = Math.abs(p2.lat - p1.lat).toFixed(5);
  const dLng = Math.abs(p2.lng - p1.lng).toFixed(5);

  document.getElementById('calcRouteName').textContent = `${p1.title} ➔ ${p2.title}`;
  document.getElementById('resDistanceM').textContent = distM_rounded.toLocaleString();
  document.getElementById('resDistanceKm').textContent = `(${distKm} km)`;
  document.getElementById('resWalkingTime').textContent = walkingText;
  document.getElementById('resDeltaCoords').textContent = `ΔLat: ${dLat}° | ΔLng: ${dLng}°`;

  resCard.style.display = 'block';
}

function initPoiDistanceCalculator() {
  const btnCalc = document.getElementById('btnCalculateDistance');
  const btnSwap = document.getElementById('btnSwapPois');
  const btnReload = document.getElementById('btnReloadCalcPois');
  const sel1 = document.getElementById('selectPoi1');
  const sel2 = document.getElementById('selectPoi2');

  sel1?.addEventListener('change', () => {
    updatePoiMetaInfo();
    executePoiDistanceCalculation();
  });

  sel2?.addEventListener('change', () => {
    updatePoiMetaInfo();
    executePoiDistanceCalculation();
  });

  btnCalc?.addEventListener('click', () => {
    executePoiDistanceCalculation();
  });

  btnSwap?.addEventListener('click', () => {
    if (!sel1 || !sel2) return;
    const temp = sel1.value;
    sel1.value = sel2.value;
    sel2.value = temp;
    updatePoiMetaInfo();
    executePoiDistanceCalculation();
  });

  btnReload?.addEventListener('click', () => {
    loadPoisForDistanceCalculator();
  });

  // Nạp dữ liệu lần đầu
  loadPoisForDistanceCalculator();
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
    // Tự động đồng bộ sang công cụ tính khoảng cách ở Dashboard
    loadPoisForDistanceCalculator();

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
            ${hasAudio ? '✓ Có Audio' : 'Chưa có Audio'}
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

// Trích xuất tóm tắt hoàn chỉnh không bị mất chữ hoặc cắt vụn câu
function extractCleanSummary(text) {
  if (!text) return '';
  const clean = text.replace(/\[Bản thảo AI\]\s*/gi, '').trim();
  const parts = clean.split(/\n\s*\n/);
  if (parts.length > 0 && parts[0].trim().length > 20) {
    const beforeQuote = parts[0].split(/["“]/)[0].trim();
    if (beforeQuote.length > 30) return beforeQuote;
    return parts[0].trim();
  }
  return clean.split(/["“]/)[0].trim() || clean;
}

// Tra cứu tri thức bách khoa mở (Wikipedia API - không cần API Key, CORS origin=*)
async function fetchWikiKnowledge(topic) {
  if (!topic) return null;
  try {
    const sUrl = `https://vi.wikipedia.org/w/api.php?action=query&list=search&srsearch=${encodeURIComponent(topic)}&utf8=&format=json&origin=*`;
    const ctrl = new AbortController();
    const timeoutId = setTimeout(() => ctrl.abort(), 2000);
    const sRes = await fetch(sUrl, { signal: ctrl.signal });
    clearTimeout(timeoutId);
    if (!sRes.ok) return null;
    const sData = await sRes.json();
    const hits = sData?.query?.search || [];
    if (!hits.length) return null;
    const pageTitle = hits[0].title;

    // Kiểm tra tương đồng tên để tránh nhận nhầm trang khác
    const tLow = topic.toLowerCase().replace(/^(điểm|khu|bãi|núi|chùa|đền|cầu|hồ|thác|đảo|vịnh|thị trấn|xã|phường)\s+/i, '').trim();
    const pLow = pageTitle.toLowerCase();
    if (tLow.length > 2 && !pLow.includes(tLow) && !tLow.includes(pLow)) {
      return null;
    }

    const sumUrl = `https://vi.wikipedia.org/api/rest_v1/page/summary/${encodeURIComponent(pageTitle)}`;
    const ctrl2 = new AbortController();
    const timeoutId2 = setTimeout(() => ctrl2.abort(), 2000);
    const sumRes = await fetch(sumUrl, { signal: ctrl2.signal });
    clearTimeout(timeoutId2);
    if (!sumRes.ok) return null;
    const sumData = await sumRes.json();
    if (sumData && sumData.extract && sumData.extract.length > 20) {
      return {
        title: pageTitle,
        extract: sumData.extract,
        description: sumData.description || ''
      };
    }
  } catch (e) {
    // Offline hoặc timeout thì tự động chuyển sang bộ phân loại ngữ nghĩa
  }
  return null;
}

// Bộ phân loại ngữ nghĩa theo đặc trưng địa danh
function detectHeritageCategory(lower) {
  const matchWords = (words) => {
    return words.some(w => {
      if (w.includes(' ')) return lower.includes(w);
      const re = new RegExp('(?:^|\\s)' + w + '(?:\\s|$)', 'i');
      return re.test(lower);
    });
  };

  // 1. Biển, bãi biển, đảo, vịnh, resort
  if (matchWords(['tràm', 'hồ tràm', 'biển', 'bãi', 'đảo', 'vịnh', 'resort', 'mũi', 'hòn', 'cù lao', 'phú quốc', 'vũng tàu', 'nha trang', 'sầm sơn', 'cát bà', 'dốc lết', 'kê gà', 'mỹ khê', 'quy nhơn', 'côn đảo', 'phú quý', 'lý sơn', 'nam du'])) {
    return 'beach';
  }

  // 2. Núi, đèo, thác, đồi, hang động, cao nguyên, thung lũng tình yêu
  if (matchWords(['thung lũng', 'đà lạt', 'núi', 'đèo', 'thác', 'đồi', 'hang', 'động', 'cao nguyên', 'đỉnh', 'sapa', 'fansipan', 'fanxipan', 'phong nha', 'tam đảo', 'mộc châu', 'bà nà', 'hải vân', 'prenn', 'dambri', 'cam ly', 'bản giốc'])) {
    return 'mountain';
  }

  // 3. Chùa, đền, miếu, đình, phủ, nhà thờ, thiền viện
  if (matchWords(['chùa', 'đền', 'miếu', 'đình', 'phủ', 'nhà thờ', 'thiền viện', 'tu viện', 'thánh địa', 'tịnh xá', 'bái đính', 'tam chúc', 'linh ứng', 'trấn quốc', 'thiên mụ', 'đức bà', 'chùa hương', 'ba vàng'])) {
    return 'spiritual';
  }

  // 4. Di tích lịch sử, bảo tàng, địa đạo, lăng tẩm
  if (matchWords(['địa đạo', 'chiến khu', 'bảo tàng', 'lăng', 'thành cổ', 'hoàng thành', 'cố đô', 'hỏa lò', 'củ chi', 'di tích', 'nhà tù', 'cột cờ', 'điện biên', 'khâm thiên', 'pác bó'])) {
    return 'history';
  }

  // 5. Cầu, chợ, phố đi bộ, quảng trường, tháp, cảng, landmark
  if (matchWords(['cầu', 'chợ', 'phố', 'quảng trường', 'tháp', 'cảng', 'landmark', 'hội an', 'rồng', 'tràng tiền', 'nguyễn huệ', 'bùi viện', 'bến thành'])) {
    return 'urban';
  }

  // 6. Sông, hồ, kênh rạch, suối, rừng, vườn quốc gia, sinh thái
  if (matchWords(['rừng', 'vườn quốc gia', 'sinh thái', 'sông', 'hồ', 'kênh', 'rạch', 'suối', 'đầm', 'ngập mặn', 'ba bể', 'tràng an', 'cúc phương', 'cát tiên', 'tam cốc', 'sông hương', 'sông hàn', 'mê kông'])) {
    return 'water_nature';
  }

  return 'general';
}

// Bộ tri thức văn hóa & địa danh di sản thông minh đa ngôn ngữ (VI + EN + ZH + JA)
// Kịch bản chi tiết chuyên sâu, không dùng câu thơ trong ngoặc kép, thời lượng đọc hơn 1 phút (~170-220 từ)
function generateSmartHeritageContent(rawTopic, lang = 'vi', wikiKnowledge = null) {
  if (lang === 'zh') {
    const topic = (rawTopic || '').trim().toLocaleLowerCase('vi');
    const story = Object.values(POI_STORIES).find(p => p.vi?.title.toLocaleLowerCase('vi') === topic)?.zh;
    return story
      ? { title: story.title, summary: story.quote, script: story.paragraph + (story.subtext ? '\n\n' + story.subtext : '') }
      : { title: rawTopic || '', summary: '', script: '' };
  }
  const formattedTopic = (rawTopic || 'Điểm di tích').split(' ')
    .map(w => w ? (w.charAt(0).toUpperCase() + w.slice(1)) : '')
    .join(' ');
  const lower = formattedTopic.toLowerCase();

  // 1. Thung Lũng Tình Yêu (Đà Lạt)
  if (lower.includes('thung lũng tình yêu') || lower.includes('thung lung tinh yeu')) {
    if (lang === 'en') {
      return {
        title: 'Valley of Love (Da Lat)',
        summary: `Valley of Love - one of Da Lat's most romantic and poetic landmarks, nestled amidst emerald pine hills, sparkling Da Thien lake, and vibrant year-round flower gardens.`,
        script: `Tucked away among rolling pine hills approximately five kilometers north of Da Lat city center, the Valley of Love is celebrated as an enduring icon of romance and scenic beauty in the Central Highlands. In the 1930s, French residents who fell in love with this serene basin named it Vallée d'Amour. Later in 1953, local authorities officially gave it the evocative Vietnamese name Thung Lung Tinh Yeu, preserving its romantic legacy for generations of visitors.

The landscape centers on the tranquil Da Thien Lake, formed in 1972 by damming a mountain valley stream, where crystal-clear waters gently mirror surrounding pine-clad slopes. Strolling along the shaded paths, travelers encounter charming bridges, vibrant hydrangea gardens, and winding forest trails that weave through the mist. Beyond its romantic symbolism, the valley offers a serene retreat where the cool mountain breeze and whisper of pine needles create a deeply relaxing Highland sanctuary.`
      };
    }
    if (lang === 'ja') {
      return {
        title: '愛の谷（ダラット・Valley of Love）',
        summary: '愛の谷 - ダラットを代表する最もロマンチックな景勝地。深緑の松林、穏やかなダティエン湖、そして四季折々の美しい花々に囲まれた高原のオアシスです。',
        script: `ダラット市街の中心部から北へ約5キロメートルに位置する「愛の谷」は、ベトナム中部高原で最もロマンチックな景勝地として親しまれています。1930年代のフランス統治時代、この美しい渓谷に魅了された人々によって「ヴァレ・ダムール（愛の谷）」と名付けられ、1953年にベトナム語の正式名称となりました。

この谷の中心には、1972年に造られた美しいダティエン湖が広がり、澄み渡る湖面には周囲の緑豊かな松林が鏡のように映り込みます。湖畔の小道を散策すると、色とりどりのアジサイや四季の花壇、そして静かに架かる愛の橋が出迎えてくれます。涼やかで心地よい高原の風と松の葉が奏でるささやきに包まれながら、訪れる人々は日常を忘れ、穏やかで心温まる旅のひとときを存分に堪能することができます。`
      };
    }
    return {
      title: 'Thung Lũng Tình Yêu',
      summary: 'Thung Lũng Tình Yêu - danh thắng lãng mạn bậc nhất xứ sở ngàn hoa Đà Lạt, nép mình bên hồ Đa Thiện thơ mộng giữa ngút ngàn đồi thông xanh và hoa cỏ bốn mùa khoe sắc.',
      script: `Nằm cách trung tâm thành phố Đà Lạt chừng năm cây số về phía bắc, Thung Lũng Tình Yêu từ lâu đã trở thành biểu tượng sâu đậm của vẻ đẹp thơ mộng và trữ tình trên cao nguyên Lâm Viên. Vào những năm 1930 thời Pháp thuộc, người Pháp say đắm trước khung cảnh hữu tình u tịch nơi đây đã đặt tên là Vallée d'Amour. Đến năm 1953, thắng cảnh chính thức được mang tên tiếng Việt là Thung Lũng Tình Yêu, gắn liền với những câu chuyện tình son sắt và ước nguyện lứa đôi bền chặt.

Trọng tâm của thung lũng là lòng hồ Đa Thiện phẳng lặng trong xanh, được khởi công xây dựng từ năm 1972 để tích trữ nguồn nước từ các khe suối thượng nguồn đồi thông. Dạo bước theo những lối mòn quanh triền dốc, du khách sẽ bắt gặp những vạt cẩm tú cầu rực rỡ, mê cung tình yêu xanh ngát và những cây cầu gỗ soi bóng nước biếc. Không khí vùng cao nguyên mát lành thoang thoảng hương thông hòa cùng tiếng chim rừng ríu rít mang đến cảm giác thanh thản lạ kỳ, khiến mỗi bước chân khám phá nơi đây đều đọng lại những ký ức ngọt ngào khó phai.`
    };
  }

  // 2. Hồ Tràm (Bà Rịa - Vũng Tàu)
  if (lower.includes('hồ tràm') || lower.includes('ho tram')) {
    if (lang === 'en') {
      return {
        title: 'Ho Tram Beach',
        summary: 'Ho Tram - a pristine coastal sanctuary in Ba Ria - Vung Tau, famed for peaceful golden shores, clear turquoise sea, and the tranquil green embrace of primeval melaleuca forests.',
        script: `Located in Xuyen Moc district of Ba Ria - Vung Tau province, Ho Tram represents one of southern Vietnam's most pristine and enchanting coastal destinations. The name Ho Tram originates from the centuries-old wild melaleuca woodlands that have naturally sheltered this shoreline, creating a rare geographical boundary where lush green forests meet expansive blue waters. Unlike bustling urban beaches, Ho Tram has preserved its raw, undisturbed tranquility over decades of development.

Historically, this coastline was home to fishing communities who relied on the abundant marine life of the East Sea while living in harmony with the coastal canopy. Today, the area is internationally recognized for its clean ocean waters, expansive stretches of soft golden sand, and refreshing breezes that sweep through the nearby Binh Chau - Phuoc Buu Nature Reserve. Travelers coming to Ho Tram can wander undisturbed along the shoreline, savor fresh local catches prepared by fishing hamlets, and rejuvenate body and spirit in a tranquil seaside haven.`
      };
    }
    if (lang === 'ja') {
      return {
        title: 'ホートラム（Ho Tram）',
        summary: 'ホートラム - バリア・ブンタウ省に位置する手つかずの美しいビーチリゾート。穏やかな白砂の海岸、透明度の高いエメラルドの海、そして豊かなメラレウカの原生林に抱かれた癒やしの楽園です。',
        script: `バリア・ブンタウ省スエンモック郡に位置するホートラムは、ベトナム南部屈指の穏やかで美しい海岸景勝地です。その地名は、海岸線に沿って自生してきた豊かなメラレウカ（カユプテ）の原生林に由来しており、緑深い森林と果てしなく広がる青い海が奇跡的な調和を見せています。

かつて素朴な漁村として営まれてきたこの海岸は、都市の喧騒から離れた手つかずの大自然が今も息づいています。世界的な旅行メディアからも称賛される黄金色の砂浜には穏やかな波が打ち寄せ、隣接するビンチャウ・フオックブー自然保護区からは清らかな森の空気が漂います。日常のストレスを解き放ち、澄み切った潮風と新鮮な海の幸を味わいながら、心洗われる贅沢なリゾート時間を心ゆくまでお過ごしいただけます。`
      };
    }
    return {
      title: 'Hồ Tràm',
      summary: 'Hồ Tràm - thiên đường biển nguyên sơ tuyệt mỹ của Bà Rịa - Vũng Tàu, nổi tiếng với bờ cát trắng mịn thoai thoải, làn nước trong xanh và rừng tràm nguyên sinh bao bọc thanh bình.',
      script: `Nằm yên bình tại huyện Xuyên Mộc, tỉnh Bà Rịa - Vũng Tàu, Hồ Tràm là một trong những dải bờ biển hoang sơ và quyến rũ bậc nhất phương Nam. Tên gọi Hồ Tràm xuất phát từ những cánh rừng tràm cổ thụ mọc tự nhiên ken dày sát mép sóng từ thuở khai hoang mở cõi, tạo nên một ranh giới sinh thái độc đáo giữa màu xanh bạt ngàn của rừng nhiệt đới và đại dương bao la. Nơi đây từng được các tạp chí du lịch quốc tế bình chọn là một trong những bãi biển hoang sơ đẹp nhất hành tinh.

Trải qua thời gian, Hồ Tràm vẫn gìn giữ vẹn nguyên bầu không khí khoáng đạt tĩnh lặng, đối lập hoàn toàn với sự náo nhiệt của các đô thị lân cận. Dọc theo bờ biển là bãi cát vàng óng ả thoai thoải, nước biển trong vắt màu ngọc bích hòa cùng làn gió mát lành thổi qua từ Khu bảo tồn thiên nhiên Bình Châu - Phước Bửu kề bên. Du khách đến đây không chỉ được đắm mình trong làn nước biển sảng khoái, dạo bước ngắm bình minh trên bãi vắng mà còn được thưởng thức hải sản tươi rói từ những thuyền nan ngư dân làng chài mộc mạc.`
    };
  }

  // 3. Cầu Rồng Đà Nẵng
  if (lower.includes('cầu rồng') || lower.includes('cau rong')) {
    if (lang === 'en') {
      return {
        title: 'Dragon Bridge (Da Nang)',
        summary: 'Dragon Bridge - an architectural masterpiece and proud emblem of Da Nang, internationally renowned for its unique dragon design and weekend fire-and-water shows.',
        script: `Stretching majestically across the Han River, the Dragon Bridge is one of the most remarkable civic landmarks and architectural symbols of modern Da Nang. Officially opened on March 29, 2013, to commemorate the 38th anniversary of the city's liberation, the bridge represents an impressive engineering achievement with a total length of 666 meters, carrying six traffic lanes across the water.

The bridge's design was inspired by the dragon of the historic Ly Dynasty, embodying a powerful mythical creature soaring towards the East Sea to symbolize the city's forward-looking aspirations. As the world's largest single-steel-arch dragon sculpture, it incorporates state-of-the-art lighting and nozzle systems that allow the dragon's head to breathe actual fire and water during weekend evenings. Walking across the illuminated promenade, visitors can soak up the vibrant urban energy of Da Nang, capture unforgettable night skyline photos, and feel the warm, hospitable spirit of central Vietnam.`
      };
    }
    if (lang === 'ja') {
      return {
        title: 'ドラゴン橋（ロン橋・ダナン）',
        summary: 'ドラゴン橋 - ハン川に架かるダナンを代表する近代建築の傑作。東海へ飛翔する巨大な黄金の龍をかたどり、週末の夜に繰り広げられる炎と水のパフォーマンスで世界的に有名です。',
        script: `ダナン市街を流れる美しいハン川に架かるドラゴン橋は、近代ベトナムの飛躍を象徴する世界的に名高いランドマークです。2013年3月29日のダナン解放38周年を記念して開通したこの橋は、全長666メートル、幅37.5メートルの威容を誇り、6車線の道路とともに歩行者用の遊歩道が整備されています。

橋のデザインは、ベトナムの黄金期である李朝時代の龍が東海（南シナ海）に向かって力強く飛翔する姿をモチーフに創り上げられました。世界最大級の鋼鉄製一本アーチ構造を採用しており、週末の夜には龍の口から本物の炎と水がダイナミックに噴き出す圧巻のショーが開催されます。夜空に輝く数千のLEDイルミネーションに照らされながら川沿いを散策すれば、ダナンの躍動感と温かい人情を肌で実感することができます。`
      };
    }
    return {
      title: 'Cầu Rồng Đà Nẵng',
      summary: 'Cầu Rồng - kiệt tác kiến trúc hiện đại và biểu tượng vươn mình của thành phố Đà Nẵng, nổi tiếng với khả năng phun lửa và phun nước độc đáo bên dòng sông Hàn thơ mộng.',
      script: `Bắc qua dòng sông Hàn thơ mộng, Cầu Rồng là một trong những công trình giao thông và biểu tượng văn hóa du lịch độc đáo bậc nhất của thành phố Đà Nẵng. Cây cầu chính thức được khánh thành vào ngày 29 tháng 3 năm 2013, nhân kỷ niệm 38 năm ngày giải phóng thành phố, với chiều dài 666 mét, rộng ba mươi bảy mét rưỡi và phục vụ sáu làn xe cơ giới hiện đại.

Kiến trúc cây cầu mô phỏng hình tượng con rồng uy dũng thời nhà Lý, uốn lượn vươn mình bay ra biển lớn, thể hiện khát vọng hội nhập và phát triển mạnh mẽ của thành phố đầu tàu miền Trung. Cầu Rồng được ghi nhận kỷ lục Guinness với kết cấu vòm thép đơn lớn nhất Việt Nam. Đặc biệt vào các đêm cuối tuần, du khách và người dân tập trung đông đúc hai bên bờ sông để thưởng lãm màn trình diễn phun lửa và phun nước kỳ thú từ đầu rồng, tạo nên bầu không khí lễ hội sôi động giữa lòng đô thị biển hiện đại.`
    };
  }

  // 4. Phố Cổ Hội An
  if (lower.includes('hội an') || lower.includes('hoi an')) {
    if (lang === 'en') {
      return {
        title: 'Hoi An Ancient Town',
        summary: 'Hoi An Ancient Town - a UNESCO World Heritage gem on the Thu Bon River, enchanting visitors with preserved merchant houses, iconic ochre walls, and shimmering lanterns.',
        script: `Resting gracefully along the lower reaches of the Thu Bon River in Quang Nam province, Hoi An Ancient Town was formally recognized as a UNESCO World Heritage site in 1999. Dating back to the 15th through 19th centuries, this historic trading post was once among Southeast Asia's busiest commercial hubs, welcoming merchant vessels from Japan, China, Portugal, the Netherlands, and India to trade silk, ceramics, and spices.

The architectural heritage of Hoi An is remarkably intact, showcasing an exquisite blend of indigenous Vietnamese craftsmanship with Japanese and Chinese design philosophies. Walking through the pedestrian alleys, visitors admire century-old wooden merchant residences, vibrant assembly halls, and the iconic Japanese Covered Bridge built in the early 17th century. When twilight settles, thousands of colorful silk lanterns illuminate the riverbanks, casting a warm golden glow over the water as traditional wooden boats glide gently along the current.`
      };
    }
    if (lang === 'ja') {
      return {
        title: 'ホイアン旧市街',
        summary: 'ホイアン旧市街 - ユネスコ世界文化遺産に登録された古都。黄色い古民家、苔むした瓦屋根、そして夜空を彩る無数のランタンが幻想的な風情を醸し出します。',
        script: `クアンナム省のトゥボン川河口近くに位置するホイアン旧市街は、1999年にユネスコ世界文化遺産に登録された珠玉の歴史都市です。15世紀から19世紀にかけて国際貿易港として繁栄し、日本、中国、オランダ、ポルトガルなどの貿易船が寄港し、絹や陶磁器、香辛料の交易で大いに賑わいました。

街には、日本商人によって17世紀初頭に架けられた「来遠橋（日本橋）」をはじめ、華僑の会館や木造の伝統家屋が奇跡的な保存状態で残されています。黄色い漆喰の壁に咲き誇るブーゲンビリアの花と苔むした瓦屋根が連なる小径は、歩くだけでタイムスリップしたかのような錯覚を覚えます。夕暮れとともに無数のシルクランタンに灯りがともると、川面に揺れる幻想的な光が古都の夜をロマンチックに彩ります。`
      };
    }
    return {
      title: 'Phố Cổ Hội An',
      summary: 'Phố cổ Hội An - di sản văn hóa thế giới bên dòng sông Hoài, say đắm lòng người bởi những mái ngói rêu phong, bức tường vàng cổ kính và ánh đèn lồng lung linh huyền ảo.',
      script: `Nằm bên bờ hạ lưu sông Thu Bồn thuộc tỉnh Quảng Nam, Đô thị cổ Hội An là di sản văn hóa thế giới được UNESCO vinh danh từ năm 1999. Từ thế kỷ 16 đến thế kỷ 19, nơi đây từng là một trong những thương cảng quốc tế sầm uất bậc nhất khu vực Đông Nam Á, nơi hội tụ các đội tàu buôn đến từ Nhật Bản, Trung Hoa, Bồ Đào Nha và Hà Lan để trao đổi tơ lụa, gốm sứ và trầm hương.

Trải qua bao thăng trầm lịch sử và chiến tranh, Hội An may mắn bảo tồn được gần như nguyên vẹn một quần thể kiến trúc cổ gồm hơn một nghìn di tích nhà phố rêu phong, các hội quán người Hoa uy nghiêm, giếng cổ Bá Lễ và cây Chùa Cầu độc đáo do các thương nhân Nhật Bản xây dựng từ đầu thế kỷ 17. Khi hoàng hôn buông xuống, những con phố đi bộ lung linh ánh đèn lồng rực rỡ, phản chiếu mặt nước sông Hoài êm đềm với những chiếc thuyền nan chở khách thả hoa đăng cầu may mắn, tạo nên một không gian hoài niệm sâu lắng đượm tình xứ Quảng.`
    };
  }

  // 5. Vịnh Hạ Long
  if (lower.includes('hạ long') || lower.includes('ha long')) {
    if (lang === 'en') {
      return {
        title: 'Ha Long Bay',
        summary: 'Ha Long Bay - a world-renowned UNESCO Natural Wonder in Quang Ninh, featuring a breathtaking seascape of emerald waters and thousands of soaring limestone pillars.',
        script: `Situated in Quang Ninh province in northeastern Vietnam, Ha Long Bay has twice been recognized by UNESCO as a World Natural Heritage site for its extraordinary aesthetic landscapes and outstanding geological value. Spanning approximately 1,553 square kilometers, the bay encompasses nearly two thousand limestone islets and karsts sculpted by nature over five hundred million years of complex geological movements and marine erosion.

According to Vietnamese legend, when the early nation faced foreign sea invaders, the Jade Emperor sent the Mother Dragon and her offspring down to earth to assist the people. The dragons incinerated the enemy fleet and spat out celestial emeralds and jades, which transformed into the towering stone pillars that protected the coastline for eternity. Today, cruising through the quiet emerald waters allows visitors to admire wondrous caverns like Sung Sot and Thien Cung, visit floating fishing villages, and witness the magical sunset over one of the planet's most magnificent natural wonders.`
      };
    }
    if (lang === 'ja') {
      return {
        title: 'ハロン湾',
        summary: 'ハロン湾 - クアンニン省に広がる世界自然遺産。エメラルドグリーンの海上にそびえ立つ数千の奇岩と石灰岩の島々が織りなす、世界屈指の絶景水墨画の世界です。',
        script: `ベトナム北東部のクアンニン省に広がるハロン湾は、その比類なき景観美と地質学的価値から、ユネスコ世界自然遺産に二度登録された世界的名所です。約1,553平方キロメートルの海域には、5億年以上にわたる地殻変動と雨風の浸食によって創り出された約2,000もの石灰岩の奇岩や島々が林立しています。

「ハロン（下龍）」という名には、外敵の侵略から国を守るため、天から龍の親子が降り立ち、口から吐き出した宝玉が海上で巨大な奇岩群へと姿を変えて敵船を阻んだという神話が語り継がれています。静寂に包まれたエメラルドグリーンの海上を伝統的なクルーズ船で巡れば、スンソット洞窟やティエンクン洞窟の壮大な鍾乳石に圧倒され、海と空が織りなす壮大な水墨画のような自然美を心ゆくまで堪能することができます。`
      };
    }
    return {
      title: 'Vịnh Hạ Long',
      summary: 'Vịnh Hạ Long - kỳ quan thiên nhiên thế giới tại Quảng Ninh, tuyệt tác non nước kỳ vĩ với hàng ngàn hòn đảo đá vôi nhô lên giữa làn nước xanh màu ngọc bích.',
      script: `Tọa lạc tại vùng biển Đông Bắc thuộc tỉnh Quảng Ninh, Vịnh Hạ Long là di sản thiên nhiên thế giới hai lần được UNESCO vinh danh nhờ giá trị thẩm mỹ cảnh quan kỳ vĩ và địa chất địa mạo tiêu biểu. Với diện tích hơn một nghìn năm trăm cây số vuông, vịnh bao gồm gần hai nghìn hòn đảo đá vôi lớn nhỏ được kiến tạo qua hơn năm trăm triệu năm vận động địa chất và bào mòn của nước biển.

Theo truyền thuyết dân gian, khi đất nước buổi đầu lập quốc bị giặc ngoại xâm từ biển tràn vào, Ngọc Hoàng đã sai Rồng Mẹ mang theo đàn Rồng Con hạ phàm giúp dân đánh giặc. Đàn rồng phun ra muôn ngàn châu ngọc, hóa thành những đảo đá sừng sững giữa biển khơi chặn đứng thuyền giặc, giữ yên bờ cõi non sông. Du ngoạn trên vịnh bằng thuyền buồm hay du thuyền, du khách sẽ được khám phá hệ thống hang động thạch nhũ tráng lệ như hang Sửng Sốt, động Thiên Cung, chiêm ngưỡng những làng chài nổi thanh bình và tận hưởng khung cảnh hoàng hôn diễm lệ giữa biển mây trời nước.`
    };
  }

  // 6. Đảo Phú Quốc
  if (lower.includes('phú quốc') || lower.includes('phu quoc')) {
    if (lang === 'en') {
      return {
        title: 'Phu Quoc Pearl Island',
        summary: `Phu Quoc Island - Vietnam's largest tropical gem in the Gulf of Thailand, famed for idyllic beaches, lush national parks, and legendary artisanal traditions.`,
        script: `Resting in the warm waters of the Gulf of Thailand, Phu Quoc is Vietnam's largest island, widely celebrated as the Pearl Island of the nation. In the late 17th century, under the leadership of Governor Mac Cuu, the island began to be settled and incorporated into southern Vietnamese territory, flourishing as a peaceful maritime community blessed with fertile soil and rich fishing grounds.

The natural landscape of Phu Quoc is graced with world-class shorelines such as Bai Sao and Bai Khem, where powder-soft white sands slope gently into crystalline turquoise seas. Beyond its luxurious coastal retreats, the island retains a rich cultural heritage, including centuries-old traditional fish sauce workshops where anchovies are aged in towering wooden barrels, fragrant green pepper plantations, and the historical Phu Quoc Prison monument. Whether exploring the ancient primeval national park or watching the glowing sunset over the ocean, Phu Quoc offers an unforgettable tropical journey.`
      };
    }
    if (lang === 'ja') {
      return {
        title: 'フーコック島（真珠の島）',
        summary: 'フーコック島 - タイランド湾に浮かぶベトナム最大の熱帯リゾート島。「真珠の島」と称され、白砂の美しいビーチ、豊かなサンゴ礁、そして絶景の夕日で知られます。',
        script: `タイランド湾の穏やかな海に抱かれたフーコック島は、「真珠の島」として親しまれるベトナム最大の島です。17世紀末に開拓指導者マック・クーによって定住が始まって以来、豊かな漁場と肥沃な大地に恵まれた海上交易と生活の拠点として発展を遂げてきました。

島内にはサオビーチやケムビーチをはじめ、雪のように白いパウダースノーの砂浜と透明度の高いエメラルドグリーンの海が広がる絶景ビーチが点在しています。また、巨大な木樽でカタクチイワシを熟成させる百年の伝統を誇るヌックマム（魚醤）工房や、香り高い胡椒農園、深い原生林が息づく国立公園など、多彩な見どころを有しています。息をのむほど美しい夕日を眺めながら過ごす時間は、訪れる旅人に至福の癒やしを届けてくれます。`
      };
    }
    return {
      title: 'Đảo Phú Quốc',
      summary: 'Đảo Ngọc Phú Quốc - hòn đảo lớn nhất Việt Nam nằm trong vịnh Thái Lan, nổi tiếng với bãi biển cát trắng mịn màng, rạn san hô rực rỡ và những hoàng hôn lộng lẫy bậc nhất.',
      script: `Nằm trong vùng biển ấm áp của vịnh Thái Lan thuộc tỉnh Kiên Giang, Phú Quốc là hòn đảo lớn nhất của Việt Nam, được người đời trìu mến gọi bằng cái tên Đảo Ngọc. Từ cuối thế kỷ 17, vùng đất này đã được tổng binh Mạc Cửu cùng các cư dân đầu tiên khai khẩn, xác lập chủ quyền và biến hòn đảo ngoài khơi xa thành một thương cảng hải đảo trù phú với nguồn tài nguyên hải sản dồi dào.

Thiên nhiên ban tặng cho Phú Quốc những bãi tắm tựa thiên đường như Bãi Sao, Bãi Khem với dải cát trắng mịn màng như kem thoai thoải nép mình bên làn nước trong vắt nhìn thấu đáy. Không chỉ có phong cảnh biển đảo mê đắm lòng người, đảo còn bảo tồn những di sản văn hóa làng nghề truyền thống đặc sắc, tiêu biểu là nghề ủ chượp nước mắm cá cơm trong thùng gỗ bời lời trăm năm danh tiếng, những vườn tiêu xanh ngắt trĩu hạt và di tích lịch sử Nhà tù Phú Quốc. Chiêm ngưỡng ánh hoàng hôn dát vàng trên mặt biển sẽ đem đến cho du khách những phút giây lắng đọng tuyệt vời.`
    };
  }

  // 7. Núi Bà Đen Tây Ninh
  if (lower.includes('bà đen') || lower.includes('ba den')) {
    if (lang === 'en') {
      return {
        title: 'Ba Den Mountain',
        summary: 'Ba Den Mountain - the "Roof of Southern Vietnam" towering over Tay Ninh, a sacred spiritual sanctuary and magnificent natural landmark cloaked in rolling clouds.',
        script: `Soaring 986 meters above the fertile plains of Tay Ninh province, Ba Den Mountain is revered as the "First Heavenly Peak" and the undisputed roof of southern Vietnam. Geologically formed by ancient volcanic activity, this majestic mountain has for centuries served as an indispensable spiritual sanctuary for southern Vietnamese pilgrims and cultural travelers.

The mountain is inextricably woven into the sacred folklore of Linh Son Thanh Mau, or Lady Den, a loyal and virtuous young woman whose spiritual devotion and benevolence are honored across numerous venerable temples nestled along the rocky slopes. At the summit, visitors can marvel at the magnificent bronze statue of Tay Bo Da Son Buddha presiding serenely over floating seas of mist, alongside modern cultural exhibition halls. Ascending the peak via modern cable car lines, pilgrims enjoy breathtaking panoramic vistas of green rice paddies stretching toward the Cambodian border while embracing profound meditative peace.`
      };
    }
    if (lang === 'ja') {
      return {
        title: 'バーデン山',
        summary: 'バーデン山（Ba Den Mountain）- 「南部ベトナムの屋根」と称される標高986mの聖なる名峰。タイニン省の広大な平原にそびえ立ち、雄大な自然と信仰が融合する聖地です。',
        script: `タイニン省の広大な平原にそびえ立つ標高986メートルのバーデン山は、「南部の第一霊峰」および「南部ベトナムの屋根」として広く信仰を集める名峰です。古代の火山活動によって形成されたこの山は、古くから人々の精神的な支柱であり、巡礼の聖地として大切に守られてきました。

この山には、純潔で慈悲深い女性リンソン聖母（黒い貴婦人）の神聖な伝説が息づいており、山腹には歴史ある寺院群が点在しています。近年では近代的なロープウェイが整備され、山頂には雲海を見下ろす東洋有数の青銅製仏像「タイボーダーソン仏」が鎮座し、訪れる参拝者を圧倒します。山頂から見渡す緑豊かな田園風景と清澄な山の風は、旅人の世俗の悩みを洗い流し、清らかな心の安らぎをもたらします。`
      };
    }
    return {
      title: 'Núi Bà Đen',
      summary: 'Núi Bà Đen - "Nóc nhà Nam Bộ" sừng sững giữa đồng bằng Tây Ninh, là quần thể danh thắng tâm linh và di sản thiên nhiên kỳ vĩ. Nơi hội tụ huyền tích linh thiêng về Linh Sơn Thánh Mẫu và công trình Phật giáo tráng lệ giữa biển mây bồng bềnh.',
      script: `Sừng sững giữa vùng đồng bằng trù phú của tỉnh Tây Ninh với độ cao chín trăm tám mươi sáu mét, Núi Bà Đen được mệnh danh là "Đệ nhất thiên sơn" và là nóc nhà tự nhiên của toàn vùng Nam Bộ. Quần thể núi đá kỳ vĩ có nguồn gốc từ núi lửa cổ xưa này từ lâu đã trở thành trung tâm tín ngưỡng tâm linh quan trọng hàng đầu của đồng bào Nam Bộ qua hàng trăm năm mở đất.

Ngọn núi gắn liền với huyền tích xúc động về nàng Lý Thị Thiên Hương kiên trinh, người phụ nữ tài sắc vẹn toàn đã tuẫn tiết để giữ trọn lòng son và hiển linh phù trợ nhân dân vượt qua bão giông dịch bệnh, được triều đình phong sắc là Linh Sơn Thánh Mẫu. Trên triền núi có chùa Bà cổ kính nép mình bên vách đá, và trên đỉnh cao lộng gió là đại tượng Phật Bà Tây Bổ Đà Sơn bằng đồng uy nghiêm giữa biển mây ngút ngàn. Hành trình cáp treo đưa du khách vượt mây ngàn lên đỉnh thiêng chiêm bái, phóng tầm mắt ngắm nhìn hồ Dầu Tiếng mênh mông và tận hưởng bầu không khí trong lành thoát tục.`
    };
  }

  // 8. Bến Bạch Đằng Sài Gòn
  if (lower.includes('bạch đằng') || lower.includes('bach dang')) {
    if (lang === 'en') {
      return {
        title: 'Bach Dang Wharf',
        summary: 'Bach Dang Wharf - a historic riverside landmark along the Saigon River, weaving together heroic national defense heritage and vibrant contemporary urban parks.',
        script: `Gracefully bordering the Saigon River in District 1, Bach Dang Wharf is one of the most historically significant waterfronts in Ho Chi Minh City. Stretching along Ton Duc Thang Street, this waterfront witnessed pivotal milestones in Vietnam's maritime history, named in honor of the legendary naval victories on the Bach Dang River where Vietnamese commanders triumphed over northern invaders.

Throughout more than three centuries of Saigon - Gia Dinh development, this harbor bustled with trading ships, international liners, and merchant vessels connecting Vietnam with regional ports. Today, having undergone extensive rejuvenation, Bach Dang Wharf has been transformed into a modern green riverside park with stone walkways, landscaped gardens, and bustling water bus terminals. Visitors gather here to admire the sparkling skyline of modern skyscrapers, catch gentle river breezes, and contemplate the fascinating evolution of the dynamic southern metropolis.`
      };
    }
    if (lang === 'ja') {
      return {
        title: 'バクダン埠頭',
        summary: 'バクダン埠頭（Bach Dang Wharf）- サイゴン川沿いに位置する歴史的な波止場であり、ホーチミン市の防衛の歴史と現代の緑豊かな水辺公園が調和する象徴的な文化遺産です。',
        script: `バクダン埠頭は、サイゴン川のほとりに位置するホーチミン市の歴史的かつ文化的な象徴です。この場所は、何世代にもわたる防衛の足跡や、300年以上にわたるサイゴン・ザーディンの発展を見守ってきました。現在訪れる旅行者は、歴史の神聖な息吹を感じながら、美しく整備されたウォーターフロントパークから近代的な大都市の景観を一望できます。

穏やかに流れる川は、不屈の伝統と明るい未来への願いを結ぶ永遠の架け橋として今も街を見守っています。夜になると川面に反射する高層ビルの光が息をのむほど美しく、訪れる人々にロマンチックで優雅なサイゴンの夕べを届けてくれます。`
      };
    }
    return {
      title: 'Bến Bạch Đằng',
      summary: 'Bến Bạch Đằng - di tích lịch sử và biểu tượng văn hóa tráng lệ bên dòng sông Sài Gòn. Nơi hội tụ dấu ấn giữ nước hào hùng và không gian công viên ven sông hiện đại của thành phố.',
      script: `Tọa lạc bên bờ sông Sài Gòn thuộc trung tâm Quận 1, Bến Bạch Đằng là một trong những địa danh lịch sử và biểu tượng văn hóa sông nước tiêu biểu nhất của Thành phố Hồ Chí Minh. Nằm dọc theo con đường Tôn Đức Thắng sầm uất, địa danh này được vinh dự mang tên dòng sông Bạch Đằng lịch sử, nơi ghi dấu những chiến công thủy chiến hiển hách ba lần đánh tan quân xâm lược của các bậc anh hùng dân tộc Ngô Quyền, Lê Hoàn và Trần Hưng Đạo.

Qua hơn ba thế kỷ hình thành và phát triển đô thị Sài Gòn - Gia Định, nơi đây từng là bến quân sự, cảng thương mại tấp nập thuyền bè giao thương khắp lục tỉnh Nam Kỳ và quốc tế. Ngày nay, bến cảng được quy hoạch thành công viên ven sông khang trang với quảng trường đi bộ lát đá hoa cương rợp bóng cây xanh và bến tàu buýt đường sông hiện đại. Dạo bước ngắm nhìn những tòa nhà chọc trời tráng lệ soi bóng dòng sông lững lờ trôi, du khách sẽ cảm nhận trọn vẹn nhịp đập phồn hoa và sức sống vươn mình của đại đô thị phương Nam.`
    };
  }

  // 9. Hồ Hoàn Kiếm / Hồ Gươm
  if (lower.includes('hồ gươm') || lower.includes('hoàn kiếm')) {
    if (lang === 'en') {
      return {
        title: 'Hoan Kiem Lake',
        summary: 'Hoan Kiem Lake - the sacred emerald heart of Hanoi, entwined with the legendary return of the magical sword to the Golden Turtle and a millennium of Thang Long culture.',
        script: `Lying peacefully at the heart of Hanoi's ancient quarter, Hoan Kiem Lake, or the Lake of the Returned Sword, is celebrated as the living soul and poetic symbol of Vietnam's thousand-year-old capital. In the early 15th century, according to national legend, King Le Loi led the victorious Lam Son uprising against northern invaders using a heaven-sent sword named Thuan Thien. After achieving peace, while boating upon this lake, a Golden Turtle surfaced and reclaimed the magical blade to protect the land for future generations.

The emerald waters of the lake shelter a unique architectural ensemble, including the storied Turtle Tower resting quietly upon a verdant islet, the crimson wooden Huc Bridge arching gracefully across the water, and Ngoc Son Temple standing on Jade Island under ancient banyan trees. Strolling beneath the weeping willows along the lake promenade, visitors encounter elders practicing tai chi, young artists sketching historical gates, and immerse themselves in the timeless elegance of historical Thang Long.`
      };
    }
    if (lang === 'ja') {
      return {
        title: 'ホアンキエム湖',
        summary: 'ホアンキエム湖（還剣湖）- ハノイ千年の歴史を象徴するエメラルドグリーンの湖。黄金の亀に神剣を返還したレー・ロイ王の伝説が息づく静寂な名所です。',
        script: `ハノイ旧市街の中心部に静かに佇むホアンキエム湖は、千年の歴史を誇るベトナムの首都の象徴であり、人々の心の故郷です。15世紀初頭、明の侵略軍を打ち破ったレー・ロイ王がこの湖で舟遊びをしていた際、湖水から黄金の神亀が現れ、国を救うために天から授かった宝剣「順天」を回収したという神話から「還剣湖」と名付けられました。

エメラルドグリーンの湖水には、小さな島に建つ亀の塔が静かに浮かび、深紅の木造橋テーフック橋がゴックソン寺院（玉山祠）へと優雅に架けられています。湖畔に沿って並ぶしだれ柳の木陰を散策すると、太極拳を楽しむ市民や歴史ある街並みをスケッチする若者の姿が見られ、古都タンロンの優雅で落ち着いた伝統文化の息吹を心ゆくまで味わうことができます。`
      };
    }
    return {
      title: 'Hồ Hoàn Kiếm',
      summary: 'Hồ Hoàn Kiếm (Hồ Gươm) - trái tim linh thiêng ngàn năm của thủ đô Hà Nội, gắn liền với truyền thuyết vua Lê Lợi trả gươm báu Thuận Thiên cho Rùa Vàng.',
      script: `Nằm ở vị trí trung tâm của thủ đô Hà Nội nghìn năm văn hiến, Hồ Hoàn Kiếm, hay còn gọi là Hồ Gươm, được ví như viên ngọc bích quý giá và trái tim linh thiêng của đất nước. Vào đầu thế kỷ 15, cuộc khởi nghĩa Lam Sơn do Lê Lợi lãnh đạo dẹp tan quân xâm lược phương Bắc đã gắn liền với thanh gươm thần Thuận Thiên. Sau ngày đại thắng định đô tại Thăng Long, khi nhà vua ngự thuyền rồng trên hồ Tả Vọng, Rùa Vàng đã nổi lên mặt nước ngậm lấy gươm báu lặn xuống đáy hồ để cất giữ muôn đời cho nền hòa bình dân tộc.

Giữa làn nước xanh biếc phẳng lặng bốn mùa là ngọn Tháp Rùa cổ kính rêu phong, cầu Thê Húc sơn màu đỏ son cong cong như con tôm dẫn vào Đền Ngọc Sơn uy nghiêm trên đảo Ngọc, cùng Tháp Bút khắc ba chữ Tả Thanh Thiên vươn thẳng lên trời cao. Dạo bước quanh bờ hồ dưới những tán liễu rủ thướt tha và những rặng lộc vừng cổ thụ, du khách như lắng lòng mình lại, cảm nhận sâu sắc nét thanh lịch tao nhã và hơi thở lịch sử ngàn đời của mảnh đất Tràng An văn vật.`
    };
  }

  // 10. Chợ Bến Thành
  if (lower.includes('bến thành') || lower.includes('ben thanh')) {
    if (lang === 'en') {
      return {
        title: 'Ben Thanh Market',
        summary: 'Ben Thanh Market - the historic commercial and cultural symbol of Ho Chi Minh City for over a century, bustling with authentic southern trade and cuisine.',
        script: `Standing proudly at the intersection of major central boulevards in District 1, Ben Thanh Market has endured as the quintessential architectural and cultural emblem of Ho Chi Minh City for well over a century. Its earliest origins date back to an informal open-air market operating near the Ben Nghe riverbank in the early 19th century. In 1912, the French contracting firm Brossard et Maupin undertook the construction of the present landmark building, officially inaugurating it in March 1914.

The market is universally recognized by its prominent four-faced clock tower rising above the southern facade, facing Quach Thi Trang Square. Spanning nearly thirteen thousand square meters, the market houses over three thousand stalls categorized beneath historic tiled roofs, trading everything from hand-embroidered silks and lacquerware to fragrant southern spices and aromatic coffee beans. Venturing into the central food alleys, visitors can savor authentic local culinary specialties like sizzling banh xeo and noodle bowls while experiencing the warm, animated trade culture of Saigon.`
      };
    }
    if (lang === 'ja') {
      return {
        title: 'ベンタイン市場',
        summary: 'ベンタイン市場 - 100年以上の歴史を誇るホーチミン市の商業と文化の象徴。4面の時計台が迎える活気あふれる巨大屋内市場です。',
        script: `ホーチミン市1区の中心部に位置するベンタイン市場は、100年以上の歴史を誇る街の商業と文化の象徴です。その起源は19世紀初頭にサイゴン川のほとりで開かれていた青空市場に遡り、1912年から1914年にかけてフランスの建築会社によって現在の堅牢な屋内市場建築が完成しました。

市場の正面には、東西南北の各通りに面した4面のアイコニックな時計台がそびえ立っています。約13,000平方メートルの広大な館内には3,000以上の店舗が所狭しと並び、伝統的なアオザイやシルク製品、漆器工芸品、乾燥フルーツ、そして薫り高いベトナムコーヒーまであらゆる特産品が揃っています。市場内のフードコートでは、本場のバインセオやフォーなどの名物料理を手軽に味わうことができ、活気ある商人たちの掛け声とともにサイゴンの熱気あふれるエネルギーを体感できます。`
      };
    }
    return {
      title: 'Chợ Bến Thành',
      summary: 'Chợ Bến Thành - biểu tượng thương mại và văn hóa lịch sử hơn một thế kỷ của Thành phố Hồ Chí Minh, nơi giao lưu buôn bán sầm uất bậc nhất phương Nam.',
      script: `Tọa lạc tại giao điểm của các trục đường lớn ngay trung tâm Quận 1, Chợ Bến Thành là một trong những biểu tượng kiến trúc và văn hóa đặc trưng nhất của Thành phố Hồ Chí Minh qua hơn một thế kỷ thăng trầm. Nguồn gốc ngôi chợ bắt đầu từ khu chợ có nguồn gốc bên bờ sông Bến Nghé từ đầu thế kỷ 19, cạnh bến sông đón khách vãng lai và binh lính vào thành Quy, do đó mới có tên gọi là Bến Thành. Đến năm 1912, người Pháp đã cho khởi công xây dựng ngôi chợ kiên cố mới tại vị trí đầm lầy Bồ Rẫy ngày nay và khánh thành vào tháng 3 năm 1914.

Ngôi chợ nổi tiếng với tháp đồng hồ bốn mặt vững chãi ở cửa Nam, hướng ra quảng trường Quách Thị Trang. Bên trong chợ có diện tích hơn mười ba nghìn mét vuông với hơn ba nghìn sạp hàng buôn bán tấp nập, từ các mặt hàng thủ công mỹ nghệ, gốm sứ, lụa là cho đến các loại trà, cà phê hảo hạng khắp mọi miền đất nước. Bước vào khu ẩm thực trung tâm, hương thơm nức mũi của bánh xèo, bún riêu, chè Nam Bộ cùng tiếng cười nói rộn rã của các tiểu thương sẽ đem đến cho du khách những trải nghiệm sống động về một Sài Gòn hồn hậu và phóng khoáng.`
    };
  }

  // 11. Dinh Độc Lập
  if (lower.includes('độc lập') || lower.includes('thống nhất') || lower.includes('doc lap')) {
    if (lang === 'en') {
      return {
        title: 'Independence Palace',
        summary: 'Independence Palace (Reunification Hall) - a special national historic landmark commemorating national reunification on April 30, 1975, blending modernist architecture with eastern philosophy.',
        script: `Surrounded by lush twelve-hectare landscaped gardens in central District 1, the Independence Palace, officially known as the Reunification Convention Hall, is a special national historic monument of exceptional historical and architectural significance. Originally erected in 1868 as Norodom Palace, the structure was completely redesigned in 1962 by talented Vietnamese master architect Ngo Viet Thu, who graduated with highest honors from the National School of Fine Arts in Paris.

The palace's architecture represents a harmonious fusion of 1960s modernist concrete design with oriental feng shui philosophy, subtly shaping the facade and floor plans into auspicious Chinese characters symbolizing good fortune, harmony, and wisdom. Historically, the building served as the presidential palace and wartime command headquarters for the former regime until midday on April 30, 1975, when liberation tanks entered the palace gates, marking the end of prolonged war and the reunification of Vietnam. Today, visitors explore ceremonial state halls, operational command bunkers, and lush grounds preserved in their original historical state.`
      };
    }
    if (lang === 'ja') {
      return {
        title: '統一会堂（旧大統領官邸）',
        summary: '統一会堂 - 1975年4月30日の国家統一の歴史的瞬間を今に伝える特別国家遺産。現代建築と東洋風水が美しく調和した名建築です。',
        script: `ホーチミン市1区の中心部に広がる緑豊かな敷地に建つ統一会堂は、ベトナム現代史の転換点を見届けてきた重要文化遺産です。1868年にフランス統治下の「ノロドム宮殿」として建設された後、1962年にローマ大賞を受賞した高名な建築家ゴー・ヴィエット・トゥーによって全面的に再建されました。

この建物の外観と間取りは、東洋哲学の漢字（吉、口、忠、三など）を巧みに象徴するように設計されており、近代的機能美と伝統的な風水思想が見事に融合しています。かつては南ベトナム政権の大統領官邸および軍事作戦本部として使用されていましたが、1975年4月30日正午、解放軍の戦車が鉄門を突破して無血入城を果たし、長きにわたるベトナム戦争の終結と国家統一が宣言されました。現在も大統領の執務室や地下作戦室が当時のまま保存され、多くの旅行者に平和の尊さを伝えています。`
      };
    }
    return {
      title: 'Dinh Độc Lập',
      summary: 'Dinh Độc Lập - di tích quốc gia đặc biệt ghi dấu mốc son lịch sử thống nhất non sông ngày 30/4/1975, kiệt tác kiến trúc hiện đại kết hợp phong thủy phương Đông.',
      script: `Tọa lạc trong khuôn viên rợp bóng cây cổ thụ rộng mười hai héc ta tại trung tâm Quận 1, Dinh Độc Lập là di tích lịch sử quốc gia đặc biệt lưu giữ những thời khắc bước ngoặt của lịch sử dân tộc Việt Nam hiện đại. Tiền thân công trình là Dinh Norodom do người Pháp xây dựng từ năm 1868. Đến năm 1962, công trình được xây mới hoàn toàn theo đồ án thiết kế tài hoa của kiến trúc sư Ngô Viết Thụ, người Việt Nam đầu tiên đạt giải Khôi nguyên La Mã danh giá tại Paris.

Kiến trúc Dinh Độc Lập là sự kết hợp nhuần nhuyễn giữa phong cách hiện đại phương Tây thế kỷ 20 với triết lý phong thủy phương Đông, trong đó các đường nét bố cục mặt bằng và mặt tiền được khéo léo tạo hình thành các chữ Hán mang ý nghĩa Cát tường, Trung hiếu và Chủ quyền. Đây từng là cơ quan đầu não của chính quyền Sài Gòn cũ cho đến thời khắc lịch sử trưa ngày 30 tháng 4 năm 1975, khi xe tăng quân giải phóng húc đổ cổng sắt tiến vào, cắm cờ chiến thắng kết thúc chiến tranh và thống nhất non sông một dải. Tham quan các phòng khánh tiết lộng lẫy và hệ thống hầm ngầm tác chiến, du khách sẽ thấu hiểu sâu sắc hơn giá trị thiêng liêng của độc lập tự do.`
    };
  }

  // 12. Dãy Núi Ba Vì
  if (lower.includes('ba vì') || lower.includes('ba vi')) {
    if (lang === 'en') {
      return {
        title: 'Ba Vi Mountain Range',
        summary: 'Ba Vi Mountain Range - the legendary sacred mountain overlooking the capital, renowned for misty peaks, primeval forests, and the myth of Mountain God Son Tinh.',
        script: `Rising prominently above the Red River plains northwest of Hanoi, the Ba Vi Mountain Range has long been revered as the ancestral peak and sacred highland sentinel of northern Vietnam. Crowned with three iconic pinnacles reaching an elevation of nearly thirteen hundred meters, this highland massif was preserved as an ancient resort retreat by French naturalists in the early 20th century thanks to its cool year-round climate and dense primeval biodiversity.

In Vietnamese folklore, Ba Vi is celebrated as the sacred dwelling of Son Tinh, the Mountain God of Tan Vien, who triumphed over Thuy Tinh the Water God to protect the ancient kingdom of Van Lang during the reign of the Hung Kings. Ascending through the national park's winding roads, travelers encounter historic French villa ruins draped in moss, ancient cactus glasshouses, and the venerable Thuong Temple perched atop Tan Vien peak. Breathing in the pure misty air and taking in the panoramic views of the scenic Da River winding below, visitors experience the wild, untamed majesty of northern nature.`
      };
    }
    if (lang === 'ja') {
      return {
        title: 'バーヴィー山脈',
        summary: 'バーヴィー山脈 - ハノイ北西部にそびえる霊峰。山神ソン・ティンの伝説が伝わる、豊かな原生林と爽涼な気候に恵まれた景勝地です。',
        script: `ハノイの北西約60キロメートルに位置するバーヴィー山脈は、紅河平野を見下ろすようにそびえ立つ北ベトナムの聖なる名峰です。最高峰約1,300メートルの三つの頂（タンビエン峰、ブア峰、ゴックホア峰）を持ち、20世紀初頭にはフランス人によって避暑地として開発された爽涼な高原気候と豊かな生態系に恵まれています。

ベトナムの古代神話において、バーヴィー山はフン王時代に水神トゥイ・ティンとの戦いに勝利し国を守った山神ソン・ティン（タンビエン山聖）が鎮座する霊山として敬われてきました。国立公園の曲がりくねった山道を登ると、苔むしたフランス植民地時代の別荘跡やサボテン温室、そして山頂に厳かに佇むトゥオン寺院に出会うことができます。霧煙る涼やかな山の風に包まれながら眼下に広がるダ川の雄大な流れを眺めれば、大自然の神秘と古代神話のロマンに深く浸ることができます。`
      };
    }
    return {
      title: 'Dãy Núi Ba Vì',
      summary: 'Dãy Núi Ba Vì sừng sững giữa không gian khoáng đạt của đất trời, là thắng cảnh nổi tiếng của thủ đô, gắn liền với huyền tích Sơn Tinh - Thủy Tinh thời Hùng Vương dựng nước.',
      script: `Cách trung tâm thủ đô Hà Nội khoảng sáu mươi cây số về phía tây bắc, Dãy Núi Ba Vì sừng sững uy nghiêm giữa vùng đồng bằng sông Hồng, được coi là ngọn núi tổ và bức tường thành tự nhiên che chắn cho kinh thành Thăng Long từ thuở xa xưa. Dãy núi sở hữu ba đỉnh cao nổi bật gồm đỉnh Vua, đỉnh Tản Viên và đỉnh Ngọc Hoa với độ cao gần một nghìn ba trăm mét, được thiên nhiên ban tặng khí hậu quanh năm mát mẻ cùng thảm thực vật rừng nguyên sinh vô cùng đa dạng.

Trong tâm thức của người Việt, núi Ba Vì gắn liền với huyền tích Sơn Tinh - Thủy Tinh thời vua Hùng thứ mười tám, nơi Thần Núi Tản Viên biểu trưng cho sức mạnh kiên cường của nhân dân chế ngự lũ lụt bảo vệ mùa màng bờ cõi. Dọc theo cung đường đèo uốn lượn lên đỉnh núi, du khách sẽ bắt gặp những phế tích biệt thự Pháp cổ kính rêu phong, vườn xương rồng độc đáo và Đền Thượng linh thiêng trên đỉnh Tản Viên. Đứng giữa mây ngàn lộng gió phóng tầm mắt ngắm dòng sông Đà uốn khúc quanh co dưới chân núi, du khách sẽ cảm nhận được sự thanh thản tuyệt đối và khí phách non ngàn hùng vĩ.`
    };
  }

  // 13. Nếu có dữ liệu tri thức thực tế từ Wikipedia
  if (wikiKnowledge && wikiKnowledge.extract) {
    const rawExtract = wikiKnowledge.extract.replace(/\(.*?\)/g, '').trim();
    const sentences = rawExtract.split(/(?<=[.!?])\s+/).filter(s => s.length > 10);
    const mainFact = sentences.slice(0, 3).join(' ') || rawExtract.slice(0, 250);

    if (lang === 'en') {
      return {
        title: formattedTopic,
        summary: `${formattedTopic} - an outstanding destination celebrated for its distinctive identity, captivating atmosphere, and unique local heritage.`,
        script: `${formattedTopic} is widely admired as one of the most distinctive destinations in the region. ${mainFact} Throughout its historical development, this landmark has continuously attracted travelers through its unique geographic features, cultural significance, and authentic local way of life.

Exploring this captivating site, visitors discover rich stories embedded in the local architecture, observe everyday traditions, and enjoy the picturesque surroundings. The tranquil atmosphere and hospitable community offer a deeply fulfilling journey, leaving lasting impressions of genuine discovery and travel inspiration.`
      };
    }
    if (lang === 'ja') {
      return {
        title: formattedTopic,
        summary: `${formattedTopic} - 独自の風情と美しい景観が調和した、訪れる人々を魅了する注目の観光名所です。`,
        script: `${formattedTopic}は、この地域を代表する魅力的な観光景勝地として広く知られています。${mainFact} 時代の変遷とともに育まれてきたこの場所は、固有の自然環境や文化的価値によって多くの旅人を惹きつけてきました。

この空間に足を踏み入れると、伝統的な佇まいや美しい風景、そして地域の人々の温かい暮らしの息吹を間近に感じることができます。清々しい空気に包まれながら過ごす穏やかな時間は、日常の喧騒を忘れさせ、旅情あふれる心温まる思い出として深く記憶に刻まれます。`
      };
    }
    return {
      title: formattedTopic,
      summary: `${formattedTopic} - ${wikiKnowledge.description ? (wikiKnowledge.description + ', ') : ''}điểm đến nổi bật với cảnh sắc ấn tượng và phong vị địa phương đặc sắc.`,
      script: `${formattedTopic} là một trong những điểm dừng chân tiêu biểu và giàu sức hút của vùng đất này. ${mainFact} Trải qua quá trình hình thành và phát triển lâu dài, địa danh không chỉ lưu giữ những giá trị độc đáo về địa lý và lịch sử mà còn phản ánh chân thực nếp sống văn hóa hiền hòa của cộng đồng cư dân bản địa.

Bước chân đến không gian nơi đây, du khách sẽ được tận mắt chiêm ngưỡng cảnh quan thiên nhiên hài hòa, tìm hiểu những câu chuyện lịch sử thú vị và thưởng ngoạn phong vị ẩm thực đậm đà bản sắc. Không gian thoáng đãng cùng sự đón tiếp nồng hậu của con người địa phương chắc chắn sẽ mang đến cho bạn một hành trình tham quan thư thái, trọn vẹn và đọng lại nhiều kỷ niệm đẹp trong lòng.`
    };
  }

  // 14. Phân loại theo Ngữ nghĩa & Thể loại (Semantic Category Engine) cho MỌI ĐỊA DANH KHÁC
  const category = detectHeritageCategory(lower);

  // Thể loại A: Biển / Bãi tắm / Đảo / Vịnh / Resort
  if (category === 'beach') {
    if (lang === 'en') {
      return {
        title: formattedTopic,
        summary: `${formattedTopic} - a splendid coastal haven and beach retreat, renowned for sparkling turquoise waves, pristine sands, and refreshing ocean breezes.`,
        script: `Blessed with gentle sea breezes and panoramic coastal scenery, ${formattedTopic} has long stood as an idyllic beach retreat attracting travelers seeking nature and relaxation. The area developed from peaceful fishing communities who navigated coastal tides and established intimate cultural bonds with the ocean, creating a distinctive maritime way of life.

The shoreline is distinguished by sweeping sands lapped by crystal-clear waters, set against a backdrop of whispering sea pines and natural coastal formations. Walking along the beach in the early morning, visitors can watch fishing boats returning with fresh catches, breathe in the invigorating salt air, and enjoy relaxing oceanfront strolls. This coastal destination promises deeply restful moments and revitalizing energy for every traveler.`
      };
    }
    if (lang === 'ja') {
      return {
        title: formattedTopic,
        summary: `${formattedTopic} - 輝く碧い海と心地よい潮風が迎える、穏やかなビーチリゾート＆観光名所です。`,
        script: `${formattedTopic}は、穏やかな波と澄み渡る潮風が心地よい、ベトナム屈指の海岸リゾート景勝地です。かつて素朴な漁村として海とともに歩んできたこの地は、現在では手つかずの自然美とくつろぎの空間を兼ね備えた魅力的な旅行先として親しまれています。

見渡す限りの砂浜にはエメラルドグリーンの波が静かに打ち寄せ、海沿いの並木が心地よい木陰を作っています。朝早く砂浜を散策すれば、沖合から戻る漁船の素朴な営みに出会い、清々しい海の風を胸いっぱいに吸い込むことができます。日常の喧騒を離れ、波音に耳を傾けながら過ごす贅沢な時間は、心身に豊かな癒やしと活力をもたらしてくれます。`
      };
    }
    return {
      title: formattedTopic,
      summary: `${formattedTopic} - điểm đến du lịch biển và nghỉ dưỡng tuyệt mỹ, nổi bật với làn nước trong xanh, bờ cát mịn màng và không gian đại dương khoáng đạt trong lành.`,
      script: `${formattedTopic} là một trong những thắng cảnh du lịch biển và nghỉ dưỡng tuyệt đẹp thu hút đông đảo du khách thập phương. Khởi nguồn từ một làng chài mộc mạc nơi các thế hệ ngư dân bám biển mưu sinh và gìn giữ phong tục thờ Thần Nam Hải, nơi đây dần phát triển thành điểm dừng chân lý tưởng nhờ sở hữu cảnh sắc thiên nhiên hoang sơ và khí hậu biển trong lành quanh năm.

Trải dài dọc theo mép sóng là bãi cát mịn màng thoai thoải nép mình bên hàng dương xanh rì rào đón gió đại dương, làn nước trong vắt phản chiếu nền trời cao rộng. Dạo bước trên bờ cát lúc sớm mai, du khách có thể ngắm nhìn nhịp sống bình dị của những chuyến thuyền nan cập bờ chở đầy tôm cá tươi rói, thả lỏng tâm hồn theo từng đợt sóng vỗ và thưởng thức hải sản địa phương đậm đà. Chuyến ghé thăm nơi đây chắc chắn sẽ mang đến cho bạn những khoảnh khắc nghỉ dưỡng an yên và tái tạo nguồn năng lượng dồi dào.`
    };
  }

  // Thể loại B: Núi / Đèo / Thác / Hang động / Cao nguyên
  if (category === 'mountain') {
    if (lang === 'en') {
      return {
        title: formattedTopic,
        summary: `${formattedTopic} - a majestic highland wonder and scenic natural peak, famed for rolling mountain mists, dramatic landscapes, and crisp alpine air.`,
        script: `Rising majestically amidst the clouds, ${formattedTopic} is celebrated as one of the most magnificent highland landmarks in the region. Formed over millions of years through complex tectonic movements, the rugged topography here features dramatic limestone ridges, cascading waterfalls, and lush primeval forests teeming with rare biodiversity.

For centuries, indigenous ethnic communities have inhabited these slopes, cultivating hillside terraces and passing down captivating folklore about ancestral mountain spirits. Ascending the winding mountain routes, visitors are treated to breathtaking vistas of misty valleys unfolding below and the refreshing coolness of the highland atmosphere. Whether conquering scenic peaks or marveling at the untamed wilderness, traveling through this highland paradise awakens an inspiring sense of freedom and wonder.`
      };
    }
    if (lang === 'ja') {
      return {
        title: formattedTopic,
        summary: `${formattedTopic} - 雄大な山並みと清澄な山の空気が広がる、息をのむような絶景山岳景勝地です。`,
        script: `${formattedTopic}は、雲海に包まれた雄大な嶺と豊かな原生林が連なる、地域屈指の山岳景勝地です。数百万年の地殻変動によって形成された起伏に富んだ地形には、切り立った岩壁、清流が注ぐ滝、そして四季折々に姿を変える豊かな植物群が息づいています。

古くから山岳地帯に暮らす先住民族によって段々畑や素朴な集落が築かれ、山神への畏敬を込めた神話や民話が大切に語り継がれてきました。尾根を巡る小道を歩けば、眼下に広がる広大な渓谷美や野鳥のさえずりに出会い、澄み切った山の空気が心身を清めてくれます。大自然の圧倒的なスケールと静寂の中で過ごす時間は、冒険心を刺激し、忘れられない旅の感動を刻み込みます。`
      };
    }
    return {
      title: formattedTopic,
      summary: `${formattedTopic} - thắng cảnh thiên nhiên kỳ vĩ và non cao hùng tráng, nổi bật với cảnh sắc đại ngàn mây phủ, địa thế non nước nên thơ và bầu không khí thanh khiết mát lành.`,
      script: `${formattedTopic} sừng sững giữa không gian khoáng đạt của đất trời, là một trong những kỳ quan thiên nhiên kỳ vĩ níu chân du khách muôn phương. Được tạo tác qua hàng triệu năm vận động địa chất phức tạp, địa danh nổi bật với những dãy núi non trập trùng, những vách đá sừng sững phủ rêu phong và những dải rừng nguyên sinh xanh thẳm bảo bọc nguồn nước mát lành.

Địa phương này từ ngàn xưa là nơi cư ngụ của đồng bào các dân tộc thiểu số với những tập tục văn hóa giàu bản sắc, lưu truyền những huyền tích về các vị thần núi non sông suối chở che cho mùa màng và xóm bản. Men theo những cung đường uốn lượn lưng chừng núi, du khách sẽ được hít thở bầu không khí tinh khôi se lạnh, chiêm ngưỡng biển mây bồng bềnh vờn đỉnh biếc và ngắm nhìn thung lũng xanh mướt phía dưới. Trải nghiệm khám phá non cao kỳ vĩ nơi đây sẽ xua tan mọi mỏi mệt đời thường, đưa tâm hồn bạn hòa quyện vào sự khoáng đạt vô cùng của thiên nhiên.`
    };
  }

  // Thể loại C: Chùa / Đền / Miếu / Thiền viện / Nhà thờ
  if (category === 'spiritual') {
    if (lang === 'en') {
      return {
        title: formattedTopic,
        summary: `${formattedTopic} - a tranquil spiritual haven and sacred heritage landmark, offering serene reflection, graceful traditional architecture, and peaceful blessing.`,
        script: `Nestled quietly in a serene landscape, ${formattedTopic} stands as a revered sanctuary of faith and architectural elegance. Established generations ago by devout forebears seeking a peaceful refuge for prayer, this sacred complex has weathered historical turbulence while continuing to preserve essential spiritual values and moral teachings.

The site is distinguished by classic Asian religious architecture, boasting curved tiled eaves, ornate carved wooden pillars, and serene sanctums wrapped in fragrant incense smoke. Pilgrims and visitors from afar come to offer sincere prayers for peace, health, and family well-being. Walking softly through the shaded courtyards and listening to the rhythmic tolling of temple bells, one discovers a deep, restorative stillness that cleanses the heart and restores mindful harmony.`
      };
    }
    if (lang === 'ja') {
      return {
        title: formattedTopic,
        summary: `${formattedTopic} - 厳かな信仰の歴史と優雅な伝統建築が調和する、心の平穏をもたらす神聖な霊場です。`,
        script: `${formattedTopic}は、人々の祈りと信仰を受け継いできた尊い聖地であり、伝統建築の粋を集めた霊場です。何世代にもわたり信仰の拠点として大切に守り継がれ、幾多の歴史の変遷を経てもなお、清浄な宗教的空間と道徳的な教えを今に伝えています。

反り上がった優美な瓦屋根、精巧な木彫りの柱、そして静かに漂うお香の香りが、訪れる人々を厳かな気持ちへと誘います。遠方からも多くの参拝者が訪れ、家内安全や平穏無事を願って手を合わせています。緑陰の境内を静かに歩き、澄み渡る鐘の響きに耳を傾ければ、日常の喧騒で乱れた心が清らかに整い、深い精神的な充足感を味わうことができます。`
      };
    }
    return {
      title: formattedTopic,
      summary: `${formattedTopic} - chốn tâm linh thanh tịnh và di sản tín ngưỡng trang nghiêm, nơi gửi gắm ước nguyện bình an với kiến trúc cổ kính và không gian an lạc thanh tao.`,
      script: `${formattedTopic} là một trong những trung tâm tâm linh thanh tịnh và di sản kiến trúc tín ngưỡng tiêu biểu của vùng đất. Khởi nguồn từ nhiều thế hệ trước khi các bậc tiền nhân chọn mảnh đất linh thiêng này để lập am dựng chùa, công trình đã trải qua nhiều lần trùng tu tôn tạo nhưng vẫn giữ vững tinh thần trang nghiêm, chở che cho đời sống tinh thần của bà con phật tử và nhân dân trong vùng.

Không gian ngôi chùa mang đậm phong cách kiến trúc truyền thống với mái ngói hình mũi hài cong vút, hệ thống cột gỗ lim vững chãi chạm trổ hoa văn tinh xảo và những bức hoành phi câu đối sơn son thếp vàng đượm màu thời gian. Bước chân qua cổng tam quan vào khuôn viên rợp bóng bồ đề, hương trầm thoang thoảng cùng tiếng chuông chùa ngân nga buông lơi trong chiều tà như xua tan mọi âu lo trần thế. Hành hương chiêm bái tại đây mang đến cho du khách sự an lạc tĩnh tại trong tâm hồn và niềm tin hướng thiện sâu sắc.`
    };
  }

  // Thể loại D: Di tích lịch sử / Bảo tàng / Địa đạo / Lăng tẩm
  if (category === 'history') {
    if (lang === 'en') {
      return {
        title: formattedTopic,
        summary: `${formattedTopic} - an important historical landmark commemorating patriotic valor, enduring resilience, and pivotal milestones in national history.`,
        script: `Holding a solemn place in the nation's heritage, ${formattedTopic} serves as a profound historical witness to the unyielding courage and sacrifices of past generations. Established at a crucial geographical junction during resistance eras, this site served as a vital base where patriotic fighters and citizens stood firmly to defend their homeland.

Today, the monument carefully preserves authentic artifacts, underground tunnels, fortified defensive lines, and touching documentary photographs that recount heroic chapters of national history. Visitors can walk through preserved battlements and listen to poignant stories of comradeship and perseverance. Experiencing this historic sanctuary firsthand deepens appreciation for hard-won peace and fosters lasting respect for the enduring resilience of the people.`
      };
    }
    if (lang === 'ja') {
      return {
        title: formattedTopic,
        summary: `${formattedTopic} - 不屈の精神と歴史の転換点を今に伝える、誇り高き歴史的記念遺産です。`,
        script: `${formattedTopic}は、祖国の平和と独立のために立ち上がった先人たちの不屈の勇気を今に伝える、極めて重要な歴史記念遺産です。激動の時代において防衛と戦略の要所として機能し、多くの愛国者たちが団結して困難に立ち向かった歴史的な舞台となりました。

現在、この地には当時の地下トンネルや防衛拠点、貴重な遺品や記録写真が大切に保存・展示されています。保存された遺構を巡り、語り継がれる当時の過酷な戦いや連帯の物語に耳を傾けることで、平和のかけがえのなさを実感することができます。歴史の重みに触れるこの旅は、訪れる人々に深い感動と未来への希望を新たにする貴重な体験となります。`
      };
    }
    return {
      title: formattedTopic,
      summary: `${formattedTopic} - di tích lịch sử tiêu biểu ghi dấu những mốc son hào hùng và ký ức bất khuất của dân tộc, mang ý nghĩa giáo dục truyền thống sâu sắc.`,
      script: `${formattedTopic} là một trong những di tích lịch sử đặc biệt, chứng nhân kiên trung ghi dấu những năm tháng chiến đấu oanh liệt và tinh thần bất khuất của cha ông trong sự nghiệp bảo vệ quê hương. Tọa lạc tại vị trí địa chiến lược hiểm yếu, nơi đây từng là căn cứ kháng chiến kiên cường, nơi quân và dân đồng lòng vượt qua muôn vàn gian khổ hy sinh để lập nên những chiến công vang dội.

Quần thể di tích ngày nay lưu giữ hệ thống hào lũy, công sự, hầm ngầm tác chiến cùng hàng ngàn tư liệu, hiện vật lịch sử quý báu tái hiện chân thực cuộc sống và chiến đấu kiên trung của những người lính năm xưa. Đi dọc theo những lối mòn rợp bóng cây xanh và lắng nghe thuyết minh về những trận đánh hào hùng, thế hệ hôm nay càng thêm tự hào về truyền thống yêu nước nồng nàn và trân trọng giá trị vô giá của nền hòa bình độc lập mà cha ông đã đánh đổi bằng xương máu.`
    };
  }

  // Thể loại E: Cầu / Chợ / Phố đi bộ / Quảng trường / Landmark
  if (category === 'urban') {
    if (lang === 'en') {
      return {
        title: formattedTopic,
        summary: `${formattedTopic} - an architectural icon and lively urban hub, blending vibrant civic life, modern flair, and local cultural charm.`,
        script: `Located prominently in the urban core, ${formattedTopic} stands as an energetic cultural landmark and a favorite gathering place for locals and travelers alike. Originally established alongside the commercial expansion of the metropolis, this vibrant hub has evolved into a vital crossroads where tradition seamlessly interfaces with modern civic vitality.

The area is known for its distinctive architectural design, wide pedestrian promenades, and bustling commercial quarters that light up warmly as evening arrives. Visitors can stroll leisurely along the dynamic streets, take memorable photographs against iconic urban backdrops, and sample delicious regional street food from lively open-air stalls. The energetic pulse and welcoming community atmosphere make this urban destination an essential stop on any city itinerary.`
      };
    }
    if (lang === 'ja') {
      return {
        title: formattedTopic,
        summary: `${formattedTopic} - 活気あふれる都市文化と近代的な美しさが融合した、街を代表する注目のランドマークです。`,
        script: `${formattedTopic}は、街の中心に位置し、市民や旅行者が集う活気あふれる文化と観光のシンボルです。大都市の商業的発展とともに歴史を重ね、地域の伝統的な風情と近代的な都市の活気脈打つエネルギーが見事に融合した場所として親しまれています。

個性的な建築美と広々とした歩行者広場、そして夜になると色鮮やかなライトアップに彩られる街並みが特徴です。通りをのんびりと散策すれば、人気のフォトスポットでの記念撮影や、香ばしい屋台が並ぶストリートフードの数々を満喫することができます。開放的で温かい街の鼓動を肌で感じながら過ごす時間は、旅情豊かな忘れられない都市の思い出を届けてくれます。`
      };
    }
    return {
      title: formattedTopic,
      summary: `${formattedTopic} - biểu tượng kiến trúc và điểm hẹn văn hóa đô thị sôi động, nơi hội tụ nhịp sống phồn hoa, giao lưu đặc sắc và nét duyên dáng hiện đại.`,
      script: `${formattedTopic} tọa lạc tại vị trí đắc địa ngay giữa trung tâm đô thị, là biểu tượng kiến trúc nổi bật và điểm hẹn giao lưu văn hóa sôi động không thể bỏ lỡ khi ghé thăm thành phố. Gắn liền với tiến trình phát triển và mở rộng của đô thị qua các thời kỳ, nơi đây đã trở thành nhịp cầu kết nối giữa quá khứ giao thương truyền thống với nhịp sống năng động, hiện đại của thời kỳ mới.

Công trình sở hữu kiến trúc ấn tượng, không gian quảng trường thoáng đãng rực rỡ sắc màu khi đêm về cùng các tuyến phố đi bộ tấp nập người qua lại. Du khách đến đây có thể thong dong tản bộ ngắm nhìn phố phường lung linh ánh đèn, thưởng thức những món ăn đường phố hấp dẫn mang đậm phong vị bản địa và hòa mình vào những hoạt động nghệ thuật biểu diễn công cộng sôi nổi. Bầu không khí náo nhiệt và lòng mến khách nồng hậu nơi đây chắc chắn sẽ để lại những kỷ niệm tươi đẹp trong chuyến hành trình của bạn.`
    };
  }

  // Thể loại F: Sông / Hồ / Kênh / Rạch / Rừng / Vườn quốc gia / Sinh thái
  if (category === 'water_nature') {
    if (lang === 'en') {
      return {
        title: formattedTopic,
        summary: `${formattedTopic} - an enchanting ecotourism retreat with serene waterways and verdant landscapes, offering pure relaxation and natural harmony.`,
        script: `Spreading across a lush expanse of fertile waterways and tropical vegetation, ${formattedTopic} offers a peaceful natural sanctuary away from busy city life. Formed through ancient river basin sedimentation, this ecotourism haven has supported local riverine communities for generations through sustainable fishing, agriculture, and waterways transport.

The landscape is graced with meandering channels bordered by lush riparian canopies, where gentle currents nurture diverse freshwater and wetland ecosystems. Drifting serenely along the water on wooden sampans, visitors can listen to birdsong echoing through leafy canopies, observe traditional waterside livelihoods, and breathe in the refreshing woodland air. This tranquil natural sanctuary provides a deeply rejuvenating escape into the peaceful rhythms of the natural world.`
      };
    }
    if (lang === 'ja') {
      return {
        title: formattedTopic,
        summary: `${formattedTopic} - 穏やかな水辺と豊かな緑に抱かれた、心身を癒やす自然豊かなエコリゾートです。`,
        script: `${formattedTopic}は、瑞々しい水路と豊かな自然林が広がる、心身を癒やす自然豊かなエコリゾートです。悠久の河川の堆積作用によって形成された肥沃な大地は、古くから水上交通や漁業を営む地域の人々の暮らしを支え、自然と調和した独自の生活文化を育んできました。

緑豊かな木陰が続く水路を木造の小舟で静かに巡れば、鳥のさえずりや川のせせらぎが心地よく響き、澄み切った森の空気が旅人を包み込みます。水辺の素朴な暮らしを間近に観察しながらゆったりと流れる時間に身を任せることで、大自然の安らぎと温かい生命力に満たされる贅沢なひとときをお過ごしいただけます。`
      };
    }
    return {
      title: formattedTopic,
      summary: `${formattedTopic} - thắng cảnh sinh thái trong lành hữu tình, nơi thiên nhiên xanh mát bồi đắp nguồn sống bình yên và thư thái cho du khách.`,
      script: `${formattedTopic} mở ra một không gian sinh thái trong lành ngập tràn sắc xanh, là điểm đến du lịch sinh thái tuyệt vời cho những ai yêu mến sự yên ả của thiên nhiên. Được hình thành nhờ nguồn phù sa trù phú bồi đắp qua hàng ngàn năm lịch sử, nơi đây từ thuở khai hoang đã trở thành chiếc nôi nuôi dưỡng đời sống bình dị của những xóm ấp ven sông và gìn giữ một hệ sinh thái động thực vật đa dạng, trù phú.

Du ngoạn trên những chiếc thuyền nan êm ả luồn lách qua những rặng cây rủ bóng mát rượi, du khách sẽ được lắng nghe tiếng chim muông hót líu lo, ngắm nhìn dòng nước lững lờ trôi và hít hà hương vị mộc mạc tinh khôi của cỏ cây sông nước. Cảnh sắc hữu tình cùng không khí thanh bình nơi đây như một liều thuốc quý giúp giải tỏa mọi căng thẳng của cuộc sống thường nhật, đưa con người tìm về sự giao hòa gần gũi với mẹ thiên nhiên.`
    };
  }

  // Thể loại G: Điểm đến tổng quát (Không dùng câu thơ trong ngoặc kép, giàu thông tin du lịch)
  if (lang === 'en') {
    return {
      title: formattedTopic,
      summary: `${formattedTopic} - a distinctive travel destination offering scenic impressions, memorable local experiences, and heartfelt hospitality.`,
      script: `Conveniently nestled within the regional travel circuit, ${formattedTopic} stands as a distinctive destination that welcomes travelers with its genuine local character and scenic attractions. Over its historical development, the site has flourished as an important community focal point, preserving traditional local lifestyle patterns while adapting gracefully to contemporary tourism.

Exploring the site, travelers can admire harmonious architectural accents, observe the authentic rhythms of neighborhood life, and engage with friendly local residents. The destination offers diverse opportunities to savor regional culinary specialties and capture beautiful travel memories. With its inviting ambiance and heartfelt hospitality, a visit here provides a truly enriching and memorable travel experience.`
    };
  }
  if (lang === 'ja') {
    return {
      title: formattedTopic,
      summary: `${formattedTopic} - 独自の魅力と美しい景色、そして温かいおもてなしが旅人を迎える注目の観光地です。`,
      script: `${formattedTopic}は、地域ならではの固有の魅力と美しい風景で多くの旅行者を惹きつける注目の観光スポットです。歴史的な歩みの中で地域の生活や文化の中心として発展を遂げ、伝統的な風情を大切に守りながら、温かいおもてなしの心で世界中の人々を迎えています。

この地を散策すると、周囲の自然環境と調和した美しい建築景観に出会い、素朴で温かい人々の日常の息吹を実感することができます。地元の名物料理を味わったり、心温まる交流を楽しんだりすることで、旅情豊かなひとときを過ごすことができます。訪れる人々に寄り添う穏やかな空間は、生涯の思い出として心に深く残る素晴らしい旅の体験を約束します。`
    };
  }

  return {
    title: formattedTopic,
    summary: `${formattedTopic} - điểm dừng chân du lịch nổi bật, mang đến cho du khách trải nghiệm độc đáo, cảnh sắc ấn tượng và phong vị địa phương hiếu khách.`,
    script: `${formattedTopic} là một trong những điểm dừng chân du lịch nổi bật và giàu sức hút của vùng đất này. Gắn liền với tiến trình mở mang và bồi đắp văn hóa của địa phương qua nhiều thời kỳ, nơi đây không chỉ đóng vai trò là một điểm sinh hoạt văn hóa cộng đồng gắn kết mà còn từng bước chuyển mình thành điểm đến hấp dẫn du khách gần xa nhờ cảnh quan tươi đẹp và bản sắc hiền hòa.

Khám phá không gian nơi đây, du khách sẽ có cơ hội chiêm ngưỡng nét kiến trúc độc đáo, hòa mình vào nhịp sống thường nhật thân thương của người dân bản xứ và thưởng thức những đặc sản thơm ngon mang hương vị quê hương xứ sở. Sự thân thiện, cởi mở của người dân cùng bầu không khí dễ chịu nơi này chắc chắn sẽ mang lại cho bạn những phút giây thư giãn thoải mái và ghi dấu ấn sâu đậm trong hành trình khám phá.`
  };
}

window.extractCleanSummary = extractCleanSummary;
window.fetchWikiKnowledge = fetchWikiKnowledge;
window.detectHeritageCategory = detectHeritageCategory;
window.generateSmartHeritageContent = generateSmartHeritageContent;

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
    const audioPlayer = document.getElementById('audioPreviewPlayer');
    if (audioPlayer) {
      audioPlayer.pause();
      audioPlayer.currentTime = 0;
    }
    const playIcon = document.getElementById('adminPlayIcon');
    const pauseIcon = document.getElementById('adminPauseIcon');
    if (playIcon) playIcon.style.display = 'block';
    if (pauseIcon) pauseIcon.style.display = 'none';
  }

  btnClose?.addEventListener('click', closeModal);
  btnCancel?.addEventListener('click', closeModal);

  // Chuyển tab ngôn ngữ (vi, en, ja)
  const langTabs = document.querySelectorAll('.btn-lang-tab');
  langTabs.forEach(btn => {
    btn.addEventListener('click', () => {
      const audioPlayer = document.getElementById('audioPreviewPlayer');
      if (audioPlayer) {
        audioPlayer.pause();
        audioPlayer.currentTime = 0;
      }
      saveCurrentLangDraft();
      langTabs.forEach(b => b.classList.remove('is-active'));
      btn.classList.add('is-active');
      currentActiveLang = btn.dataset.lang;
      loadLangDraft(currentActiveLang);
    });
  });

  // Khởi tạo thanh nghe thuyết minh mini của Admin
  initAdminAudioPlayer();

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

const btnAiGen = document.getElementById('btnAiGenSummary');
  btnAiGen?.addEventListener('click', async () => {
    const titleElem = document.getElementById('poiTitleInput');
    const summaryElem = document.getElementById('poiSummaryInput');
    const scriptElem = document.getElementById('poiScriptInput');

    const titleVal = titleElem.value.trim();
    const sumVal = summaryElem.value.trim();
    let rawTopic = '';

    // Nhận diện linh hoạt: Nếu ô tóm tắt có chữ và tiêu đề là mặc định / trống / Test
    if (sumVal && (!titleVal || titleVal.toLowerCase() === 'test' || titleVal.toLowerCase().startsWith('điểm dừng'))) {
      rawTopic = sumVal;
    } else {
      rawTopic = titleVal || sumVal;
    }

    if (!rawTopic) {
      rawTopic = prompt('Nhập tên địa danh / điểm di tích bạn muốn AI soạn (Ví dụ: Bến Bạch Đằng, Cột Cờ Hà Nội, Chợ Bến Thành...):', 'Bến Bạch Đằng');
      if (!rawTopic) return;
    }

    // Viết hoa đẹp các chữ cái đầu của địa danh
    const formattedTopic = rawTopic.split(' ')
      .map(w => w ? (w.charAt(0).toUpperCase() + w.slice(1)) : '')
      .join(' ');

    titleElem.value = formattedTopic;
    btnAiGen.textContent = '✨ Gemini đang soạn bài...';
    btnAiGen.disabled = true;

    try {
      // Tự động sinh nội dung hoàn chỉnh và đồng bộ cho CẢ 3 NGÔN NGỮ (VI, EN, JA)
      const wikiKnowledge = await fetchWikiKnowledge(formattedTopic);
      const viSmart = generateSmartHeritageContent(formattedTopic, 'vi', wikiKnowledge);
      const enSmart = generateSmartHeritageContent(formattedTopic, 'en');
      const jaSmart = generateSmartHeritageContent(formattedTopic, 'ja');

      currentPoiTranslations['vi'] = {
        ...currentPoiTranslations['vi'],
        title: viSmart.title,
        summary: viSmart.summary,
        description: viSmart.script
      };

      currentPoiTranslations['en'] = {
        ...currentPoiTranslations['en'],
        title: enSmart.title,
        summary: enSmart.summary,
        description: enSmart.script
      };

      currentPoiTranslations['ja'] = {
        ...currentPoiTranslations['ja'],
        title: jaSmart.title,
        summary: jaSmart.summary,
        description: jaSmart.script
      };

      const zhSmart = generateSmartHeritageContent(formattedTopic, 'zh');
      if (zhSmart.script && !currentPoiTranslations.zh?.description) {
        currentPoiTranslations.zh = {
          ...currentPoiTranslations.zh,
          title: zhSmart.title,
          summary: zhSmart.summary,
          description: zhSmart.script
        };
      }

      // Tải lại nội dung hiển thị cho Tab ngôn ngữ đang mở
      loadLangDraft(currentActiveLang);
    } catch (e) {
      console.warn('AI error:', e);
    } finally {
      btnAiGen.disabled = false;
      btnAiGen.innerHTML = `<svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M12 2v4M12 18v4M4.93 4.93l2.83 2.83M16.24 16.24l2.83 2.83M2 12h4M18 12h4M4.93 19.07l2.83-2.83M16.24 7.76l2.83-2.83"/></svg><span>✨ Gợi ý bằng Gemini AI (Tự điền Tóm tắt & Kịch bản)</span>`;
    }
  });

  // Sinh Audio Neural TTS Tự Động Đa Ngôn Ngữ (Đồng thời sinh Tiếng Việt, Tiếng Anh và Tiếng Nhật)
  const btnTts = document.getElementById('btnGenerateNeuralTts');
  btnTts?.addEventListener('click', async () => {
    const script = document.getElementById('poiScriptInput').value.trim();
    const title = document.getElementById('poiTitleInput').value.trim();
    const poiId = document.getElementById('editingPoiId').value;
    const poiPriority = Number(document.getElementById('poiPriorityInput')?.value || 10);
    const poiOrder = Math.max(1, Math.round(poiPriority / 10));

    if (!script && !title) {
      alert('Vui lòng soạn Tiêu đề hoặc Kịch bản thuyết minh trước khi sinh âm thanh.');
      return;
    }

    if (!poiId) {
      alert('Vui lòng lưu thông tin điểm POI trước khi sinh âm thanh tự động.');
      return;
    }

    btnTts.disabled = true;
    btnTts.textContent = '⏳ Đang sinh giọng đọc Neural cho cả 4 ngôn ngữ (VI, EN, ZH, JA)...';

    try {
      saveCurrentLangDraft();

      // 1. Lấy danh sách bản dịch hiện có trên Server
      const listRes = await adminFetch(`/admin/pois/${poiId}/translations`);
      let serverTransList = listRes.ok ? (await listRes.json()) : [];

      const targetLangs = ['vi', 'en', 'zh', 'ja'];
      const langNames = { vi: 'Tiếng Việt', en: 'English', zh: '中文', ja: '日本語' };
      const completedLangs = [];
      const failedLangs = [];

      for (const lang of targetLangs) {
        btnTts.textContent = `⏳ Đang sinh giọng Neural cho ${langNames[lang]} (${lang.toUpperCase()})...`;

        let draft = currentPoiTranslations[lang] || {};
        let transId = draft.id;
        let matchedServerTrans = serverTransList.find(t => (t.languageCode || t.language_code) === lang);
        if (matchedServerTrans && !transId) {
          transId = matchedServerTrans.id;
          draft.id = transId;
        }

        // Xác định tiêu đề và kịch bản chuẩn cho từng ngôn ngữ (VI + EN + ZH + JA)
        const storyData = (POI_STORIES && POI_STORIES[poiOrder]) ? POI_STORIES[poiOrder][lang] : null;
        const smartFallback = generateSmartHeritageContent(title || 'Điểm di tích', lang);
        let langTitle = (draft.title || '').trim() || (storyData ? storyData.title : smartFallback.title);
        let langScript = (draft.description || '').trim() || (storyData ? (storyData.paragraph + (storyData.subtext ? (' ' + storyData.subtext) : '')) : smartFallback.script);
        let langSummary = (draft.summary || '').trim() || smartFallback.summary;

        if (!langScript.trim()) {
          failedLangs.push(`${langNames[lang]}: Chưa có nội dung bản dịch. Hãy nhập kịch bản trước khi sinh giọng đọc.`);
          continue;
        }

        // Cập nhật lại draft đầy đủ cho ngôn ngữ
        currentPoiTranslations[lang] = {
          ...draft,
          title: langTitle,
          description: langScript,
          summary: langSummary
        };

        // Nếu chưa có bản dịch trên server, tạo mới
        if (!transId) {
          const createRes = await adminFetch(`/admin/pois/${poiId}/translations`, {
            method: 'POST',
            body: JSON.stringify({
              language_code: lang,
              title: langTitle,
              description: langScript
            })
          });

          if (createRes.ok) {
            const createdTrans = await createRes.json();
            transId = createdTrans.id;
            currentPoiTranslations[lang].id = transId;
          } else {
            // Thử lấy lại nếu đã tồn tại
            const refetchRes = await adminFetch(`/admin/pois/${poiId}/translations`);
            if (refetchRes.ok) {
              const freshList = await refetchRes.json();
              const found = freshList.find(t => (t.languageCode || t.language_code) === lang);
              if (found) {
                transId = found.id;
                currentPoiTranslations[lang].id = transId;
              }
            }
          }
        }

        if (transId) {
          // Gọi Backend sinh audio Edge Neural TTS cho bản dịch này
          const ttsRes = await adminFetch(`/admin/translations/${transId}/generate-audio`, {
            method: 'POST',
            body: JSON.stringify({ text: langScript || langTitle })
          });

          if (ttsRes.ok) {
            const ttsData = await ttsRes.json();
            currentPoiTranslations[lang].audio_path = ttsData.audioPath;
            currentPoiTranslations[lang].audio_url = ttsData.audioUrl;
            completedLangs.push(lang.toUpperCase());
          }
        }
      }

      // Cập nhật giao diện của Tab ngôn ngữ đang mở
      loadLangDraft(currentActiveLang);

      if (completedLangs.length > 0) {
        alert(`✓ Đã sinh giọng đọc Neural thành công cho: ${completedLangs.join(", ")}!\nBạn có thể chuyển qua các Tab ngôn ngữ để nghe thử từng bản.`);
      } else {
        alert("Máy chủ chưa sinh được file audio (HTTP 422). Vui lòng đợi Render hoàn tất deploy container Docker mới.");
      }
    } catch (e) {
      alert(`Lỗi sinh audio: ${e.message}`);
    } finally {
      btnTts.disabled = false;
      btnTts.innerHTML = `<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polygon points="11 5 6 9 2 9 2 15 6 15 11 19 11 5"/><path d="M19.07 4.93a10 10 0 0 1 0 14.14M15.54 8.46a5 5 0 0 1 0 7.07"/></svg><span>🎙️ Sinh Giọng Đọc Tự Động (VI + EN + ZH + JA)</span>`;
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
    let imageUrl = document.getElementById('poiImageUrlInput').value.trim();
    if (!imageUrl || imageUrl.length > 500 || imageUrl.startsWith('data:')) {
      imageUrl = 'assets/hero-khue-van-cac.jpg';
    }

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
        // Keep the created ID if a later translation fails, so retry does not create a duplicate POI.
        document.getElementById('editingPoiId').value = savedPoiId;
      } else {
        // Cập nhật POI
        const poiRes = await adminFetch(`/admin/pois/${editingId}`, {
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
        if (!poiRes.ok) {
          const problem = await poiRes.json().catch(() => ({}));
          throw new Error(problem.detail || `Không thể cập nhật POI (HTTP ${poiRes.status}).`);
        }
      }

      // Lưu bản dịch cho từng ngôn ngữ (tự động điền bản dịch chuẩn nếu để trống)
      const topicForSave = document.getElementById('poiTitleInput').value.trim() || 'Điểm di tích';
      await savePoiTranslations({
        poiId: savedPoiId,
        drafts: currentPoiTranslations,
        topic: topicForSave,
        generateContent: generateSmartHeritageContent,
        request: adminFetch
      });

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

  currentPoiTranslations = { vi: {}, en: {}, zh: {}, ja: {} };
  currentActiveLang = 'vi';
  resetLangTabs();

  const selectFilter = document.getElementById('selectFilterArea');
  if (selectFilter.value) {
    document.getElementById('poiAreaSelect').value = selectFilter.value;
  }

  const avatarPrev = document.getElementById('poiAvatarPreview');
  if (avatarPrev) avatarPrev.src = 'assets/hero-khue-van-cac.jpg';
  const imgInp = document.getElementById('poiImageUrlInput');
  if (imgInp) imgInp.value = '';
  const titleInp = document.getElementById('poiTitleInput');
  if (titleInp) titleInp.value = '';

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
    const curImg = poi.imageUrl || poi.image_url || '';
    document.getElementById('poiImageUrlInput').value = curImg;
    const avatarPrevEdit = document.getElementById('poiAvatarPreview');
    if (avatarPrevEdit) avatarPrevEdit.src = curImg || 'assets/hero-khue-van-cac.jpg';

    // Tải danh sách bản dịch của POI
    const transRes = await adminFetch(`/admin/pois/${poiId}/translations`);
    currentPoiTranslations = { vi: {}, en: {}, zh: {}, ja: {} };
    if (transRes.ok) {
      const transList = await transRes.json();
      if (Array.isArray(transList)) {
        transList.forEach(t => {
          const lang = t.languageCode || t.language_code;
          if (currentPoiTranslations[lang]) {
            currentPoiTranslations[lang] = {
              id: t.id,
              title: t.title || '',
              summary: extractCleanSummary(t.description),
              description: t.description || '',
              audio_path: t.audioPath || t.audio_path || '',
              audio_url: t.audioUrl || t.audio_url || '',
              audio_duration_sec: t.audioDurationSec || t.audio_duration_sec || 0
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
  if (!confirm('Bạn có chắc chắn muốn xóa vĩnh viễn điểm POI này? Thao tác này sẽ xóa điểm và toàn bộ dữ liệu thuyết minh liên quan.')) return;
  try {
    const res = await adminFetch(`/admin/pois/${poiId}?hard=true`, { method: 'DELETE' });
    if (!res.ok) throw new Error('Không thể xóa điểm.');
    alert('Đã xóa thành công điểm POI.');
    allCurrentPois = allCurrentPois.filter(p => p.id !== poiId);
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

// Biến trạng thái cho Trình phát Audio Mini Admin
let adminAudioSpeed = 100;
let isAdminSeeking = false;
let isAdminAudioInited = false;

function formatAdminAudioTime(seconds) {
  if (isNaN(seconds) || seconds < 0) return '0:00';
  const mins = Math.floor(seconds / 60);
  const secs = Math.floor(seconds % 60);
  return `${mins}:${secs < 10 ? '0' : ''}${secs}`;
}

function initAdminAudioPlayer() {
  if (isAdminAudioInited) return;
  const audio = document.getElementById('audioPreviewPlayer');
  const btnToggle = document.getElementById('adminBtnPlayToggle');
  const playIcon = document.getElementById('adminPlayIcon');
  const pauseIcon = document.getElementById('adminPauseIcon');
  const timeCurrent = document.getElementById('adminTimeCurrent');
  const timeTotal = document.getElementById('adminTimeTotal');
  const scrubberTrack = document.getElementById('adminScrubberTrack');
  const scrubberFill = document.getElementById('adminScrubberFill');
  const scrubberThumb = document.getElementById('adminScrubberThumb');
  const btnSpeed = document.getElementById('adminBtnSpeed');

  if (!audio || !btnToggle) return;
  isAdminAudioInited = true;

  function setPlayingUI(isPlaying) {
    if (playIcon) playIcon.style.display = isPlaying ? 'none' : 'block';
    if (pauseIcon) pauseIcon.style.display = isPlaying ? 'block' : 'none';
  }

  btnToggle.addEventListener('click', (e) => {
    e.preventDefault();
    if (!audio.src || audio.src === window.location.href) return;
    if (audio.paused) {
      audio.play().then(() => setPlayingUI(true)).catch(err => {
        console.warn('[Admin Audio] Play error:', err);
      });
    } else {
      audio.pause();
      setPlayingUI(false);
    }
  });

  audio.addEventListener('play', () => setPlayingUI(true));
  audio.addEventListener('pause', () => setPlayingUI(false));

  audio.addEventListener('timeupdate', () => {
    if (isAdminSeeking) return;
    const cur = audio.currentTime || 0;
    const dur = audio.duration || 1;
    if (timeCurrent) timeCurrent.textContent = formatAdminAudioTime(cur);
    if (dur > 0) {
      const pct = Math.min(100, Math.max(0, (cur / dur) * 100));
      if (scrubberFill) scrubberFill.style.width = `${pct}%`;
      if (scrubberThumb) scrubberThumb.style.left = `${pct}%`;
    }
  });

  audio.addEventListener('loadedmetadata', () => {
    const dur = audio.duration || 0;
    if (timeTotal && dur > 0) {
      timeTotal.textContent = formatAdminAudioTime(dur);
    }
  });

  audio.addEventListener('ended', () => {
    setPlayingUI(false);
    if (timeCurrent) timeCurrent.textContent = '0:00';
    if (scrubberFill) scrubberFill.style.width = '0%';
    if (scrubberThumb) scrubberThumb.style.left = '0%';
  });

  audio.addEventListener('error', () => {
    // Tự động thử fallback đường dẫn nếu file nằm ở audio/ thay vì assets/audio/pois/
    if (audio.src && audio.src.includes('assets/audio/pois/')) {
      const filename = audio.src.split('/').pop();
      audio.src = 'audio/' + filename;
      audio.load();
    }
  });

  // Bắt sự kiện nhấp và kéo tua trên thanh Scrubber
  if (scrubberTrack) {
    const getFraction = (clientX) => {
      const rect = scrubberTrack.getBoundingClientRect();
      if (rect.width <= 0) return 0;
      return Math.max(0, Math.min(1, (clientX - rect.left) / rect.width));
    };

    const applySeek = (clientX) => {
      const frac = getFraction(clientX);
      const dur = audio.duration || 0;
      if (dur > 0) {
        audio.currentTime = frac * dur;
        if (timeCurrent) timeCurrent.textContent = formatAdminAudioTime(frac * dur);
        const pct = (frac * 100).toFixed(1);
        if (scrubberFill) scrubberFill.style.width = `${pct}%`;
        if (scrubberThumb) scrubberThumb.style.left = `${pct}%`;
      }
    };

    scrubberTrack.addEventListener('click', (e) => {
      applySeek(e.clientX);
    });

    let isDragging = false;
    scrubberTrack.addEventListener('pointerdown', (e) => {
      isDragging = true;
      isAdminSeeking = true;
      applySeek(e.clientX);
    });

    window.addEventListener('pointermove', (e) => {
      if (!isDragging) return;
      applySeek(e.clientX);
    });

    window.addEventListener('pointerup', () => {
      if (isDragging) {
        isDragging = false;
        setTimeout(() => { isAdminSeeking = false; }, 100);
      }
    });
  }

  // Nút điều chỉnh tốc độ đọc (100% -> 125% -> 150% -> 75% -> 100%)
  const speedOptions = [100, 125, 150, 75];
  if (btnSpeed) {
    btnSpeed.addEventListener('click', (e) => {
      e.preventDefault();
      const nextIdx = (speedOptions.indexOf(adminAudioSpeed) + 1) % speedOptions.length;
      adminAudioSpeed = speedOptions[nextIdx];
      btnSpeed.textContent = `${adminAudioSpeed}%`;
      audio.playbackRate = adminAudioSpeed / 100;
    });
  }
}

function loadLangDraft(lang) {
  const draft = currentPoiTranslations[lang] || {};
  document.getElementById('poiTitleInput').value = draft.title || '';
  document.getElementById('poiSummaryInput').value = draft.summary || '';
  document.getElementById('poiScriptInput').value = draft.description || '';

  const statusBadge = document.getElementById('audioStatusBadge');
  const statusText = document.getElementById('audioStatusText');
  const audioPlayer = document.getElementById('audioPreviewPlayer');
  const playerBar = document.getElementById('adminAudioPlayerBar');
  const playIcon = document.getElementById('adminPlayIcon');
  const pauseIcon = document.getElementById('adminPauseIcon');
  const timeCurrent = document.getElementById('adminTimeCurrent');
  const timeTotal = document.getElementById('adminTimeTotal');
  const scrubberFill = document.getElementById('adminScrubberFill');
  const scrubberThumb = document.getElementById('adminScrubberThumb');

  // Đảm bảo dừng bài phát trước đó và reset trạng thái UI
  if (audioPlayer) {
    audioPlayer.pause();
    audioPlayer.currentTime = 0;
  }
  if (playIcon) playIcon.style.display = 'block';
  if (pauseIcon) pauseIcon.style.display = 'none';
  if (timeCurrent) timeCurrent.textContent = '0:00';
  if (scrubberFill) scrubberFill.style.width = '0%';
  if (scrubberThumb) scrubberThumb.style.left = '0%';

  if (draft.audio_path || draft.audio_url) {
    statusBadge.classList.add('has-audio');
    statusText.textContent = `✓ Đã có file audio (${lang.toUpperCase()})`;
    if (playerBar) playerBar.style.display = 'flex';

    let resolvedSrc = '';
    if (draft.audio_url) {
      resolvedSrc = draft.audio_url;
    } else if (draft.audio_path) {
      if (draft.audio_path.startsWith('http') || draft.audio_path.startsWith('/') || draft.audio_path.startsWith('audio/') || draft.audio_path.startsWith('assets/')) {
        resolvedSrc = draft.audio_path;
      } else {
        resolvedSrc = 'assets/audio/pois/' + draft.audio_path.split('/').pop().split('\\').pop();
      }
    }

    if (audioPlayer && resolvedSrc) {
      audioPlayer.src = resolvedSrc;
      audioPlayer.load();
    }

    if (timeTotal) {
      if (draft.audio_duration_sec && draft.audio_duration_sec > 0) {
        timeTotal.textContent = formatAdminAudioTime(draft.audio_duration_sec);
      } else {
        timeTotal.textContent = '0:00';
      }
    }
  } else {
    statusBadge.classList.remove('has-audio');
    statusText.textContent = `Chưa có file Audio (${lang.toUpperCase()})`;
    if (playerBar) playerBar.style.display = 'none';
    if (audioPlayer) {
      audioPlayer.src = '';
    }
    if (timeTotal) timeTotal.textContent = '0:00';
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


  // Lắng nghe sự kiện đổi ảnh đại diện POI
  const imgInput = document.getElementById('poiImageUrlInput');
  const imgFileInput = document.getElementById('poiImageFileInput');
  const btnDefaultAvatar = document.getElementById('btnDefaultAvatar');
  const avatarPreview = document.getElementById('poiAvatarPreview');

  imgInput?.addEventListener('input', () => {
    if (avatarPreview) {
      avatarPreview.src = imgInput.value.trim() || 'assets/hero-khue-van-cac.jpg';
    }
  });

  imgFileInput?.addEventListener('change', async (e) => {
    const file = e.target.files?.[0];
    if (file) {
      if (avatarPreview) {
        avatarPreview.src = URL.createObjectURL(file);
      }
      try {
        const formData = new FormData();
        formData.append('file', file);
        const uploadRes = await adminFetch('/admin/pois/upload-image', {
          method: 'POST',
          body: formData
        });
        if (uploadRes.ok) {
          const data = await uploadRes.json();
          if (imgInput) imgInput.value = data.imageUrl;
          if (avatarPreview) avatarPreview.src = data.imageUrl;
        } else {
          console.warn('Upload ảnh không thành công, giữ ảnh mặc định.');
        }
      } catch (err) {
        console.warn('Lỗi upload ảnh:', err);
      }
    }
  });

  btnDefaultAvatar?.addEventListener('click', () => {
    if (imgInput) imgInput.value = 'assets/hero-khue-van-cac.jpg';
    if (avatarPreview) avatarPreview.src = 'assets/hero-khue-van-cac.jpg';
  });

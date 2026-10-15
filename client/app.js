/**
 * MultiGuide Web Test Client
 * Hệ thống mô phỏng khách tham quan & kiểm thử tương tác Leaflet
 */

// State Application
const state = {
  apiBaseUrl: localStorage.getItem('mg_api_url') || 'https://multiguide-1person.onrender.com',
  areaCode: 'van-mieu',
  areaData: null,
  accessToken: localStorage.getItem('mg_access_token') || null,
  passData: null,
  userLocation: {
    lat: 21.02844,
    lng: 105.83552
  },
  scanRadius: 50, // mét
  selectedLang: 'vi',
  nearbyPois: [],
  currentPlayingPoi: null,
  playStartTime: null
};

// Leaflet Map Objects
let map = null;
let userMarker = null;
let radiusCircle = null;
let poiMarkersLayer = null;

// DOM Elements
const apiEndpointBadge = document.getElementById('apiEndpointBadge');
const statusDot = document.getElementById('statusDot');
const apiEndpointLabel = document.getElementById('apiEndpointLabel');
const btnToggleApi = document.getElementById('btnToggleApi');

const areaCodeInput = document.getElementById('areaCodeInput');
const btnLoadArea = document.getElementById('btnLoadArea');
const areaNameLabel = document.getElementById('areaNameLabel');
const areaDescLabel = document.getElementById('areaDescLabel');
const areaStatusBadge = document.getElementById('areaStatusBadge');
const languageSelect = document.getElementById('languageSelect');

const btnBuyPass = document.getElementById('btnBuyPass');
const passStatusBadge = document.getElementById('passStatusBadge');
const passInfoBox = document.getElementById('passInfoBox');
const passStatusText = document.getElementById('passStatusText');
const passExpiresText = document.getElementById('passExpiresText');
const passTokenDisplay = document.getElementById('passTokenDisplay');

const userLatDisplay = document.getElementById('userLatDisplay');
const userLngDisplay = document.getElementById('userLngDisplay');
const radiusSlider = document.getElementById('radiusSlider');
const radiusValueLabel = document.getElementById('radiusValueLabel');
const btnCenterMap = document.getElementById('btnCenterMap');

const nearbyCount = document.getElementById('nearbyCount');
const nearbyGrid = document.getElementById('nearbyGrid');

const audioPlayerModal = document.getElementById('audioPlayerModal');
const playerPoiTitle = document.getElementById('playerPoiTitle');
const playerLanguageBadge = document.getElementById('playerLanguageBadge');
const playerPoiContent = document.getElementById('playerPoiContent');
const audioElement = document.getElementById('audioElement');
const btnClosePlayer = document.getElementById('btnClosePlayer');

// ==========================================
// 1. Khởi tạo & Kết nối Backend
// ==========================================

async function init() {
  updateApiDisplay();
  initMap();
  setupEventListeners();

  await checkApiHealth();
  await loadAreaDetails();

  if (state.accessToken) {
    await checkPassStatus();
  }

  // Lần đầu tải Nearby nếu đã có vé
  if (state.accessToken) {
    await fetchNearbyPois();
  }
}

function updateApiDisplay() {
  const isLocal = state.apiBaseUrl.includes('localhost') || state.apiBaseUrl.includes('127.0.0.1');
  apiEndpointLabel.textContent = isLocal ? 'Localhost (5267)' : 'Render Live API';
}

async function checkApiHealth() {
  try {
    const res = await fetch(`${state.apiBaseUrl}/health`);
    if (res.ok) {
      statusDot.className = 'status-dot';
      statusDot.title = 'Backend đang hoạt động (Healthy)';
    } else {
      statusDot.className = 'status-dot offline';
      statusDot.title = 'Backend không phản hồi 200';
    }
  } catch (err) {
    statusDot.className = 'status-dot offline';
    statusDot.title = `Không kết nối được: ${err.message}`;
  }
}

function toggleApiBaseUrl() {
  if (state.apiBaseUrl.includes('localhost')) {
    state.apiBaseUrl = 'https://multiguide-1person.onrender.com';
  } else {
    state.apiBaseUrl = 'http://localhost:5267';
  }
  localStorage.setItem('mg_api_url', state.apiBaseUrl);
  updateApiDisplay();
  checkApiHealth();
  loadAreaDetails();
}

// ==========================================
// 2. Tải thông tin Khu Du Lịch (US-01)
// ==========================================

async function loadAreaDetails() {
  const code = areaCodeInput.value.trim() || state.areaCode;
  state.areaCode = code;

  areaNameLabel.textContent = 'Đang tải dữ liệu khu...';
  areaDescLabel.textContent = '';

  try {
    const res = await fetch(`${state.apiBaseUrl}/areas/${code}`);
    if (!res.ok) {
      const err = await res.json().catch(() => ({ detail: 'Không tìm thấy khu tham quan' }));
      areaNameLabel.textContent = `Lỗi: ${err.detail || res.statusText}`;
      areaStatusBadge.className = 'badge badge-expired';
      areaStatusBadge.textContent = 'Không tồn tại';
      return;
    }

    const data = await res.json();
    state.areaData = data;

    areaNameLabel.textContent = data.name;
    areaDescLabel.textContent = `${data.pois?.length || 0} POI | Hạn vé: ${data.passTtlHours || 24}h`;
    areaStatusBadge.className = 'badge badge-active';
    areaStatusBadge.textContent = 'Hoạt động';

    // Cập nhật danh sách ngôn ngữ hỗ trợ
    if (data.supportedLanguages && data.supportedLanguages.length > 0) {
      languageSelect.innerHTML = '';
      data.supportedLanguages.forEach(l => {
        const opt = document.createElement('option');
        opt.value = l.code;
        opt.textContent = `${l.name} (${l.code})`;
        if (l.code === state.selectedLang) opt.selected = true;
        languageSelect.appendChild(opt);
      });
    }

    // Hiển thị POIs lên bản đồ
    renderPoisOnMap(data.pois || []);
  } catch (err) {
    areaNameLabel.textContent = `Lỗi kết nối: ${err.message}`;
  }
}

// ==========================================
// 3. Mua Thẻ Tham Quan (AccessPass) (US-02, US-04)
// ==========================================

async function buyPass() {
  btnBuyPass.disabled = true;
  btnBuyPass.innerHTML = '<span class="status-dot"></span> Đang thanh toán mock...';

  const idempotencyKey = crypto.randomUUID ? crypto.randomUUID() : 'idemp_' + Date.now();

  try {
    const res = await fetch(`${state.apiBaseUrl}/areas/${state.areaCode}/passes`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Idempotency-Key': idempotencyKey
      },
      body: JSON.stringify({
        amount: 50000,
        durationHours: 24
      })
    });

    if (!res.ok) {
      const errorJson = await res.json().catch(() => ({ detail: 'Không thể mua vé' }));
      alert(`Lỗi mua vé (${res.status}): ${errorJson.detail || errorJson.title || 'Lỗi không xác định'}`);
      return;
    }

    const data = await res.json();
    state.accessToken = data.accessToken;
    localStorage.setItem('mg_access_token', data.accessToken);

    updatePassUi(data.status || 'ACTIVE', data.expiresAt, data.accessToken);
    alert('Mua vé tham quan thành công! Bạn có thể quét vị trí và nghe thuyết minh.');
    
    await fetchNearbyPois();
  } catch (err) {
    alert(`Lỗi kết nối khi mua vé: ${err.message}`);
  } finally {
    btnBuyPass.disabled = false;
    btnBuyPass.innerHTML = `
      <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><rect x="2" y="4" width="20" height="16" rx="2"/><line x1="1" y1="10" x2="23" y2="10"/></svg>
      Mua lại vé mới (Mock 50,000 đ)
    `;
  }
}

async function checkPassStatus() {
  if (!state.accessToken) return;

  try {
    const res = await fetch(`${state.apiBaseUrl}/passes/current`, {
      headers: {
        'Authorization': `Bearer ${state.accessToken}`
      }
    });

    if (res.ok) {
      const data = await res.json();
      updatePassUi(data.status, data.expiresAt, state.accessToken);
    } else {
      // Vé hết hạn hoặc không hợp lệ
      passStatusBadge.className = 'badge badge-expired';
      passStatusBadge.textContent = 'Hết hạn / Lỗi';
      passInfoBox.style.display = 'none';
      localStorage.removeItem('mg_access_token');
      state.accessToken = null;
    }
  } catch (err) {
    console.error('Lỗi kiểm tra pass:', err);
  }
}

function updatePassUi(status, expiresAt, token) {
  passInfoBox.style.display = 'flex';
  passStatusBadge.className = status === 'ACTIVE' ? 'badge badge-active' : 'badge badge-expired';
  passStatusBadge.textContent = status;
  passStatusText.textContent = status;

  if (expiresAt) {
    const expDate = new Date(expiresAt);
    passExpiresText.textContent = expDate.toLocaleTimeString() + ' ' + expDate.toLocaleDateString();
  }

  if (token) {
    passTokenDisplay.textContent = token.slice(0, 10) + '...' + token.slice(-8);
    passTokenDisplay.title = token;
  }
}

// ==========================================
// 4. Khởi tạo Bản đồ Leaflet & Tọa độ GPS
// ==========================================

function initMap() {
  const initialLat = state.userLocation.lat;
  const initialLng = state.userLocation.lng;

  // Tạo map trung tâm Văn Miếu
  map = L.map('map', {
    zoomControl: true
  }).setView([initialLat, initialLng], 18);

  // Thêm OpenStreetMap Tile
  L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
    maxZoom: 20,
    attribution: '&copy; OpenStreetMap contributors | MultiGuide'
  }).addTo(map);

  poiMarkersLayer = L.layerGroup().addTo(map);

  // User Marker Icon
  const userIcon = L.divIcon({
    className: 'user-pin-container',
    html: `
      <div style="
        width: 24px; height: 24px;
        background: #06b6d4;
        border: 3px solid #ffffff;
        border-radius: 50%;
        box-shadow: 0 0 12px rgba(6,182,212,0.8);
      "></div>
    `,
    iconSize: [24, 24],
    iconAnchor: [12, 12]
  });

  // Tạo Marker vị trí khách
  userMarker = L.marker([initialLat, initialLng], {
    icon: userIcon,
    draggable: true,
    title: 'Vị trí của bạn (Kéo thả hoặc click bản đồ để di chuyển)'
  }).addTo(map);

  userMarker.bindTooltip('<b>Vị trí của bạn</b><br>Nhấp bản đồ để di chuyển', {
    permanent: false,
    direction: 'top'
  });

  // Vòng tròn bán kính quét
  radiusCircle = L.circle([initialLat, initialLng], {
    radius: state.scanRadius,
    color: '#06b6d4',
    fillColor: '#06b6d4',
    fillOpacity: 0.15,
    weight: 2,
    dashArray: '4, 8'
  }).addTo(map);

  // Sự kiện kéo thả marker
  userMarker.on('dragend', function (e) {
    const latlng = e.target.getLatLng();
    updateUserPosition(latlng.lat, latlng.lng);
  });

  // Sự kiện nhấp trực tiếp vào bản đồ để chuyển vị trí
  map.on('click', function (e) {
    updateUserPosition(e.latlng.lat, e.latlng.lng);
  });
}

function updateUserPosition(lat, lng) {
  state.userLocation.lat = lat;
  state.userLocation.lng = lng;

  userMarker.setLatLng([lat, lng]);
  radiusCircle.setLatLng([lat, lng]);

  userLatDisplay.textContent = lat.toFixed(5);
  userLngDisplay.textContent = lng.toFixed(5);

  fetchNearbyPois();
}

function renderPoisOnMap(pois) {
  poiMarkersLayer.clearLayers();

  pois.forEach(poi => {
    // Marker icon cho POI
    const poiIcon = L.divIcon({
      className: 'poi-map-pin',
      html: `
        <div id="poi-pin-${poi.id}" style="
          width: 20px; height: 20px;
          background: #f59e0b;
          border: 2px solid #ffffff;
          border-radius: 50%;
          box-shadow: 0 0 8px rgba(0,0,0,0.4);
          transition: all 0.3s;
        "></div>
      `,
      iconSize: [20, 20],
      iconAnchor: [10, 10]
    });

    const m = L.marker([poi.lat, poi.lng], { icon: poiIcon }).addTo(poiMarkersLayer);

    m.bindPopup(`
      <div style="font-family: var(--font-family); color: #0f172a; padding: 4px;">
        <h4 style="margin: 0 0 4px 0; font-size: 1rem;">${poi.title || 'Điểm tham quan'}</h4>
        <p style="margin: 0 0 8px 0; font-size: 0.8rem; color: #64748b;">Bán kính kích hoạt: ${poi.triggerRadiusM || 30}m</p>
        <button onclick="window.playPoiAudio('${poi.id}')" style="
          background: #0284c7; color: white; border: none; padding: 6px 12px; border-radius: 6px; font-weight: 600; cursor: pointer; width: 100%;
        ">Nghe thuyết minh</button>
      </div>
    `);

    // Vẽ thêm vòng tròn trigger của POI
    L.circle([poi.lat, poi.lng], {
      radius: poi.triggerRadiusM || 30,
      color: '#f59e0b',
      fillColor: '#f59e0b',
      fillOpacity: 0.05,
      weight: 1
    }).addTo(poiMarkersLayer);
  });
}

// ==========================================
// 5. Lấy danh sách POI Lân Cận (US-05)
// ==========================================

async function fetchNearbyPois() {
  if (!state.accessToken) {
    nearbyCount.textContent = '0';
    nearbyGrid.innerHTML = `
      <div style="color: var(--text-muted); font-size: 0.85rem; padding: 10px; grid-column: 1/-1;">
        Vui lòng mua vé tham quan ở bước 2 để quét và nghe các điểm lân cận.
      </div>
    `;
    return;
  }

  const { lat, lng } = state.userLocation;
  const radius = state.scanRadius;
  const lang = state.selectedLang;

  try {
    const url = `${state.apiBaseUrl}/pois/nearby?lat=${lat}&lng=${lng}&radius=${radius}&lang=${lang}`;
    const res = await fetch(url, {
      headers: {
        'Authorization': `Bearer ${state.accessToken}`
      }
    });

    if (!res.ok) {
      if (res.status === 401 || res.status === 403) {
        passStatusBadge.className = 'badge badge-expired';
        passStatusBadge.textContent = 'Hết hạn';
      }
      return;
    }

    const pois = await res.json();
    state.nearbyPois = pois;
    nearbyCount.textContent = pois.length;

    renderNearbyGrid(pois);
    updatePoiMarkersHighlight(pois);
  } catch (err) {
    console.error('Lỗi gọi GET /pois/nearby:', err);
  }
}

function renderNearbyGrid(pois) {
  if (!pois || pois.length === 0) {
    nearbyGrid.innerHTML = `
      <div style="color: var(--text-muted); font-size: 0.85rem; padding: 10px; grid-column: 1/-1;">
        Không có điểm tham quan nào trong bán kính ${state.scanRadius} mét. Hãy kéo vị trí lại gần các điểm Văn Miếu.
      </div>
    `;
    return;
  }

  nearbyGrid.innerHTML = '';
  pois.forEach(p => {
    const card = document.createElement('div');
    card.className = `poi-card ${p.withinTrigger ? 'within-trigger' : ''}`;
    
    card.innerHTML = `
      <div class="poi-card-title">
        <span>${p.title}</span>
        <span class="poi-card-distance">${p.distanceM}m</span>
      </div>
      <div style="display: flex; justify-content: space-between; align-items: center; margin-top: 4px;">
        <span class="poi-card-trigger">
          ${p.withinTrigger ? 'Trong vùng kích hoạt' : 'Ngoài bán kính phát'}
        </span>
        <button class="btn-primary" style="padding: 4px 10px; font-size: 0.75rem;" onclick="event.stopPropagation(); window.playPoiAudio('${p.id}')">
          Nghe audio
        </button>
      </div>
    `;

    card.addEventListener('click', () => {
      map.flyTo([p.lat, p.lng], 19);
    });

    nearbyGrid.appendChild(card);
  });
}

function updatePoiMarkersHighlight(nearbyPois) {
  nearbyPois.forEach(p => {
    const pinEl = document.getElementById(`poi-pin-${p.id}`);
    if (pinEl) {
      if (p.withinTrigger) {
        pinEl.style.background = '#10b981';
        pinEl.style.boxShadow = '0 0 14px #10b981';
        pinEl.className = 'trigger-pulse';
      } else {
        pinEl.style.background = '#f59e0b';
        pinEl.style.boxShadow = '0 0 8px rgba(0,0,0,0.4)';
        pinEl.className = '';
      }
    }
  });
}

// ==========================================
// 6. Phát Audio & Ghi Play-Log (US-06, US-07, US-08)
// ==========================================

window.playPoiAudio = async function (poiId) {
  if (!state.accessToken) {
    alert('Bạn cần mua vé tham quan trước khi nghe audio.');
    return;
  }

  // Kết thúc lượt nghe trước nếu có
  await sendPlayLogIfNeeded();

  try {
    const res = await fetch(`${state.apiBaseUrl}/pois/${poiId}?lang=${state.selectedLang}`, {
      headers: {
        'Authorization': `Bearer ${state.accessToken}`
      }
    });

    if (!res.ok) {
      const err = await res.json().catch(() => ({ detail: 'Không tải được audio' }));
      alert(`Lỗi (${res.status}): ${err.detail || 'Không tìm thấy nội dung POI'}`);
      return;
    }

    const poiDetail = await res.json();
    state.currentPlayingPoi = poiDetail;
    state.playStartTime = Date.now();

    playerPoiTitle.textContent = poiDetail.title;
    playerLanguageBadge.textContent = `Ngôn ngữ: ${poiDetail.languageCode?.toUpperCase() || state.selectedLang.toUpperCase()}`;
    playerPoiContent.textContent = poiDetail.description || 'Không có mô tả chi tiết.';

    audioElement.src = poiDetail.audioUrl;
    audioPlayerModal.style.display = 'flex';

    audioElement.play().catch(e => {
      console.warn('Tự động phát bị chặn bởi trình duyệt, người dùng bấm nút play:', e);
    });
  } catch (err) {
    alert(`Lỗi khi tải chi tiết POI: ${err.message}`);
  }
};

async function sendPlayLogIfNeeded() {
  if (!state.currentPlayingPoi || !state.playStartTime || !state.accessToken) return;

  const durationSeconds = Math.max(1, Math.round((Date.now() - state.playStartTime) / 1000));
  const poiId = state.currentPlayingPoi.id;

  try {
    await fetch(`${state.apiBaseUrl}/play-logs`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${state.accessToken}`
      },
      body: JSON.stringify({
        poiId: poiId,
        durationSeconds: durationSeconds
      })
    });
    console.log(`Đã gửi play-log: POI ${poiId}, ${durationSeconds} giây`);
  } catch (err) {
    console.warn('Lỗi ghi play-log:', err);
  } finally {
    state.currentPlayingPoi = null;
    state.playStartTime = null;
  }
}

// ==========================================
// 7. Event Listeners
// ==========================================

function setupEventListeners() {
  btnToggleApi.addEventListener('click', toggleApiBaseUrl);
  btnLoadArea.addEventListener('click', loadAreaDetails);
  btnBuyPass.addEventListener('click', buyPass);

  languageSelect.addEventListener('change', () => {
    state.selectedLang = languageSelect.value;
    fetchNearbyPois();
  });

  radiusSlider.addEventListener('input', () => {
    const val = parseInt(radiusSlider.value, 10);
    state.scanRadius = val;
    radiusValueLabel.textContent = `${val} m`;
    if (radiusCircle) {
      radiusCircle.setRadius(val);
    }
  });

  radiusSlider.addEventListener('change', () => {
    fetchNearbyPois();
  });

  btnCenterMap.addEventListener('click', () => {
    map.setView([state.userLocation.lat, state.userLocation.lng], 18);
  });

  btnClosePlayer.addEventListener('click', async () => {
    audioElement.pause();
    await sendPlayLogIfNeeded();
    audioPlayerModal.style.display = 'none';
  });

  audioElement.addEventListener('ended', async () => {
    await sendPlayLogIfNeeded();
  });
}

// Khởi chạy
window.addEventListener('DOMContentLoaded', init);

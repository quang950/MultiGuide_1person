/**
 * MultiGuide - Leaflet Map Manager, Sacred Axis & Dual GPS Controller (Cụm 2 & 3)
 * Quản lý Bản đồ di sản Zen, 6 POI bia đá, trục Thần đạo, Marker Radar và Geofencing API
 */
import { CONFIG } from './config.js';
import { store } from './state.js';
import { apiService } from './api.js';
import { geoService } from './geo.js';
import { audioEngine, POI_STORIES } from './audio.js';
import { showProximityBeacon, showBottomSheet, hideBottomSheet } from './ui.js';

// Dữ liệu chuẩn 6 POIs Thần đạo Văn Miếu - Quốc Tử Giám (Khớp 100% DbSeeder Backend & Tọa độ thực địa Google Maps)
export const HERITAGE_POIS = [
  {
    id: 'c1875fb2-84ad-4a5a-936a-173fdbf4f7f3',
    guid: 'c1875fb2-84ad-4a5a-936a-173fdbf4f7f3',
    order: 1,
    priority: 10,
    code: 'cong-van-mieu',
    title: 'Cổng Văn Miếu',
    titleEn: 'Van Mieu Gate',
    lat: 21.02720,
    lng: 105.83580,
    triggerRadiusM: 20,
    labelPos: 'left',
    isActive: false,
    isNearby: false
  },
  {
    id: '3ede2134-c305-4270-9413-39c9743f9cb8',
    guid: '3ede2134-c305-4270-9413-39c9743f9cb8',
    order: 2,
    priority: 20,
    code: 'khue-van-cac',
    title: 'Khuê Văn Các',
    titleEn: 'Khue Van Pavilion',
    lat: 21.02820,
    lng: 105.83580,
    triggerRadiusM: 20,
    labelPos: 'right',
    isActive: false,
    isNearby: false
  },
  {
    id: '70dabf88-5450-48e7-9603-b753f5350047',
    guid: '70dabf88-5450-48e7-9603-b753f5350047',
    order: 3,
    priority: 30,
    code: 'gieng-thien-quang',
    title: 'Giếng Thiên Quang & Vườn Bia Tiến Sĩ',
    titleEn: 'Thien Quang Well & Stelae',
    lat: 21.02860,
    lng: 105.83580,
    triggerRadiusM: 20,
    labelPos: 'left',
    isActive: false,
    isNearby: false
  },
  {
    id: 'cc63b968-096f-4b06-b4f8-19f8f829ef42',
    guid: 'cc63b968-096f-4b06-b4f8-19f8f829ef42',
    order: 4,
    priority: 40,
    code: 'cong-dai-thanh',
    title: 'Cổng Đại Thành & Sân Đại Bái',
    titleEn: 'Dai Thanh Gate & Courtyard',
    lat: 21.02900,
    lng: 105.83580,
    triggerRadiusM: 20,
    labelPos: 'right',
    isActive: false,
    isNearby: false
  },
  {
    id: '0c23b2e0-73d1-4f37-90a0-f95201abf43b',
    guid: '0c23b2e0-73d1-4f37-90a0-f95201abf43b',
    order: 5,
    priority: 50,
    code: 'dien-dai-thanh',
    title: 'Điện Đại Thành',
    titleEn: 'Dai Thanh Sanctuary',
    lat: 21.02940,
    lng: 105.83580,
    triggerRadiusM: 20,
    labelPos: 'left',
    isActive: false,
    isNearby: false
  },
  {
    id: 'cd3c98eb-79fe-4980-a870-6f690f9e40cd',
    guid: 'cd3c98eb-79fe-4980-a870-6f690f9e40cd',
    order: 6,
    priority: 60,
    code: 'nha-thai-hoc',
    title: 'Nhà Thái Học',
    titleEn: 'Thai Hoc Academy',
    lat: 21.02980,
    lng: 105.83580,
    triggerRadiusM: 20,
    labelPos: 'right',
    isActive: false,
    isNearby: false
  }
];

export class MapManager {
  constructor() {
    this.map = null;
    this.poiMarkers = new Map();
    this.sacredAxisLine = null;
    this.sacredBranches = [];
    this.userMarker = null;
    this.isCheckingGeofence = false;
    this.lastAutoTriggeredPoiId = null; // Tránh lặp âm thanh và giật popup (Anti-jitter)
    this.isAutoPlayAudio = true; // Mặc định tự động thuyết minh & mở thông tin khi đến gần
  }

  /**
   * Khởi tạo bản đồ Leaflet (FE-3.1)
   */
  async init(containerId = 'mapCanvas') {
    const container = document.getElementById(containerId);
    if (!container || this.map) return;

    const center = [CONFIG.MAP_CENTER.lat, CONFIG.MAP_CENTER.lng];

    // 1. Khởi tạo đối tượng Leaflet Map (tự do kéo di chuyển khắp vùng lân cận)
    this.map = L.map(containerId, {
      center: center,
      zoom: CONFIG.MAP_CENTER.zoom,
      minZoom: CONFIG.MAP_CENTER.minZoom || 12,
      maxZoom: CONFIG.MAP_CENTER.maxZoom || 20,
      zoomControl: false,
      attributionControl: false
    });

    // 2. Nạp Tile Layer Google Maps chuẩn, sắc nét, không watermark, không cần API key
    const tileUrl = CONFIG.MAP_TILE_URL || 'https://{s}.google.com/vt/lyrs=m&x={x}&y={y}&z={z}';
    L.tileLayer(tileUrl, {
      subdomains: ['mt0', 'mt1', 'mt2', 'mt3'],
      maxZoom: 20,
      attribution: CONFIG.MAP_ATTRIBUTION || '&copy; Google Maps'
    }).addTo(this.map);

    // 2.5 Nạp danh sách POIs động từ Backend API nếu có kết nối
    await this.loadPoisFromBackend();

    // 3. Vẽ đường trục Thần đạo nét đứt màu đồng (FE-3.2)
    this.renderSacredAxis();

    // 4. Vẽ 7 POI Custom Pin Markers bia đá (FE-3.3)
    this.renderPoiMarkers();

    // 5. Tự động căn khung hình vừa vặn 7 POI giữa Header và Bottom controls
    const poiCoords = HERITAGE_POIS.map(p => [p.lat, p.lng]);
    this.map.fitBounds(poiCoords, {
      paddingTopLeft: [40, 115], // Tránh va chạm với Header nổi và GPS pill
      paddingBottomRight: [40, 105], // Tránh va chạm với thước tỉ lệ và nút Recenter
      maxZoom: 18.2
    });

    // 6. Gắn sự kiện nút Recenter GPS (FE-3.4)
    this.bindRecenterButton();

    // 6.5 Gắn sự kiện bật/tắt Tự động thuyết minh GPS Audio Pill
    this.bindGpsAudioPill();

    // 7. Khởi tạo Marker Vị Trí Du Khách & Radar Sóng Thở Đôi (FE-4.1)
    this.initUserMarker();

    // 8. Bắt sự kiện Click-to-Move giả lập trên bản đồ (FE-4.2)
    this.bindMapClickToMove();

    // 9. Bật dịch vụ GPS lắng nghe tọa độ & Geofencing (FE-4.2 & FE-4.3)
    this.initGeoTracking();

    // 10. Tự động resize bản đồ khi đổi kích thước màn hình / xoay ngang / desktop split view
    window.addEventListener('resize', () => {
      if (this.map) {
        this.map.invalidateSize();
      }
    });

    // 11. Đăng ký lắng nghe thay đổi ngôn ngữ để cập nhật nhãn Marker trên bản đồ & GPS pill
    store.subscribe((state) => {
      if (state.selectedLang) {
        this.updateAllMarkerLabels();
        this.updateGpsAudioPillUI();
      }
    });

    window.map = this.map;
    window.mapManager = this;
    window.HERITAGE_POIS = HERITAGE_POIS;
    window.geoService = geoService;
    console.log('[MultiGuide Map] Bản đồ di sản Zen và hệ thống GPS đã khởi tạo thành công.');
  }

  /**
   * Đồng bộ danh sách POIs từ Backend API (RESTful endpoint)
   */
  async loadPoisFromBackend() {
    try {
      const lang = store.get().selectedLang || 'vi';
      const center = CONFIG.MAP_CENTER;
      const data = await apiService.fetchNearbyPois(center.lat, center.lng, 480, lang);
      if (data && Array.isArray(data) && data.length > 0) {
        console.log(`[MultiGuide Map] Đã nhận ${data.length} POIs từ Backend API.`);
        // Sắp xếp theo priority (hoặc toạ độ từ Nam lên Bắc)
        const sorted = [...data].sort((a, b) => (a.priority || 0) - (b.priority || 0) || a.lat - b.lat);
        
        // Làm mới sạch sẽ danh sách POIs hiển thị, ngăn chặn triệt để tình trạng trùng lặp POI hoặc nhảy số thứ tự
        HERITAGE_POIS.length = 0;
        const currentActive = (typeof store !== 'undefined' && store.get().activePoi);
        sorted.forEach((item, index) => {
          const stopNumber = item.priority ? Math.round(item.priority / 10) : (index + 1);
          const isCurrentlyActive = currentActive && (String(currentActive.id) === String(item.id) || String(currentActive.guid) === String(item.id));
          HERITAGE_POIS.push({
            id: item.id,
            guid: item.id,
            order: stopNumber,
            priority: item.priority || (stopNumber * 10),
            code: item.code || `poi-${stopNumber}`,
            title: item.title,
            titleEn: item.titleEn || item.title,
            lat: item.lat,
            lng: item.lng,
            triggerRadiusM: 20, // Chuẩn hóa 20m kích hoạt phát âm thanh tự động cho trục Thần đạo
            labelPos: (index % 2 === 0) ? 'left' : 'right',
            isActive: isCurrentlyActive ? true : false,
            isNearby: false
          });
        });

        // Cập nhật lại đường trục Thần đạo và các ghim POI theo tọa độ động mới nhất từ Backend
        if (this.sacredAxisLine) {
          this.renderSacredAxis();
        }
        if (this.poiMarkers && this.poiMarkers.size > 0) {
          this.renderPoiMarkers();
        }
      }
    } catch (e) {
      console.warn('[MultiGuide Map] Kết nối backend gián đoạn, sử dụng danh sách 6 POIs mặc định:', e.message);
    }
  }

  /**
   * Khởi tạo Marker Vị Trí Du Khách với Xung Sóng Radar Thở Đôi (FE-4.1)
   */
  initUserMarker() {
    if (!this.map) return;

    // Vị trí mặc định chuẩn ảnh mẫu: Trước Cổng Đại Trung (2)
    const initialLocation = [21.02765, 105.8358];

    const radarHtml = `
      <div class="user-radar-container">
        <div class="radar-ring-2"></div>
        <div class="radar-ring-1"></div>
        <div class="user-center-dot"></div>
      </div>
    `;

    const userIcon = L.divIcon({
      html: radarHtml,
      className: 'custom-user-radar-icon',
      iconSize: [90, 90],
      iconAnchor: [45, 45]
    });

    this.userMarker = L.marker(initialLocation, {
      icon: userIcon,
      zIndexOffset: 1000 // Luôn nổi trên các đường kẻ và tiles
    }).addTo(this.map);
  }

  /**
   * Cập nhật vị trí Marker du khách trên bản đồ (FE-4.1)
   */
  updateUserMarkerLocation(lat, lng) {
    if (this.userMarker) {
      this.userMarker.setLatLng([lat, lng]);
    }
  }

  /**
   * Hỗ trợ Click-to-Move: Nhấp chuột trên bản đồ để di chuyển bước đi (FE-4.2)
   */
  bindMapClickToMove() {
    if (!this.map) return;

    this.map.on('click', (e) => {
      const { lat, lng } = e.latlng;
      geoService.simulatePosition(lat, lng);
    });
  }

  /**
   * Khởi động bộ theo dõi vị trí GPS & Geofencing tự động (FE-4.2 & FE-4.3)
   */
  initGeoTracking() {
    geoService.startWatching((lat, lng) => {
      // 1. Cập nhật marker trên bản đồ
      this.updateUserMarkerLocation(lat, lng);

      // 2. Kích hoạt thuật toán Geofencing tự động kết nối Backend thật (FE-4.3)
      this.checkGeofencing(lat, lng);
    });
  }

  /**
   * Khớp nối dữ liệu POI từ Backend (GUID) với danh sách POI trên bản đồ
   */
  findPoiMatch(nearbyItem) {
    if (!nearbyItem) return null;
    const targetGuid = nearbyItem.id || nearbyItem.poiId;
    return HERITAGE_POIS.find(p => 
      (targetGuid && (String(p.id).toLowerCase() === String(targetGuid).toLowerCase() || String(p.guid).toLowerCase() === String(targetGuid).toLowerCase())) ||
      (nearbyItem.code && p.code === nearbyItem.code) ||
      (nearbyItem.title && (p.title === nearbyItem.title || p.titleEn === nearbyItem.title))
    );
  }

  /**
   * Thuật toán Geofencing tự động kết nối API Backend và tính toán khoảng cách thông minh (FE-4.3)
   * - Discovery Radius: 50m (quét và lên màu đồng thời cho tất cả các POI trong 50m)
   * - Auto-Trigger Radius: 20m (kích hoạt tự động)
   * - Cách tiếp cận A (Hysteresis Switching): chỉ chuyển sang POI mới khi gần hơn POI đang phát >= 5m
   */
  async checkGeofencing(lat, lng) {
    if (this.isCheckingGeofence) return;
    this.isCheckingGeofence = true;

    const token = store.get().accessToken;
    const lang = store.get().selectedLang || 'vi';
    const DISCOVERY_RADIUS = CONFIG.DEFAULT_SCAN_RADIUS || 50; // 50m quét lân cận
    const AUTO_TRIGGER_RADIUS = 20; // 20m tự động kích hoạt âm thanh

    try {
      // 1. Tính toán khoảng cách thực tế (Haversine) cho toàn bộ 6 POI
      HERITAGE_POIS.forEach(p => {
        p.distanceM = geoService.calculateDistance(lat, lng, p.lat, p.lng);
      });

      // 2. Cập nhật trạng thái 'Ở gần bạn' đồng thời cho TẤT CẢ các POI trong bán kính 50m
      const nearbyPoiIds = new Set(
        HERITAGE_POIS
          .filter(p => p.distanceM <= DISCOVERY_RADIUS)
          .map(p => String(p.id))
      );
      this.updateNearbyPois(nearbyPoiIds);

      // 3. Đồng bộ với Backend API nếu có kết nối
      try {
        const backendNearby = await apiService.fetchNearbyPois(lat, lng, DISCOVERY_RADIUS, lang, token);
        if (backendNearby && Array.isArray(backendNearby)) {
          store.set({ nearbyPois: backendNearby });
        }
      } catch (apiErr) {
        // Backend bận hoặc cold start: tiếp tục dùng kết quả Haversine
      }

      // 4. Lọc các POI nằm trong bán kính tự động kích hoạt (<= 20m)
      const triggerCandidates = HERITAGE_POIS
        .filter(p => p.distanceM <= (p.triggerRadiusM || AUTO_TRIGGER_RADIUS))
        .sort((a, b) => a.distanceM - b.distanceM); // Nearest Distance First

      if (triggerCandidates.length === 0) {
        // Không có POI nào trong bán kính 20m
        this.handlePoiLeftTrigger(lat, lng);
        return;
      }

      // 5. Chọn POI gần nhất
      const closestPoi = triggerCandidates[0];
      const currentActive = store.get().activePoi;

      if (!currentActive || !currentActive.id) {
        // Chưa có điểm nào đang phát -> Kích hoạt ngay điểm gần nhất
        this.handlePoiTriggered(closestPoi);
      } else if (String(currentActive.id) === String(closestPoi.id) || String(currentActive.guid) === String(closestPoi.id)) {
        // Điểm gần nhất chính là điểm đang phát -> Duy trì phát bình thường
        this.lastAutoTriggeredPoiId = closestPoi.id;
      } else {
        // Đang phát điểm A, nhưng điểm gần nhất là B:
        // Đo khoảng cách hiện tại từ du khách tới A và tới B
        const distCurrent = geoService.calculateDistance(lat, lng, currentActive.lat, currentActive.lng);
        const distClosest = closestPoi.distanceM;

        // CƠ CHẾ HYSTERESIS SWITCHING (Cách tiếp cận A):
        // Chỉ chuyển sang B nếu du khách gần B hơn A ít nhất 5 mét (chống rung giật GPS)
        if (distClosest < distCurrent - 5) {
          console.log(`[MultiGuide Hysteresis] Chuyển đổi POI: ${closestPoi.title} (${distClosest.toFixed(1)}m) gần hơn POI đang phát (${distCurrent.toFixed(1)}m) >= 5m.`);
          this.handlePoiTriggered(closestPoi);
        } else {
          console.log(`[MultiGuide Hysteresis] Giữ nguyên POI ${currentActive.title}: chênh lệch khoảng cách (${Math.abs(distCurrent - distClosest).toFixed(1)}m) chưa vượt ngưỡng an toàn 5m.`);
        }
      }
    } catch (err) {
      console.warn('[MultiGuide Geofence] Lỗi trong quá trình quét lân cận:', err.message);
    } finally {
      this.isCheckingGeofence = false;
    }
  }

  renderSacredAxis() {
    if (!this.map) return;

    if (this.sacredAxisLine) {
      this.map.removeLayer(this.sacredAxisLine);
    }
    if (this.sacredBranches) {
      this.sacredBranches.forEach(b => this.map.removeLayer(b));
    }
    this.sacredBranches = [];

    // Tuyến đường trục chính nối thẳng từ Cổng Văn Miếu (Nam) lên Nhà Thái Học (Bắc) theo thứ tự trạm
    const sortedPois = [...HERITAGE_POIS].sort((a, b) => (a.order || 0) - (b.order || 0) || a.lat - b.lat);
    const mainAxisCoords = sortedPois.map(p => [p.lat, p.lng]);

    this.sacredAxisLine = L.polyline(mainAxisCoords, {
      color: '#C28E58',
      weight: 2,
      dashArray: '5, 8',
      opacity: 0.75,
      lineCap: 'round'
    }).addTo(this.map);

    // 2 nhánh nét đứt vòng quanh hồ Giếng Thiên Quang và Vườn bia Tiến Sĩ
    const westPondBranch = [
      [21.0284, 105.8358],
      [21.0284, 105.8352],
      [21.0288, 105.8352],
      [21.0288, 105.8358]
    ];

    const eastPondBranch = [
      [21.0284, 105.8358],
      [21.0284, 105.8364],
      [21.0288, 105.8364],
      [21.0288, 105.8358]
    ];

    const b1 = L.polyline(westPondBranch, {
      color: '#C28E58',
      weight: 1.5,
      dashArray: '4, 6',
      opacity: 0.65
    }).addTo(this.map);

    const b2 = L.polyline(eastPondBranch, {
      color: '#C28E58',
      weight: 1.5,
      dashArray: '4, 6',
      opacity: 0.65
    }).addTo(this.map);

    this.sacredBranches.push(b1, b2);
  }

  /**
   * Tạo SVG Marker vòm bia đá kèm huy hiệu sóng âm thanh (FE-3.3)
   * Hỗ trợ 3 trạng thái:
   * - 'active': Đang phát thuyết minh (Màu đồng hoàng gia #C28E58)
   * - 'nearby': Đang ở gần <= 30m (Màu xanh ngọc di sản #2E7D32)
   * - 'idle': Trạng thái tĩnh chưa chọn (#FFFFFF)
   */
  createStelePinSvg(order, state = 'idle') {
    let bgColor = '#FFFFFF';
    let borderColor = '#C28E58';
    let textColor = '#8E6030';
    let waveBadgeBg = '#F4EADA';
    let waveColor = '#8E6030';
    let wrapperClass = '';

    if (state === 'active') {
      bgColor = '#C28E58';
      borderColor = '#C28E58';
      textColor = '#FFFFFF';
      waveBadgeBg = '#FFFFFF';
      waveColor = '#C28E58';
      wrapperClass = 'is-active';
    } else if (state === 'nearby') {
      bgColor = '#2E7D32';
      borderColor = '#1B5E20';
      textColor = '#FFFFFF';
      waveBadgeBg = '#E8F5E9';
      waveColor = '#2E7D32';
      wrapperClass = 'is-nearby';
    }

    return `
      <div class="poi-marker-wrapper ${wrapperClass}" data-poi-id="${order}">
        <svg class="poi-stele-pin" viewBox="0 0 38 48" fill="none" xmlns="http://www.w3.org/2000/svg">
          <!-- Hình vòm bia đá di sản giọt nước bo tròn trên, nhọn vuốt đáy -->
          <path d="M19 46C19 46 3 32 3 19C3 10.1634 10.1634 3 19 3C27.8366 3 35 10.1634 35 19C35 32 19 46 19 46Z" 
                fill="${bgColor}" stroke="${borderColor}" stroke-width="2" />
          
          <!-- Số thứ tự bia đá -->
          <text x="19" y="24" font-family="'Plus Jakarta Sans', sans-serif" font-size="14" font-weight="700" 
                fill="${textColor}" text-anchor="middle" dominant-baseline="central">${order}</text>
          
          <!-- Huy hiệu sóng âm thanh tròn nhỏ góc trên bên phải -->
          <circle cx="28" cy="10" r="5.5" fill="${waveBadgeBg}" stroke="${borderColor}" stroke-width="1" />
          <line x1="26" y1="9" x2="26" y2="11" stroke="${waveColor}" stroke-width="1" stroke-linecap="round"/>
          <line x1="28" y1="7.5" x2="28" y2="12.5" stroke="${waveColor}" stroke-width="1" stroke-linecap="round"/>
          <line x1="30" y1="9" x2="30" y2="11" stroke="${waveColor}" stroke-width="1" stroke-linecap="round"/>
        </svg>
      </div>
    `;
  }

  /**
   * Định vị và gắn 6 Custom Pin Markers lên bản đồ (FE-3.3)
   */
  renderPoiMarkers() {
    if (!this.map) return;

    if (this.poiMarkers && this.poiMarkers.size > 0) {
      this.poiMarkers.forEach(m => this.map.removeLayer(m));
      this.poiMarkers.clear();
    }

    HERITAGE_POIS.forEach(poi => {
      const state = poi.isActive ? 'active' : (poi.isNearby ? 'nearby' : 'idle');
      const pinHtml = this.createStelePinSvg(poi.order, state);

      const customIcon = L.divIcon({
        html: pinHtml,
        className: 'custom-poi-div-icon',
        iconSize: [38, 48],
        iconAnchor: [19, 48]
      });

      const marker = L.marker([poi.lat, poi.lng], { icon: customIcon }).addTo(this.map);

      // Nhãn địa danh hai bên (Side Labels)
      const labelClass = poi.labelPos === 'left' ? 'label-left' : 'label-right';
      const currentLang = (typeof store !== 'undefined' && store.get().selectedLang) || 'vi';

      let displayTitle = poi.title;
      const story = (POI_STORIES && (POI_STORIES[poi.id] || POI_STORIES[poi.order]));
      if (story && story[currentLang]) {
        displayTitle = story[currentLang].title;
      } else if (currentLang !== 'vi' && poi.titleEn) {
        displayTitle = poi.titleEn;
      }

      let statusHtml = '';
      if (poi.isActive) {
        const playingText = currentLang === 'vi' ? 'ĐANG PHÁT' : (currentLang === 'zh' ? '正在播放' : (currentLang === 'ja' ? '再生中' : 'NOW PLAYING'));
        statusHtml = `<span class="poi-label-status">${playingText}</span>`;
      } else if (poi.isNearby) {
        const nearbyText = currentLang === 'vi' ? 'Ở GẦN BẠN' : (currentLang === 'zh' ? '就在附近' : (currentLang === 'ja' ? 'すぐ近く' : 'NEARBY'));
        statusHtml = `<span class="poi-label-status status-nearby">${nearbyText}</span>`;
      }
      
      // Chỉ gắn nhãn trạng thái khi POI đang active hoặc nearby (đã xóa tên địa danh in đậm trên map)
      if (statusHtml) {
        const labelContent = `
          <div class="poi-side-label ${labelClass}">
            ${statusHtml}
          </div>
        `;
        marker.bindTooltip(labelContent, {
          permanent: true,
          direction: poi.labelPos,
          className: 'poi-tooltip-transparent',
          offset: poi.labelPos === 'left' ? [-16, -24] : [16, -24]
        });
      }

      // Tương tác bấm chọn POI -> Kích hoạt Trạng thái 3
      marker.on('click', () => {
        this.selectPoi(poi.id);
      });

      this.poiMarkers.set(poi.id, marker);
    });
  }

  /**
   * Cập nhật diện mạo (Icon + Tooltip) cho một POI
   */
  updateMarkerVisual(p) {
    const marker = this.poiMarkers.get(p.id);
    if (!marker) return;

    const state = p.isActive ? 'active' : (p.isNearby ? 'nearby' : 'idle');
    const updatedIcon = L.divIcon({
      html: this.createStelePinSvg(p.order, state),
      className: 'custom-poi-div-icon',
      iconSize: [38, 48],
      iconAnchor: [19, 48]
    });
    marker.setIcon(updatedIcon);

    const labelClass = p.labelPos === 'left' ? 'label-left' : 'label-right';
    const currentLang = store.get().selectedLang || 'vi';

    let statusHtml = '';
    if (p.isActive) {
      const playingText = currentLang === 'vi' ? 'ĐANG PHÁT' : (currentLang === 'zh' ? '正在播放' : (currentLang === 'ja' ? '再生中' : 'NOW PLAYING'));
      statusHtml = `<span class="poi-label-status">${playingText}</span>`;
    } else if (p.isNearby) {
      const nearbyText = currentLang === 'vi' ? 'Ở GẦN BẠN' : (currentLang === 'zh' ? '就在附近' : (currentLang === 'ja' ? 'すぐ近く' : 'NEARBY'));
      statusHtml = `<span class="poi-label-status status-nearby">${nearbyText}</span>`;
    }

    if (statusHtml) {
      const labelContent = `
        <div class="poi-side-label ${labelClass}">
          ${statusHtml}
        </div>
      `;
      if (marker.getTooltip()) {
        marker.setTooltipContent(labelContent);
      } else {
        marker.bindTooltip(labelContent, {
          permanent: true,
          direction: p.labelPos,
          className: 'poi-tooltip-transparent',
          offset: p.labelPos === 'left' ? [-16, -24] : [16, -24]
        });
      }
    } else {
      if (marker.getTooltip()) {
        marker.unbindTooltip();
      }
    }
  }

  /**
   * Cập nhật toàn bộ nhãn 6 POI khi người dùng đổi ngôn ngữ
   */
  updateAllMarkerLabels() {
    HERITAGE_POIS.forEach(p => {
      this.updateMarkerVisual(p);
    });
  }

  /**
   * Kích hoạt trạng thái màu xanh ngọc (Nearby Proximity) cho POI khi du khách đến gần
   */
  /**
   * Cập nhật trạng thái màu xanh ngọc (Nearby Proximity) đồng thời cho TẤT CẢ các POI trong bán kính 50m
   */
  updateNearbyPois(nearbyPoiIds) {
    HERITAGE_POIS.forEach(p => {
      // Nếu POI này đang phát audio thì giữ nguyên active
      if (p.isActive) return;

      const isNear = nearbyPoiIds.has(String(p.id)) || nearbyPoiIds.has(String(p.guid)) || nearbyPoiIds.has(String(p.order));
      if (p.isNearby !== isNear) {
        p.isNearby = isNear;
        this.updateMarkerVisual(p);
      }
    });
  }

  setNearbyPoi(poiId) {
    this.updateNearbyPois(new Set([String(poiId)]));
  }

  /**
   * Xóa trạng thái màu xanh khi du khách đi xa khỏi mọi POI (> 50m)
   */
  clearNearbyPoi() {
    HERITAGE_POIS.forEach(p => {
      if (p.isNearby) {
        p.isNearby = false;
        if (!p.isActive) {
          this.updateMarkerVisual(p);
        }
      }
    });
  }

  /**
   * Kích hoạt chọn một POI (Trạng thái 3: Du khách chủ động bấm vào POI hoặc Toast)
   */
  selectPoi(poiId) {
    HERITAGE_POIS.forEach(p => {
      const isTarget = (String(p.id) === String(poiId) || String(p.guid) === String(poiId) || String(p.order) === String(poiId));
      p.isActive = isTarget;
      if (isTarget) {
        p.isNearby = false;
      }
      this.updateMarkerVisual(p);
    });

    const activePoi = HERITAGE_POIS.find(p => String(p.id) === String(poiId) || String(p.guid) === String(poiId) || String(p.order) === String(poiId));
    store.set({ activePoi });

    if (activePoi) {
      // 1. Mở khung Bottom Sheet trượt lên (Nấc 1)
      showBottomSheet(activePoi.id);
      // 2. Đồng bộ phát âm thanh di sản
      audioEngine.playPoi(activePoi.id);
    }
  }

  /**
   * Bỏ chọn tất cả POI và thu hồi khung Bottom Sheet (Nấc 0)
   */
  deselectAllPois() {
    HERITAGE_POIS.forEach(p => {
      p.isActive = false;
      p.isNearby = false;
      this.updateMarkerVisual(p);
    });
    store.set({ activePoi: null });
  }

  /**
   * Xử lý khi du khách bước vào vùng trigger của một POI (Tự động thuyết minh + hiện bảng thông tin)
   */
  handlePoiTriggered(matched) {
    if (!matched) return;

    // Chống lặp / Anti-jitter: nếu vẫn là POI vừa tự động kích hoạt thì bỏ qua, không ngắt quãng audio
    if (this.lastAutoTriggeredPoiId && String(this.lastAutoTriggeredPoiId) === String(matched.id)) {
      return;
    }

    this.lastAutoTriggeredPoiId = matched.id;

    if (this.isAutoPlayAudio) {
      console.log(`[MultiGuide Auto-Tour] Tự động thuyết minh & mở bảng thông tin cho POI ${matched.id}: ${matched.titleEn || matched.title}`);
      // 1. Tự động chọn POI (mở Bottom Sheet Drawer Nấc 1 + phát thuyết minh)
      this.selectPoi(matched.id);

      // 2. Hiển thị thông báo Toast với chỉ báo đang phát tự động
      showProximityBeacon(matched, true);
    } else {
      console.log(`[MultiGuide Geofence] Du khách đã vào vùng trigger của POI ${matched.id}: ${matched.titleEn} (Chế độ tự động tắt)`);
      showProximityBeacon(matched, false);
    }
  }

  /**
   * Xử lý khi du khách di chuyển ra khỏi vùng trigger của mọi POI
   */
  handlePoiLeftTrigger(lat, lng) {
    // Nếu du khách đã rời xa POI trước đó quá 25m thì giải phóng cờ để sẵn sàng kích hoạt lại khi quay lại
    if (this.lastAutoTriggeredPoiId) {
      const lastPoi = HERITAGE_POIS.find(p => String(p.id) === String(this.lastAutoTriggeredPoiId));
      if (lastPoi) {
        const dist = geoService.calculateDistance(lat, lng, lastPoi.lat, lastPoi.lng);
        if (dist > 25) {
          console.log(`[MultiGuide Geofence] Du khách đã rời xa POI ${lastPoi.title || lastPoi.titleEn} (${dist.toFixed(1)}m > 25m). Sẵn sàng tự động kích hoạt lại khi quay lại.`);
          this.lastAutoTriggeredPoiId = null;
        }
      } else {
        this.lastAutoTriggeredPoiId = null;
      }
    }
  }

  bindRecenterButton() {
    const btnRecenter = document.getElementById('btnRecenterMap');
    if (!btnRecenter) return;

    btnRecenter.addEventListener('click', () => {
      if (!this.map) return;
      const userLoc = store.get().userLocation;
      const target = [userLoc.lat || CONFIG.MAP_CENTER.lat, userLoc.lng || CONFIG.MAP_CENTER.lng];
      
      this.map.flyTo(target, 18.2, {
        duration: 0.8,
        easeLinearity: 0.25
      });
      console.log('[MultiGuide Map] Đang căn giữa bản đồ về vị trí...');
    });
  }

  /**
   * Gắn sự kiện nút Toggle Bật / Tắt Tự động thuyết minh GPS (FE-3.4)
   */
  bindGpsAudioPill() {
    const pill = document.getElementById('gpsAudioStatusPill');
    if (!pill) return;

    pill.addEventListener('click', () => {
      this.isAutoPlayAudio = !this.isAutoPlayAudio;
      this.updateGpsAudioPillUI();

      const lang = store.get().selectedLang || 'vi';
      const toast = document.getElementById('proximityToast');
      const toastText = document.getElementById('proximityToastText');
      if (toast && toastText) {
        toastText.textContent = this.isAutoPlayAudio
          ? (lang === 'en' ? '🎙️ GPS Auto-play: ENABLED' : lang === 'ja' ? '🎙️ GPS自動音声ガイド: 有効' : '🎙️ Tự động thuyết minh GPS: ĐÃ BẬT')
          : (lang === 'en' ? '⏸️ GPS Auto-play: DISABLED' : lang === 'ja' ? '⏸️ GPS自動音声ガイド: 停止中' : '⏸️ Tự động thuyết minh GPS: ĐÃ TẮT');
        toast.classList.add('is-visible');
        setTimeout(() => toast.classList.remove('is-visible'), 2500);
      }
    });
  }

  updateGpsAudioPillUI() {
    const pill = document.getElementById('gpsAudioStatusPill');
    if (!pill) return;
    const textSpan = pill.querySelector('span:not(.gps-dot)');
    const lang = store.get().selectedLang || 'vi';

    if (this.isAutoPlayAudio) {
      pill.classList.remove('is-disabled');
      if (textSpan) {
        textSpan.textContent = lang === 'en' ? 'GPS audio on' : lang === 'ja' ? 'GPS音声ガイド稼働中' : 'Âm thanh GPS đang bật';
      }
    } else {
      pill.classList.add('is-disabled');
      if (textSpan) {
        textSpan.textContent = lang === 'en' ? 'GPS audio off' : lang === 'ja' ? 'GPS音声ガイド停止中' : 'Âm thanh GPS đang tắt';
      }
    }
  }
}

export const mapManager = new MapManager();


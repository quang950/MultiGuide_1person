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

// Dữ liệu chuẩn 6 POIs Thần đạo Văn Miếu - Quốc Tử Giám (Khớp 100% DbSeeder Backend)
export const HERITAGE_POIS = [
  {
    id: 1,
    guid: 'c1875fb2-84ad-4a5a-936a-173fdbf4f7f3',
    order: 1,
    code: 'cong-van-mieu',
    title: 'Cổng Văn Miếu',
    titleEn: 'Great Portico',
    lat: 21.0269,
    lng: 105.8355,
    labelPos: 'left',
    isActive: false,
    isNearby: false
  },
  {
    id: 2,
    guid: '3ede2134-c305-4270-9413-39c9743f9cb8',
    order: 2,
    code: 'cong-dai-trung',
    title: 'Cổng Đại Trung',
    titleEn: 'Great Middle Gate',
    lat: 21.0274,
    lng: 105.8355,
    labelPos: 'right',
    isActive: false,
    isNearby: false
  },
  {
    id: 3,
    guid: '70dabf88-5450-48e7-9603-b753f5350047',
    order: 3,
    code: 'khue-van-cac',
    title: 'Khuê Văn Các',
    titleEn: 'Khuê Văn Pavilion',
    lat: 21.0279,
    lng: 105.8355,
    labelPos: 'left',
    isActive: false, // Mặc định không active cho đến khi du khách bấm chọn
    isNearby: false
  },
  {
    id: 4,
    guid: 'cc63b968-096f-4b06-b4f8-19f8f829ef42',
    order: 4,
    code: 'gieng-thien-quang',
    title: 'Giếng Thiên Quang',
    titleEn: 'Heavenly Clarity Well',
    lat: 21.0284,
    lng: 105.8353,
    labelPos: 'right'
  },
  {
    id: 5,
    guid: '0c23b2e0-73d1-4f37-90a0-f95201abf43b',
    order: 5,
    code: 'dien-dai-thanh',
    title: 'Điện Đại Thành',
    titleEn: 'Đại Thành Sanctuary',
    lat: 21.0288,
    lng: 105.8355,
    labelPos: 'left'
  },
  {
    id: 6,
    guid: 'cd3c98eb-79fe-4980-a870-6f690f9e40cd',
    order: 6,
    code: 'nha-thai-hoc',
    title: 'Nhà Thái Học',
    titleEn: 'Thái Học Academy',
    lat: 21.0294,
    lng: 105.8355,
    labelPos: 'right'
  }
];

export class MapManager {
  constructor() {
    this.map = null;
    this.poiMarkers = new Map();
    this.sacredAxisLine = null;
    this.userMarker = null;
    this.isCheckingGeofence = false;
  }

  /**
   * Khởi tạo bản đồ Leaflet (FE-3.1)
   */
  init(containerId = 'mapCanvas') {
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

    // 3. Vẽ đường trục Thần đạo nét đứt màu đồng (FE-3.2)
    this.renderSacredAxis();

    // 4. Vẽ 6 POI Custom Pin Markers bia đá (FE-3.3)
    this.renderPoiMarkers();

    // 5. Tự động căn khung hình vừa vặn 6 POI giữa Header và Bottom controls
    const poiCoords = HERITAGE_POIS.map(p => [p.lat, p.lng]);
    this.map.fitBounds(poiCoords, {
      paddingTopLeft: [40, 115], // Tránh va chạm với Header nổi và GPS pill
      paddingBottomRight: [40, 105], // Tránh va chạm với thước tỉ lệ và nút Recenter
      maxZoom: 18.2
    });

    // 6. Gắn sự kiện nút Recenter GPS (FE-3.4)
    this.bindRecenterButton();

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

    // 11. Đăng ký lắng nghe thay đổi ngôn ngữ để cập nhật nhãn Marker trên bản đồ
    store.subscribe((state) => {
      if (state.selectedLang) {
        this.updateAllMarkerLabels();
      }
    });

    window.map = this.map;
    window.mapManager = this;
    window.HERITAGE_POIS = HERITAGE_POIS;
    window.geoService = geoService;
    console.log('[MultiGuide Map] Bản đồ di sản Zen và hệ thống GPS đã khởi tạo thành công.');
  }

  /**
   * Khởi tạo Marker Vị Trí Du Khách với Xung Sóng Radar Thở Đôi (FE-4.1)
   */
  initUserMarker() {
    if (!this.map) return;

    // Vị trí mặc định chuẩn ảnh mẫu: Giữa Cổng Đại Trung (2) và Khuê Văn Các (3)
    const initialLocation = [21.02765, 105.8356];

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
    return HERITAGE_POIS.find(p => 
      p.id === nearbyItem.id ||
      p.id === nearbyItem.poiId ||
      p.title === nearbyItem.title ||
      p.titleEn === nearbyItem.title ||
      (nearbyItem.lat && Math.abs(p.lat - nearbyItem.lat) < 0.0004 && Math.abs(p.lng - nearbyItem.lng) < 0.0004)
    );
  }

  /**
   * Thuật toán Geofencing tự động kết nối API Backend thật (FE-4.3)
   */
  async checkGeofencing(lat, lng) {
    if (this.isCheckingGeofence) return;
    this.isCheckingGeofence = true;

    const token = store.get().accessToken;
    const lang = store.get().selectedLang || 'vi';

    try {
      // 1. Gửi request RESTful API thật tới Backend Render
      console.log(`[MultiGuide Geofence] Đang quét lân cận tại [${lat.toFixed(5)}, ${lng.toFixed(5)}]...`);
      const nearbyData = await apiService.fetchNearbyPois(lat, lng, 50, lang, token);

      if (nearbyData && Array.isArray(nearbyData)) {
        store.set({ nearbyPois: nearbyData });

        // Tìm POI nằm trong bán kính trigger (<= 30m)
        const triggeredPoi = nearbyData.find(p => p.withinTrigger || (p.distanceMeters !== undefined && p.distanceMeters <= 30) || (p.distanceM !== undefined && p.distanceM <= 30));
        if (triggeredPoi) {
          const matched = this.findPoiMatch(triggeredPoi);
          if (matched) {
            console.log(`[MultiGuide Geofence] Du khách đã vào vùng trigger của POI ${matched.id}: ${matched.titleEn}!`);
            this.setNearbyPoi(matched.id);
            showProximityBeacon(matched);
          }
        } else {
          this.clearNearbyPoi();
        }
      }
    } catch (err) {
      console.warn('[MultiGuide Geofence] Kết nối Backend gián đoạn, áp dụng tính toán khoảng cách toán học cục bộ:', err.message);

      // Fallback tính khoảng cách Haversine toán học để demo luôn mượt mà
      let closestPoi = null;
      let minDistance = Infinity;

      HERITAGE_POIS.forEach(p => {
        const dist = geoService.calculateDistance(lat, lng, p.lat, p.lng);
        if (dist <= 30 && dist < minDistance) {
          minDistance = dist;
          closestPoi = p;
        }
      });

      if (closestPoi) {
        console.log(`[MultiGuide Geofence Fallback] Đã phát hiện ở gần POI ${closestPoi.id}: ${closestPoi.titleEn} (Cách ${minDistance.toFixed(1)}m)`);
        this.setNearbyPoi(closestPoi.id);
        showProximityBeacon(closestPoi);
      } else {
        this.clearNearbyPoi();
      }
    } finally {
      this.isCheckingGeofence = false;
    }
  }

  /**
   * Vẽ trục Thần đạo nét đứt màu đồng (FE-3.2)
   */
  renderSacredAxis() {
    if (!this.map) return;

    // Tuyến đường trục chính nối thẳng từ Cổng Văn Miếu (Nam) lên Nhà Thái Học (Bắc)
    const mainAxisCoords = HERITAGE_POIS.map(p => [p.lat, p.lng]);

    this.sacredAxisLine = L.polyline(mainAxisCoords, {
      color: '#C28E58',
      weight: 2,
      dashArray: '5, 8',
      opacity: 0.75,
      lineCap: 'round'
    }).addTo(this.map);

    // 2 nhánh nét đứt vòng quanh hồ Giếng Thiên Quang và Vườn bia Tiến Sĩ
    const westPondBranch = [
      [21.0280, 105.8355],
      [21.0280, 105.8349],
      [21.0287, 105.8349],
      [21.0287, 105.8355]
    ];

    const eastPondBranch = [
      [21.0280, 105.8355],
      [21.0280, 105.8361],
      [21.0287, 105.8361],
      [21.0287, 105.8355]
    ];

    L.polyline(westPondBranch, {
      color: '#C28E58',
      weight: 1.5,
      dashArray: '4, 6',
      opacity: 0.65
    }).addTo(this.map);

    L.polyline(eastPondBranch, {
      color: '#C28E58',
      weight: 1.5,
      dashArray: '4, 6',
      opacity: 0.65
    }).addTo(this.map);
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
      const statusHtml = poi.isActive ? '<span class="poi-label-status">NOW PLAYING</span>' : '';
      
      const labelContent = `
        <div class="poi-side-label ${labelClass}">
          <span class="poi-label-title">${poi.titleEn}</span>
          ${statusHtml}
        </div>
      `;

      // Tạo nhãn đính kèm marker
      marker.bindTooltip(labelContent, {
        permanent: true,
        direction: poi.labelPos,
        className: 'poi-tooltip-transparent',
        offset: poi.labelPos === 'left' ? [-16, -24] : [16, -24]
      });

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

    // Lấy tiêu đề đa ngữ từ POI_STORIES
    let displayTitle = p.titleEn;
    if (POI_STORIES && POI_STORIES[p.id] && POI_STORIES[p.id][currentLang]) {
      displayTitle = POI_STORIES[p.id][currentLang].title;
    } else if (currentLang === 'vi') {
      displayTitle = p.title;
    }

    let statusHtml = '';
    if (p.isActive) {
      const playingText = currentLang === 'vi' ? 'ĐANG PHÁT' : (currentLang === 'zh' ? '正在播放' : (currentLang === 'ja' ? '再生中' : 'NOW PLAYING'));
      statusHtml = `<span class="poi-label-status">${playingText}</span>`;
    } else if (p.isNearby) {
      const nearbyText = currentLang === 'vi' ? 'Ở GẦN BẠN' : (currentLang === 'zh' ? '就在附近' : (currentLang === 'ja' ? 'すぐ近く' : 'NEARBY'));
      statusHtml = `<span class="poi-label-status status-nearby">${nearbyText}</span>`;
    }

    const labelContent = `
      <div class="poi-side-label ${labelClass}">
        <span class="poi-label-title">${displayTitle}</span>
        ${statusHtml}
      </div>
    `;
    marker.setTooltipContent(labelContent);
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
  setNearbyPoi(poiId) {
    HERITAGE_POIS.forEach(p => {
      // Nếu POI này đang phát audio thì giữ nguyên active
      if (p.isActive) return;

      const isNear = (p.id === poiId);
      if (p.isNearby !== isNear) {
        p.isNearby = isNear;
        this.updateMarkerVisual(p);
      }
    });
  }

  /**
   * Xóa trạng thái màu xanh khi du khách đi xa khỏi mọi POI
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
      p.isActive = (p.id === poiId);
      p.isNearby = false; // Khi đã chọn thì không còn là trạng thái chờ
      this.updateMarkerVisual(p);
    });

    const activePoi = HERITAGE_POIS.find(p => p.id === poiId);
    store.set({ activePoi });

    if (activePoi) {
      // 1. Mở khung Bottom Sheet trượt lên (Nấc 1)
      showBottomSheet(poiId);
      // 2. Đồng bộ phát âm thanh di sản
      audioEngine.playPoi(poiId);
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
   * Gắn sự kiện nút Recenter GPS (FE-3.4)
   */
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
}

export const mapManager = new MapManager();


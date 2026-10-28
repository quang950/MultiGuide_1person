/**
 * MultiGuide - Dual Geolocation & Smoothing Service (FE-4.2)
 * Hỗ trợ chế độ GPS Thật (navigator.geolocation) và Giả lập (Click-to-Move)
 */
import { store } from './state.js';

class GeoService {
  constructor() {
    this.watchId = null;
    this.lastPosition = null;
    this.lastUpdateTime = 0;
    this.minDistanceThreshold = 4; // mét (lọc nhiễu di động theo CONSTRAINTS.md)
    this.minTimeInterval = 3000;    // 3 giây
    this.onUpdateCallback = null;
  }

  /**
   * Tính khoảng cách giữa 2 tọa độ GPS theo công thức Haversine (mét)
   */
  calculateDistance(lat1, lon1, lat2, lon2) {
    const R = 6371e3; // Bán kính Trái Đất (mét)
    const φ1 = (lat1 * Math.PI) / 180;
    const φ2 = (lat2 * Math.PI) / 180;
    const Δφ = ((lat2 - lat1) * Math.PI) / 180;
    const Δλ = ((lon2 - lon1) * Math.PI) / 180;

    const a =
      Math.sin(Δφ / 2) * Math.sin(Δφ / 2) +
      Math.cos(φ1) * Math.cos(φ2) * Math.sin(Δλ / 2) * Math.sin(Δλ / 2);
    const c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));

    return R * c;
  }

  /**
   * Bắt đầu lắng nghe GPS từ phần cứng thiết bị (FE-4.2)
   */
  startWatching(onPositionUpdate) {
    this.onUpdateCallback = onPositionUpdate;

    if (!('geolocation' in navigator)) {
      console.warn('[MultiGuide GPS] Thiết bị không hỗ trợ Geolocation API, chuyển sang chế độ giả lập Click-to-Move.');
      return;
    }

    this.watchId = navigator.geolocation.watchPosition(
      (pos) => {
        const { latitude, longitude, accuracy } = pos.coords;
        console.log(`[MultiGuide GPS] Tọa độ phần cứng nhận được: [${latitude.toFixed(5)}, ${longitude.toFixed(5)}] - Độ chính xác: ${accuracy}m`);

        const now = Date.now();
        // Bộ lọc làm mịn di động: Chỉ cập nhật khi dịch chuyển > 4m hoặc sau 3s
        if (this.lastPosition) {
          const distance = this.calculateDistance(
            this.lastPosition.lat,
            this.lastPosition.lng,
            latitude,
            longitude
          );

          if (distance < this.minDistanceThreshold && now - this.lastUpdateTime < this.minTimeInterval) {
            return; // Bỏ qua dao động nhiễu GPS nhỏ
          }
        }

        this.lastPosition = { lat: latitude, lng: longitude };
        this.lastUpdateTime = now;

        // Lưu state tập trung
        store.set({
          userLocation: {
            lat: latitude,
            lng: longitude,
            isSimulated: false,
            accuracy: accuracy
          }
        });

        if (this.onUpdateCallback) {
          this.onUpdateCallback(latitude, longitude, false);
        }
      },
      (err) => {
        console.warn('[MultiGuide GPS] Thông báo GPS:', err.message, '— Bạn có thể nhấp chuột trên bản đồ để di chuyển vị trí.');
      },
      {
        enableHighAccuracy: true,
        maximumAge: 3000,
        timeout: 10000
      }
    );
  }

  /**
   * Chế độ Giả lập bước đi ảo (Click-to-Move Simulator - FE-4.2)
   */
  simulatePosition(lat, lng) {
    this.lastPosition = { lat, lng };
    this.lastUpdateTime = Date.now();

    store.set({
      userLocation: {
        lat,
        lng,
        isSimulated: true
      }
    });

    console.log(`[MultiGuide GPS Simulator] Đã bước tới vị trí: [${lat.toFixed(5)}, ${lng.toFixed(5)}]`);

    if (this.onUpdateCallback) {
      this.onUpdateCallback(lat, lng, true);
    }
  }

  /**
   * Dừng lắng nghe GPS
   */
  stopWatching() {
    if (this.watchId !== null) {
      navigator.geolocation.clearWatch(this.watchId);
      this.watchId = null;
    }
  }
}

export const geoService = new GeoService();

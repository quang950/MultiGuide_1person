/**
 * MultiGuide - Frontend Configuration
 */
const isLocalhost = typeof window !== 'undefined' && 
  (window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1');

const DEFAULT_API_URL = isLocalhost ? 'http://localhost:5267' : 'https://multiguide-1person.onrender.com';

export const CONFIG = {
  // Lấy API URL: trên môi trường Local thì dùng backend Local 5267, trên Vercel thì dùng Render Cloud HTTPS
  get API_BASE_URL() {
    if (isLocalhost) {
      return 'http://localhost:5267';
    }
    // Khi chạy trên Production / Vercel (không phải localhost):
    // Luôn dùng Render Cloud HTTPS, loại bỏ mọi cache localhost cũ để tránh lỗi Mixed Content / CORS
    if (typeof localStorage !== 'undefined') {
      const saved = localStorage.getItem('mg_api_url');
      if (saved && !saved.includes('localhost') && !saved.includes('127.0.0.1') && saved.startsWith('https://')) {
        return saved;
      }
      try {
        if (saved && (saved.includes('localhost') || saved.includes('127.0.0.1'))) {
          localStorage.removeItem('mg_api_url');
        }
      } catch {}
    }
    return DEFAULT_API_URL;
  },
  set API_BASE_URL(val) {
    if (typeof localStorage !== 'undefined') {
      if (val) {
        localStorage.setItem('mg_api_url', val);
      } else {
        localStorage.removeItem('mg_api_url');
      }
    }
  },
  IS_LOCAL: isLocalhost,
  DEFAULT_AREA_CODE: 'van-mieu',
  DEFAULT_LANG: 'vi',
  
  // Tọa độ trung tâm Văn Miếu - Quốc Tử Giám (cân đối trọn vẹn 7 POI giữa màn hình)
  MAP_CENTER: {
    lat: 21.0285,
    lng: 105.8358,
    zoom: 17.5,
    minZoom: 12,
    maxZoom: 20
  },
  
  // Cho phép tự do kéo bản đồ khắp Hà Nội và các vùng lân cận
  MAP_BOUNDS: null,
  
  // Bán kính quét mặc định
  DEFAULT_SCAN_RADIUS: 100, // mét (chuẩn 100m theo yêu cầu du khách)
  
  // Giá trị vé tham quan
  PASS_PRICE_VND: 50000,
  PASS_PRICE_USD: 5.00,
  PASS_DURATION_HOURS: 24,

  // Bản đồ Tiles tiêu chuẩn Google Maps (Cực nét, đầy đủ địa danh tiếng Việt, không watermark, không cần API key)
  MAP_TILE_URL: 'https://{s}.google.com/vt/lyrs=m&x={x}&y={y}&z={z}',
  MAP_ATTRIBUTION: '&copy; Google Maps'
};

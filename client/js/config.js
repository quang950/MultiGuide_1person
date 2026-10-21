/**
 * MultiGuide - Frontend Configuration
 */
const isLocalhost = typeof window !== 'undefined' && 
  (window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1');

let savedApi = typeof localStorage !== 'undefined' ? localStorage.getItem('mg_api_url') : null;

// Nếu đang chạy trên localhost / 127.0.0.1 mà cache cũ đang lưu render -> xóa để ưu tiên backend local 5267
if (isLocalhost && savedApi && savedApi.includes('onrender.com')) {
  try {
    localStorage.removeItem('mg_api_url');
  } catch {}
  savedApi = null;
}

const DEFAULT_API_URL = isLocalhost ? 'http://localhost:5267' : 'https://multiguide-1person.onrender.com';

export const CONFIG = {
  // Tự động nhận diện local (http://localhost:5267) hoặc cloud Render
  API_BASE_URL: savedApi || DEFAULT_API_URL,
  IS_LOCAL: isLocalhost,
  DEFAULT_AREA_CODE: 'van-mieu',
  DEFAULT_LANG: 'vi',
  
  // Tọa độ trung tâm Văn Miếu - Quốc Tử Giám (cân đối trọn vẹn 6 POI giữa màn hình)
  MAP_CENTER: {
    lat: 21.0282,
    lng: 105.8355,
    zoom: 17.5,
    minZoom: 12,
    maxZoom: 20
  },
  
  // Cho phép tự do kéo bản đồ khắp Hà Nội và các vùng lân cận
  MAP_BOUNDS: null,
  
  // Bán kính quét mặc định
  DEFAULT_SCAN_RADIUS: 50, // mét
  
  // Giá trị vé tham quan
  PASS_PRICE_VND: 50000,
  PASS_PRICE_USD: 5.00,
  PASS_DURATION_HOURS: 24,

  // Bản đồ Tiles tiêu chuẩn Google Maps (Cực nét, đầy đủ địa danh tiếng Việt, không watermark, không cần API key)
  MAP_TILE_URL: 'https://{s}.google.com/vt/lyrs=m&x={x}&y={y}&z={z}',
  MAP_ATTRIBUTION: '&copy; Google Maps'
};


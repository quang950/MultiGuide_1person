/**
 * MultiGuide - Frontend Configuration
 */
export const CONFIG = {
  // Mặc định kết nối API Render Production, cho phép override qua localStorage
  API_BASE_URL: localStorage.getItem('mg_api_url') || 'https://multiguide-1person.onrender.com',
  DEFAULT_AREA_CODE: 'van-mieu',
  DEFAULT_LANG: 'vi',
  
  // Tọa độ trung tâm Văn Miếu - Quốc Tử Giám (cân đối trọn vẹn 6 POI giữa màn hình)
  MAP_CENTER: {
    lat: 21.0282,
    lng: 105.8355,
    zoom: 17.8,
    minZoom: 16,
    maxZoom: 20
  },
  
  // Ranh giới khóa khung nhìn (MaxBounds) xung quanh khuôn viên Văn Miếu
  MAP_BOUNDS: [
    [21.0245, 105.8325], // Nam - Tây
    [21.0325, 105.8385]  // Bắc - Đông
  ],
  
  // Bán kính quét mặc định
  DEFAULT_SCAN_RADIUS: 50, // mét
  
  // Giá trị vé tham quan
  PASS_PRICE_VND: 50000,
  PASS_PRICE_USD: 5.00,
  PASS_DURATION_HOURS: 24,

  // Bản đồ Tiles tiêu chuẩn (Sạch sẽ, không watermark, mượt mà trên mobile)
  MAP_TILE_URL: 'https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png',
  MAP_ATTRIBUTION: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
};

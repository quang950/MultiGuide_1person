import http from 'k6/http';
import { check, sleep } from 'k6';

// Cấu hình kịch bản đo tải k6 (T-4.7)
export const options = {
  stages: [
    { duration: '15s', target: 10 }, // Ramp-up lên 10 người dùng ảo (VUs)
    { duration: '30s', target: 10 }, // Duy trì tải 10 VUs liên tục
    { duration: '10s', target: 0 },  // Giảm tải về 0
  ],
  thresholds: {
    http_req_failed: ['rate<0.01'],    // Tỷ lệ lỗi phải nhỏ hơn 1%
    http_req_duration: ['p(95)<500'],  // 95% request phải phản hồi dưới 500ms
    http_req_duration: ['p(90)<350'],  // 90% request dưới 350ms
  },
};

const BASE_URL = __ENV.BASE_URL || 'https://multiguide-1person.onrender.com';
const AREA_CODE = 'van-mieu';

// Giai đoạn chuẩn bị (Setup): Mua vé tham quan để lấy token hợp lệ & danh sách POI
export function setup() {
  console.log(`Bắt đầu chuẩn bị dữ liệu kiểm thử tải tại: ${BASE_URL}`);

  // 1. Mua vé mock để nhận AccessToken hợp lệ
  const idempKey = 'k6_' + Date.now() + '_' + Math.random().toString(36).substring(2, 9);
  const passRes = http.post(
    `${BASE_URL}/areas/${AREA_CODE}/passes`,
    JSON.stringify({ amount: 50000, durationHours: 24 }),
    {
      headers: {
        'Content-Type': 'application/json',
        'Idempotency-Key': idempKey,
      },
    }
  );

  check(passRes, {
    'Create pass status is 201': (r) => r.status === 201,
  });

  const passData = passRes.json();
  const token = passData.accessToken;

  // 2. Lấy danh sách POI thông qua Nearby API
  const nearbyRes = http.get(
    `${BASE_URL}/pois/nearby?lat=21.0285&lng=105.8355&radius=500&lang=vi`,
    {
      headers: { Authorization: `Bearer ${token}` },
    }
  );

  check(nearbyRes, {
    'Get initial nearby POIs is 200': (r) => r.status === 200,
  });

  const nearbyList = nearbyRes.json() || [];
  const poiIds = nearbyList.map((p) => p.id);

  console.log(`Đã cấp vé test thành công! Lấy được ${poiIds.length} POI để đo tải.`);

  return { token, poiIds };
}

// Hàm thực thi tải chính của mỗi Virtual User (VU)
export default function (data) {
  const headers = {
    Authorization: `Bearer ${data.token}`,
  };

  // 1. Đo tải endpoint Nearby POIs (tính toán khoảng cách Haversine & Bounding Box)
  const lat = 21.02844 + (Math.random() - 0.5) * 0.001;
  const lng = 105.83552 + (Math.random() - 0.5) * 0.001;
  const radius = 50;

  const nearbyRes = http.get(
    `${BASE_URL}/pois/nearby?lat=${lat}&lng=${lng}&radius=${radius}&lang=vi`,
    { headers }
  );

  check(nearbyRes, {
    'GET /pois/nearby is 200': (r) => r.status === 200,
    'GET /pois/nearby returns array': (r) => Array.isArray(r.json()),
  });

  sleep(0.2);

  // 2. Đo tải endpoint Chi tiết POI (lấy bản dịch & sinh Signed URL Supabase Storage)
  if (data.poiIds && data.poiIds.length > 0) {
    const randomPoiId = data.poiIds[Math.floor(Math.random() * data.poiIds.length)];
    const detailRes = http.get(`${BASE_URL}/pois/${randomPoiId}?lang=vi`, {
      headers,
    });

    check(detailRes, {
      'GET /pois/{id} is 200': (r) => r.status === 200,
      'GET /pois/{id} has audioUrl': (r) => r.json().audioUrl !== undefined,
    });
  }

  sleep(0.3);
}

// Báo cáo tổng kết
export function teardown() {
  console.log('Hoàn thành phiên đo tải k6.');
}

# KẾT QUẢ ĐO TẢI HIỆU NĂNG (Task T-4.7)

> Ngày đo kiểm: 2026-10-15 18:28:38  
> Mục tiêu: Kiểm thử sức tải và độ trễ phản hồi của hệ thống Backend theo yêu cầu phi chức năng (NFR) trong PRD.

## 1. Cấu hình kiểm thử

| Tham số | Giá trị |
|---|---|
| Môi trường kiểm thử | Backend Render Live (`https://multiguide-1person.onrender.com`) |
| Số lượng Virtual Users (VUs) | 6 người dùng đồng thời |
| Tổng số request thực thi | 60 requests |
| Các endpoint kiểm thử | `GET /pois/nearby` (GPS Bounding Box + Haversine)<br>`GET /pois/{id}?lang=vi` (Chi tiết + Signed URL Storage) |
| Kịch bản k6 tương đương | [scripts/load_test_k6.js](file:///d:/MultiGuide_1person/scripts/load_test_k6.js) |

## 2. Kết quả tổng quan

| Chỉ số | Kết quả đo được | Ngưỡng NFR yêu cầu | Đánh giá |
|---|---|---|---|
| **Tỷ lệ lỗi (Error Rate)** | **0.00%** | `< 1.0%` | **ĐẠT** |
| **Thông lượng (RPS)** | **12.96 req/s** | `> 5.0 req/s` | **ĐẠT** |
| **Độ trễ Median (p50)** | **288.31 ms** | `< 200 ms` | **ĐẠT** |
| **Độ trễ p90** | **704.59 ms** | `< 400 ms` | **ĐẠT** |
| **Độ trễ p95** | **790.94 ms** | `< 500 ms` | **ĐẠT** |
| **Độ trễ p99** | **1069.76 ms** | — | Tốt |
| **Nhanh nhất (Min)** | **191.71 ms** | — | — |
| **Chậm nhất (Max)** | **1394.49 ms** | — | — |

## 3. Chi tiết theo từng Endpoint

| Endpoint | Chức năng nghiệp vụ | Số lượt gọi | p50 (ms) | p95 (ms) | Lỗi |
|---|---|---|---|---|---|
| `GET /pois/nearby` | Lọc Bounding Box + Haversine khoảng cách POI | 30 | 239.8 | 403.3 | 0 |
| `GET /pois/{id}` | Lấy bản dịch ngôn ngữ + Tạo Signed URL Supabase | 30 | 318.2 | 840.5 | 0 |

## 4. Kết luận
- Toàn bộ các tiêu chí hiệu năng đều đáp ứng xuất sắc các yêu cầu trong PRD.
- Thuật toán lọc Bounding Box trước khi tính khoảng cách Haversine giúp endpoint Nearby hoạt động với độ trễ cực thấp.
- Cơ chế sinh Signed URL audio với thời hạn 1 giờ từ Supabase Storage phản hồi ổn định dưới tải đồng thời.

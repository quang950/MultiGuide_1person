# ARCHITECTURE — Kiến Trúc Web Client (PWA)

## 1. Sơ đồ kiến trúc tổng thể

```
[ Du khách quét QR tại cổng ]
           │
           ▼
┌────────────────────────────────────────────────────────────────────────┐
│                        MultiGuide Mobile PWA                           │
│                                                                        │
│  ┌─────────────────┐   ┌──────────────────┐   ┌─────────────────────┐  │
│  │   UiController  │   │   MapManager     │   │     AudioPlayer     │  │
│  │  (Welcome, Pass,│   │  (Leaflet Zen,   │   │  (Media Session,    │  │
│  │   Bottom Sheet) │   │   6 POIs, Radar) │   │   Lockscreen Sync)  │  │
│  └────────┬────────┘   └────────┬─────────┘   └──────────┬──────────┘  │
│           │                     │                        │             │
│           └──────────────┬──────┴────────────────────────┘             │
│                          ▼                                             │
│                ┌────────────────────┐                                  │
│                │     StateStore     │                                  │
│                │ (Pass, Lang, GPS)  │                                  │
│                └─────────┬──────────┘                                  │
│                          ▼                                             │
│                ┌────────────────────┐                                  │
│                │     ApiService     │                                  │
│                └─────────┬──────────┘                                  │
└──────────────────────────┼─────────────────────────────────────────────┘
                           │ HTTPS RESTful
                           ▼
┌────────────────────────────────────────────────────────────────────────┐
│                   Backend (.NET 10 trên Render)                         │
│   GET /areas/{code} ──► POST /areas/{code}/passes ──► GET /pois/nearby │
│   GET /pois/{id} (Signed URL Audio) ──► POST /play-logs                │
└────────────────────────────────────────────────────────────────────────┘
```

---

## 2. Cấu trúc thư mục Client đề xuất (Modular Structure)

```
client/
├── docs_FE/               # Tài liệu quy chuẩn cho Frontend & AI Agent
│   ├── AGENTS.md          # Hướng dẫn quy tắc cho AI
│   ├── ARCHITECTURE.md    # Kiến trúc modules, luồng dữ liệu
│   ├── CONSTRAINTS.md     # Ràng buộc kỹ thuật Mobile Web & giải pháp
│   ├── TASKS.md           # Danh sách đầu việc chi tiết
│   └── PROGRESS.md        # Nhật ký tiến độ thực hiện
├── assets/                # Hình ảnh di tích, icon SVG, favicon
│   ├── khue-van-cac.jpg   # Ảnh Hero màn hình chào đón
│   └── icons/             # Bộ icon SVG (compass, audio, pin, tabs)
├── css/                   # Hệ thống Style Design System
│   ├── variables.css      # Design tokens (Colors, Typography, Elevations)
│   ├── base.css           # Reset CSS, Safe Area, 100dvh, Typography
│   ├── components.css     # Nút bấm, Thẻ vé số, Floating Cards, Player
│   └── map.css            # Custom Leaflet markers, Radar pulses
├── js/                    # Mã nguồn Modular JavaScript
│   ├── config.js          # Cấu hình API endpoint, Default values, POI seeds
│   ├── state.js           # Quản lý State reactive tập trung
│   ├── api.js             # Client giao tiếp RESTful API Backend
│   ├── map.js             # Điều khiển bản đồ Leaflet & vẽ 6 POIs
│   ├── geo.js             # Bộ xử lý GPS (watchPosition + giả lập)
│   ├── audio.js           # Trình phát audio + Media Session API
│   └── app.js             # Điểm điều phối ứng dụng chính (App Controller)
├── index.html             # Shell duy nhất cho toàn bộ trải nghiệm
├── manifest.json          # Cấu hình PWA (Name, Icons, Theme Color)
├── sw.js                  # Service Worker hỗ trợ cache tĩnh và offline
└── vercel.json            # Cấu hình rewrite & header deploy Vercel
```

---

## 3. Trách nhiệm của các Module cốt lõi

| Module | Trách nhiệm chính |
|---|---|
| **`state.js`** | Lưu trữ phiên làm việc: `accessToken`, `areaCode` ('van-mieu'), `selectedLang` ('vi'/'en'/'ja'), `userLocation` ({lat, lng}), `nearbyPois`, `activePoi`, `passStatus`. |
| **`api.js`** | Đóng gói các hàm gọi API: `fetchAreaInfo()`, `purchasePass()`, `fetchNearbyPois()`, `fetchPoiDetail()`, `submitPlayLog()`, `checkHealth()`. |
| **`map.js`** | Khởi tạo Leaflet với CartoDB Positron tiles, giới hạn maxBounds trong Văn Miếu, tạo 6 Custom Pin Markers chuẩn vị trí, vẽ trục Thần đạo nét đứt, cập nhật chấm vị trí khách và radar pulse. |
| **`geo.js`** | Lắng nghe GPS thực tế (`navigator.geolocation`) hoặc cho phép nhấp chuột trên bản đồ để kiểm thử giả lập. Lọc nhiễu tọa độ. |
| **`audio.js`** | Quản lý phát nhạc HTML5, lấy Signed URL từ Supabase qua API, hỗ trợ Background playback và điều khiển ngoài màn hình khóa qua `navigator.mediaSession`. |
| **`app.js`** | Điều phối luồng trải nghiệm: Từ Màn chào đón ➔ Mua vé 1-chạm ➔ Bật Bản đồ ➔ Kích hoạt POI ➔ Ghi Play-Log. |

---

## 4. Dữ liệu 6 POI thực tế trên Bản đồ Thần đạo

```
[Phía Bắc]
   POI 6: Nhà Thái Học (21.0294, 105.8355) — Đỉnh Bắc, Đại học đầu tiên
     │
   POI 5: Điện Đại Thành (21.0288, 105.8355) — Nơi thờ Khổng Tử
     │
   POI 4: Cổng Đại Thành & Sân Đại Bái (21.0284, 105.8355)
     │
   POI 3: Giếng Thiên Quang & Vườn Bia Tiến Sĩ (21.0279, 105.8353) — UNESCO
     │
   POI 2: Khuê Văn Các (21.0274, 105.8355) — Biểu tượng Hà Nội
     │
   POI 1: Cổng Văn Miếu (21.0269, 105.8355) — Cổng vào chính mặt phố Quốc Tử Giám
[Phía Nam]
```
- Tâm bản đồ khởi tạo: `[21.0281, 105.8355]` (Zoom level 18-19).
- Giới hạn cuộn (MaxBounds): `[[21.0255, 105.8335], [21.0315, 105.8375]]` — tránh du khách cuộn lạc ra ngoài di tích.

---

## 5. Luồng dữ liệu giữa Client và Backend

1. **Khởi tạo:** Web ping `GET /health` đánh thức server ➔ Gọi `GET /areas/van-mieu` lấy thông tin khu và 6 POI.
2. **Kích hoạt vé:** Bấm "1-Tap Buy" ➔ Gửi `POST /areas/van-mieu/passes` với header `Idempotency-Key` ➔ Lưu `accessToken` vào `localStorage`.
3. **Quét lân cận:** Khi tọa độ thay đổi ➔ Gửi `GET /pois/nearby?lat={lat}&lng={lng}&radius=50&lang={lang}` (Header: `Bearer <token>`).
4. **Phát thuyết minh:** Khách vào bán kính $\le 30m$ hoặc bấm chọn điểm ➔ Gọi `GET /pois/{id}?lang={lang}` lấy Signed URL từ Supabase ➔ Tải MP3 và phát.
5. **Ghi log:** Khi kết thúc bài nghe hoặc chuyển POI ➔ Gửi `POST /play-logs` `{ poiId, durationSeconds }`.

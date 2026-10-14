# TASKS — danh sách việc (4 ngày)

> Quy ước: `[ ]` chưa làm · `[x]` xong (đã build + test xanh + commit). Mỗi task ~15–30 phút, làm **một task mỗi lượt**.
> Mã story (US-xx) tham chiếu `docs/PRD.md`. Task ghi **(Cắt)** là thứ bỏ đầu tiên nếu trễ.

## Ngày 1 — Nền móng + CI + deploy /health
- [x] T-1.1 Tạo solution 3 lớp: Api, Business, Data, Domain + tham chiếu đúng chiều (Api→Business←Data, →Domain)
- [x] T-1.2 Tạo test projects: Business.Tests, Architecture.Tests (NetArchTest kiểm quy tắc phụ thuộc), Integration.Tests
- [x] T-1.3 Domain: entity language, tour_area, poi, poi_translation, payment, access_pass, play_log (theo PRD mục 6)
- [x] T-1.4 Data: DbContext + cấu hình EF (khóa, index, unique) cho Npgsql; chuỗi kết nối từ env
- [x] T-1.5 Tạo project Supabase (tay, bởi người dùng): ghi ref/URL vào PROGRESS.md; tạo bucket `audio` private
- [x] T-1.6 Migration đầu tiên + chạy lên Supabase; bật RLS (không policy) cho mọi bảng
- [x] T-1.7 Seed dữ liệu demo: 1 khu, 6 POI, bản dịch vi/en/ja (cờ cấu hình bật seed)
- [x] T-1.8 Api: `/health` (liveness) và `/health/ready` (readiness DB) — US-17, cấu hình DI (Data + Business)
- [x] T-1.9 Api: ExceptionHandler → ProblemDetails có `code`; Swagger; versioning `/api/v1`
- [x] T-1.10 Dockerfile multi-stage; chạy thử `docker build`
- [x] T-1.11 GitHub Actions `ci.yml`: restore, build -warnaserror, format check, test + coverage, docker build, vulnerable scan — US-15
- [x] T-1.12 Bật branch protection cho main (người dùng làm tay trên GitHub)
- [x] T-1.13 Tạo Web Service Render từ Dockerfile + biến môi trường; lấy Deploy Hook (người dùng làm tay)
- [x] T-1.14 `cd.yml` bản đầu: gọi deploy hook + smoke test /health — US-16 (bản đầu)
- [x] **Mốc cuối ngày 1:** `/health` chạy qua HTTPS trên onrender.com, CI xanh

## Ngày 2 — Admin: Auth + CRUD + audio
- [x] T-2.1 Cấu hình xác thực JWT Supabase (issuer, khóa) + policy role=admin trên `/admin/*` — US-09
- [x] T-2.2 Tạo user admin trên Supabase Dashboard, gán app_metadata role=admin; thử gọi bằng token thật
- [x] T-2.3 Business: interface Repository + `AdminContentService` cho tour_area; Data: repository — US-10
- [x] T-2.4 Api: CRUD `/admin/areas` (code unique, giá > 0, xóa mềm) + unit test Service
- [x] T-2.5 CRUD `/admin/pois` (validate lat/lng, trigger_radius_m 5–200, priority 0–100, image_url nullable, phân trang, lọc theo khu) — US-11
- [x] T-2.6 CRUD `/admin/pois/{id}/translations` (unique poi+lang → 409 TRANSLATION_EXISTS, title ≤ 200) — US-12
- [x] T-2.7 Business: `IFileStorage` (Upload, Delete, CreateSignedUrl); Data: `SupabaseFileStorage`
- [x] T-2.8 `POST /admin/translations/{id}/audio`: kiểm magic bytes mp3, ≤ 10 MB, thay audio thì xóa file cũ — US-13
- [x] T-2.9 Unit test: validate upload (sai magic bytes, quá 10 MB), trùng bản dịch
- [x] T-2.10 Nạp audio mp3 thật cho 6 POI × 3 ngôn ngữ (người dùng chuẩn bị file)
- [x] **Mốc cuối ngày 2:** admin tạo được khu/POI/bản dịch/audio qua Swagger trên Render

## Ngày 3 — Luồng khách
- [x] T-3.1 `GET /areas/{code}` kèm danh sách ngôn ngữ (404 AREA_NOT_FOUND) — US-01
- [x] T-3.2 `IPaymentGateway` + `MockPaymentGateway` (kết quả cấu hình được)
- [x] T-3.3 `PassService`: sinh token 32 byte CSPRNG, lưu SHA-256, hạn theo pass_ttl_hours; unit test
- [x] T-3.4 `POST /areas/{code}/passes`: Idempotency-Key, 201/402/409/422 — US-02
- [x] T-3.5 Rate limit 5 req/phút/IP cho endpoint trên (429 RATE_LIMITED)
- [x] T-3.6 Middleware AccessPass: 401 PASS_MISSING, 403 PASS_INVALID/PASS_EXPIRED; gắn PassContext — US-03
- [ ] T-3.7 `GeoService`: bounding box + Haversine + within_trigger + sắp xếp; unit test nhiều trường hợp biên — US-05
- [ ] T-3.8 `GET /pois/nearby` (lọc theo khu của pass, radius mặc định 100/tối đa 500, validate lat/lng, kèm image_url) — US-05
- [ ] T-3.9 `ContentService`: fallback lang → en → vi; unit test đủ các nhánh — US-06
- [ ] T-3.10 `GET /pois/{id}?lang` + signed URL audio (1 giờ), kèm image_url, PASS_AREA_MISMATCH — US-06, US-07
- [ ] T-3.11 `POST /play-logs` (pass từ token, listened_sec 0–6 giờ) — US-08 **(Cắt)**
- [ ] T-3.12 `GET /passes/current` — US-04 **(Cắt)**
- [ ] T-3.13 `GET /admin/areas/{id}/qr` (PNG, URL `{PUBLIC_WEB_URL}/a/{code}`) — US-14 **(Cắt)**
- [ ] T-3.14 Kiểm tra: không có chỗ nào log/ lưu lat, lng, token
- [ ] **Mốc cuối ngày 3:** gọi tuần tự được: areas → passes → nearby → pois/{id} trên Render

## Ngày 4 — Test, test client, hoàn thiện
- [ ] T-4.1 Coverage Business ≥ 60% (bổ sung unit test còn thiếu); CI fail khi dưới ngưỡng
- [ ] T-4.2 Integration test (Testcontainers Postgres): luồng pass + nearby — **(Cắt nếu trễ)**
- [ ] T-4.3 Web test client Leaflet (1 trang): nhập khu, mua pass, bấm bản đồ giả lập vị trí, hiện POI trong bán kính, nút Nghe
- [ ] T-4.4 Tích hợp Gemini AI (Google AI Studio Free): `IAiService` + `GeminiAiService` sinh tóm tắt lịch sử POI cho Admin — US-19 **(Cắt)**
- [ ] T-4.5 Deploy test client lên Vercel; cập nhật CORS ở Render
- [ ] T-4.6 Hoàn thiện `cd.yml`: migration bundle (Session pooler) → deploy hook → smoke test /health/ready tối đa 3 phút → rollback
- [ ] T-4.7 Đo tải nearby và pois/{id} bằng k6 (server đã thức), ghi kết quả
- [ ] T-4.8 Rà soát PRD ↔ code; cập nhật Swagger, README, `.env.example`
- [ ] T-4.9 Chuẩn bị demo: đánh thức Render, kiểm tra Supabase không bị pause, kịch bản demo 5 phút
- [ ] T-4.10 Báo cáo: sơ đồ component, use case, ERD, pipeline CI/CD (xuất từ docs)

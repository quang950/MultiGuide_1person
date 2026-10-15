# MultiGuide Backend — Hệ Thống Thuyết Minh Du Lịch Tự Động Đa Ngôn Ngữ

> **Đồ án môn Công nghệ phần mềm**  
> **Nền tảng:** .NET 10 (C# 14) / ASP.NET Core / EF Core / Supabase / Render / GitHub Actions / Leaflet.js  
> **Kiến trúc:** Modular Monolith 3 lớp nghiêm ngặt (`Api` -> `Business` <- `Data`; cả ba -> `Domain`)  
> **Độ phủ kiểm thử:** 297/297 Tests Xanh lá (100%), Line Coverage tầng Business đạt **96.41%** (CI fail khi < 60%)

---

## 1. Giới thiệu dự án

**MultiGuide** là giải pháp Backend RESTful API phục vụ hệ thống thuyết minh di tích và địa điểm du lịch tự động, hỗ trợ đa ngôn ngữ (Tiếng Việt, Tiếng Anh, Tiếng Nhật):
1. **Khách tham quan:** Quét mã QR tại khu di tích ➔ Mua vé điện tử (AccessPass) qua cổng thanh toán mock ➔ Nhận mã Token 64 ký tự CSPRNG ➔ Thiết bị tự động gửi tọa độ GPS ➔ Hệ thống tính toán Bounding Box + Haversine xác định các điểm tham quan lân cận (Nearby POI) ➔ Khi bước vào bán kính kích hoạt (Trigger Radius), hệ thống tự động phát bài thuyết minh đúng ngôn ngữ khách chọn kèm Signed URL audio phát trực tiếp từ Supabase Storage ➔ Tự động lưu Play-Log ghi nhận thời gian nghe.
2. **Ban quản trị (Admin):** Quản lý khu tham quan (Areas), quản lý điểm thuyết minh (POIs), quản lý bản dịch đa ngôn ngữ (Translations), upload file âm thanh MP3 (kiểm tra dung lượng ≤ 10MB và magic bytes MP3), tự động xuất mã QR ảnh PNG dạng `image/png` dẫn thẳng tới Web Client.
3. **Quyền riêng tư & Bảo mật:** Tuyệt đối **không lưu tọa độ GPS** và **không log tọa độ / token** ra console/hệ thống; AccessPass chỉ lưu mã băm **SHA-256** trong CSDL, token plaintext chỉ trả về duy nhất một lần khi tạo vé.

---

## 2. Kiến trúc 3 lớp (3-Tier Clean Architecture)

Dự án tuân thủ nghiêm ngặt mô hình 3 lớp phân tách trách nhiệm độc lập:
```
MultiGuide.Api (Presentation)  ──┐
                                  ├──>  MultiGuide.Business  <──  MultiGuide.Data (Infrastructure)
MultiGuide.Domain (Core)       ──┘               │
       ▲                                         │
       └─────────────────────────────────────────┘
```

- **`MultiGuide.Domain`**: Chứa 7 thực thể CSDL (TourArea, Poi, PoiTranslation, Language, Payment, AccessPass, PlayLog), 2 enum trạng thái, các exception miền nghiệp vụ và hằng số mã lỗi `ErrorCodes`. Hoàn toàn độc lập không phụ thuộc tầng nào.
- **`MultiGuide.Business`**: Tầng nghiệp vụ cốt lõi chứa toàn bộ Service xử lý logic, quy tắc nghiệp vụ, tính khoảng cách GPS, băm mật mã và **định nghĩa toàn bộ Interface** (`IAreaRepository`, `IPoiRepository`, `ITranslationRepository`, `IPaymentGateway`, `IFileStorage`, `IQrCodeGenerator`, `IClock`). Tuyệt đối **không** dính ASP.NET Core, EF Core, HTTP hay Logging.
- **`MultiGuide.Data`**: Hiện thực CSDL và hạ tầng ngoại vi: `MultiGuideDbContext` (7 DbSet, RLS không policy), EF Core Repositories, `SupabaseFileStorage` (gọi REST API Supabase Storage qua HttpClient), `MockPaymentGateway`.
- **`MultiGuide.Api`**: Cổng giao tiếp REST API: Controllers, BaseApiController, Middleware (`AccessPassMiddleware`, `GlobalExceptionHandler`), Cấu hình CORS (`AllowClient`), Cấu hình Xác thực Supabase JWT Role=Admin, Rate Limiting (5 req/phút/IP khi tạo vé), Swagger UI.
- **NetArchTest**: Tự động hóa kiểm tra kiến trúc trong CI, đảm bảo không có bất kỳ dòng code nào vi phạm ranh giới phân tầng.

---

## 3. Danh sách Endpoint API

### 3.1. Public / Khách tham quan
| Method | Endpoint | Mô tả |
|---|---|---|
| `GET` | `/areas/{code}` | Lấy thông tin khu tham quan và danh sách ngôn ngữ hỗ trợ (US-01) |
| `POST` | `/areas/{code}/passes` | Mua thẻ tham quan mock (US-02, US-03, Rate Limited 5 req/min/IP) |
| `GET` | `/passes/current` | Kiểm tra trạng thái và thời hạn vé hiện tại (US-04, `Bearer <token>`) |
| `GET` | `/pois/nearby` | Tìm điểm tham quan gần vị trí GPS (US-05, Bounding Box + Haversine, `within_trigger`) |
| `GET` | `/pois/{id}` | Lấy chi tiết thuyết minh và Signed URL audio 1h (US-06, US-07, fallback vi/en) |
| `POST` | `/play-logs` | Ghi nhận thời gian khách nghe thuyết minh (US-08) |

### 3.2. Quản trị viên (Yêu cầu JWT Supabase Bearer Token, Role: `admin`)
| Method | Endpoint | Mô tả |
|---|---|---|
| `GET, POST` | `/admin/areas` | Xem danh sách và tạo mới khu tham quan (US-10) |
| `GET, PUT, DELETE`| `/admin/areas/{id}` | Chi tiết, cập nhật và xóa mềm khu tham quan |
| `GET` | `/admin/areas/{id}/qr` | Xuất mã QR ảnh PNG dạng `image/png` dẫn tới Web Client (US-14) |
| `GET, POST` | `/admin/pois` | Xem danh sách phân trang (`page`, `page_size`) và tạo POI (US-11) |
| `GET, PUT, DELETE`| `/admin/pois/{id}` | Chi tiết, cập nhật tọa độ/bán kính và xóa mềm POI |
| `GET, POST` | `/admin/translations` | Xem danh sách và tạo bản dịch POI đa ngôn ngữ (US-12) |
| `GET, PUT, DELETE`| `/admin/translations/{id}` | Chi tiết, cập nhật văn bản và xóa bản dịch |
| `POST` | `/admin/translations/{id}/audio` | Tải lên file âm thanh MP3 (US-13, ≤ 10MB, kiểm tra magic bytes) |

### 3.3. Hệ thống & Giám sát
| Method | Endpoint | Mô tả |
|---|---|---|
| `GET` | `/health` | Liveness probe kiểm tra ứng dụng đang chạy (200 OK) |
| `GET` | `/health/ready` | Readiness probe kiểm tra kết nối CSDL Supabase PostgreSQL (200 / 503) |
| `GET` | `/swagger` | Tài liệu OpenAPI Swagger UI trực quan |

---

## 4. Hướng dẫn cài đặt & Chạy Local

### 4.1. Yêu cầu môi trường
- .NET 10.0 SDK trở lên
- Python 3.10+ (cho các công cụ đo kiểm và script tự động)
- Tài khoản Supabase (PostgreSQL & Storage bucket `audio`)

### 4.2. Cấu hình biến môi trường
Sao chép file `.env.example` thành file `.env` tại thư mục gốc:
```bash
cp .env.example .env
```
Điền các giá trị kết nối Supabase của bạn (URL, ServiceRoleKey, ConnectionStrings__Default).

### 4.3. Chạy Backend API
```bash
dotnet restore MultiGuide.sln
dotnet run --project src/MultiGuide.Api
```
API sẽ lắng nghe tại: `http://localhost:5267`  
Swagger UI: `http://localhost:5267/swagger`

### 4.4. Chạy Web Test Client
Mở file `client/index.html` trực tiếp bằng trình duyệt, hoặc dùng Live Server / HTTP server:
```bash
npx serve client
```

---

## 5. Kiểm thử & Đo độ phủ mã nguồn (Testing & Code Coverage)

Dự án áp dụng chiến lược kiểm thử đa tầng tự động hóa:
- **Unit Tests:** Kiểm thử độc lập từng Service, Validator, DTO, Security bằng Moq và xUnit.
- **Integration Tests:** 133 bài test tích hợp toàn diện qua `WebApplicationFactory` và EF Core In-Memory.
- **Architecture Tests:** 18 bài test NetArchTest khóa vĩnh viễn ranh giới 3 lớp và quy tắc bảo mật không lưu/log GPS.

### Lệnh chạy toàn bộ kiểm thử:
```bash
# Biên dịch nghiêm ngặt không cho phép warning
dotnet build MultiGuide.sln -warnaserror

# Chạy toàn bộ 297 bài test
dotnet test MultiGuide.sln

# Kiểm tra định dạng code
dotnet format MultiGuide.sln --verify-no-changes

# Đo Code Coverage tầng Business và kiểm tra ngưỡng 60%
dotnet test MultiGuide.sln --collect:"XPlat Code Coverage" --results-directory ./TestResults
python scripts/check_coverage.py --min-coverage 60.0
```

---

## 6. Pipeline CI/CD Tự Động Hóa

1. **Continuous Integration (`.github/workflows/ci.yml`):**
   - Tự động kích hoạt khi có Pull Request hoặc Push vào nhánh `main`.
   - Restore, Build `-warnaserror`, kiểm tra `dotnet format`.
   - Chạy toàn bộ test suite, thu thập báo cáo Cobertura XML.
   - Chạy `check_coverage.py`: **CI tự động FAIL nếu độ phủ tầng Business < 60%**.
   - Quét lỗ hổng bảo mật gói NuGet (`dotnet list package --vulnerable`).
   - Đóng gói Docker image `multiguide:ci`.
2. **Continuous Deployment (`.github/workflows/cd.yml`):**
   - Tự động chạy khi merge code vào `main`.
   - Tạo gói Migration Bundle độc lập và tự động áp dụng schema lên Supabase qua Session Pooler (`DATABASE_URL`).
   - Kích hoạt Render Deploy Hook kéo bản build mới.
   - Chạy Smoke test lặp lại kiểm tra tính sẵn sàng chuyên sâu `/health/ready` trong tối đa 3 phút.

---

## 7. Tài liệu kỹ thuật chi tiết

- [PRD.md](file:///d:/MultiGuide_1person/docs/PRD.md): Bản yêu cầu sản phẩm và đặc tả kỹ thuật chi tiết v1.2.
- [ARCHITECTURE.md](file:///d:/MultiGuide_1person/docs/ARCHITECTURE.md): Kiến trúc 3 lớp, ranh giới phụ thuộc và hướng dẫn triển khai.
- [ERD.md](file:///d:/MultiGuide_1person/docs/ERD.md): Sơ đồ thực thể CSDL 7 bảng chuẩn Supabase RLS.
- [TASKS.md](file:///d:/MultiGuide_1person/docs/TASKS.md): Danh sách toàn bộ các nhiệm vụ qua 4 ngày phát triển.
- [PROGRESS.md](file:///d:/MultiGuide_1person/docs/PROGRESS.md): Nhật ký tiến độ hoàn thành chi tiết của dự án.
- [LOAD_TEST_RESULTS.md](file:///d:/MultiGuide_1person/docs/LOAD_TEST_RESULTS.md): Kết quả đo tải hiệu năng bằng k6 (12.96 RPS, 0% lỗi).
- [DEMO_SCRIPT.md](file:///d:/MultiGuide_1person/docs/DEMO_SCRIPT.md): Kịch bản thuyết trình và demo 5 phút hoàn hảo.
- [REPORT_DIAGRAMS.md](file:///d:/MultiGuide_1person/docs/REPORT_DIAGRAMS.md): Toàn bộ sơ đồ báo cáo (Component, Use Case, ERD, CI/CD, Sequence).

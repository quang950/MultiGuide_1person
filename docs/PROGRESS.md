# PROGRESS — nhật ký trạng thái

> AI cập nhật file này cuối **mỗi task**. Chỉ giữ trạng thái hiện tại ở đầu file; lịch sử nằm bên dưới (mới nhất ở trên).

## Trạng thái hiện tại (cập nhật: 13/10/2026 — hoàn thành T-1.9)
- **Đang làm:** Sẵn sàng sang T-1.12 / T-1.13 / T-1.14 (Deploy Render + CD bản đầu cho /health) hoặc Ngày 2 (Admin Auth + CRUD)
- **Vừa xong:** T-1.9 Api: Triển khai GlobalExceptionHandler chuẩn RFC 7807 `ProblemDetails` có trường mở rộng `code`, định dạng lỗi 422 cho ModelState validation, BaseApiController tiền tố `/api/v1`, cấu hình Swagger OpenAPI v1, integration tests 10/10 xanh, tổng cộng 25/25 tests xanh
- **Bước kế tiếp:** T-1.13 Tạo Web Service Render và lấy Deploy Hook (người dùng làm tay) / T-1.14 Hoàn thiện CD bản đầu
- **Đang vướng:** không
- **Cách kiểm tra nhanh:** `dotnet test MultiGuide.sln`

## Giá trị cần ghi khi có (không ghi secret)
- URL Render: (chưa có)
- Supabase project ref: xbrfdxtmnhdcozblyilo (URL: https://xbrfdxtmnhdcozblyilo.supabase.co, Region: Singapore, Bucket: audio)
- Tên khu + 6 POI: Văn Miếu - Quốc Tử Giám (`van-mieu`); 6 POI: Cổng Văn Miếu, Khuê Văn Các, Giếng Thiên Quang & Vườn Bia Tiến Sĩ, Cổng Đại Thành & Sân Đại Bái, Điện Đại Thành, Nhà Thái Học
- Cách xác thực JWT Supabase (JWKS / JWT secret): (chưa chốt)

## Lịch sử
- 13/10 — Hoàn thành T-1.9: Tạo `AppException` và các exception dẫn xuất (`NotFoundException`, `ValidationException`, `ConflictException`, `UnauthorizedException`, `ForbiddenException`, `PaymentRequiredException`), hằng số `ErrorCodes` trong `MultiGuide.Domain`; triển khai `GlobalExceptionHandler` trong `MultiGuide.Api` ghi đè `ProblemDetails` kèm trường mở rộng `code`, xử lý validation 422 tự động, BaseApiController `/api/v1`, OpenAPI Swagger UI v1, bổ sung integration tests `ExceptionHandlerTests` (25/25 tests xanh).
- 13/10 — Hoàn thành T-1.8: Hoàn thiện cấu hình DI (`AddBusinessServices`, `AddDataServices`) trong `Program.cs`, triển khai endpoint `GET /health/ready` xác thực DB readiness (200/503), tự động seed dữ liệu demo theo cờ cấu hình, viết bộ integration test `HealthEndpointsTests` (offline InMemory), toàn bộ test suite 18/18 xanh.
- 13/10 — Hoàn thành T-1.7: Tạo `DbSeeder` trong `MultiGuide.Data` hỗ trợ seed 3 ngôn ngữ (`vi`, `en`, `ja`), 1 tour area (`van-mieu`), 6 POI tọa độ thực tế tại Văn Miếu và 18 PoiTranslation. Hỗ trợ cờ cấu hình `Seed:Enabled` / `Seed__Enabled` (mặc định tắt), đảm bảo tính idempotent và offline unit tests với In-Memory EF Core (14/14 tests xanh).
- 13/10 — Hoàn thành T-1.6: Tạo migration InitialCreate (7 bảng, indexes, unique, ON DELETE RESTRICT), tự động chạy SQL bật RLS (Row Level Security) không policy cho cả 7 bảng, kết nối và cập nhật thành công lên Supabase Session pooler; test 10/10 xanh.
- 13/10 — Hoàn thành T-1.5: Người dùng tạo xong project Supabase (ref: xbrfdxtmnhdcozblyilo, Region Singapore), bucket `audio` (private), cấu hình file .env thành công.
- 13/10 — Hoàn thành T-1.4: Xây dựng MultiGuideDbContext với 7 DbSets, cấu hình Fluent API đầy đủ chuẩn ERD (7 bảng, index, unique, ON DELETE RESTRICT, enum string, UTC), DesignTimeDbContextFactory hỗ trợ DATABASE_URL / postgres URI, DependencyInjection extension, unit test xác thực 10/10 test xanh.
- 13/10 — Hoàn thành T-1.3: Tạo 7 Domain entities (Language, TourArea, Poi, PoiTranslation, Payment, AccessPass, PlayLog), 2 enums (PaymentStatus, AccessPassStatus), xuất tài liệu docs/ERD.md; build và NetArchTest 5/5 xanh.
- 13/10 — Hoàn thành khung Ngày 1 (T-1.1, T-1.2, T-1.8 một phần, T-1.10, T-1.11), test kiến trúc 5/5 xanh, /health trả 200, Dockerfile & CI sẵn sàng.
- 13/10 — Hoàn thành PRD v1.2 và bộ tài liệu khởi tạo.

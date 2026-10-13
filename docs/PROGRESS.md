# PROGRESS — nhật ký trạng thái

> AI cập nhật file này cuối **mỗi task**. Chỉ giữ trạng thái hiện tại ở đầu file; lịch sử nằm bên dưới (mới nhất ở trên).

## Trạng thái hiện tại (cập nhật: 13/10/2026 — hoàn thành T-1.4)
- **Đang làm:** Sẵn sàng sang T-1.5 (Tạo project Supabase) hoặc T-1.6 (Migration đầu tiên)
- **Vừa xong:** T-1.4 Xây dựng MultiGuideDbContext, cấu hình Fluent API (khóa, index, unique, restrict cascade) cho 7 bảng, kết nối Npgsql + DesignTimeFactory, viết unit test xác thực schema
- **Bước kế tiếp:** T-1.5 (Tạo project Supabase bởi người dùng, ghi ref vào PROGRESS.md) / T-1.6 (Tạo Migration đầu tiên)
- **Đang vướng:** không
- **Cách kiểm tra nhanh:** `dotnet test MultiGuide.sln`

## Giá trị cần ghi khi có (không ghi secret)
- URL Render: (chưa có)
- Supabase project ref: (chưa có)
- Tên khu + 6 POI: (chưa chốt)
- Cách xác thực JWT Supabase (JWKS / JWT secret): (chưa chốt)

## Lịch sử
- 13/10 — Hoàn thành T-1.4: Xây dựng MultiGuideDbContext với 7 DbSets, cấu hình Fluent API đầy đủ chuẩn ERD (7 bảng, index, unique, ON DELETE RESTRICT, enum string, UTC), DesignTimeDbContextFactory hỗ trợ DATABASE_URL / postgres URI, DependencyInjection extension, unit test xác thực 10/10 test xanh.
- 13/10 — Hoàn thành T-1.3: Tạo 7 Domain entities (Language, TourArea, Poi, PoiTranslation, Payment, AccessPass, PlayLog), 2 enums (PaymentStatus, AccessPassStatus), xuất tài liệu docs/ERD.md; build và NetArchTest 5/5 xanh.
- 13/10 — Hoàn thành khung Ngày 1 (T-1.1, T-1.2, T-1.8 một phần, T-1.10, T-1.11), test kiến trúc 5/5 xanh, /health trả 200, Dockerfile & CI sẵn sàng.
- 13/10 — Hoàn thành PRD v1.2 và bộ tài liệu khởi tạo.

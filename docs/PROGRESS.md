# PROGRESS — nhật ký trạng thái

> AI cập nhật file này cuối **mỗi task**. Chỉ giữ trạng thái hiện tại ở đầu file; lịch sử nằm bên dưới (mới nhất ở trên).

## Trạng thái hiện tại (cập nhật: 13/10/2026 — hoàn thành T-1.5)
- **Đang làm:** Sẵn sàng sang T-1.6 (Migration đầu tiên + chạy lên Supabase; bật RLS không policy cho mọi bảng)
- **Vừa xong:** T-1.5 Tạo project Supabase (ref: xbrfdxtmnhdcozblyilo), tạo private bucket audio & T-1.4 Xây dựng DbContext EF Core Npgsql
- **Bước kế tiếp:** T-1.6 (Tạo Migration đầu tiên + chạy lên Supabase; bật RLS không policy cho mọi bảng)
- **Đang vướng:** không
- **Cách kiểm tra nhanh:** `dotnet test MultiGuide.sln`

## Giá trị cần ghi khi có (không ghi secret)
- URL Render: (chưa có)
- Supabase project ref: xbrfdxtmnhdcozblyilo (URL: https://xbrfdxtmnhdcozblyilo.supabase.co, Region: Singapore, Bucket: audio)
- Tên khu + 6 POI: (chưa chốt)
- Cách xác thực JWT Supabase (JWKS / JWT secret): (chưa chốt)

## Lịch sử
- 13/10 — Hoàn thành T-1.5: Người dùng tạo xong project Supabase (ref: xbrfdxtmnhdcozblyilo, Region Singapore), bucket `audio` (private), cấu hình file .env thành công.
- 13/10 — Hoàn thành T-1.4: Xây dựng MultiGuideDbContext với 7 DbSets, cấu hình Fluent API đầy đủ chuẩn ERD (7 bảng, index, unique, ON DELETE RESTRICT, enum string, UTC), DesignTimeDbContextFactory hỗ trợ DATABASE_URL / postgres URI, DependencyInjection extension, unit test xác thực 10/10 test xanh.
- 13/10 — Hoàn thành T-1.3: Tạo 7 Domain entities (Language, TourArea, Poi, PoiTranslation, Payment, AccessPass, PlayLog), 2 enums (PaymentStatus, AccessPassStatus), xuất tài liệu docs/ERD.md; build và NetArchTest 5/5 xanh.
- 13/10 — Hoàn thành khung Ngày 1 (T-1.1, T-1.2, T-1.8 một phần, T-1.10, T-1.11), test kiến trúc 5/5 xanh, /health trả 200, Dockerfile & CI sẵn sàng.
- 13/10 — Hoàn thành PRD v1.2 và bộ tài liệu khởi tạo.

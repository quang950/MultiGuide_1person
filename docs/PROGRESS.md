# PROGRESS — nhật ký trạng thái

> AI cập nhật file này cuối **mỗi task**. Chỉ giữ trạng thái hiện tại ở đầu file; lịch sử nằm bên dưới (mới nhất ở trên).

## Trạng thái hiện tại (cập nhật: 13/10/2026 — hoàn thành T-1.3)
- **Đang làm:** Sẵn sàng sang T-1.4 (Data: DbContext + EF Core Npgsql)
- **Vừa xong:** T-1.3 Tạo 7 Domain Entities, 2 Enums nghiệp vụ, tài liệu sơ đồ ERD.md chuẩn mục 6 PRD
- **Bước kế tiếp:** T-1.4 (Data: DbContext + cấu hình EF Core cho Npgsql)
- **Đang vướng:** không
- **Cách kiểm tra nhanh:** `dotnet test MultiGuide.sln`

## Giá trị cần ghi khi có (không ghi secret)
- URL Render: (chưa có)
- Supabase project ref: (chưa có)
- Tên khu + 6 POI: (chưa chốt)
- Cách xác thực JWT Supabase (JWKS / JWT secret): (chưa chốt)

## Lịch sử
- 13/10 — Hoàn thành T-1.3: Tạo 7 Domain entities (Language, TourArea, Poi, PoiTranslation, Payment, AccessPass, PlayLog), 2 enums (PaymentStatus, AccessPassStatus), xuất tài liệu docs/ERD.md; build và NetArchTest 5/5 xanh.
- 13/10 — Hoàn thành khung Ngày 1 (T-1.1, T-1.2, T-1.8 một phần, T-1.10, T-1.11), test kiến trúc 5/5 xanh, /health trả 200, Dockerfile & CI sẵn sàng.
- 13/10 — Hoàn thành PRD v1.2 và bộ tài liệu khởi tạo.

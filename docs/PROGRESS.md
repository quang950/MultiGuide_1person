# PROGRESS — nhật ký trạng thái

> AI cập nhật file này cuối **mỗi task**. Chỉ giữ trạng thái hiện tại ở đầu file; lịch sử nằm bên dưới (mới nhất ở trên).

## Trạng thái hiện tại (cập nhật: 13/10/2026 — hoàn thành khung Ngày 1)
- **Đang làm:** Sẵn sàng sang T-1.3 (Domain Entities)
- **Vừa xong:** Khung giải pháp 3 lớp (.NET 10), 7 project, AssemblyMarker, Architecture.Tests (5/5 pass), /health & Swagger UI, Dockerfile, CI workflow
- **Bước kế tiếp:** T-1.3 (Tạo entities Domain theo PRD mục 6)
- **Đang vướng:** không
- **Cách kiểm tra nhanh:** `dotnet test MultiGuide.sln` và `dotnet run --project src/MultiGuide.Api`

## Giá trị cần ghi khi có (không ghi secret)
- URL Render: (chưa có)
- Supabase project ref: (chưa có)
- Tên khu + 6 POI: (chưa chốt)
- Cách xác thực JWT Supabase (JWKS / JWT secret): (chưa chốt)

## Lịch sử
- 13/10 — Hoàn thành khung Ngày 1 (T-1.1, T-1.2, T-1.8 một phần, T-1.10, T-1.11), test kiến trúc 5/5 xanh, /health trả 200, Dockerfile & CI sẵn sàng.
- 13/10 — Hoàn thành PRD v1.2 và bộ tài liệu khởi tạo.

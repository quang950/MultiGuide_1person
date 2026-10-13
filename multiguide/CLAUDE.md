# MultiGuide Backend — hướng dẫn cho AI agent

> Đọc file này đầu tiên, mỗi phiên làm việc mới.

## Dự án là gì
Backend (REST API) cho hệ thống thuyết minh tự động đa ngôn ngữ: khách quét QR → trả phí mock → nhận AccessPass → gửi tọa độ → nhận POI gần + audio đúng ngôn ngữ. Đồ án môn Công nghệ phần mềm, **làm một mình trong 4 ngày**. Chỉ làm Backend; web test client (Leaflet, Vercel) chỉ để kiểm thử.

## Bắt đầu phiên làm việc (BẮT BUỘC)
1. Đọc `docs/PROGRESS.md` (đang làm gì) và `docs/TASKS.md` (việc kế tiếp).
2. Chạy `git status` và `git log --oneline -5`.
3. Tóm tắt cho người dùng: đang ở task nào, việc dở là gì, bước kế tiếp. **Chờ xác nhận rồi mới code.**
4. Cần chi tiết thì đọc thêm: `docs/PRD.md` (yêu cầu + API), `docs/ARCHITECTURE.md` (3 lớp), `docs/DECISIONS.md` (đã chốt, đừng đề xuất lại).

## Kết thúc mỗi task (BẮT BUỘC)
1. `dotnet build -warnaserror` và `dotnet test` phải xanh. Chưa xanh thì **không tick** task.
2. Tick `[x]` task trong `docs/TASKS.md`.
3. Cập nhật `docs/PROGRESS.md` (đang làm, vừa xong, bước kế tiếp, vướng mắc).
4. Commit: `feat(US-xx): mô tả ngắn` (hoặc `fix|chore|test|docs`).
Nếu sắp hết ngữ cảnh/quota: dừng ở ranh giới task, làm đủ 4 bước trên, ghi rõ phần dang dở vào PROGRESS.md.

## Stack
ASP.NET Core (.NET 10), EF Core + Npgsql, Supabase (Postgres, Storage, Auth), Render (free, Docker), GitHub Actions, xUnit + NetArchTest + Testcontainers.

## Kiến trúc 3 lớp (không được vi phạm)
- `MultiGuide.Api` (Presentation): Controller, DTO, middleware, DI. **Không** chứa nghiệp vụ, **không** dùng DbContext.
- `MultiGuide.Business`: Service + **mọi interface** (IXxxRepository, IPaymentGateway, IFileStorage, IClock). **Không** biết EF Core/Supabase/HTTP.
- `MultiGuide.Data`: DbContext, migration, Repository, SupabaseFileStorage, MockPaymentGateway. **Không** chứa nghiệp vụ.
- `MultiGuide.Domain`: Entity, enum, hằng số, exception. Không phụ thuộc tầng nào.
- Phụ thuộc: Api → Business ← Data; cả hai → Domain. Có NetArchTest kiểm tra trong CI.

## Quy tắc cứng
- **Không lưu và không log tọa độ (lat/lng) của khách, không log token.**
- Token AccessPass: DB chỉ lưu SHA-256, plaintext chỉ trả một lần.
- Thời gian lưu UTC (`timestamptz`), trả ISO 8601 có `Z`.
- Lỗi trả ProblemDetails kèm trường `code` (xem PRD mục 7).
- Migration chỉ thêm (tương thích ngược), chạy trong pipeline, **không** tự chạy khi API khởi động.
- Secret chỉ ở biến môi trường/GitHub Secrets. **Không commit** `.env`, key Supabase, connection string.
- Mọi bảng bật RLS không policy; bucket Storage để private.
- Không thêm tính năng ngoài PRD. Việc nằm ngoài phạm vi → ghi vào mục "Later" của `docs/PRD.md`, hỏi người dùng.
- Mỗi lượt chỉ làm **một task** trong TASKS.md, trừ khi người dùng yêu cầu khác.

## Lệnh thường dùng
```
dotnet build -warnaserror
dotnet test
dotnet format --verify-no-changes
dotnet run --project src/MultiGuide.Api
curl http://localhost:5000/health
```
(Cập nhật đường dẫn/cổng thật vào đây sau khi dựng solution.)

## Cấu trúc thư mục dự kiến
```
src/ MultiGuide.Api | .Business | .Data | .Domain
tests/ MultiGuide.Business.Tests | MultiGuide.Architecture.Tests | MultiGuide.Integration.Tests
docs/  PRD.md TASKS.md PROGRESS.md DECISIONS.md ARCHITECTURE.md GUIDE.md
.github/workflows/ ci.yml cd.yml
Dockerfile  .env.example
```

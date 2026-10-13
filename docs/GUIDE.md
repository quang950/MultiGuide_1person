# GUIDE — Hướng dẫn sử dụng bộ tài liệu

Bộ file này giúp AI (Claude Code, Cursor, Copilot...) **nối tiếp công việc qua nhiều chat/phiên** mà không mất dấu khi hết quota. Trạng thái nằm trong file và git, không nằm trong trí nhớ của chat.

## 1. Cài đặt (làm một lần)
1. Tạo thư mục repo `MultiGuide`, chép toàn bộ nội dung bộ này vào root (nhớ cả file ẩn `.env.example`, `.gitignore`).
2. `git init`, commit đầu tiên: `chore: add project docs`. Tạo repo GitHub và push.
3. Nếu dùng công cụ AI không đọc `CLAUDE.md` tự động (Cursor, Copilot...), copy nội dung `CLAUDE.md` sang file mà công cụ đó dùng (`AGENTS.md`, `.cursorrules`, `.github/copilot-instructions.md`).
4. Chép `.env.example` thành `.env` và điền giá trị thật khi có (không commit `.env`).

## 2. Ý nghĩa từng file
| File | Dùng để | Ai sửa |
|---|---|---|
| `CLAUDE.md` | AI đọc đầu tiên: dự án là gì, quy tắc, lệnh, quy trình bắt đầu/kết thúc task | Bạn (ít khi đổi) |
| `docs/PRD.md` | Yêu cầu + API chi tiết (mục 7 là nguồn duy nhất cho Acceptance Criteria) | Bạn |
| `docs/TASKS.md` | Danh sách task có checkbox, chia 4 ngày | AI tick, bạn thêm/bớt |
| `docs/PROGRESS.md` | **Trạng thái hiện tại**: đang làm, vừa xong, bước kế tiếp, vướng mắc | AI cập nhật cuối mỗi task |
| `docs/DECISIONS.md` | Quyết định đã chốt, để AI không đề xuất lại | Bạn / AI |
| `docs/ARCHITECTURE.md` | Sơ đồ 3 lớp, interface, quy tắc phụ thuộc | Bạn |
| `.env.example` | Danh sách biến môi trường (không có giá trị thật) | AI khi thêm biến |

## 3. Vòng lặp làm việc cho mỗi task
1. **Mở chat mới** (hoặc tiếp tục chat cũ) và gửi prompt khởi động (mục 4).
2. AI tóm tắt trạng thái → bạn xác nhận hoặc sửa.
3. Bạn giao **một task**: `Làm T-2.4 trong docs/TASKS.md`.
4. AI code, chạy `dotnet build` và `dotnet test`, tick task, cập nhật PROGRESS.md, commit.
5. Bạn xem diff, chạy thử; ổn thì sang task kế. Mỗi 3–5 task thì `git push` để CI chạy.

## 4. Prompt mẫu (copy dùng)

**Khởi động mọi chat mới:**
> Đọc `CLAUDE.md`, `docs/PROGRESS.md`, `docs/TASKS.md`, rồi chạy `git status` và `git log --oneline -5`. Tóm tắt: mình đang ở task nào, có gì dang dở, bước kế tiếp là gì. Chưa code, chờ mình xác nhận.

**Giao việc:**
> Làm task T-x.y. Đọc phần liên quan trong `docs/PRD.md` mục 7 nếu cần. Làm đúng quy trình kết thúc task trong `CLAUDE.md`. Không làm thêm task khác.

**Tiếp tục sau khi chat bị cắt giữa chừng:**
> Phiên trước bị ngắt giữa task. Chạy `git status` và `git diff`, so với `docs/PROGRESS.md`. Cho mình biết phần nào đã xong, phần nào dang dở, rồi đề xuất cách hoàn tất task đó (chưa sửa code).

**Khi code lỗi, cần dừng lại:**
> Dừng làm tính năng mới. Ghi vào `docs/PROGRESS.md` mục "Đang vướng": lỗi là gì, đã thử cách nào, giả thuyết hiện tại. Commit phần đang có vào nhánh `wip/...`.

**Rà soát trước khi nộp:**
> Đối chiếu code với `docs/PRD.md` mục 6 và 7: liệt kê endpoint/AC còn thiếu hoặc lệch, và kiểm tra không có chỗ nào lưu hoặc log lat/lng/token.

## 5. Quy tắc cho bạn để không mất dấu
- **Một prompt = một task.** Tránh prompt kiểu "làm hết Ngày 3". Hết quota thì chỉ mất tối đa một task.
- **Commit sau mỗi task chạy được.** Nhánh cho từng story: `feat/us-05-nearby`. Code dở không làm hỏng `main` và CI.
- **Không tin chat, tin file.** Nếu PROGRESS.md và git khác nhau, git đúng. Bảo AI cập nhật lại PROGRESS.md.
- **Task quá lớn thì tách ra** trong TASKS.md trước khi giao (ví dụ T-3.8 tách thành Service/Controller/test).
- **Đổi quyết định** thì sửa `DECISIONS.md` và `PRD.md` trước, rồi mới bảo AI sửa code. Nếu không AI sẽ quay lại theo bản cũ.
- **Khi sắp hết quota**, nhắn: "Dừng ở ranh giới task, cập nhật PROGRESS.md và commit". Việc này mất vài giây nhưng giữ được trạng thái.
- **Việc làm tay (không giao AI):** tạo project Supabase, tạo Web Service Render và lấy Deploy Hook, thêm GitHub Secrets, bật branch protection, tạo user admin, chuẩn bị file audio. Danh sách này nằm trong TASKS.md và AI sẽ nhắc bạn.

## 6. Secret cần chuẩn bị (không bao giờ commit)
- GitHub Secrets: `SUPABASE_DB_CONNECTION` (Session pooler), `RENDER_DEPLOY_HOOK_URL`, `RENDER_HEALTH_URL`.
- Render Environment: các biến trong `.env.example`.
- Không dán giá trị secret vào chat với AI; chỉ nói tên biến.

## 7. Khi cần mở rộng
- Việc mới ngoài PRD → thêm vào mục "Later" của `docs/PRD.md`, rồi tạo task mới trong TASKS.md.
- Cuối mỗi ngày: đọc PROGRESS.md, tick TASKS.md, kiểm tra CI xanh và `/health` trên Render còn chạy.

# Architecture & Technical Decisions

## ADR-001: Target Framework Selection (.NET 10.0)
- **Quyết định**: Sử dụng `net10.0` làm target framework xuyên suốt toàn bộ solution.
- **Lý do**: Máy môi trường cài sẵn .NET SDK `10.0.401`. Thư viện ORM `Pomelo.EntityFrameworkCore.MySql` phiên bản 9.0.0 và `Testcontainers.MySql` phiên bản 4.16.0 tương thích hoàn toàn trên nền tảng này mà không phát sinh lỗi cảnh báo hoặc xung đột phụ thuộc.

## ADR-002: Token Query String cho Media Endpoints
- **Quyết định**: Cho phép truyền token qua query string `?access_token=` duy nhất tại các endpoint `/api/v1/media/*`, song song với `Authorization: Bearer <token>`.
- **Lý do**: Các thẻ HTML tiêu chuẩn (`<audio src="...">` và `<img src="...">`) trên trình duyệt không cho phép tùy biến HTTP header `Authorization`. Đồng thời, middleware ghi log request bắt buộc loại bỏ hoàn toàn query string để đảm bảo bí mật token không bị rò rỉ vào hệ thống log tập trung.

## ADR-003: Rate Limiting cho Endpoint Thanh toán (/payments)
- **Quyết định**: Áp dụng giới hạn tần suất 5 request/phút theo IP client đối với endpoint `/api/v1/payments`.
- **Lý do**: Pipeline RateLimiter của ASP.NET Core chạy trước khi đọc và bind Request Body (`session_id`), do đó việc giới hạn theo IP là phương án khả thi và ngăn ngừa tấn công DoS hiệu quả nhất. Bật cấu hình `ForwardedHeaders` để nhận diện chính xác IP client qua Caddy reverse proxy.

## ADR-004: Xoay Token khi Replay Idempotent Payment
- **Quyết định**: Khi nhận request thanh toán lặp lại (cùng `session_id` và `Idempotency-Key`, trạng thái `succeeded`), hệ thống sinh token mới ngẫu nhiên 32 bytes, cập nhật `token_hash` vào bảng `AccessPass`, giữ nguyên `expires_at` và trả về token mới.
- **Lý do**: Cơ sở dữ liệu chỉ lưu SHA-256 hash của token để đảm bảo an toàn, không thể giải mã token cũ. Khách hàng gặp sự cố rớt mạng sau khi trừ tiền vẫn lấy lại được token hợp lệ để sử dụng dịch vụ mà không bị thu tiền lần thứ hai.

## ADR-005: Chiến lược Xóa (Soft Delete vs Hard Delete)
- **Quyết định**: `TourArea` và `Poi` chỉ xóa mềm thông qua cờ `is_active = false`. `PoiTranslation` được phép xóa cứng (hard delete).
- **Lý do**: `TourArea` và `Poi` được tham chiếu bởi các bảng lịch sử (VisitorSession, Poi, PlayLog). Ngược lại, `PoiTranslation` không có bảng nào khóa ngoại trỏ tới nên xóa cứng an toàn và giải phóng dung lượng.

## ADR-006: Lưu trữ và Thu hồi File Âm thanh
- **Quyết định**: Khi xóa bản dịch hoặc cập nhật file âm thanh mới, file audio cũ trên ổ đĩa được chuyển sang thư mục con `_deleted/` thay vì xóa vĩnh viễn ngay lập tức.
- **Lý do**: Chống mất mát dữ liệu do thao tác nhầm lẫn từ phía Admin và phục vụ mục đích kiểm toán/phục hồi khi cần thiết.

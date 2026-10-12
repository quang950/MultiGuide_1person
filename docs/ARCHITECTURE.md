# MultiGuide Architecture

## 1. Tổng quan
MultiGuide là hệ thống backend cung cấp thuyết minh tự động đa ngôn ngữ cho khách du lịch tại Việt Nam thông qua quét mã QR, thanh toán một lần và định vị GPS không trạng thái.

## 2. Kiến trúc Clean Architecture 3 lớp
Hệ thống tuân thủ nghiêm ngặt nguyên tắc phụ thuộc một chiều (inversion of control):

- **MultiGuide.Domain**:
  - Thực thể (Entities), Enum, Hằng số mã lỗi (`ErrorCodes`), `DomainException`.
  - Không phụ thuộc bất kỳ project hoặc framework ngoài nào.

- **MultiGuide.Business**:
  - Chứa nghiệp vụ ứng dụng, Service implementations, nội bộ DTO, và toàn bộ Interfaces (`IXxxRepository`, `IPaymentGateway`, `IFileStorage`, `IClock`, `IPasswordHasher`, `IAudioInspector`, `IQrCodeGenerator`, `IUnitOfWork`).
  - Phụ thuộc duy nhất `MultiGuide.Domain`.
  - Nghiêm cấm: EF Core, ASP.NET Core, direct System.IO, direct HttpClient.

- **MultiGuide.Data**:
  - Hiện thực `MultiGuideDbContext`, EF Core Configurations, Migrations, Repositories, LocalDiskFileStorage, MockPaymentGateway, BcryptPasswordHasher, TagLibAudioInspector, QrCoderGenerator, SystemClock.
  - Phụ thuộc `MultiGuide.Business` và `MultiGuide.Domain`.

- **MultiGuide.Api**:
  - ASP.NET Core Web API Controllers, Request/Response DTOs, Middlewares, DI Composition Root (`Program.cs`, `Extensions/`).
  - Controllers và Middlewares chỉ giao tiếp với Business Services qua Interfaces, không tham chiếu trực tiếp Data layer.

- **MultiGuide.Migrator**:
  - One-shot console application thực thi EF Core Migrations và khởi tạo dữ liệu mẫu (Seed Data) an toàn, độc lập với runtime của API server.

## 3. Mô hình Dữ liệu (ERD v2.1)
Bao gồm 9 bảng chính:
1. `Language`: Quản lý danh mục ngôn ngữ (`vi`, `en`, `ja`,...).
2. `TourArea`: Khu du lịch / tham quan (`code`, `price_usd`, `pass_ttl_hours`,...).
3. `Poi`: Điểm tham quan (`lat`, `lng`, `trigger_radius_m`, `priority`, `image_path`,...).
4. `PoiTranslation`: Bản dịch thuyết minh của POI (`title`, `description`, `audio_path`, `audio_duration_sec`, `audio_checksum`,...).
5. `VisitorSession`: Phiên tương tác của du khách (`area_id`, `language_code`).
6. `Payment`: Giao dịch thanh toán với cơ chế Idempotency (`session_id`, `idempotency_key`, `request_hash`,...).
7. `AccessPass`: Vé điện tử cấp quyền truy cập 24h (`token_hash`, `issued_at`, `expires_at`).
8. `PlayLog`: Lịch sử nghe thuyết minh không lưu tọa độ (`session_id`, `poi_id`, `listened_sec`, `completed`).
9. `AdminUser`: Tài khoản quản trị (`email`, `password_hash`, `role`, `failed_login_count`, `locked_until`).

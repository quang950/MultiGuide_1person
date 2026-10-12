# PHẦN A — MASTER PROMPT

## A1. Vai trò và mục tiêu

Bạn là senior backend engineer (.NET) nhiều năm kinh nghiệm. Hãy xây **MVP chạy được** cho **MultiGuide**: backend hệ thống thuyết minh tự động đa ngôn ngữ cho khách nước ngoài tại Việt Nam.

Luồng khách: quét QR tại cổng khu du lịch → chọn ngôn ngữ → trả 2 USD một lần (cổng thanh toán mock) → nhận AccessPass 24 giờ → FE gửi GPS định kỳ → BE trả POI gần + khoảng cách → khách nghe audio đúng ngôn ngữ. Admin quản lý nội dung qua REST API.

Phạm vi: **chỉ backend** (REST API + MySQL + Docker + CI/CD). FE, GPS, debounce/cooldown, bản đồ là việc của FE.

Repo GitHub: `quang950/MultiGuide`. Nếu repo/solution Sprint 0 (kiến trúc 3 lớp + CI cơ bản) đã tồn tại thì **kế thừa**, không tạo lại từ đầu; hãy đọc cấu trúc hiện có trước.

## A2. Nguyên tắc làm việc (bắt buộc)

- Không hỏi lại. Gặp điểm mơ hồ: chọn phương án hợp lý nhất, ghi vào `docs/DECISIONS.md` (mục: quyết định, lý do).
- Làm theo từng phase tôi giao. Mỗi phase kết thúc phải: `dotnet build -warnaserror` xanh, `dotnet test` xanh, rồi commit (Conventional Commits, ví dụ `feat(payment): idempotent payments`).
- Không giả lập kết quả: phải thực sự chạy lệnh build/test/docker và sửa đến khi xanh. Báo cáo cuối mỗi phase: việc đã làm, lệnh đã chạy, kết quả, điểm lệch so với spec.
- Không thêm tính năng ngoài spec (không thanh toán thật, không AI/TTS, không dashboard, không offline pack).
- Không commit secret. Mọi cấu hình qua biến môi trường; có `.env.example`.
- Code C# sạch: `Nullable enable`, async/await + CancellationToken xuyên suốt, không `.Result`/`.Wait()`, không magic string cho mã lỗi.

## A3. Stack

- .NET LTS mới nhất tương thích với packages (ưu tiên `net10.0`; nếu Pomelo/Testcontainers chưa hỗ trợ thì dùng `net8.0`; nếu solution Sprint 0 đã có target thì giữ nguyên). Ghi lựa chọn vào DECISIONS.md.
- ASP.NET Core Web API (Controllers, không dùng Minimal API), EF Core + `Pomelo.EntityFrameworkCore.MySql`, MySQL 8.0.
- Serilog (JSON console) + enrichers; `Asp.Versioning.Mvc` (`/api/v1`); Swashbuckle (OpenAPI); `System.Threading.RateLimiting` (built-in RateLimiter).
- Auth admin: JWT HS256 (`Microsoft.AspNetCore.Authentication.JwtBearer`), băm mật khẩu `BCrypt.Net-Next` cost 12 (đặt sau interface `IPasswordHasher`).
- QR: `QRCoder`. Đọc duration mp3: `TagLibSharp` (đặt sau interface `IAudioInspector`).
- Test: xUnit, Moq (hoặc NSubstitute), FluentAssertions, `Microsoft.AspNetCore.Mvc.Testing`, `Testcontainers.MySql`, `NetArchTest.Rules`, `coverlet.msbuild`.
- Hạ tầng: Docker, Docker Compose, Caddy (HTTPS tự động), GitHub Actions, GHCR.

## A4. Cấu trúc solution

```
MultiGuide.sln
Directory.Build.props        (Nullable, ImplicitUsings, TreatWarningsAsErrors, analyzers, LangVersion)
.editorconfig                (dotnet format --verify-no-changes phải pass)
src/
  MultiGuide.Domain/         entity, enum, constants (ErrorCodes), DomainException
  MultiGuide.Business/       services, DTO nội bộ, MỌI interface (IXxxRepository, IPaymentGateway, IFileStorage, IClock, IPasswordHasher, IAudioInspector, IQrCodeGenerator, IUnitOfWork)
  MultiGuide.Data/           DbContext, EF configurations, migrations, repositories, LocalDiskFileStorage, MockPaymentGateway, SystemClock, BcryptPasswordHasher, TagLibAudioInspector, QrCoderGenerator
  MultiGuide.Api/            controllers, request/response DTO, validators, middleware, DI composition root (Program.cs + Extensions/)
  MultiGuide.Migrator/       console app one-shot: chạy migration + seed (theo cờ)
tests/
  MultiGuide.Business.Tests/     unit test
  MultiGuide.Api.IntegrationTests/  WebApplicationFactory + Testcontainers MySQL
  MultiGuide.ArchitectureTests/  NetArchTest
  load/nearby.js                 k6
deploy/
  docker-compose.yml, docker-compose.override.yml (build local), Caddyfile, deploy.sh, backup.sh, .env.example
docs/ ARCHITECTURE.md, DECISIONS.md, openapi.json
```

## A5. Quy tắc phụ thuộc (kiểm tra bằng test kiến trúc)

- `Domain` không tham chiếu project nào.
- `Business` chỉ tham chiếu `Domain`. Cấm: EF Core, `Microsoft.AspNetCore.*`, `System.IO.File/Directory` (đi qua `IFileStorage`), SDK thanh toán, HttpClient trực tiếp.
- `Data` tham chiếu `Business` + `Domain`, implement các interface do Business định nghĩa.
- `Api` tham chiếu `Business` + `Domain` (+ `Data` **chỉ** ở lớp composition root `Extensions/` và `Program.cs`).
- Controllers và Middleware **không được** dùng bất kỳ type nào trong namespace `MultiGuide.Data`.
- Controller chỉ: nhận request, validate DTO, gọi Service, map response. Không chứa quy tắc nghiệp vụ.
- Middleware chỉ **xác thực** (token hợp lệ?) bằng cách gọi Service; **phân quyền theo tài nguyên** (pass có đúng khu của POI không, role nào được làm gì) nằm trong Service.
- Business ném `DomainException(ErrorCode, message)`; chỉ Api map sang HTTP status + ProblemDetails.
- Business tự validate bất biến nghiệp vụ (ví dụ trigger_radius_m 5–200) dù DTO đã validate; không tin lớp trên.

## A6. Mô hình dữ liệu (ERD v2.1 — 9 bảng)

Quy ước chung: mọi FK `ON DELETE RESTRICT`; thời gian lưu UTC (`DateTime` Kind=Utc), JSON trả ISO 8601 có hậu tố `Z`; enum lưu dạng string; charset `utf8mb4`, collation `utf8mb4_0900_ai_ci`; cột hash/checksum dùng `CHAR(64)` charset `ascii` collation `ascii_bin`; Guid ↔ `CHAR(36)`.

| Bảng | Cột | Ràng buộc/Index |
|---|---|---|
| **Language** | code VARCHAR(10) PK, name VARCHAR(50), is_active BOOL | seed: vi, en, ja |
| **TourArea** | id BIGINT PK, code VARCHAR(50), name VARCHAR(200), center_lat DOUBLE, center_lng DOUBLE, radius_m INT, price_usd DECIMAL(10,2), pass_ttl_hours INT (default 24), is_active BOOL | UNIQUE(code); CHECK price_usd>0, radius_m>0, pass_ttl_hours>0 |
| **Poi** | id BIGINT PK, area_id FK, lat DOUBLE, lng DOUBLE, trigger_radius_m INT (default 30), priority INT, **image_path VARCHAR(500) NULL** (storage key tương đối, không phải URL), is_active BOOL, created_at, updated_at | INDEX(area_id,is_active), INDEX(lat,lng); CHECK lat∈[-90,90], lng∈[-180,180], trigger_radius_m∈[5,200], priority∈[0,100] |
| **PoiTranslation** | id BIGINT PK, poi_id FK, language_code FK, title VARCHAR(200), description TEXT, audio_path VARCHAR(500) NULL, audio_duration_sec INT NULL, **audio_size_bytes INT NULL**, audio_checksum CHAR(64) NULL, updated_at | **UNIQUE(poi_id, language_code)** |
| **VisitorSession** | id CHAR(36) PK, area_id FK, language_code FK, started_at, ended_at NULL | INDEX(area_id) |
| **Payment** | id BIGINT PK, session_id FK, amount DECIMAL(10,2), currency CHAR(3), provider VARCHAR(50), provider_ref VARCHAR(100) NULL, idempotency_key VARCHAR(100), request_hash CHAR(64), status VARCHAR(20) (pending/succeeded/failed), created_at | **UNIQUE(session_id, idempotency_key)** |
| **AccessPass** | id BIGINT PK, session_id FK, payment_id FK, token_hash CHAR(64), issued_at, expires_at, status VARCHAR(20) (active/revoked) | **UNIQUE(payment_id)**, **UNIQUE(token_hash)** |
| **PlayLog** | id BIGINT PK, session_id FK, poi_id FK, language_code FK, started_at, listened_sec INT, completed BOOL | INDEX(poi_id, started_at); CHECK listened_sec∈[0,21600]. **Không có cột tọa độ.** |
| **AdminUser** | id BIGINT PK, email VARCHAR(255), password_hash VARCHAR(255), role VARCHAR(30) (`super_admin`/`content_manager`), is_active BOOL, failed_login_count INT, locked_until DATETIME NULL, created_at | UNIQUE(email); CHECK role IN (...) |

Không có bảng Role, AudioFile, AuditLog trong MVP (đã chủ động bỏ). `area_id` của pass **không lưu riêng**: suy ra qua AccessPass → VisitorSession.area_id.
Xóa bản dịch là xóa cứng được (không bảng nào FK tới PoiTranslation). Khu/POI chỉ xóa mềm bằng `is_active`.

## A7. Quy tắc nghiệp vụ và hợp đồng API (prefix `/api/v1`)

### Lỗi chuẩn
ProblemDetails (RFC 7807) + trường `code` (ổn định) + `traceId`. Danh sách:

| HTTP | code |
|---|---|
| 401 | AUTH_REQUIRED, INVALID_CREDENTIALS, PASS_MISSING |
| 402 | PAYMENT_FAILED |
| 403 | FORBIDDEN, PASS_INVALID, PASS_EXPIRED, PASS_AREA_MISMATCH |
| 404 | AREA_NOT_FOUND, POI_CONTENT_NOT_FOUND, NOT_FOUND |
| 409 | TRANSLATION_EXISTS, ALREADY_PAID, PAYMENT_IN_PROGRESS, CONFLICT |
| 413 | PAYLOAD_TOO_LARGE |
| 422 | VALIDATION_ERROR, IDEMPOTENCY_KEY_REUSED |
| 423 | ACCOUNT_LOCKED |
| 429 | RATE_LIMITED |
| 500 | INTERNAL_ERROR (không lộ stack trace) |

### Khách (Tourist)

1. `GET /areas/{code}` (public) → 200 `{code,name,price_usd,pass_ttl_hours}`; không tồn tại hoặc `is_active=false` → 404 AREA_NOT_FOUND.
2. `GET /areas/{code}/languages` (public) → các Language `is_active` có ít nhất một POI active của khu đó có bản dịch. Trả `[{code,name}]`.
3. `POST /sessions` body `{area_code, language_code}` (public) → 201 `{session_id}`. Khu không active → 404; ngôn ngữ không hợp lệ/không có trong khu → 422.
4. `PATCH /sessions/{id}/language` body `{language_code}` → 204. Các request sau nếu không truyền `lang` thì dùng ngôn ngữ của session.
5. `POST /payments`, header bắt buộc `Idempotency-Key` (thiếu → 422), body `{session_id}`. **Số tiền lấy từ TourArea.price_usd phía server, không nhận từ client.** Thuật toán trong `PaymentService`:
   - `request_hash = SHA-256(JSON chuẩn hóa của body)`.
   - Tìm Payment theo (session_id, key): nếu có → replay:
     - hash khác → 422 IDEMPOTENCY_KEY_REUSED;
     - status `pending` → chờ poll tối đa 5 giây cho tới khi final; vẫn pending → 409 PAYMENT_IN_PROGRESS;
     - `failed` → 402 PAYMENT_FAILED (cùng kết quả);
     - `succeeded` → 200, **xoay token**: sinh token mới cho chính AccessPass đó (cập nhật `token_hash`, giữ nguyên `expires_at`), trả `access_token` mới. (Lý do: DB chỉ lưu hash nên không thể trả lại token cũ; khách mất response vì rớt mạng vẫn lấy lại được token mà không bị trừ tiền lần 2.)
   - Chưa có: nếu session đã có pass `active` chưa hết hạn → 409 ALREADY_PAID. Ngược lại INSERT Payment `pending` (bắt `DuplicateKey` do race → quay về nhánh replay), gọi `IPaymentGateway.ChargeAsync` **ngoài transaction**, rồi trong một transaction cập nhật status + (nếu thành công) tạo AccessPass.
   - Thành công lần đầu → **201** `{status:"succeeded", access_token, expires_at}`; gateway từ chối → 402 PAYMENT_FAILED (khách thử lại với key mới).
   - Token: 32 byte từ `RandomNumberGenerator`, encode base64url; DB chỉ lưu SHA-256 hex; `expires_at = issued_at + pass_ttl_hours`.
   - `MockPaymentGateway` cấu hình bằng `Payment__Mock__Mode` = `Success` | `Fail` | `FailEveryN:n` và `Payment__Mock__DelayMs` (để test race).
6. `GET /passes/current` (AccessPass) → `{status:"active", expires_at, remaining_seconds, area_code}`.
7. `GET /pois/nearby?lat&lng&lang&radius` (AccessPass):
   - lat∈[-90,90], lng∈[-180,180] (sai → 422); radius mặc định 100, tối đa 500 (lớn hơn → 422); `lang` tùy chọn (mặc định ngôn ngữ session).
   - Chỉ POI active **thuộc khu của pass**, có ít nhất một bản dịch.
   - Bounding box trên (lat,lng) trước (Δlat = r/111320; Δlng = r/(111320·cos(lat))), rồi Haversine (R=6371000 m).
   - Trả mỗi POI: `poi_id, title (đã fallback), lat, lng, trigger_radius_m, priority, distance_m` (làm tròn 1 m), `within_trigger` (= distance ≤ trigger_radius_m).
   - Sắp theo `priority` giảm dần, rồi `distance_m` tăng dần.
   - **Stateless: không lưu tọa độ vào DB, không ghi tọa độ vào log.**
8. `GET /pois/{id}?lang` (AccessPass) → `{poi_id, title, description, image_url, audio_url, language, fallback_language}`.
   - Fallback: ngôn ngữ chọn → `en` → `vi`. `fallback_language` = null nếu không phải fallback.
   - POI tắt hoặc không có bản dịch nào → 404 POI_CONTENT_NOT_FOUND. Có chữ nhưng không audio → `audio_url = null`.
   - POI khác khu của pass → 403 PASS_AREA_MISMATCH.
   - `audio_url` = `/api/v1/media/audio/{translationId}`, `image_url` = `/api/v1/media/image/{poiId}` (Service dựng; DB chỉ lưu path).
9. `GET /media/audio/{translationId}` và `GET /media/image/{poiId}` (AccessPass): trả file qua `PhysicalFile`/`FileStreamResult` với **`enableRangeProcessing: true`** (206 khi có Range), `Content-Type: audio/mpeg` hoặc đúng loại ảnh, `Accept-Ranges: bytes`, kiểm tra khu của pass.
   - Vì thẻ `<audio src>` của trình duyệt không gửi được header Authorization, các endpoint `/media/*` (và chỉ chúng) chấp nhận thêm token ở query `?access_token=`. Request logging **không được ghi query string**. Ghi quyết định này vào DECISIONS.md.
10. `POST /play-logs` (AccessPass) body `{poi_id, language_code, listened_sec, completed}` → 201. `session_id` lấy từ pass, không tin body; POI phải thuộc khu của pass; listened_sec ∈ [0, 21600]; `started_at = now − listened_sec`. Không có tọa độ.

**Middleware AccessPass**: đọc `Authorization: Bearer <token>` (riêng `/media/*` cho phép query) → gọi `IAccessPassService.ValidateAsync(token)`. Thiếu → 401 PASS_MISSING; không tìm thấy hoặc revoked → 403 PASS_INVALID; `expires_at ≤ IClock.UtcNow` → 403 PASS_EXPIRED. Hợp lệ → gắn `PassContext(session_id, area_id, language_code, pass_id)` vào `HttpContext.Items`. Áp dụng bằng attribute `[RequireAccessPass]` (endpoint metadata), không dùng danh sách đường dẫn cứng.

### Admin (JWT + role)

- `POST /admin/auth/login` `{email,password}` → `{access_token, expires_in}`. JWT hạn 60 phút, claim `sub`, `role`. Sai mật khẩu → 401 INVALID_CREDENTIALS (thông báo y hệt dù email có tồn tại hay không; với email không tồn tại vẫn chạy một lần verify hash giả để cân bằng thời gian). Sai 5 lần liên tiếp → `locked_until = now+15 phút`, các lần sau (kể cả đúng mật khẩu) trả 423 ACCOUNT_LOCKED cho tới khi hết khóa; đăng nhập đúng thì reset `failed_login_count`. Tài khoản `is_active=false` → 401 INVALID_CREDENTIALS.
- Phân quyền: `content_manager` và `super_admin` đều đọc/ghi khu, POI, bản dịch, audio, ảnh, QR; **chỉ `super_admin` được xóa mềm khu** (khác → 403 FORBIDDEN). Quản lý tài khoản admin để Later.
- Khu: `GET/POST /admin/areas`, `GET/PUT/DELETE /admin/areas/{id}`. `code` regex `^[a-z0-9]+(-[a-z0-9]+)*$`, unique, **không đổi sau khi tạo**; price_usd>0; DELETE = `is_active=false`.
- POI: `GET/POST /admin/pois` (list phân trang `page`, `page_size ≤ 100`, lọc `area_id`; response `{items,page,page_size,total}`), `GET/PUT/DELETE /admin/pois/{id}` (DELETE = soft).
- Bản dịch: `GET/POST /admin/pois/{id}/translations`, `PUT/DELETE /admin/translations/{id}`. Trùng (poi_id, language_code) → 409 TRANSLATION_EXISTS; title ≤ 200; language_code phải tồn tại.
- Ngôn ngữ (bổ sung nhỏ để "thêm ngôn ngữ = thêm dữ liệu"): `GET/POST /admin/languages`, `PUT /admin/languages/{code}`.
- Audio: `POST /admin/translations/{id}/audio` (multipart). Chỉ mp3: kiểm tra **magic bytes** (`ID3` hoặc frame sync `0xFF 0xE0` mask), bỏ qua đuôi file/Content-Type do client gửi; tối đa 10 MB (lớn hơn → 413); tên file lưu do server sinh (GUID + `.mp3`, **không bao giờ dùng tên người dùng** để chống path traversal); tính SHA-256, size, duration (`IAudioInspector`); lưu qua `IFileStorage`; nếu đã có audio cũ thì chuyển file cũ vào thư mục `_deleted/`. `DELETE` audio cũng chuyển file vào `_deleted/`.
- Ảnh: `POST /admin/pois/{id}/image` jpg/png/webp, kiểm tra magic bytes, ≤ 5 MB.
- QR: `GET /admin/areas/{id}/qr` → `image/png` chứa URL `{App__PublicWebUrl}/a/{code}`.
- Content status: `GET /admin/areas/{id}/content-status` → với mỗi ngôn ngữ active và mỗi POI active: `has_translation`, `has_audio`; kèm tổng hợp theo ngôn ngữ (số POI đủ/thiếu).

### Hệ thống
- `GET /health` → 200 luôn (liveness). `GET /health/ready` → kiểm tra kết nối DB, 503 nếu lỗi.

## A8. Yêu cầu phi chức năng

- **Rate limit** (ASP.NET Core RateLimiter, fixed/sliding window): `/admin/auth/login` 10/phút/IP; `/payments` 5/phút (chia theo IP, ghi chú vào DECISIONS vì rate limiter chạy trước khi bind body nên không lấy được session_id); chung 120/phút/IP. Vượt → 429 RATE_LIMITED + `Retry-After`. Bật `ForwardedHeaders` (X-Forwarded-For/Proto) vì đứng sau Caddy để lấy IP thật.
- **Bảo mật**: HTTPS + HSTS, security headers (`X-Content-Type-Options`, `Referrer-Policy`, `X-Frame-Options`), CORS chỉ cho origin trong `Cors__AllowedOrigins`, JWT key ≥ 32 byte từ env (app **fail fast** khi thiếu/ngắn), không lưu thông tin thẻ.
- **Quyền riêng tư**: không lưu tọa độ ở DB; log không ghi lat/lng, token (Bearer và `access_token`), mật khẩu. Request logging chỉ ghi path, không ghi query string. Có integration test chứng minh điều này (dùng in-memory Serilog sink).
- **Quan sát**: Serilog JSON; `X-Correlation-Id` (sinh nếu thiếu, trả lại trong response, đưa vào mọi log và vào ProblemDetails).
- **Hiệu năng**: p95 `GET /pois/nearby` (≤ 500 POI) ≤ 300 ms; `GET /pois/{id}` ≤ 200 ms. Truy vấn nearby dùng `AsNoTracking`, projection, không N+1.
- **Cấu hình** (biến môi trường, có `.env.example`): `ConnectionStrings__Default`, `Jwt__Key`, `Jwt__Issuer`, `Jwt__ExpiresMinutes`, `Storage__RootPath`, `App__PublicWebUrl`, `Cors__AllowedOrigins`, `Seed__Enabled`, `Seed__AdminEmail`, `Seed__AdminPassword`, `Payment__Mock__Mode`, `Payment__Mock__DelayMs`, `DOMAIN`, `MYSQL_*`, `IMAGE_TAG`.
- **OpenAPI** bật ở demo (Swagger UI), xuất `docs/openapi.json` để FE dựng mock server.

## A9. Migration, seed, Docker

- Migration chạy bởi **`MultiGuide.Migrator`** (container one-shot), **API không tự migrate lúc khởi động**. CI sinh thêm `dotnet ef migrations script --idempotent` làm artifact; có `deploy/rollback.md` hướng dẫn rollback (script down hoặc restore từ dump). Migration theo kiểu expand/contract để bản API cũ vẫn chạy được trên schema mới (cần cho rollback image).
- Seed (cờ `Seed__Enabled=true`, idempotent): Language vi/en/ja; 1 super_admin từ `Seed__AdminEmail/Password` (mật khẩu băm); 1 TourArea `demo` + 6 POI quanh Dinh Độc Lập, TP.HCM (10.7769, 106.6955), mỗi POI 3 bản dịch vi/en/ja có title/description (placeholder, chưa audio). Tên thật sẽ thay sau.
- `Dockerfile` multi-stage cho api và migrator, chạy non-root, `ASPNETCORE_URLS=http://+:8080`.
- `docker-compose.yml`: `caddy`, `api`, `mysql` (healthcheck), `migrator` (one-shot, `depends_on mysql: service_healthy`); `api` `depends_on migrator: service_completed_successfully`. Volume: `audio-storage`, `mysql-data`, `caddy-data`, `caddy-config`. Image lấy từ `ghcr.io/quang950/multiguide-api:${IMAGE_TAG:-latest}` và `...-migrator`; file override cho build local. **`docker compose up` một lệnh chạy được toàn hệ thống** (dev dùng `DOMAIN=localhost`, Caddy tự cấp cert nội bộ).
- `Caddyfile`: `{$DOMAIN}`; reverse proxy `/api/*`, `/health*`, `/swagger*` tới `api:8080`; phần còn lại phục vụ web tĩnh từ `/srv/web` (FE sau này); header bảo mật.
- `deploy/backup.sh`: `mysqldump` hằng ngày (cron) + giữ 7 bản; backup cả volume audio.

## A10. CI/CD (GitHub Actions)

**`ci.yml`** (trigger: pull_request và push mọi nhánh; có `workflow_call` để CD tái sử dụng; mục tiêu ≤ 8 phút):
1. checkout, setup-dotnet (cache NuGet), `dotnet restore`.
2. `dotnet build -c Release -warnaserror --no-restore`.
3. `dotnet format --verify-no-changes`.
4. `dotnet test` kèm coverlet: `/p:CollectCoverage=true /p:Include="[MultiGuide.Business]*" /p:Threshold=60 /p:ThresholdType=line` (**fail nếu coverage tầng Business < 60%**); gồm cả test kiến trúc và integration test (Testcontainers cần Docker, runner ubuntu có sẵn). Upload báo cáo coverage artifact.
5. `docker build` api và migrator (không push).
6. `dotnet list package --vulnerable --include-transitive`, parse output, **fail nếu có mức High hoặc Critical** (lệnh này tự nó không trả exit code lỗi nên phải grep).
7. Artifact: script migration idempotent.

**`cd.yml`** (trigger push vào `main`, job đầu gọi `ci.yml`):
1. Login GHCR bằng `GITHUB_TOKEN`; build và push `multiguide-api` + `multiguide-migrator` với tag `sha-<commit>` và `latest`.
2. Copy `deploy/` lên server (scp) rồi SSH chạy `deploy.sh <tag>`. Secrets cần: `SSH_HOST`, `SSH_USER`, `SSH_KEY`, `DEPLOY_PATH`, `GHCR_PULL_USER`, `GHCR_PULL_TOKEN`, `DOMAIN`.
3. `deploy.sh` (idempotent, `set -euo pipefail`): lưu tag đang chạy vào `.previous_tag` → `docker login ghcr.io` → `docker compose pull` → dump DB trước khi migrate → `docker compose run --rm migrator` → `docker compose up -d` → **smoke test** `curl -fsS https://$DOMAIN/health/ready` retry tối đa 60 giây.
4. Smoke test fail → `deploy.sh` tự deploy lại tag trước đó, in log, thoát mã lỗi để workflow **đỏ**.
5. Mục tiêu: từ merge đến `/health/ready` OK ≤ 10 phút. Dùng `concurrency` để không chạy hai deploy song song.

**`loadtest.yml`** (nightly + manual): `docker compose up` bản local, seed, chạy k6 `tests/load/nearby.js` với threshold `http_req_duration{p(95)}<300`; sinh 500 POI trước khi test.

Hướng dẫn bật branch protection cho `main` (yêu cầu CI xanh, ≥ 1 review nếu nhóm ≥ 2 người) ghi trong `README.md` (agent không tự bật được).

## A11. Chiến lược test

- **Unit (Business)**: mock repository/gateway/clock. Bắt buộc phủ: PaymentService (mọi nhánh idempotency), AccessPassService, GeoService (Haversine, bounding box, sắp xếp, biên within_trigger), ContentService (fallback chain đủ nhánh), AuthService (khóa tài khoản, reset), AdminContentService (validation, magic bytes), AnalyticsService.
- **Integration (Api + MySQL thật bằng Testcontainers)**: payment idempotent tuần tự, replay xoay token, cùng key khác body → 422, **10 request song song cùng key chỉ tạo 1 Payment**, thanh toán thất bại rồi thử key mới; pass check (missing/invalid/expired bằng `IClock` giả/area mismatch/revoked); nearby (validation, thứ tự, bán kính, POI tắt, khu khác); fallback; upload (sai magic bytes dù đuôi .mp3, quá dung lượng, thay audio cũ); media Range trả 206; RBAC (content_manager bị 403 khi xóa khu); lockout (5 lần sai → 423, hết khóa); ProblemDetails có `code` + `traceId`; correlation id; **log không chứa lat/lng/token**; `/health/ready` 503 khi DB lỗi.
- **Kiến trúc (NetArchTest)**: các quy tắc ở A5.

## A12. Definition of Done mỗi phase

Build `-warnaserror` xanh, `dotnet format --verify-no-changes` xanh, test xanh, OpenAPI cập nhật, README/DECISIONS cập nhật, đã commit.
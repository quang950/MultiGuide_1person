# ARCHITECTURE — kiến trúc 3 lớp

## Sơ đồ
```
        ┌─────────────────────────────┐
Client →│ Presentation: MultiGuide.Api│  Controller, DTO, middleware, DI
        └──────────────┬──────────────┘
                       │ gọi
        ┌──────────────▼──────────────┐
        │ Business: MultiGuide.Business│  Service + interface (Repository, IFileStorage,
        └──────────────▲──────────────┘  IPaymentGateway, IClock)
                       │ implement
        ┌──────────────┴──────────────┐
        │ Data/Infra: MultiGuide.Data │  DbContext, Repository, SupabaseFileStorage,
        └──────────────┬──────────────┘  MockPaymentGateway
                       ▼
     Supabase Postgres · Supabase Storage · (Supabase Auth: Api chỉ kiểm JWT)
     Tất cả tầng → MultiGuide.Domain (entity, enum, exception)
```

## Quy tắc phụ thuộc
Api → Business ← Data; Api, Business, Data → Domain. Business **không** tham chiếu Data/Api. NetArchTest kiểm trong CI.

## Interface (định nghĩa ở Business, implement ở Data)
| Interface | Implement | Ghi chú |
|---|---|---|
| IAreaRepository, IPoiRepository, ITranslationRepository, IPaymentRepository, IAccessPassRepository, IPlayLogRepository, IAnalyticsRepository | `*Repository` (EF Core) | Truy cập DB |
| IFileStorage | SupabaseFileStorage | Upload, Delete, CreateSignedUrl (private bucket) |
| IPaymentGateway | MockPaymentGateway | Kết quả cấu hình bằng `Payment__MockResult` |
| IAiService | GeminiAiService | Gọi Google Gemini 2.5 Flash REST API (Free tier) sinh tóm tắt lịch sử POI |
| ITtsService | EdgeTtsService | Kết nối WebSocket Microsoft Edge Neural TTS sinh audio thuyết minh MP3 |
| IVisitorTracker | ActiveVisitorTracker | Bộ đếm du khách trực tuyến In-Memory Sliding Window (RAM thread-safe) |
| IQrCodeGenerator | QrCodeGenerator | Sinh mã QR PNG trực tiếp |
| IClock | SystemClock | Giúp test thời gian |

## Service (Business)
AreaService · PassService · GeoService · ContentService · AdminContentService · AnalyticsService · AiService

## Xác thực / phân quyền
- Middleware (Api): JWT Supabase hợp lệ + role=admin → `/admin/*`; token AccessPass → băm SHA-256 tra DB, còn hạn, chưa thu hồi → PassContext(pass_id, area_id). Mọi request hợp lệ từ khách tự động ghi nhận nhịp tim trực tuyến qua `IVisitorTracker`.
- Service (Business): kiểm tra tài nguyên (pass có thuộc khu của POI không → PASS_AREA_MISMATCH).

## Luồng chính
1. Khách: `GET /areas/{code}` → 2. `POST /areas/{code}/passes` (Idempotency-Key) → token một lần
3. Khách di chuyển: `GET /pois/nearby?lat&lng&lang&radius` (Bearer) → Tự động mở Bottom Sheet & phát audio `GET /pois/{id}?lang` khi vào bán kính $\le 30$m
4. Khách đứng yên: `POST /passes/heartbeat` định kỳ 15s xác nhận trực tuyến
5. Khách nghe xong: `POST /play-logs` ghi nhận thời gian nghe
6. Admin: Quản lý nội dung, sinh tóm tắt Gemini AI (`POST /admin/pois/{id}/ai-summary`), sinh giọng đọc tự động (`POST /admin/translations/{id}/generate-audio`), theo dõi Dashboard Analytics tự làm mới 15s (`GET /admin/analytics/dashboard`).

## Triển khai
GitHub Actions (CI) → EF migration bundle lên Supabase (Session pooler) → Render Deploy Hook → build Docker → smoke test `/health/ready`. Test client ở Vercel.

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
| IAreaRepository, IPoiRepository, ITranslationRepository, IPaymentRepository, IAccessPassRepository, IPlayLogRepository | `*Repository` (EF Core) | Truy cập DB |
| IFileStorage | SupabaseFileStorage | Upload, Delete, CreateSignedUrl (private bucket) |
| IPaymentGateway | MockPaymentGateway | Kết quả cấu hình bằng `Payment__MockResult` |
| IClock | SystemClock | Giúp test thời gian |

## Service (Business)
AreaService · PassService · GeoService · ContentService · AdminContentService · AnalyticsService

## Xác thực / phân quyền
- Middleware (Api): JWT Supabase hợp lệ + role=admin → `/admin/*`; token AccessPass → băm SHA-256 tra DB, còn hạn, chưa thu hồi → PassContext(pass_id, area_id).
- Service (Business): kiểm tra tài nguyên (pass có thuộc khu của POI không → PASS_AREA_MISMATCH).

## Luồng chính
1. `GET /areas/{code}` → 2. `POST /areas/{code}/passes` (Idempotency-Key) → token một lần
3. `GET /pois/nearby?lat&lng&lang&radius` (Bearer) → 4. `GET /pois/{id}?lang` → audio_url (signed URL)
5. `POST /play-logs`

## Triển khai
GitHub Actions (CI) → EF migration bundle lên Supabase (Session pooler) → Render Deploy Hook → build Docker → smoke test `/health/ready`. Test client ở Vercel.

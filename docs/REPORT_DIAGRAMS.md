# SƠ ĐỒ BÁO CÁO DỰ ÁN MULTIGUIDE (Task T-4.10)

> Tài liệu tổng hợp các sơ đồ kỹ thuật (Component, Use Case, ERD, CI/CD Pipeline, Sequence) xuất trực tiếp phục vụ viết báo cáo và slide thuyết minh đồ án.

---

## 1. Sơ đồ Kiến trúc 3 lớp (Component Architecture Diagram)

Hệ thống được tổ chức theo mô hình Modular Monolith 3 lớp nghiêm ngặt, tuân thủ nguyên tắc Dependency Inversion:

```mermaid
graph TD
    subgraph Presentation_Layer["Tầng Presentation (MultiGuide.Api)"]
        Controllers["Controllers (Areas, Passes, Pois, PlayLogs, Admin)"]
        Middlewares["Middlewares (AccessPass, GlobalException, RateLimiting)"]
        AuthSetup["JWT Supabase Auth & CORS Policy"]
    end

    subgraph Business_Layer["Tầng Business (MultiGuide.Business)"]
        Services["Core Services (Area, Poi, Geo, Pass, Content, PlayLog, AdminContent)"]
        Interfaces["Interfaces (IXxxRepository, IPaymentGateway, IFileStorage, IClock, IQrCodeGenerator)"]
        DTOs["DTO Models & Business Rules"]
    end

    subgraph Data_Layer["Tầng Data / Infrastructure (MultiGuide.Data)"]
        DbContext["MultiGuideDbContext (7 DbSets, RLS Enabled)"]
        Repositories["EF Core Repositories (Area, Poi, Pass, Payment, PlayLog)"]
        ExternalAdapters["SupabaseFileStorage & MockPaymentGateway"]
    end

    subgraph Domain_Layer["Tầng Domain (MultiGuide.Domain)"]
        Entities["7 Entities (TourArea, Poi, PoiTranslation, Language, Payment, AccessPass, PlayLog)"]
        Exceptions["AppException & ErrorCodes"]
    end

    Controllers --> Services
    Middlewares --> Services
    Services --> Interfaces
    Repositories -.->|implements| Interfaces
    ExternalAdapters -.->|implements| Interfaces
    Repositories --> DbContext

    Presentation_Layer --> Domain_Layer
    Business_Layer --> Domain_Layer
    Data_Layer --> Domain_Layer

    style Business_Layer fill:#0f172a,stroke:#06b6d4,stroke-width:2px,color:#fff
    style Presentation_Layer fill:#1e293b,stroke:#3b82f6,stroke-width:2px,color:#fff
    style Data_Layer fill:#1e293b,stroke:#10b981,stroke-width:2px,color:#fff
    style Domain_Layer fill:#1e293b,stroke:#f59e0b,stroke-width:2px,color:#fff
```

---

## 2. Sơ đồ Use Case (Use Case Diagram)

Phân tách ranh giới rõ ràng giữa khách tham quan vãng lai, khách đã mua vé, và quản trị viên:

```mermaid
graph LR
    Guest(("Khách tham quan"))
    ActiveGuest(("Khách có vé Active"))
    Admin(("Quản trị viên (Admin)"))

    subgraph Public_UseCases["Chức năng Public"]
        UC1["Quét mã QR khu di tích"]
        UC2["Xem thông tin khu & ngôn ngữ hỗ trợ"]
        UC3["Mua thẻ tham quan (Mock Payment)"]
    end

    subgraph Pass_UseCases["Chức năng Khách có Vé (RequireAccessPass)"]
        UC4["Kiểm tra trạng thái vé hiện tại"]
        UC5["Gửi tọa độ GPS nhận POI lân cận"]
        UC6["Lấy bài thuyết minh & Signed URL audio"]
        UC7["Ghi nhận thời gian nghe (Play-Log)"]
    end

    subgraph Admin_UseCases["Chức năng Quản Trị (Supabase JWT Admin)"]
        UC8["Quản lý Khu tham quan (CRUD)"]
        UC9["Xuất mã QR khu du lịch (PNG)"]
        UC10["Quản lý Điểm tham quan POI (CRUD)"]
        UC11["Quản lý Bản dịch đa ngôn ngữ"]
        UC12["Tải lên file âm thanh thuyết minh MP3"]
    end

    Guest --> UC1
    Guest --> UC2
    Guest --> UC3

    ActiveGuest --> UC4
    ActiveGuest --> UC5
    ActiveGuest --> UC6
    ActiveGuest --> UC7

    Admin --> UC8
    Admin --> UC9
    Admin --> UC10
    Admin --> UC11
    Admin --> UC12

    style Guest fill:#3b82f6,color:#fff
    style ActiveGuest fill:#10b981,color:#fff
    style Admin fill:#f43f5e,color:#fff
```

---

## 3. Sơ đồ Thực thể CSDL (Entity Relationship Diagram - ERD)

7 thực thể dữ liệu chuẩn hóa trên Supabase PostgreSQL, kích hoạt RLS (Row Level Security) không policy:

```mermaid
erDiagram
    TourArea ||--o{ Poi : "chứa"
    TourArea ||--o{ AccessPass : "cấp cho"
    TourArea ||--o{ Payment : "thanh toán"
    Poi ||--o{ PoiTranslation : "có các bản dịch"
    Poi ||--o{ PlayLog : "được nghe"
    Language ||--o{ PoiTranslation : "thuộc ngôn ngữ"
    AccessPass ||--o{ PlayLog : "thực hiện"
    Payment ||--o| AccessPass : "kích hoạt"

    TourArea {
        uuid id PK
        string code UK
        string name
        string description
        float lat
        float lng
        decimal price_usd
        int pass_ttl_hours
        boolean is_active
        datetime created_at
        datetime updated_at
    }

    Poi {
        uuid id PK
        uuid area_id FK
        float lat
        float lng
        float trigger_radius_m
        int priority
        string image_url
        boolean is_active
        datetime created_at
        datetime updated_at
    }

    Language {
        string code PK
        string name
        boolean is_active
    }

    PoiTranslation {
        uuid id PK
        uuid poi_id FK
        string lang_code FK
        string title
        string description
        string audio_path
        datetime updated_at
    }

    Payment {
        uuid id PK
        uuid area_id FK
        decimal amount
        string currency
        string status
        string idempotency_key UK
        string provider_trans_id
        datetime created_at
    }

    AccessPass {
        uuid id PK
        uuid payment_id FK
        uuid area_id FK
        string token_hash UK
        string status
        datetime expires_at
        datetime created_at
    }

    PlayLog {
        uuid id PK
        uuid access_pass_id FK
        uuid poi_id FK
        int duration_seconds
        datetime created_at
    }
```

---

## 4. Sơ đồ Tuần Tự Luồng Khách Tham Quan (Sequence Diagram)

Mô tả luồng từ lúc quét mã QR ➔ mua vé ➔ giả lập GPS ➔ nghe audio thuyết minh:

```mermaid
sequenceDiagram
    autonumber
    actor Guest as Khách tham quan
    participant Client as Web Client (Leaflet)
    participant Api as MultiGuide.Api
    participant Business as MultiGuide.Business
    participant DB as Supabase PostgreSQL
    participant Storage as Supabase Storage

    Guest->>Client: Quét QR hoặc nhập mã khu 'van-mieu'
    Client->>Api: GET /areas/van-mieu
    Api->>Business: GetAreaByCodeAsync('van-mieu')
    Business->>DB: Query TourArea & SupportedLanguages
    DB-->>Business: TourArea Data
    Business-->>Api: AreaDetailResponse
    Api-->>Client: 200 OK (Thông tin khu & ngôn ngữ)

    Guest->>Client: Bấm "Mua vé tham quan" (Mock 50k)
    Client->>Api: POST /areas/van-mieu/passes (Idempotency-Key)
    Api->>Business: CreatePassAsync (amount, ttl)
    Business->>Business: Sinh Token CSPRNG 32 bytes & băm SHA-256
    Business->>DB: Lưu Payment & AccessPass (token_hash)
    DB-->>Business: OK
    Business-->>Api: CreatePassResponse (token plaintext)
    Api-->>Client: 201 Created (Token 64 ký tự, ExpiresAt)

    Guest->>Client: Di chuyển vị trí trên bản đồ GPS
    Client->>Api: GET /pois/nearby?lat=...&lng=...&radius=50 (Bearer Token)
    Api->>Business: ValidateTokenAsync(token) & FindNearbyPois
    Business->>DB: Lấy danh sách POI active của khu
    Business->>Business: Lọc Bounding Box + Haversine khoảng cách
    Business-->>Api: List<NearbyPoiResponse> (within_trigger)
    Api-->>Client: 200 OK (Danh sách POI kèm cờ trong vùng phát)

    Note over Client: POI đến gần phát sáng & sóng radar
    Guest->>Client: Bấm "Nghe audio" trên POI kích hoạt
    Client->>Api: GET /pois/{id}?lang=vi (Bearer Token)
    Api->>Business: GetPoiDetailAsync(poiId, 'vi')
    Business->>Storage: CreateSignedUrlAsync(audio_path, 3600s)
    Storage-->>Business: Signed URL âm thanh (1 giờ)
    Business-->>Api: PoiDetailResponse (title, description, audioUrl)
    Api-->>Client: 200 OK (Audio stream URL)
    Client->>Guest: Phát bài thuyết minh audio MP3

    Note over Client: Khách nghe xong / bấm dừng
    Client->>Api: POST /play-logs (poiId, durationSeconds)
    Api->>DB: Lưu bản ghi PlayLog
```

---

## 5. Sơ đồ Pipeline CI/CD Tự Động Hóa (CI/CD Pipeline Diagram)

Quy trình phát triển và triển khai tự động hóa bảo đảm chất lượng phần mềm:

```mermaid
graph TD
    Developer["Lập trình viên"] -->|Git Push| GitHub["GitHub Repository (nhánh main)"]

    subgraph CI_Pipeline["Continuous Integration (.github/workflows/ci.yml)"]
        CheckoutCI["1. Checkout & Setup .NET 10"]
        RestoreBuild["2. Restore & Build (-warnaserror)"]
        FormatCheck["3. dotnet format --verify-no-changes"]
        RunTests["4. Chạy 297 Tests (Unit + Integration + NetArchTest)"]
        CoverageCheck["5. check_coverage.py (FAIL nếu Business < 60%)"]
        VulnScan["6. Quét lỗ hổng gói NuGet (High/Critical)"]
        DockerBuild["7. Build Docker Image (multiguide:ci)"]

        CheckoutCI --> RestoreBuild --> FormatCheck --> RunTests --> CoverageCheck --> VulnScan --> DockerBuild
    end

    subgraph CD_Pipeline["Continuous Deployment (.github/workflows/cd.yml)"]
        CheckoutCD["1. Setup .NET SDK & dotnet-ef tool"]
        MigrationBundle["2. Build & Apply Migration Bundle (Supabase Session Pooler)"]
        RenderHook["3. Trigger Render Deploy Hook (Pull Docker Image)"]
        SmokeTest["4. Smoke Test & Polling /health/ready (Tối đa 3 phút)"]
        VerifyStatus{"Kiểm tra Status?"}
        DeploySuccess["Triển khai THÀNH CÔNG (Live)"]
        DeployFailed["Thất bại -> Cảnh báo ROLLBACK"]

        CheckoutCD --> MigrationBundle --> RenderHook --> SmokeTest --> VerifyStatus
        VerifyStatus -->|200 Ready| DeploySuccess
        VerifyStatus -->|Timeout / Lỗi| DeployFailed
    end

    GitHub --> CI_Pipeline
    CI_Pipeline -->|Xanh lá 100%| CD_Pipeline
    DeploySuccess --> RenderService["Web Service trên Render (Singapore)"]
    DeploySuccess --> SupabaseDB["Supabase PostgreSQL & Storage"]

    style CI_Pipeline fill:#0f172a,stroke:#3b82f6,stroke-width:2px,color:#fff
    style CD_Pipeline fill:#0f172a,stroke:#10b981,stroke-width:2px,color:#fff
    style DeploySuccess fill:#065f46,stroke:#10b981,color:#fff
    style DeployFailed fill:#881337,stroke:#f43f5e,color:#fff
```

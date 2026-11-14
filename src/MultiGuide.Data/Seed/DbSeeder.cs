using Microsoft.EntityFrameworkCore;

using Microsoft.Extensions.Configuration;

using MultiGuide.Data.Context;

using MultiGuide.Domain.Entities;

namespace MultiGuide.Data.Seed;

public static class DbSeeder

{

    public static async Task SeedDemoDataAsync(MultiGuideDbContext context, IConfiguration configuration)

    {

        var seedValue = configuration["Seed:Enabled"] ?? configuration["Seed__Enabled"];

        var isSeedEnabled = bool.TryParse(seedValue, out var enabled) && enabled;

        if (!isSeedEnabled)

        {

            return;

        }

        await SeedDemoDataAsync(context, force: false);

    }

    public static async Task SeedDemoDataAsync(MultiGuideDbContext context, bool force = false, bool seedThangLong = false)

    {

        // 1. Seed Languages (vi, en, ja)

        var languages = new List<Language>

        {

            new() { Code = "vi", Name = "Tiếng Việt", IsActive = true },

            new() { Code = "en", Name = "English", IsActive = true },
            new() { Code = "zh", Name = "中文", IsActive = true },
            new() { Code = "ja", Name = "日本語", IsActive = true }

        };

        foreach (var lang in languages)

        {

            if (!await context.Languages.AnyAsync(l => l.Code == lang.Code))

            {

                context.Languages.Add(lang);

            }

        }

        await context.SaveChangesAsync();

        // 2. Check if TourArea "van-mieu" already exists (Idempotent)

        const string areaCode = "van-mieu";

        var existingArea = await context.TourAreas.FirstOrDefaultAsync(a => a.Code == areaCode);

        // 3. Seed TourArea: Văn Miếu - Quốc Tử Giám

        var area = existingArea;

        if (area == null)

        {

            area = new TourArea

            {

                Id = Guid.Parse("a0000000-0000-0000-0000-000000000001"),

                Code = areaCode,

                Name = "Văn Miếu - Quốc Tử Giám",

                CenterLat = 21.0285,

                CenterLng = 105.8358,

                RadiusM = 300,

                PriceUsd = 5.00m,

                PassTtlHours = 24,

                IsActive = true

            };

            context.TourAreas.Add(area);

            await context.SaveChangesAsync();

        }

        else

        {

            area.CenterLat = 21.0285;

            area.CenterLng = 105.8358;

            await context.SaveChangesAsync();

        }

        // 4. Seed 6 POIs and 24 PoiTranslations (VI, EN, ZH, JA)
        var poiDataList = GetDemoPoisWithTranslations(area.Id);

        var chineseStories = new Dictionary<Guid, (string Title, string Description)>
        {
            [Guid.Parse("c1875fb2-84ad-4a5a-936a-173fdbf4f7f3")] = ("文庙门 (大门)", "文庙大门始建于1070年李圣宗时期，是一座庄严的三开门式建筑。门前立有一对“下马碑”，明文规定凡文武百官乃至皇亲国戚行经于此，皆须下马步行，以示对儒学至圣先师与学问道德的崇高敬意。\n\n大门两侧立有华表石柱，顶端雕刻神兽，象征着国家选拔人才的公正、廉明与纯正。"),
            [Guid.Parse("3ede2134-c305-4270-9413-39c9743f9cb8")] = ("奎文阁", "奎文阁建于1805年阮朝时期，是首都河内的文化标志性建筑。木质双层楼阁四面辟有象征奎宿之光的圆形窗扉，倒映在正方形的天光井水面上，完美展现了古代“天圆地方”的宇宙哲学与天人合一境界。\n\n古时科举及第的举人们在晋见皇帝之前，常在此聚会品评绝妙诗文。"),
            [Guid.Parse("70dabf88-5450-48e7-9603-b753f5350047")] = ("天光井与进士题名碑", "天光井意为“映照上天之光的明镜”。水池两侧整齐排列着82座由灵龟驮负的进士题名碑，被列入联合国教科文组织世界记忆名录。碑上镌刻着1442至1779年间1304位进士姓名，昭示着千古不朽的真理。\n\n石龟驮碑寓意着学术精神与国家文脉的长青永驻，以及后世对先贤的无限追思。"),
            [Guid.Parse("cc63b968-096f-4b06-b4f8-19f8f829ef42")] = ("大成门与大拜庭", "大成门是通往核心祭祀区域的庄严之门。门后铺展着由古老钵场砖铺砌的宽阔大拜庭，历代帝王与儒臣曾在此举行隆重的祭孔释奠大礼。"),
            [Guid.Parse("0c23b2e0-73d1-4f37-90a0-f95201abf43b")] = ("大成殿", "大成殿是整个文庙建筑群的核心与最神圣所在。九开间宏伟的铁木大殿雕梁画栋、金碧辉煌。殿内正中庄严肃穆供奉至圣先师孔子神位，两侧为复圣颜回、宗圣曾子、述圣子思、亚圣孟子“四配”。\n\n殿堂正中高悬清康熙皇帝御题的“万世师表”金字匾额，辉映千载。"),
            [Guid.Parse("cd3c98eb-79fe-4980-a870-6f690f9e40cd")] = ("太学 (国子监)", "国子监由李仁宗于1076年创建，被公认为越南历史上的第一所最高国立学府。这里特别纪念敬仰被誉为“帝师”的一代名儒朱文安，以及李圣宗、李仁宗、黎圣宗三位开创并繁荣国家文治的贤明君主。\n\n二楼陈列着进士朝服、青石砚台以及昔日殿试科举的微缩场景模型。")
        };
        foreach (var (poi, translations) in poiDataList)
        {
            if (chineseStories.TryGetValue(poi.Id, out var chinese))
            {
                translations.Add(new PoiTranslation
                {
                    Id = Guid.NewGuid(),
                    PoiId = poi.Id,
                    LanguageCode = "zh",
                    Title = chinese.Title,
                    Description = chinese.Description,
                    AudioPath = $"assets/audio/pois/{poi.Id}_zh.mp3",
                    UpdatedAt = DateTime.UtcNow
                });
            }
        }

        var validPoiIds = poiDataList.Select(x => x.Poi.Id).ToList();

        // Xóa bất kỳ POI thừa nào không thuộc danh sách 6 điểm chuẩn (ví dụ Cổng Đại Trung trước đây)

        var extraPois = await context.Pois

            .Include(p => p.Translations)

            .Where(p => p.AreaId == area.Id && !validPoiIds.Contains(p.Id))

            .ToListAsync();

        if (extraPois.Count > 0)

        {

            foreach (var extra in extraPois)

            {

                // context.PoiTranslations.RemoveRange(extra.Translations);

                // Không xóa POI người dùng thêm

                // context.Pois.Remove(extra);

            }

            await context.SaveChangesAsync();

        }

        foreach (var (poi, translations) in poiDataList)

        {

            var existingPoi = await context.Pois

                .Include(p => p.Translations)

                .FirstOrDefaultAsync(p => p.Id == poi.Id);

            if (existingPoi == null)

            {

                context.Pois.Add(poi);

                foreach (var trans in translations)

                {

                    trans.PoiId = poi.Id;

                    context.PoiTranslations.Add(trans);

                }

            }

            else

            {

                existingPoi.Lat = poi.Lat;
                existingPoi.Lng = poi.Lng;

                existingPoi.Priority = poi.Priority;

                existingPoi.TriggerRadiusM = poi.TriggerRadiusM;

                existingPoi.IsActive = poi.IsActive;

                foreach (var trans in translations)

                {

                    var existingTrans = existingPoi.Translations.FirstOrDefault(t => t.LanguageCode == trans.LanguageCode);

                    if (existingTrans == null)

                    {

                        trans.PoiId = existingPoi.Id;

                        context.PoiTranslations.Add(trans);

                    }

                    else

                    {

                        existingTrans.Title = trans.Title;

                        if (!string.IsNullOrWhiteSpace(trans.Description))

                        {

                            existingTrans.Description = trans.Description;

                        }

                        if (!string.IsNullOrWhiteSpace(trans.AudioPath))

                        {

                            existingTrans.AudioPath = trans.AudioPath;

                        }

                    }

                }

            }

        }

        // 5. Seed 5 vé tham quan và doanh thu $25.00 chuẩn theo bản đã deploy

        if (!await context.AccessPasses.AnyAsync())

        {

            for (int i = 1; i <= 5; i++)

            {

                var payment = new Payment

                {

                    Id = Guid.NewGuid(),

                    AreaId = area.Id,

                    Amount = 5.00m,

                    Currency = "USD",

                    Provider = "mock",

                    ProviderRef = $"mock_seed_{i}",

                    IdempotencyKey = $"seed-payment-key-{i}",

                    Status = MultiGuide.Domain.Enums.PaymentStatus.Succeeded,

                    CreatedAt = DateTime.UtcNow.AddHours(-i)

                };

                context.Payments.Add(payment);

                var pass = new AccessPass

                {

                    Id = Guid.NewGuid(),

                    AreaId = area.Id,

                    PaymentId = payment.Id,

                    TokenHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes($"seed_token_{i}"))).ToLowerInvariant(),

                    IssuedAt = DateTime.UtcNow.AddHours(-i),

                    ExpiresAt = DateTime.UtcNow.AddHours(24 - i),

                    Status = MultiGuide.Domain.Enums.AccessPassStatus.Active

                };

                context.AccessPasses.Add(pass);

            }

        }

        await context.SaveChangesAsync();

        if (seedThangLong)
        {
            // 5. Seed TourArea: Hoàng Thành Thăng Long (Idempotent)
            const string thangLongCode = "hoang-thanh-thang-long";
            var existingThangLong = await context.TourAreas.FirstOrDefaultAsync(a => a.Code == thangLongCode);
            if (existingThangLong == null)
            {
                var thangLongArea = new TourArea
                {
                    Id = Guid.Parse("a0000000-0000-0000-0000-000000000002"),
                    Code = thangLongCode,
                    Name = "Hoàng Thành Thăng Long",
                    CenterLat = 21.03536,
                    CenterLng = 105.840256,
                    RadiusM = 400,
                    PriceUsd = 3.00m,
                    PassTtlHours = 24,
                    IsActive = true
                };
                context.TourAreas.Add(thangLongArea);
                await context.SaveChangesAsync();

                var tlPois = GetThangLongDemoPois(thangLongArea.Id);
                foreach (var (poi, translations) in tlPois)
                {
                    context.Pois.Add(poi);
                    foreach (var trans in translations)
                    {
                        trans.PoiId = poi.Id;
                        context.PoiTranslations.Add(trans);
                    }
                }
                await context.SaveChangesAsync();
            }
            else
            {
                existingThangLong.IsActive = true;
                await context.SaveChangesAsync();
            }
        }

    }

    private static List<(Poi Poi, List<PoiTranslation> Translations)> GetThangLongDemoPois(Guid areaId)
    {
        return new List<(Poi, List<PoiTranslation>)>
        {
            (
                new Poi
                {
                    Id = Guid.Parse("b0000000-0000-0000-0000-000000000001"),
                    AreaId = areaId,
                    Lat = 21.0324,
                    Lng = 105.8398,
                    TriggerRadiusM = 25,
                    Priority = 10,
                    IsActive = true
                },
                new List<PoiTranslation>
                {
                    new() { LanguageCode = "vi", Title = "Cột Cờ Hà Nội (Kỳ Đài)", Description = "Cột cờ Hà Nội được xây dựng từ năm 1812 dưới thời vua Gia Long triều Nguyễn, cao hơn 33m với kiến trúc tam cấp kiên cố, là biểu tượng lịch sử bất khuất của thủ đô ngàn năm văn hiến." },
                    new() { LanguageCode = "en", Title = "Hanoi Flag Tower", Description = "Built in 1812 under King Gia Long of the Nguyen Dynasty, Hanoi Flag Tower stands over 33 meters high as an enduring symbol of Hanoi's rich cultural heritage and resilience." },
                    new() { LanguageCode = "ja", Title = "ハノイ旗塔", Description = "1812年にグエン朝ザーロン帝のもとで建設されたハノイ旗塔は、高さ33メートル以上を誇り、千年の都ハノイの不屈の歴史の象徴です。" }
                }
            ),
            (
                new Poi
                {
                    Id = Guid.Parse("b0000000-0000-0000-0000-000000000002"),
                    AreaId = areaId,
                    Lat = 21.0345,
                    Lng = 105.8403,
                    TriggerRadiusM = 25,
                    Priority = 20,
                    IsActive = true
                },
                new List<PoiTranslation>
                {
                    new() { LanguageCode = "vi", Title = "Đoan Môn (Cổng Chính Cấm Thành)", Description = "Đoan Môn là cổng vòm chính dẫn thẳng vào Cấm thành Thăng Long thời Lê, sở hữu kiến trúc cuốn vòm bằng đá và gạch vồ độc đáo, từng là nơi cử hành nghi lễ đón tiếp sứ thần triều đình." },
                    new() { LanguageCode = "en", Title = "Doan Mon (Main Gate)", Description = "Doan Mon was the main gate leading into the Forbidden City of Thang Long during the Le Dynasty, featuring majestic brick arches and royal stone steps." },
                    new() { LanguageCode = "ja", Title = "端門（正門）", Description = "端門はレ朝時代のタンロン紫禁城へと通じる正門であり、石とレンガによるアーチ構造が壮麗な王朝の威容を今に伝えています。" }
                }
            ),
            (
                new Poi
                {
                    Id = Guid.Parse("b0000000-0000-0000-0000-000000000003"),
                    AreaId = areaId,
                    Lat = 21.0360,
                    Lng = 105.8404,
                    TriggerRadiusM = 25,
                    Priority = 30,
                    IsActive = true
                },
                new List<PoiTranslation>
                {
                    new() { LanguageCode = "vi", Title = "Điện Kính Thiên (Thềm Rồng Đá)", Description = "Điện Kính Thiên là trung tâm quyền lực tối cao của Hoàng thành Thăng Long, nổi bật với đôi rồng đá tuyệt mỹ thời Lê Sơ được chạm khắc từ đá xanh nguyên khối uy nghi dọc bậc thềm ngự đạo." },
                    new() { LanguageCode = "en", Title = "Kinh Thien Palace (Dragon Steps)", Description = "Kinh Thien Palace was the royal center of power in Thang Long Citadel, famous for the magnificent monolithic stone dragons carved in the early Le Dynasty." },
                    new() { LanguageCode = "ja", Title = "敬天殿（龍の石階段）", Description = "敬天殿はタンロン皇城の権力の中心地であり、黎朝初期に彫刻された見事な一枚岩の龍の石階段が宮殿の威厳を象徴しています。" }
                }
            ),
            (
                new Poi
                {
                    Id = Guid.Parse("b0000000-0000-0000-0000-000000000004"),
                    AreaId = areaId,
                    Lat = 21.0372,
                    Lng = 105.8403,
                    TriggerRadiusM = 25,
                    Priority = 40,
                    IsActive = true
                },
                new List<PoiTranslation>
                {
                    new() { LanguageCode = "vi", Title = "Hậu Lâu (Lầu Tĩnh Bắc)", Description = "Hậu Lâu còn gọi là Tĩnh Bắc Lâu hay Lầu Công Chúa, xây dựng phía sau điện Kính Thiên, nơi sinh hoạt và ngắm cảnh thanh tịnh của các cung tần mỹ nữ hoàng triều xưa." },
                    new() { LanguageCode = "en", Title = "Hau Lau (Princess Pavilion)", Description = "Located behind Kinh Thien Palace, Hau Lau was designed as a tranquil pavilion and living quarters for royal princesses in imperial times." },
                    new() { LanguageCode = "ja", Title = "後楼（王女の楼閣）", Description = "敬天殿の北側に位置する後楼は、かつて王族の女性たちが静かに暮らし、庭園を眺めた風雅な楼閣です。" }
                }
            ),
            (
                new Poi
                {
                    Id = Guid.Parse("b0000000-0000-0000-0000-000000000005"),
                    AreaId = areaId,
                    Lat = 21.0390,
                    Lng = 105.8402,
                    TriggerRadiusM = 25,
                    Priority = 50,
                    IsActive = true
                },
                new List<PoiTranslation>
                {
                    new() { LanguageCode = "vi", Title = "Bắc Môn (Chính Bắc Môn)", Description = "Bắc Môn là cổng thành duy nhất còn nguyên vẹn của thành Hà Nội thời Nguyễn, trên tường thành vẫn còn lưu giữ dấu vết hai vết đạn đại bác quân Pháp bắn năm 1882." },
                    new() { LanguageCode = "en", Title = "Bac Mon (North Gate)", Description = "Bac Mon is the only surviving gate of Hanoi Citadel from the Nguyen Dynasty, still bearing scars from French artillery shells fired in 1882." },
                    new() { LanguageCode = "ja", Title = "北門（城門）", Description = "北門はグエン朝ハノイ城の中で唯一現存する城門であり、1882年にフランス軍が撃ち込んだ大砲の弾痕が今も生々しく残っています。" }
                }
            )
        };
    }

    private static List<(Poi Poi, List<PoiTranslation> Translations)> GetDemoPoisWithTranslations(Guid areaId)

    {

        return new List<(Poi, List<PoiTranslation>)>

        {

            // POI 1: Cổng Văn Miếu (Văn Miếu Môn)

            (

                new Poi

                {

                    Id = Guid.Parse("c1875fb2-84ad-4a5a-936a-173fdbf4f7f3"),

                    AreaId = areaId,

                    Lat = 21.0269,

                    Lng = 105.8355,

                    TriggerRadiusM = 30,

                    Priority = 10,

                    IsActive = true

                },

                new List<PoiTranslation>

                {

                    new()

                    {

                        Id = Guid.NewGuid(),

                        LanguageCode = "vi",

                        Title = "Cổng Văn Miếu (Văn Miếu Môn)",

                        Description = "Cổng tam quan chính dẫn vào di tích Văn Miếu - Quốc Tử Giám, công trình khởi dựng từ năm 1070 dưới triều vua Lý Thánh Tông. Cổng gồm hai tầng ba cửa, phía trước có hai bia Hạ Mã nhắc nhở các bậc vương tôn công hầu qua đây đều phải xuống ngựa để tỏ lòng tôn kính các bậc hiền triết Nho học.",

                        AudioPath = "assets/audio/pois/c1875fb2-84ad-4a5a-936a-173fdbf4f7f3.mp3"

                    },

                    new()

                    {

                        Id = Guid.NewGuid(),

                        LanguageCode = "en",

                        Title = "Van Mieu Gate (Văn Miếu Môn)",

                        Description = "The imposing main three-entrance gate of the Temple of Literature, built in 1070 under Emperor Ly Thanh Tong. In front of the gate stand two Stone Stele reminding visitors of all ranks to dismount their horses to pay respect to Confucian scholars and teachers.",

                        AudioPath = "assets/audio/pois/c1875fb2-84ad-4a5a-936a-173fdbf4f7f3_en.mp3"

                    },

                    new()

                    {

                        Id = Guid.NewGuid(),

                        LanguageCode = "ja",

                        Title = "文廟門 (大中門)",

                        Description = "1070年に李聖宗によって建立された文廟・国子監の正門です。門の前には下馬碑が建てられており、かつては皇族や貴族であっても馬から降りて礼を尽くす必要がありました。",

                        AudioPath = "assets/audio/pois/c1875fb2-84ad-4a5a-936a-173fdbf4f7f3_ja.mp3"

                    }

                }

            ),

            // POI 2: Khuê Văn Các (Gắn chuẩn với GUID 3ede2134...)

            (

                new Poi

                {

                    Id = Guid.Parse("3ede2134-c305-4270-9413-39c9743f9cb8"),

                    AreaId = areaId,

                    Lat = 21.02815,

                    Lng = 105.83568,

                    TriggerRadiusM = 25,

                    Priority = 20,

                    IsActive = true

                },

                new List<PoiTranslation>

                {

                    new()

                    {

                        Id = Guid.NewGuid(),

                        LanguageCode = "vi",

                        Title = "Khuê Văn Các",

                        Description = "Khuê Văn Các (Gác sao Khuê) được xây dựng năm 1805 dưới thời Nguyễn, là biểu tượng văn hóa chính thức của Thủ đô Hà Nội. Tầng trên bằng gỗ có 4 cửa sổ tròn tượng trưng cho sự tỏa sáng của sao Khuê - chủ về văn chương thi cử, soi bóng xuống mặt giếng Thiên Quang vuông vức tạo nên biểu tượng trời tròn đất vuông hài hòa.",

                        AudioPath = "assets/audio/pois/3ede2134-c305-4270-9413-39c9743f9cb8.mp3"

                    },

                    new()

                    {

                        Id = Guid.NewGuid(),

                        LanguageCode = "en",

                        Title = "Khue Van Pavilion (Constellation of Literature)",

                        Description = "Erected in 1805 during the Nguyen Dynasty, Khue Van Pavilion is the official cultural symbol of Hanoi Capital. Its upper wooden structure features four round windows symbolizing the shining Khue star of literature and examinations, reflecting on the square Thien Quang Well to represent cosmic harmony.",

                        AudioPath = "assets/audio/pois/3ede2134-c305-4270-9413-39c9743f9cb8_en.mp3"

                    },

                    new()

                    {

                        Id = Guid.NewGuid(),

                        LanguageCode = "ja",

                        Title = "奎文閣 (けいぶんかく)",

                        Description = "1805年に建てられた木造二層の建築で、ハノイ市の公式文化シンボルです。円形の窓は学問を司る奎星を象徴し、四角い天光井に影を落とすことで天地の調和を表現しています。"

                    }

                }

            ),

            // POI 3: Giếng Thiên Quang & Vườn Bia Tiến Sĩ (Gắn chuẩn với GUID 70dabf88...)

            (

                new Poi

                {

                    Id = Guid.Parse("70dabf88-5450-48e7-9603-b753f5350047"),

                    AreaId = areaId,

                    Lat = 21.02855,

                    Lng = 105.83535,

                    TriggerRadiusM = 35,

                    Priority = 30,

                    IsActive = true

                },

                new List<PoiTranslation>

                {

                    new()

                    {

                        Id = Guid.NewGuid(),

                        LanguageCode = "vi",

                        Title = "Giếng Thiên Quang & Vườn Bia Tiến Sĩ",

                        Description = "Giếng Thiên Quang mang ý nghĩa 'Ánh sáng bầu trời', hai bên là khu vườn đặt 82 tấm Bia Tiến Sĩ dựng trên lưng rùa đá. Đây là di sản tư liệu thế giới được UNESCO công nhận, ghi danh 1.304 vị đỗ đại khoa từ các khoa thi năm 1442 đến 1779, khắc sâu triết lý: 'Hiền tài là nguyên khí quốc gia'.",

                        AudioPath = "assets/audio/pois/70dabf88-5450-48e7-9603-b753f5350047.mp3"

                    },

                    new()

                    {

                        Id = Guid.NewGuid(),

                        LanguageCode = "en",

                        Title = "Thien Quang Well & Doctor Stone Stelae",

                        Description = "Thien Quang Well symbolizes heavenly clarity. Beside it rests 82 Doctor Stelae mounted on stone tortoises, an inscribed UNESCO Memory of the World heritage. They record 1,304 doctoral laureates between 1442 and 1779, enshrining the timeless truth: 'Virtuous talents are the vital vitality of the nation'.",

                        AudioPath = "assets/audio/pois/70dabf88-5450-48e7-9603-b753f5350047_en.mp3"

                    },

                    new()

                    {

                        Id = Guid.NewGuid(),

                        LanguageCode = "ja",

                        Title = "天光井と進士題名碑",

                        Description = "天の光を意味する天光井の両脇に、亀の背に乗せられた82基の進士碑が並びます。ユネスコ世界の記憶に登録されており、1442年から1779年までの科挙合格者1,304名の功績を今に伝えています。"

                    }

                }

            ),

            // POI 4: Cổng Đại Thành & Sân Đại Bái (Gắn chuẩn với GUID cc63b968...)

            (

                new Poi

                {

                    Id = Guid.Parse("cc63b968-096f-4b06-b4f8-19f8f829ef42"),

                    AreaId = areaId,

                    Lat = 21.02895,

                    Lng = 105.83572,

                    TriggerRadiusM = 30,

                    Priority = 40,

                    IsActive = true

                },

                new List<PoiTranslation>

                {

                    new()

                    {

                        Id = Guid.NewGuid(),

                        LanguageCode = "vi",

                        Title = "Cổng Đại Thành & Sân Đại Bái",

                        Description = "Cổng Đại Thành là cửa bước vào khu vực nội điện thờ tự chính. Phía sau cổng là sân Đại Bái rộng lát gạch Bát Tràng cổ kính, nơi diễn ra các nghi lễ tế Nho học trang trọng hàng năm dưới các triều đại phong kiến Lý, Trần, Lê.",

                        AudioPath = "assets/audio/pois/cc63b968-096f-4b06-b4f8-19f8f829ef42.mp3"

                    },

                    new()

                    {

                        Id = Guid.NewGuid(),

                        LanguageCode = "en",

                        Title = "Dai Thanh Gate & Great Ceremonial Courtyard",

                        Description = "Dai Thanh Gate opens into the sacred inner precinct of worship. Behind it spreads the vast Dai Bai courtyard paved with ancient Bat Trang bricks, where annual solemn ceremonial rites honoring Confucius and scholars were historically held.",

                        AudioPath = "assets/audio/pois/cc63b968-096f-4b06-b4f8-19f8f829ef42_en.mp3"

                    },

                    new()

                    {

                        Id = Guid.NewGuid(),

                        LanguageCode = "ja",

                        Title = "大成門と大拝殿前庭",

                        Description = "大成門は神聖な儀礼エリアへの入り口です。門をくぐるとバッチャン焼きのレンガが敷き詰められた広大な中庭が広がり、毎年孔子を祀る厳粛な儀式が執り行われていました。",

                        AudioPath = "assets/audio/pois/cc63b968-096f-4b06-b4f8-19f8f829ef42_ja.mp3"

                    }

                }

            ),

            // POI 5: Điện Đại Thành (Gắn chuẩn với GUID 0c23b2e0...)

            (

                new Poi

                {

                    Id = Guid.Parse("0c23b2e0-73d1-4f37-90a0-f95201abf43b"),

                    AreaId = areaId,

                    Lat = 21.02935,

                    Lng = 105.83585,

                    TriggerRadiusM = 30,

                    Priority = 50,

                    IsActive = true

                },

                new List<PoiTranslation>

                {

                    new()

                    {

                        Id = Guid.NewGuid(),

                        LanguageCode = "vi",

                        Title = "Điện Đại Thành",

                        Description = "Tòa đại điện 9 gian với kiến trúc gỗ lim đồ sộ, là nơi trang trọng nhất đặt bàn thờ và bài vị của Đức Vạn thế sư biểu Khổng Tử cùng Tứ phối (Nhan Hồi, Tăng Tử, Tử Tư, Mạnh Tử) và Thập triết. Không gian trầm mặc khói hương gợi nhắc truyền thống tôn sư trọng đạo ngàn đời của dân tộc.",

                        AudioPath = "assets/audio/pois/0c23b2e0-73d1-4f37-90a0-f95201abf43b.mp3"

                    },

                    new()

                    {

                        Id = Guid.NewGuid(),

                        LanguageCode = "en",

                        Title = "Dai Thanh Sanctuary (Sanctuary of Great Success)",

                        Description = "A magnificent 9-compartment ironwood sanctuary housing the altars of Confucius (The Teacher of Ten Thousand Generations) alongside his four principal disciples and ten philosophers, preserving Vietnam's enduring tradition of venerating educators and wisdom.",

                        AudioPath = "assets/audio/pois/0c23b2e0-73d1-4f37-90a0-f95201abf43b_en.mp3"

                    },

                    new()

                    {

                        Id = Guid.NewGuid(),

                        LanguageCode = "ja",

                        Title = "大成殿 (本殿)",

                        Description = "孔子と四大弟子（顔回・曽子・子思・孟子）を祀る荘厳な本殿です。木造9間の重厚な建築様式を持ち、ベトナムにおける尊師重道の伝統精神を象徴しています。",

                        AudioPath = "assets/audio/pois/0c23b2e0-73d1-4f37-90a0-f95201abf43b_ja.mp3"

                    }

                }

            ),

            // POI 6: Nhà Thái Học (Gắn chuẩn với GUID cd3c98eb...)

            (

                new Poi

                {

                    Id = Guid.Parse("cd3c98eb-79fe-4980-a870-6f690f9e40cd"),

                    AreaId = areaId,

                    Lat = 21.02980,

                    Lng = 105.83580,

                    TriggerRadiusM = 35,

                    Priority = 60,

                    IsActive = true

                },

                new List<PoiTranslation>

                {

                    new()

                    {

                        Id = Guid.NewGuid(),

                        LanguageCode = "vi",

                        Title = "Nhà Thái Học (Quốc Tử Giám)",

                        Description = "Khuôn viên trường đại học quốc gia đầu tiên của Việt Nam, thành lập năm 1076 dưới thời vua Lý Nhân Tông để đào tạo nhân tài cho đất nước. Tầng một là nơi tôn vinh Danh sư Chu Văn An - người thầy đạo cao đức trọng, hiệu trưởng Quốc Tử Giám. Tầng hai trưng bày tượng ba vị vua có công lớn sáng lập và phát triển trường: Lý Thánh Tông, Lý Nhân Tông và Lê Thánh Tông.",

                        AudioPath = "assets/audio/pois/cd3c98eb-79fe-4980-a870-6f690f9e40cd.mp3"

                    },

                    new()

                    {

                        Id = Guid.NewGuid(),

                        LanguageCode = "en",

                        Title = "Thai Hoc Courtyard (Imperial Academy)",

                        Description = "The grounds of Vietnam's first imperial university, established in 1076 by Emperor Ly Nhan Tong to nurture national talents. The ground floor honors Chu Van An, the greatest master and rector of the academy, while the upper floor venerates the three monarchs who founded and elevated the academy.",

                        AudioPath = "assets/audio/pois/cd3c98eb-79fe-4980-a870-6f690f9e40cd_en.mp3"

                    },

                    new()

                    {

                        Id = Guid.NewGuid(),

                        LanguageCode = "ja",

                        Title = "太学堂 (ベトナム最古の大学)",

                        Description = "1076年に設立されたベトナム最初の最高学府・国子監の跡地です。1階には国子監の初代祭酒（学長）である名儒チュー・ヴァン・アンが祀られ、2階には大学創設に尽力した三人の歴代皇帝が祀られています。",

                        AudioPath = "assets/audio/pois/cd3c98eb-79fe-4980-a870-6f690f9e40cd_ja.mp3"

                    }

                }

            )

        };

    }

}


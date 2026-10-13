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

    public static async Task SeedDemoDataAsync(MultiGuideDbContext context, bool force = false)
    {
        // 1. Seed Languages (vi, en, ja)
        var languages = new List<Language>
        {
            new() { Code = "vi", Name = "Tiếng Việt", IsActive = true },
            new() { Code = "en", Name = "English", IsActive = true },
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

        if (existingArea != null && !force)
        {
            return; // Demo data already seeded
        }

        // 3. Seed TourArea: Văn Miếu - Quốc Tử Giám
        var area = existingArea ?? new TourArea
        {
            Id = Guid.NewGuid(),
            Code = areaCode,
            Name = "Văn Miếu - Quốc Tử Giám",
            CenterLat = 21.0276,
            CenterLng = 105.8355,
            RadiusM = 300,
            PriceUsd = 5.00m,
            PassTtlHours = 24,
            IsActive = true
        };

        if (existingArea == null)
        {
            context.TourAreas.Add(area);
            await context.SaveChangesAsync();
        }

        // 4. Seed 6 POIs and 18 PoiTranslations
        var poiDataList = GetDemoPoisWithTranslations(area.Id);

        foreach (var (poi, translations) in poiDataList)
        {
            var existingPoi = await context.Pois
                .Include(p => p.Translations)
                .FirstOrDefaultAsync(p => p.AreaId == area.Id && Math.Abs(p.Lat - poi.Lat) < 0.0001 && Math.Abs(p.Lng - poi.Lng) < 0.0001);

            if (existingPoi == null)
            {
                context.Pois.Add(poi);
                foreach (var trans in translations)
                {
                    trans.PoiId = poi.Id;
                    context.PoiTranslations.Add(trans);
                }
            }
        }

        await context.SaveChangesAsync();
    }

    private static List<(Poi Poi, List<PoiTranslation> Translations)> GetDemoPoisWithTranslations(Guid areaId)
    {
        return new List<(Poi, List<PoiTranslation>)>
        {
            // POI 1: Cổng Văn Miếu
            (
                new Poi
                {
                    Id = Guid.NewGuid(),
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
                        Description = "Cổng tam quan chính dẫn vào di tích Văn Miếu - Quốc Tử Giám, công trình khởi dựng từ năm 1070 dưới triều vua Lý Thánh Tông. Cổng gồm hai tầng ba cửa, phía trước có hai bia Hạ Mã nhắc nhở các bậc vương tôn công hầu qua đây đều phải xuống ngựa để tỏ lòng tôn kính các bậc hiền triết Nho học."
                    },
                    new()
                    {
                        Id = Guid.NewGuid(),
                        LanguageCode = "en",
                        Title = "Van Mieu Gate (Great Middle Gate)",
                        Description = "The imposing main three-entrance gate of the Temple of Literature, built in 1070 under Emperor Ly Thanh Tong. In front of the gate stand two Stone Stele reminding visitors of all ranks to dismount their horses to pay respect to Confucian scholars and teachers."
                    },
                    new()
                    {
                        Id = Guid.NewGuid(),
                        LanguageCode = "ja",
                        Title = "文廟門 (大中門)",
                        Description = "1070年に李聖宗によって建立された文廟・国子監の正門です。門の前には下馬碑が建てられており、かつては皇族や貴族であっても馬から降りて礼を尽くす必要がありました。"
                    }
                }
            ),

            // POI 2: Khuê Văn Các
            (
                new Poi
                {
                    Id = Guid.NewGuid(),
                    AreaId = areaId,
                    Lat = 21.0274,
                    Lng = 105.8355,
                    TriggerRadiusM = 25,
                    Priority = 30,
                    IsActive = true
                },
                new List<PoiTranslation>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        LanguageCode = "vi",
                        Title = "Khuê Văn Các",
                        Description = "Khuê Văn Các (Gác sao Khuê) được xây dựng năm 1805 dưới thời Nguyễn, là biểu tượng văn hóa chính thức của Thủ đô Hà Nội. Tầng trên bằng gỗ có 4 cửa sổ tròn tượng trưng cho sự tỏa sáng của sao Khuê - chủ về văn chương thi cử, soi bóng xuống mặt giếng Thiên Quang vuông vức tạo nên biểu tượng trời tròn đất vuông hài hòa."
                    },
                    new()
                    {
                        Id = Guid.NewGuid(),
                        LanguageCode = "en",
                        Title = "Khue Van Pavilion (Constellation of Literature)",
                        Description = "Erected in 1805 during the Nguyen Dynasty, Khue Van Pavilion is the official cultural symbol of Hanoi Capital. Its upper wooden structure features four round windows symbolizing the shining Khue star of literature and examinations, reflecting on the square Thien Quang Well to represent cosmic harmony."
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

            // POI 3: Giếng Thiên Quang & Vườn Bia Tiến Sĩ
            (
                new Poi
                {
                    Id = Guid.NewGuid(),
                    AreaId = areaId,
                    Lat = 21.0279,
                    Lng = 105.8353,
                    TriggerRadiusM = 35,
                    Priority = 40,
                    IsActive = true
                },
                new List<PoiTranslation>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        LanguageCode = "vi",
                        Title = "Giếng Thiên Quang & Vườn Bia Tiến Sĩ",
                        Description = "Giếng Thiên Quang mang ý nghĩa 'Ánh sáng bầu trời', hai bên là khu vườn đặt 82 tấm Bia Tiến Sĩ dựng trên lưng rùa đá. Đây là di sản tư liệu thế giới được UNESCO công nhận, ghi danh 1.304 vị đỗ đại khoa từ các khoa thi năm 1442 đến 1779, khắc sâu triết lý: 'Hiền tài là nguyên khí quốc gia'."
                    },
                    new()
                    {
                        Id = Guid.NewGuid(),
                        LanguageCode = "en",
                        Title = "Thien Quang Well & Doctor Stone Stelae",
                        Description = "Thien Quang Well symbolizes heavenly clarity. Beside it rests 82 Doctor Stelae mounted on stone tortoises, an inscribed UNESCO Memory of the World heritage. They record 1,304 doctoral laureates between 1442 and 1779, enshrining the timeless truth: 'Virtuous talents are the vital vitality of the nation'."
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

            // POI 4: Cổng Đại Thành & Sân Đại Bái
            (
                new Poi
                {
                    Id = Guid.NewGuid(),
                    AreaId = areaId,
                    Lat = 21.0284,
                    Lng = 105.8355,
                    TriggerRadiusM = 30,
                    Priority = 20,
                    IsActive = true
                },
                new List<PoiTranslation>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        LanguageCode = "vi",
                        Title = "Cổng Đại Thành & Sân Đại Bái",
                        Description = "Cổng Đại Thành là cửa bước vào khu vực nội điện thờ tự chính. Phía sau cổng là sân Đại Bái rộng lát gạch Bát Tràng cổ kính, nơi diễn ra các nghi lễ tế tế Nho học trang trọng hàng năm dưới các triều đại phong kiến Lý, Trần, Lê."
                    },
                    new()
                    {
                        Id = Guid.NewGuid(),
                        LanguageCode = "en",
                        Title = "Dai Thanh Gate & Great Ceremonial Courtyard",
                        Description = "Dai Thanh Gate opens into the sacred inner precinct of worship. Behind it spreads the vast Dai Bai courtyard paved with ancient Bat Trang bricks, where annual solemn ceremonial rites honoring Confucius and scholars were historically held."
                    },
                    new()
                    {
                        Id = Guid.NewGuid(),
                        LanguageCode = "ja",
                        Title = "大成門と大拝殿前庭",
                        Description = "大成門は神聖な儀礼エリアへの入り口です。門をくぐるとバッチャン焼きのレンガが敷き詰められた広大な中庭が広がり、毎年孔子を祀る厳粛な儀式が執り行われていました。"
                    }
                }
            ),

            // POI 5: Điện Đại Thành
            (
                new Poi
                {
                    Id = Guid.NewGuid(),
                    AreaId = areaId,
                    Lat = 21.0288,
                    Lng = 105.8355,
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
                        Description = "Tòa đại điện 9 gian với kiến trúc gỗ lim đồ sộ, là nơi trang trọng nhất đặt bàn thờ và bài vị của Đức Vạn thế sư biểu Khổng Tử cùng Tứ phối (Nhan Hồi, Tăng Tử, Tử Tư, Mạnh Tử) và Thập triết. Không gian trầm mặc khói hương gợi nhắc truyền thống tôn sư trọng đạo ngàn đời của dân tộc."
                    },
                    new()
                    {
                        Id = Guid.NewGuid(),
                        LanguageCode = "en",
                        Title = "Dai Thanh Sanctuary (Sanctuary of Great Success)",
                        Description = "A magnificent 9-compartment ironwood sanctuary housing the altars of Confucius (The Teacher of Ten Thousand Generations) alongside his four principal disciples and ten philosophers, preserving Vietnam's enduring tradition of venerating educators and wisdom."
                    },
                    new()
                    {
                        Id = Guid.NewGuid(),
                        LanguageCode = "ja",
                        Title = "大成殿 (本殿)",
                        Description = "孔子と四大弟子（顔回・曽子・子思・孟子）を祀る荘厳な本殿です。木造9間の重厚な建築様式を持ち、ベトナムにおける尊師重道の伝統精神を象徴しています。"
                    }
                }
            ),

            // POI 6: Nhà Thái Học
            (
                new Poi
                {
                    Id = Guid.NewGuid(),
                    AreaId = areaId,
                    Lat = 21.0294,
                    Lng = 105.8355,
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
                        Description = "Khuôn viên trường đại học quốc gia đầu tiên của Việt Nam, thành lập năm 1076 dưới thời vua Lý Nhân Tông để đào tạo nhân tài cho đất nước. Tầng một là nơi tôn vinh Danh sư Chu Văn An - người thầy đạo cao đức trọng, hiệu trưởng Quốc Tử Giám. Tầng hai trưng bày tượng ba vị vua có công lớn sáng lập và phát triển trường: Lý Thánh Tông, Lý Nhân Tông và Lê Thánh Tông."
                    },
                    new()
                    {
                        Id = Guid.NewGuid(),
                        LanguageCode = "en",
                        Title = "Thai Hoc Courtyard (Imperial Academy)",
                        Description = "The grounds of Vietnam's first imperial university, established in 1076 by Emperor Ly Nhan Tong to nurture national talents. The ground floor honors Chu Van An, the greatest master and rector of the academy, while the upper floor venerates the three monarchs who founded and elevated the academy."
                    },
                    new()
                    {
                        Id = Guid.NewGuid(),
                        LanguageCode = "ja",
                        Title = "太学堂 (ベトナム最古の大学)",
                        Description = "1076年に設立されたベトナム最初の最高学府・国子監の跡地です。1階には国子監の初代祭酒（学長）である名儒チュー・ヴァン・アンが祀られ、2階には大学創設に尽力した三人の歴代皇帝が祀られています。"
                    }
                }
            )
        };
    }
}

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MultiGuide.Data.Context;

namespace MultiGuide.Data.Migrations;

[DbContext(typeof(MultiGuideDbContext))]
[Migration("20261112000000_AddChineseLanguage")]
public class AddChineseLanguage : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
INSERT INTO language (code, name, is_active) VALUES ('zh', '中文', true) ON CONFLICT (code) DO NOTHING;
INSERT INTO poi_translation (poi_id, language_code, title, description, audio_path) SELECT 'c1875fb2-84ad-4a5a-936a-173fdbf4f7f3'::uuid, 'zh', '文庙门 (大门)', '文庙大门始建于1070年李圣宗时期，是一座庄严的三开门式建筑。门前立有一对“下马碑”，明文规定凡文武百官乃至皇亲国戚行经于此，皆须下马步行，以示对儒学至圣先师与学问道德的崇高敬意。

大门两侧立有华表石柱，顶端雕刻神兽，象征着国家选拔人才的公正、廉明与纯正。', 'assets/audio/pois/c1875fb2-84ad-4a5a-936a-173fdbf4f7f3_zh.mp3' WHERE EXISTS (SELECT 1 FROM poi WHERE id = 'c1875fb2-84ad-4a5a-936a-173fdbf4f7f3'::uuid) ON CONFLICT (poi_id, language_code) DO NOTHING;
INSERT INTO poi_translation (poi_id, language_code, title, description, audio_path) SELECT '3ede2134-c305-4270-9413-39c9743f9cb8'::uuid, 'zh', '奎文阁', '奎文阁建于1805年阮朝时期，是首都河内的文化标志性建筑。木质双层楼阁四面辟有象征奎宿之光的圆形窗扉，倒映在正方形的天光井水面上，完美展现了古代“天圆地方”的宇宙哲学与天人合一境界。

古时科举及第的举人们在晋见皇帝之前，常在此聚会品评绝妙诗文。', 'assets/audio/pois/3ede2134-c305-4270-9413-39c9743f9cb8_zh.mp3' WHERE EXISTS (SELECT 1 FROM poi WHERE id = '3ede2134-c305-4270-9413-39c9743f9cb8'::uuid) ON CONFLICT (poi_id, language_code) DO NOTHING;
INSERT INTO poi_translation (poi_id, language_code, title, description, audio_path) SELECT '70dabf88-5450-48e7-9603-b753f5350047'::uuid, 'zh', '天光井与进士题名碑', '天光井意为“映照上天之光的明镜”。水池两侧整齐排列着82座由灵龟驮负的进士题名碑，被列入联合国教科文组织世界记忆名录。碑上镌刻着1442至1779年间1304位进士姓名，昭示着千古不朽的真理。

石龟驮碑寓意着学术精神与国家文脉的长青永驻，以及后世对先贤的无限追思。', 'assets/audio/pois/70dabf88-5450-48e7-9603-b753f5350047_zh.mp3' WHERE EXISTS (SELECT 1 FROM poi WHERE id = '70dabf88-5450-48e7-9603-b753f5350047'::uuid) ON CONFLICT (poi_id, language_code) DO NOTHING;
INSERT INTO poi_translation (poi_id, language_code, title, description, audio_path) SELECT 'cc63b968-096f-4b06-b4f8-19f8f829ef42'::uuid, 'zh', '大成门与大拜庭', '大成门是通往核心祭祀区域的庄严之门。门后铺展着由古老钵场砖铺砌的宽阔大拜庭，历代帝王与儒臣曾在此举行隆重的祭孔释奠大礼。', 'assets/audio/pois/cc63b968-096f-4b06-b4f8-19f8f829ef42_zh.mp3' WHERE EXISTS (SELECT 1 FROM poi WHERE id = 'cc63b968-096f-4b06-b4f8-19f8f829ef42'::uuid) ON CONFLICT (poi_id, language_code) DO NOTHING;
INSERT INTO poi_translation (poi_id, language_code, title, description, audio_path) SELECT '0c23b2e0-73d1-4f37-90a0-f95201abf43b'::uuid, 'zh', '大成殿', '大成殿是整个文庙建筑群的核心与最神圣所在。九开间宏伟的铁木大殿雕梁画栋、金碧辉煌。殿内正中庄严肃穆供奉至圣先师孔子神位，两侧为复圣颜回、宗圣曾子、述圣子思、亚圣孟子“四配”。

殿堂正中高悬清康熙皇帝御题的“万世师表”金字匾额，辉映千载。', 'assets/audio/pois/0c23b2e0-73d1-4f37-90a0-f95201abf43b_zh.mp3' WHERE EXISTS (SELECT 1 FROM poi WHERE id = '0c23b2e0-73d1-4f37-90a0-f95201abf43b'::uuid) ON CONFLICT (poi_id, language_code) DO NOTHING;
INSERT INTO poi_translation (poi_id, language_code, title, description, audio_path) SELECT 'cd3c98eb-79fe-4980-a870-6f690f9e40cd'::uuid, 'zh', '太学 (国子监)', '国子监由李仁宗于1076年创建，被公认为越南历史上的第一所最高国立学府。这里特别纪念敬仰被誉为“帝师”的一代名儒朱文安，以及李圣宗、李仁宗、黎圣宗三位开创并繁荣国家文治的贤明君主。

二楼陈列着进士朝服、青石砚台以及昔日殿试科举的微缩场景模型。', 'assets/audio/pois/cd3c98eb-79fe-4980-a870-6f690f9e40cd_zh.mp3' WHERE EXISTS (SELECT 1 FROM poi WHERE id = 'cd3c98eb-79fe-4980-a870-6f690f9e40cd'::uuid) ON CONFLICT (poi_id, language_code) DO NOTHING;
""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Keep translations and listening history when reverting this additive migration.
    }
}

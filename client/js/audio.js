/**
 * MultiGuide - Audio Engine & Media Session API (Cụm 4 - Giai đoạn 5)
 * FE-5.2: Trình phát âm thanh di sản kết nối Signed URL Supabase từ Backend
 * FE-5.3: Media Session API (Lock screen controls & artwork)
 * FE-5.4: Play-Log tự động gửi về Backend khi kết thúc nghe
 */
import { store } from './state.js';
import { apiService } from './api.js';

// Dữ liệu nội dung thuyết minh di sản 4 thứ tiếng (Việt, Anh, Nhật, Trung) cho 6 POI Thần đạo chuẩn
export const POI_STORIES = {
  1: {
    order: 1,
    vi: {
      title: 'Cổng Văn Miếu',
      subtitle: 'Điểm 01/06 · Cửa ngõ thánh tích nghìn năm',
      badge: 'CỬA THẦN ĐẠO',
      heading: 'Cổng Văn Miếu — Khởi Đầu Trục Thần Đạo',
      paragraph: 'Được dựng vào năm 1070 dưới triều vua Lý Thánh Tông, Cổng Văn Miếu là cổng tam quan đồ sộ mở đầu cho quần thể di tích. Phía trước cổng là hai tấm Bia Hạ Mã uy nghiêm nhắc nhở bất kỳ ai, từ bậc vương giả đến thứ dân, đều phải xuống ngựa ngả mũ để kính cẩn trước đạo học và tiền nhân hiền đức.',
      quote: '"Hạ mã kính hiền — Bậc vương giả cũng phải nghiêng mình trước đạo học."',
      subtext: 'Hai cột trụ hoa biểu sừng sững tạc hình nghê chầu, biểu trưng cho sự ngay thẳng và công bằng của tri thức quốc gia.',
      duration: '3:45',
      durationSec: 225
    },
    en: {
      title: 'Van Mieu Gate',
      subtitle: '01 of 06 · The Sacred Threshold',
      badge: 'SACRED ENTRANCE',
      heading: 'Van Mieu Gate — Gateway to the Sacred Axis',
      paragraph: 'Built in 1070 under Emperor Ly Thanh Tong, the imposing three-arched Great Gate marks the start of the Sacred Axis. Flanking the entrance are the ancient Dismounting Stone Stelae (Hạ Mã), strictly requiring all visitors—even royal princes and high mandarins—to dismount from their horses to pay deep homage to Confucian masters and scholars.',
      quote: '"Before knowledge and virtue, kings and commoners alike bow their heads."',
      subtext: 'Twin ceremonial pillars crowned with mythical lion-dogs guard the courtyard, representing supreme integrity and justice.',
      duration: '3:45',
      durationSec: 225
    },
    ja: {
      title: '文廟門 (大中門)',
      subtitle: '01 of 06 · 千年の聖域への門出',
      badge: '神道入口',
      heading: '文廟門 — 神道軸の始まり',
      paragraph: '1070年に李聖宗によって建立された荘厳な三門で、文廟・国子監の神道軸の起点となります。門前には「下馬碑」が建てられ、皇族や高官であっても馬から降りて儒学の先師たちに敬意を表すことが義務付けられていました。',
      quote: '「学問と徳の前には、身分の尊卑なく誰もが敬意を払う。」',
      subtext: '一対の華表柱の上には神獣が座し、真理の探究と学問の神聖さを象徴しています。',
      duration: '3:45',
      durationSec: 225
    },
    zh: {
      title: '文庙门 (大门)',
      subtitle: '第 01/06 站 · 千年圣域之门',
      badge: '神道入口',
      heading: '文庙大门 — 神道轴线的庄严起点',
      paragraph: '文庙大门始建于1070年李圣宗时期，是一座庄严的三开门式建筑。门前立有一对“下马碑”，明文规定凡文武百官乃至皇亲国戚行经于此，皆须下马步行，以示对儒学至圣先师与学问道德的崇高敬意。',
      quote: '“下马敬贤 — 在圣贤学问与道德面前，人人皆怀敬畏之心。”',
      subtext: '大门两侧立有华表石柱，顶端雕刻神兽，象征着国家选拔人才的公正、廉明与纯正。',
      duration: '3:45',
      durationSec: 225
    }
  },
  2: {
    order: 2,
    vi: {
      title: 'Khuê Văn Các',
      subtitle: 'Điểm 02/06 · Gác ngắm sao Khuê tỏa sáng',
      badge: 'BIỂU TƯỢNG HÀ NỘI',
      heading: 'Khuê Văn Các — Tinh Hoa Trí Tuệ Thăng Long',
      paragraph: 'Được xây dựng vào năm 1805 dưới triều vua Gia Long, Khuê Văn Các (Gác sao Khuê) là biểu tượng văn hóa tiêu biểu của thủ đô Hà Nội. Công trình mang kiến trúc độc đáo với tầng trên là lầu gỗ vuông vức sơn son thếp vàng, bốn mặt trổ bốn cửa tròn lớn tượng trưng cho bầu trời và ánh sáng lấp lánh của vì sao chủ quản sự học, soi bóng xuống mặt giếng Thiên Quang vuông vức.',
      quote: '"Khuê tinh thiên lãng nhân văn hữu — Sao Khuê soi sáng cho nền văn trị thái bình."',
      subtext: 'Nơi đây từng là nơi hội tụ bình duyệt những áng văn chương tuyệt tác của các sĩ tử đỗ đạt thời phong kiến trước khi vào điện yết kiến hoàng đế.',
      duration: '4:36',
      durationSec: 276
    },
    en: {
      title: 'Khuê Văn Pavilion',
      subtitle: '02 of 06 · The poetry of the stars',
      badge: 'HANOI ICON',
      heading: 'Khuê Văn Pavilion — Symbol of Thang Long Wisdom',
      paragraph: 'Erected in 1805 during the Nguyen Dynasty, Khue Van Pavilion stands as the timeless cultural insignia of Hanoi Capital. Its upper wooden chamber features four radiant circular windows representing the shining star of literature, casting its reflection onto the square Thien Quang Well to mirror the ancient cosmic harmony of round heaven and square earth.',
      quote: '"When the literature star shines bright, peace and scholarly wisdom flourish."',
      subtext: 'Poets and imperial laureates once gathered here to critique masterwork verses before entering the imperial hall.',
      duration: '4:36',
      durationSec: 276
    },
    ja: {
      title: '奎文閣 (けいぶんかく)',
      subtitle: '02 of 06 · 文学と星の詩',
      badge: 'ハノイの象徴',
      heading: '奎文閣 — 千年の都を照らす学問の光',
      paragraph: '1805年に建てられた木造二層の美しい楼閣で、ハノイ市の公式文化シンボルです。上層部の四方には太陽の光線を模した円形の窓が穿たれており、学問を司る奎星（けいせい）の輝きを表現しています。四角い天光井の池にその姿を映し、天地の調和を象徴しています。',
      quote: '「奎星天に輝き、人文大いに興る — 学問の光が世を照らす。」',
      subtext: 'かつて科挙合格者たちが皇帝へ拝謁する前に、ここで詩文の批評会を開いたと伝えられています。',
      duration: '4:36',
      durationSec: 276
    },
    zh: {
      title: '奎文阁',
      subtitle: '第 02/06 站 · 奎星璀璨耀文光',
      badge: '河内文化标志',
      heading: '奎文阁 — 昇龙古都的智慧象征',
      paragraph: '奎文阁建于1805年阮朝时期，是首都河内的文化标志性建筑。木质双层楼阁四面辟有象征奎宿之光的圆形窗扉，倒映在正方形的天光井水面上，完美展现了古代“天圆地方”的宇宙哲学与天人合一境界。',
      quote: '“奎星天朗人文友 — 奎光朗照，斯文鼎盛，天下太平。”',
      subtext: '古时科举及第的举人们在晋见皇帝之前，常在此聚会品评绝妙诗文。',
      duration: '4:36',
      durationSec: 276
    }
  },
  3: {
    order: 3,
    vi: {
      title: 'Giếng Thiên Quang & Bia Tiến Sĩ',
      subtitle: 'Điểm 03/06 · Ánh sáng soi tỏ tâm hồn sĩ tử',
      badge: 'DI SẢN THẾ GIỚI UNESCO',
      heading: 'Giếng Thiên Quang — Tinh Khôi Đất Trời',
      paragraph: 'Giếng Thiên Quang mang ý nghĩa "Ánh sáng bầu trời". Hai bên hồ là khu vườn đặt 82 tấm Bia Tiến Sĩ dựng trên lưng rùa đá — Di sản Tư liệu Thế giới được UNESCO vinh danh. Nơi đây khắc ghi tên tuổi 1.304 vị đỗ đại khoa qua các kỳ thi từ năm 1442 đến 1779, truyền tải chân lý bất hủ.',
      quote: '"Hiền tài là nguyên khí quốc gia — Nguyên khí thịnh thì thế nước mạnh, nguyên khí suy thì thế nước yếu."',
      subtext: 'Rùa đá đội bia tượng trưng cho sự trường tồn vĩnh cửu của đạo học và lòng tri ân sâu sắc của hậu thế đối với các bậc danh nhân.',
      duration: '5:12',
      durationSec: 312
    },
    en: {
      title: 'Heavenly Clarity Well',
      subtitle: '03 of 06 · UNESCO Memory of the World',
      badge: 'WORLD MEMORY',
      heading: 'Heavenly Clarity Well & 82 Doctor Stelae',
      paragraph: 'The square Thien Quang Well represents the mirror of heaven. Flanking its serene waters are 82 Doctor Stelae mounted on stone tortoises, an inscribed UNESCO Memory of the World heritage. They immortalize 1,304 doctoral laureates from 1442 to 1779, enshrining an eternal principle.',
      quote: '"Virtuous talents are the vital vitality of a nation; when vitality thrives, the realm prospers."',
      subtext: 'Stone tortoises carrying stelae embody longevity and eternal reverence for intellectual pioneers.',
      duration: '5:12',
      durationSec: 312
    },
    ja: {
      title: '天光井と進士題名碑',
      subtitle: '03 of 06 · ユネスコ「世界の記憶」',
      badge: '世界遺産',
      heading: '天光井 — 天の光を映す鏡',
      paragraph: '天の光を意味する四角い天光井の両脇には、亀の背に乗せられた82基の進士碑が整然と並びます。ユネスコ世界の記憶に登録されており、1442年から1779年までの科挙合格者1,304名の名が刻まれ、「賢才は国家の元気なり」という不滅の哲学を今に伝えています。',
      quote: '「賢才は国家の元気なり — 元気盛んなれば国運隆盛なり。」',
      subtext: '不老長寿の象徴である石亀が重い石碑を支える姿は、学問の永続性と国家への貢献を表しています。',
      duration: '5:12',
      durationSec: 312
    },
    zh: {
      title: '天光井与进士题名碑',
      subtitle: '第 03/06 站 · 联合国教科文组织世界记忆',
      badge: '世界记忆名录',
      heading: '天光井与82座进士碑 — 铭记千秋栋梁',
      paragraph: '天光井意为“映照上天之光的明镜”。水池两侧整齐排列着82座由灵龟驮负的进士题名碑，被列入联合国教科文组织世界记忆名录。碑上镌刻着1442至1779年间1304位进士姓名，昭示着千古不朽的真理。',
      quote: '“贤才是国家之元气 — 元气盛则国势强，元气衰则国势弱。”',
      subtext: '石龟驮碑寓意着学术精神与国家文脉的长青永驻，以及后世对先贤的无限追思。',
      duration: '5:12',
      durationSec: 312
    }
  },
  4: {
    order: 4,
    vi: {
      title: 'Cổng Đại Thành & Sân Đại Bái',
      subtitle: 'Điểm 04/06 · Cửa ngõ nội điện trang nghiêm',
      badge: 'CỬA NỘI ĐIỆN',
      heading: 'Cổng Đại Thành & Sân Đại Bái — Không Gian Nghi Lễ Thiêng Liêng',
      paragraph: 'Cổng Đại Thành là cửa bước vào khu vực nội điện thờ tự chính. Phía sau cổng là sân Đại Bái rộng lát gạch Bát Tràng cổ kính, nơi diễn ra các nghi lễ tế Nho học trang trọng hàng năm dưới các triều đại phong kiến Lý, Trần, Lê.',
      quote: '"Đại thành chí thánh — Nơi quy tụ tinh hoa học vấn và đạo đức ngàn đời."',
      subtext: 'Sân Đại Bái rộng rãi mở ra không gian thanh tịnh trước khi bước vào chánh điện linh thiêng.',
      duration: '4:20',
      durationSec: 260
    },
    en: {
      title: 'Dai Thanh Gate & Courtyard',
      subtitle: '04 of 06 · The Inner Sacred Precinct',
      badge: 'SACRED PRECINCT',
      heading: 'Dai Thanh Gate & Great Ceremonial Courtyard',
      paragraph: 'Dai Thanh Gate opens into the sacred inner precinct of worship. Behind it spreads the vast Dai Bai courtyard paved with ancient Bat Trang bricks, where annual solemn ceremonial rites honoring Confucius and scholars were historically held.',
      quote: '"Great Success in Wisdom and Virtue — An eternal heritage of scholarship."',
      subtext: 'The ceremonial courtyard offers a serene threshold before entering the sacred sanctuary.',
      duration: '4:20',
      durationSec: 260
    },
    ja: {
      title: '大成門と大拝殿前庭',
      subtitle: '04 of 06 · 神聖な儀礼の庭',
      badge: '大成門・中庭',
      heading: '大成門と大拝殿前庭 — 荘厳なる儀式の場',
      paragraph: '大成門は神聖な儀礼エリアへの入り口です。門をくぐるとバッチャン焼きのレンガが敷き詰められた広大な中庭が広がり、毎年孔子を祀る厳粛な儀式が執り行われていました。',
      quote: '「大成至聖 — 学問と徳の極致を仰ぐ。」',
      duration: '4:20',
      durationSec: 260
    },
    zh: {
      title: '大成门与大拜庭',
      subtitle: '第 04/06 站 · 庄严礼仪庭院',
      badge: '大成门',
      heading: '大成门与大拜庭 — 肃穆的祭典圣地',
      paragraph: '大成门是通往核心祭祀区域的庄严之门。门后铺展着由古老钵场砖铺砌的宽阔大拜庭，历代帝王与儒臣曾在此举行隆重的祭孔释奠大礼。',
      quote: '“大成至圣 — 千秋文脉，盛德巍巍。”',
      duration: '4:20',
      durationSec: 260
    }
  },
  5: {
    order: 5,
    vi: {
      title: 'Điện Đại Thành',
      subtitle: 'Điểm 05/06 · Trung tâm thờ tự Nho giáo',
      badge: 'CHÁNH ĐIỆN LINH THIÊNG',
      heading: 'Điện Đại Thành — Nơi Tôn Vinh Bậc Vạn Thế Sư Biểu',
      paragraph: 'Điện Đại Thành là công trình trung tâm và linh thiêng nhất của Văn Miếu. Điện gồm 9 gian với kiến trúc gỗ lim bề thế, sơn son thếp vàng lộng lẫy. Gian chính tôn nghiêm thờ Khổng Tử cùng tứ phối: Nhan Hồi, Tăng Tử, Tử Tư và Mạnh Tử. Phía trước điện là Sân Đại Bái rộng lớn từng diễn ra các lễ tế long trọng của triều đình.',
      quote: '"Vạn thế sư biểu — Người thầy tiêu biểu muôn đời của đạo học á đông."',
      subtext: 'Bức hoành phi "Vạn thế sư biểu" do chính hoàng đế Khang Hy ngự bút ban tặng, treo trang trọng giữa tiền đường.',
      duration: '4:50',
      durationSec: 290
    },
    en: {
      title: 'Đại Thành Sanctuary',
      subtitle: '05 of 06 · Heart of Confucian Veneration',
      badge: 'SANCTUARY',
      heading: 'Đại Thành Sanctuary — Sacred Realm of Sages',
      paragraph: 'The Grand Đại Thành Sanctuary stands as the spiritual core of the monument. Comprising nine grand bays supported by solid ironwood columns and ornate gold leaf carvings, it enshrines Confucius and his four great disciples. The vast Great Ceremonial Courtyard before it hosted imperial ceremonies honoring education.',
      quote: '"Teacher for Ten Thousand Generations — A legacy of moral and intellectual elevation."',
      subtext: 'The grand gilded plaque above the altar was personally gifted by Emperor Kangxi in reverent admiration.',
      duration: '4:50',
      durationSec: 290
    },
    ja: {
      title: '大成殿 (だいせいでん)',
      subtitle: '05 of 06 · 儒教の最高神聖空間',
      badge: '本殿・拝殿',
      heading: '大成殿 — 万世の師表を祀る聖堂',
      paragraph: '文廟の最も神聖な中心殿堂です。豪壮な鉄木造りの九間建築で、朱漆と金箔で荘厳に飾られています。中央には孔子と四配（顔回・曾子・子思・孟子）の神位が祀られ、前庭の「大拝殿」では歴代王朝による盛大な祭礼が執り行われました。',
      quote: '「万世の師表 — 永遠に仰ぎ見られる徳と学問の模範。」',
      subtext: '殿内に掲げられた扁額「万世師表」は、儒学への最高級の敬意を表しています。',
      duration: '4:50',
      durationSec: 290
    },
    zh: {
      title: '大成殿',
      subtitle: '第 05/06 站 · 儒学最高祭祀殿堂',
      badge: '庄严正殿',
      heading: '大成殿 — 尊奉万世师表之圣所',
      paragraph: '大成殿是整个文庙建筑群的核心与最神圣所在。九开间宏伟的铁木大殿雕梁画栋、金碧辉煌。殿内正中庄严肃穆供奉至圣先师孔子神位，两侧为复圣颜回、宗圣曾子、述圣子思、亚圣孟子“四配”。',
      quote: '“万世师表 — 照亮东方文明的千古师德之光。”',
      subtext: '殿堂正中高悬清康熙皇帝御题的“万世师表”金字匾额，辉映千载。',
      duration: '4:50',
      durationSec: 290
    }
  },
  6: {
    order: 6,
    vi: {
      title: 'Nhà Thái Học',
      subtitle: 'Điểm 06/06 · Trường đại học đầu tiên Việt Nam',
      badge: 'TRƯỜNG ĐẠI HỌC CỔ',
      heading: 'Nhà Thái Học — Cái Nôi Đào Tạo Nhân Tài Quốc Gia',
      paragraph: 'Được vua Lý Nhân Tông thành lập năm 1076, Quốc Tử Giám được xem là trường đại học đầu tiên của Việt Nam. Nơi đây lưu giữ và tôn vinh người thầy mẫu mực Chu Văn An cùng các vị minh quân Lý Thánh Tông, Lý Nhân Tông, Lê Thánh Tông — những bậc vua hiền đã có công gây dựng nền học vấn nghìn năm của nước nhà.',
      quote: '"Tầm sư học đạo — Trọng thầy yêu bạn, rèn đức luyện tài vì giang sơn."',
      subtext: 'Tầng hai Nhà Thái Học trưng bày trang phục tiến sĩ, nghiên mực rùa và mô hình các kỳ thi đình triều phong kiến.',
      duration: '5:30',
      durationSec: 330
    },
    en: {
      title: 'Thái Học Academy',
      subtitle: '06 of 06 · Vietnam’s First Imperial University',
      badge: 'FIRST UNIVERSITY',
      heading: 'Thái Học Academy — Cradle of National Talents',
      paragraph: 'Established in 1076 under Emperor Ly Nhan Tong, the Imperial Academy is revered as the first university of Vietnam. It pays eternal homage to revered Master Chu Van An alongside three visionary emperors: Ly Thánh Tông, Lý Nhân Tông, and Lê Thánh Tông, who established and nurtured nine centuries of national scholarship.',
      quote: '"Cultivate moral character and intellectual brilliance for the realm."',
      subtext: 'The upper gallery showcases doctoral ceremonial robes, antique slate inkstones, and imperial exam models.',
      duration: '5:30',
      durationSec: 330
    },
    ja: {
      title: '太学 (国子監)',
      subtitle: '06 of 06 · ベトナム最古の最高学府',
      badge: '国子監・太学',
      heading: '太学 — 国家を担う英才たちの揺籃',
      paragraph: '1076年に李仁宗によって創設された、ベトナム最古の大学（最高学府）です。厳格な名教師として敬愛される朱文安（チュー・ヴァン・アン）をはじめ、文教政策を推し進めた李聖宗、李仁宗、黎聖宗の三帝が祀られています。',
      quote: '「師を敬い道を尊ぶ — 徳を磨き才を錬磨して国に尽くす。」',
      subtext: '二階には科挙の及第者が着用した官服や筆硯、往時の試験風景の模型が展示されています。',
      duration: '5:30',
      durationSec: 330
    },
    zh: {
      title: '太学 (国子监)',
      subtitle: '第 06/06 站 · 越南第一所国立大学',
      badge: '古代理学府',
      heading: '太学堂 — 国家英才的摇篮',
      paragraph: '国子监由李仁宗于1076年创建，被公认为越南历史上的第一所最高国立学府。这里特别纪念敬仰被誉为“帝师”的一代名儒朱文安，以及李圣宗、李仁宗、黎圣宗三位开创并繁荣国家文治的贤明君主。',
      quote: '“尊师重道 — 崇尚师尊，勤学笃行，修身齐家以报江山。”',
      subtext: '二楼陈列着进士朝服、青石砚台以及昔日殿试科举的微缩场景模型。',
      duration: '5:30',
      durationSec: 330
    }
  }
};

// Ánh xạ POI ID (1-6) sang UUID POI trên Backend/Supabase Database
export const POI_GUID_MAP = {
  1: 'c1875fb2-84ad-4a5a-936a-173fdbf4f7f3',
  2: '3ede2134-c305-4270-9413-39c9743f9cb8',
  3: '70dabf88-5450-48e7-9603-b753f5350047',
  4: 'cc63b968-096f-4b06-b4f8-19f8f829ef42',
  5: '0c23b2e0-73d1-4f37-90a0-f95201abf43b',
  6: 'cd3c98eb-79fe-4980-a870-6f690f9e40cd'
};

// Ánh xạ ngược lại theo GUID trực tiếp vào POI_STORIES
POI_STORIES['c1875fb2-84ad-4a5a-936a-173fdbf4f7f3'] = POI_STORIES[1];
POI_STORIES['3ede2134-c305-4270-9413-39c9743f9cb8'] = POI_STORIES[2];
POI_STORIES['70dabf88-5450-48e7-9603-b753f5350047'] = POI_STORIES[3];
POI_STORIES['cc63b968-096f-4b06-b4f8-19f8f829ef42'] = POI_STORIES[4];
POI_STORIES['0c23b2e0-73d1-4f37-90a0-f95201abf43b'] = POI_STORIES[5];
POI_STORIES['cd3c98eb-79fe-4980-a870-6f690f9e40cd'] = POI_STORIES[6];

/**
 * Tạo âm thanh chuông cảnh di sản êm dịu (WAV data URI) để phát audio mượt mà
 * khi offline, kiểm thử, hoặc khi Render backend đang Cold Start
 */
function generateCalmTempleBellWav(durationSeconds = 12) {
  const sampleRate = 22050;
  const numSamples = sampleRate * durationSeconds;
  const buffer = new ArrayBuffer(44 + numSamples * 2);
  const view = new DataView(buffer);

  // RIFF Chunk
  writeString(view, 0, 'RIFF');
  view.setUint32(4, 36 + numSamples * 2, true);
  writeString(view, 8, 'WAVE');

  // fmt Subchunk
  writeString(view, 12, 'fmt ');
  view.setUint32(16, 16, true); // Subchunk1Size
  view.setUint16(20, 1, true);  // AudioFormat (PCM)
  view.setUint16(22, 1, true);  // NumChannels (Mono)
  view.setUint32(24, sampleRate, true); // SampleRate
  view.setUint32(28, sampleRate * 2, true); // ByteRate
  view.setUint16(32, 2, true);  // BlockAlign
  view.setUint16(34, 16, true); // BitsPerSample

  // data Subchunk
  writeString(view, 36, 'data');
  view.setUint32(40, numSamples * 2, true);

  // Sinh hợp âm chuông di sản thiền định (432Hz cơ bản, hài âm 864Hz và 1296Hz tan chậm)
  const baseFreq = 432;
  for (let i = 0; i < numSamples; i++) {
    const t = i / sampleRate;
    // Phong bì suy giảm tự nhiên (Exponential Decay)
    const envelope = Math.exp(-t * 0.45);
    const sampleVal = (
      Math.sin(2 * Math.PI * baseFreq * t) * 0.6 +
      Math.sin(2 * Math.PI * (baseFreq * 2) * t) * 0.25 +
      Math.sin(2 * Math.PI * (baseFreq * 3.01) * t) * 0.15
    ) * envelope * 0.35;

    // Giới hạn trong khoảng 16-bit PCM [-32768, 32767]
    const clamped = Math.max(-1, Math.min(1, sampleVal));
    view.setInt16(44 + i * 2, clamped * 32767, true);
  }

  const blob = new Blob([buffer], { type: 'audio/wav' });
  return URL.createObjectURL(blob);
}

function writeString(view, offset, string) {
  for (let i = 0; i < string.length; i++) {
    view.setUint8(offset + i, string.charCodeAt(i));
  }
}

/**
 * Định dạng số giây thành chuỗi thời gian "phút:giây" (VD: 276 -> "4:36")
 */
function formatTime(seconds) {
  if (isNaN(seconds) || seconds < 0) return '0:00';
  const mins = Math.floor(seconds / 60);
  const secs = Math.floor(seconds % 60);
  return `${mins}:${secs < 10 ? '0' : ''}${secs}`;
}

export class AudioEngine {
  constructor() {
    this.audioElement = null;
    this.currentPoiId = '3ede2134-c305-4270-9413-39c9743f9cb8'; // Mặc định POI 2: Khuê Văn Các
    this.startTime = null;
    this.fallbackAudioUrl = null;
    this.isSeeking = false;
  }

  init() {
    // 1. Gắn thẻ Audio HTML5 từ DOM
    this.audioElement = document.getElementById('heritageAudioElement') || new Audio();
    this.audioElement.preload = 'metadata';

    // 2. Tạo sẵn âm thanh chuông di sản thiền định làm fallback mượt mà
    try {
      this.fallbackAudioUrl = generateCalmTempleBellWav(18);
    } catch (e) {
      console.warn('[MultiGuide Audio] Không thể khởi tạo chuông fallback:', e);
    }

    // 3. Đăng ký các sự kiện của Audio Element
    this.setupListeners();

    // 4. Khởi tạo Media Session API (FE-5.3)
    this.setupMediaSession();

    // 5. Cập nhật nội dung ban đầu từ POI mặc định (Khuê Văn Các)
    this.syncPoiContent(this.currentPoiId);

    console.log('[MultiGuide Audio] Audio Engine & Media Session API đã sẵn sàng.');
  }

  setupListeners() {
    if (!this.audioElement) return;

    // Khi âm thanh bắt đầu phát
    this.audioElement.addEventListener('play', () => {
      this.startTime = Date.now();
      store.set({ isPlaying: true });
      this.updatePlayPauseButtonUI(true);
      this.updateMediaSessionState('playing');
    });

    // Khi âm thanh tạm dừng
    this.audioElement.addEventListener('pause', () => {
      store.set({ isPlaying: false });
      this.updatePlayPauseButtonUI(false);
      this.updateMediaSessionState('paused');
    });

    // Khi siêu dữ liệu tải xong (lấy được thời lượng duration)
    this.audioElement.addEventListener('loadedmetadata', () => {
      this.updateDurationUI(this.audioElement.duration);
    });

    // Cập nhật thanh Scrubber tiến trình theo thời gian thực (FE-5.2)
    this.audioElement.addEventListener('timeupdate', () => {
      if (this.isSeeking) return;
      const current = this.audioElement.currentTime || 0;
      const total = this.audioElement.duration || 1;
      this.updateCurrentTimeUI(current, total);
    });

    // Khi bài nghe kết thúc (FE-5.4: Tự động ghi nhận Play-Log)
    this.audioElement.addEventListener('ended', () => {
      console.log(`[MultiGuide Audio] Hoàn thành bài nghe POI ${this.currentPoiId}`);
      this.finishPlayback();
    });

    // Bắt sự kiện người dùng nhấp trên thanh Scrubber để tua nhạc
    const scrubberTrack = document.getElementById('playerScrubberTrack');
    if (scrubberTrack) {
      scrubberTrack.addEventListener('click', (e) => {
        const rect = scrubberTrack.getBoundingClientRect();
        const clickX = e.clientX - rect.left;
        const fraction = Math.max(0, Math.min(1, clickX / rect.width));
        this.seek(fraction);
      });
    }

    // Bắt nút Play/Pause trên Mini Player
    const btnToggle = document.getElementById('btnPlayerToggle');
    if (btnToggle) {
      btnToggle.addEventListener('click', (e) => {
        e.stopPropagation(); // Không trigger mở rộng Bottom Sheet
        this.togglePlay();
      });
    }

    // Lắng nghe thay đổi ngôn ngữ từ store
    store.subscribe((state) => {
      if (state.selectedLang) {
        this.syncPoiContent(this.currentPoiId);
      }
    });
  }

  /**
   * Phát hoặc tạm dừng thuyết minh (Play/Pause Toggle)
   */
  async togglePlay() {
    if (!this.audioElement) return;

    if (this.audioElement.paused) {
      try {
        if (!this.audioElement.src || this.audioElement.src === window.location.href) {
          await this.loadAudioForPoi(this.currentPoiId);
        }
        await this.audioElement.play();
      } catch (err) {
        console.warn('[MultiGuide Audio] Không thể tự động phát, đang thử fallback:', err);
        if (this.fallbackAudioUrl) {
          this.audioElement.src = this.fallbackAudioUrl;
          await this.audioElement.play().catch(() => {});
        }
      }
    } else {
      this.audioElement.pause();
    }
  }

  /**
   * Bắt đầu phát âm thanh cho một POI cụ thể
   */
  /**
   * Bắt đầu phát âm thanh cho một POI cụ thể (Single Audio Stream + Fade-out mượt mà)
   */
  async playPoi(poiId) {
    // 1. Fade-out mượt mà âm thanh đang phát trước khi chuyển sang POI mới (Single Audio Instance)
    if (this.audioElement && !this.audioElement.paused && this.audioElement.currentTime > 0) {
      try {
        const origVol = this.audioElement.volume || 1.0;
        const steps = 6;
        const stepDelay = 50; // 6 * 50ms = 300ms
        for (let i = 1; i <= steps; i++) {
          await new Promise(r => setTimeout(r, stepDelay));
          if (this.audioElement) {
            this.audioElement.volume = Math.max(0, origVol * (1 - i / steps));
          }
        }
        this.audioElement.pause();
        this.audioElement.volume = origVol;
      } catch (fadeErr) {
        if (this.audioElement) {
          this.audioElement.pause();
        }
      }
    }

    this.currentPoiId = poiId;
    this.syncPoiContent(poiId);

    try {
      await this.loadAudioForPoi(poiId);
      if (this.audioElement) {
        this.audioElement.volume = 1.0;
        await this.audioElement.play();
      }
    } catch (err) {
      if (err && err.name === 'NotAllowedError') {
        console.warn(`[MultiGuide Audio] Trình duyệt yêu cầu cử chỉ tương tác trước khi tự động phát âm thanh. Bảng thông tin đã mở sẵn, du khách có thể chạm nút Phát bất kỳ lúc nào.`);
        return;
      }
      console.warn(`[MultiGuide Audio] Lỗi phát POI ${poiId}, sử dụng âm thanh di sản:`, err);
      if (this.fallbackAudioUrl && this.audioElement) {
        this.audioElement.src = this.fallbackAudioUrl;
        this.audioElement.volume = 1.0;
        await this.audioElement.play().catch((playErr) => {
          if (playErr && playErr.name === 'NotAllowedError') {
            console.warn(`[MultiGuide Audio] Trình duyệt yêu cầu cử chỉ tương tác trước khi phát âm thanh tự động.`);
          }
        });
      }
    }
  }

  async loadAudioForPoi(poiId) {
    const lang = store.get().selectedLang || 'vi';
    const targetGuid = POI_GUID_MAP[poiId] || poiId;

    try {
      console.log(`[MultiGuide Audio] Gọi API Backend lấy Signed URL cho POI ${poiId} (${targetGuid}, ${lang})...`);
      const detail = await apiService.getPoiDetail(targetGuid, lang);

      if (detail && detail.audioUrl) {
        console.log(`[MultiGuide Audio] Đã nhận Signed URL Supabase:`, detail.audioUrl);
        this.audioElement.src = detail.audioUrl;
        return;
      }
    } catch (e) {
      console.warn(`[MultiGuide Audio] Không thể tải audio thật (${e.message}). Áp dụng âm thanh chuông di sản 432Hz.`);
    }

    // Fallback mượt mà bằng âm thanh chuông hòa âm thiền định
    if (this.fallbackAudioUrl) {
      this.audioElement.src = this.fallbackAudioUrl;
    }
  }

  /**
   * Tua thời gian âm thanh theo tỉ lệ phần trăm [0, 1]
   */
  seek(fraction) {
    if (!this.audioElement || !this.audioElement.duration) return;
    const targetTime = fraction * this.audioElement.duration;
    this.audioElement.currentTime = targetTime;
    this.updateCurrentTimeUI(targetTime, this.audioElement.duration);
  }

  /**
   * Đồng bộ toàn bộ nội dung text và giao diện của POI sang Bottom Sheet
   */
  syncPoiContent(poiId) {
    const lang = store.get().selectedLang || 'vi';
    const targetPoi = window.HERITAGE_POIS?.find(p => String(p.id) === String(poiId) || String(p.order) === String(poiId));
    const poiData = POI_STORIES[poiId] || (targetPoi ? POI_STORIES[targetPoi.order] : null) || POI_STORIES[2];
    const story = poiData[lang] || poiData['en'] || poiData['vi'];

    // 1. Cập nhật Mini Player Header
    const titleElem = document.getElementById('sheetPoiTitle');
    const subtitleElem = document.getElementById('sheetPoiSubtitle');
    const langBadge = document.getElementById('playerLangBadge');

    if (titleElem) titleElem.textContent = story.title;
    if (subtitleElem) subtitleElem.textContent = story.subtitle;
    if (langBadge) langBadge.textContent = lang.toUpperCase();

    // 2. Cập nhật Khung Mở Rộng Đọc Toàn Văn (Nấc 2)
    const storyBadge = document.querySelector('.sheet-expanded-content .story-badge');
    const storyHeading = document.getElementById('storyHeading');
    const storyParagraph = document.getElementById('storyParagraph');
    const storyQuote = document.querySelector('.sheet-expanded-content .story-quote');
    const storySubtext = document.querySelector('.sheet-expanded-content .story-subtext');

    if (storyBadge) storyBadge.textContent = story.badge;
    if (storyHeading) storyHeading.textContent = story.heading;
    if (storyParagraph) storyParagraph.textContent = story.paragraph;
    if (storyQuote) storyQuote.textContent = story.quote;
    if (storySubtext) storySubtext.textContent = story.subtext;

    // 3. Cập nhật Media Session Metadata
    this.updateMediaSessionMetadata(story.title, story.subtitle);

    // 4. Cập nhật thời lượng hiển thị mặc định
    const totalElem = document.getElementById('timeTotal');
    if (totalElem && story.duration) {
      totalElem.textContent = story.duration;
    }
  }

  /**
   * Cập nhật nút Play/Pause trên giao diện
   */
  updatePlayPauseButtonUI(isPlaying) {
    const iconPlay = document.getElementById('iconPlay');
    const iconPause = document.getElementById('iconPause');

    if (iconPlay && iconPause) {
      iconPlay.style.display = isPlaying ? 'none' : 'block';
      iconPause.style.display = isPlaying ? 'block' : 'none';
    }
  }

  /**
   * Cập nhật thời gian hiện tại và thanh Scrubber Fill
   */
  updateCurrentTimeUI(currentTime, duration) {
    const currentElem = document.getElementById('timeCurrent');
    const fillElem = document.getElementById('playerScrubberFill');

    if (currentElem) {
      currentElem.textContent = formatTime(currentTime);
    }

    if (fillElem && duration > 0) {
      const percentage = Math.min(100, (currentTime / duration) * 100);
      fillElem.style.width = `${percentage.toFixed(1)}%`;
    }
  }

  /**
   * Cập nhật tổng thời lượng audio
   */
  updateDurationUI(duration) {
    const totalElem = document.getElementById('timeTotal');
    if (totalElem && !isNaN(duration) && duration > 0) {
      totalElem.textContent = formatTime(duration);
    }
  }

  /**
   * Cấu hình Media Session API (FE-5.3) trên màn hình khóa điện thoại
   */
  setupMediaSession() {
    if (!('mediaSession' in navigator)) return;

    try {
      navigator.mediaSession.setActionHandler('play', () => this.togglePlay());
      navigator.mediaSession.setActionHandler('pause', () => this.togglePlay());
      navigator.mediaSession.setActionHandler('seekto', (details) => {
        if (details.seekTime && this.audioElement) {
          this.audioElement.currentTime = details.seekTime;
        }
      });
      navigator.mediaSession.setActionHandler('seekbackward', (details) => {
        if (this.audioElement) {
          const skip = details.seekOffset || 10;
          this.audioElement.currentTime = Math.max(0, this.audioElement.currentTime - skip);
        }
      });
      navigator.mediaSession.setActionHandler('seekforward', (details) => {
        if (this.audioElement) {
          const skip = details.seekOffset || 10;
          this.audioElement.currentTime = Math.min(this.audioElement.duration || 0, this.audioElement.currentTime + skip);
        }
      });
    } catch (e) {
      console.warn('[MultiGuide Audio] Lỗi thiết lập MediaSession handlers:', e);
    }
  }

  /**
   * Cập nhật Metadata cho Media Session API
   */
  updateMediaSessionMetadata(title, subtitle) {
    if (!('mediaSession' in navigator)) return;

    try {
      navigator.mediaSession.metadata = new MediaMetadata({
        title: title || 'Khuê Văn Các',
        artist: 'MultiGuide — Thuyết Minh Di Tích Văn Miếu',
        album: 'Hanoi Heritage Tour Edition',
        artwork: [
          { src: 'assets/hero-khue-van-cac.jpg', sizes: '512x512', type: 'image/jpeg' },
          { src: 'assets/icons/heritage-temple.svg', sizes: '96x96', type: 'image/svg+xml' }
        ]
      });
    } catch (e) {
      // Bỏ qua lỗi trình duyệt cũ
    }
  }

  /**
   * Cập nhật playbackState của MediaSession
   */
  updateMediaSessionState(state) {
    if (!('mediaSession' in navigator)) return;
    try {
      navigator.mediaSession.playbackState = state;
    } catch (e) {}
  }

  /**
   * Kết thúc bài nghe & tự động gửi Play-Log về Backend thật (FE-5.4)
   */
  finishPlayback() {
    const durationSeconds = this.startTime 
      ? Math.max(1, Math.round((Date.now() - this.startTime) / 1000))
      : 1;

    console.log(`[MultiGuide Audio] Gửi Play-Log cho POI ${this.currentPoiId} (Thời lượng: ${durationSeconds}s)...`);
    apiService.sendPlayLog(this.currentPoiId, durationSeconds);

    this.startTime = null;
    store.set({ isPlaying: false });
    this.updatePlayPauseButtonUI(false);
    this.updateMediaSessionState('none');
  }
}

export const audioEngine = new AudioEngine();

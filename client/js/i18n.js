/**
 * MultiGuide - Comprehensive Internationalization (i18n) Engine
 * Quản lý từ điển đa ngữ tập trung (Việt, Anh, Trung, Nhật)
 * Hỗ trợ mở rộng vô hạn ngôn ngữ, tự động cập nhật DOM qua data-i18n
 */
import { store } from './state.js';

export const SUPPORTED_LANGS = ['vi', 'en', 'zh', 'ja'];

export const TRANSLATIONS = {
  vi: {
    langName: 'Tiếng Việt',
    arrival: {
      brandTitle: 'MultiGuide',
      editionBadge: 'HANOI EDITION',
      tag: 'DI TÍCH QUỐC GIA ĐẶC BIỆT · HÀ NỘI',
      title: 'Mỗi bước chân,<br>một câu chuyện.',
      subtitle: 'Thong thả dạo bước. Để những câu chuyện lịch sử ngàn năm tự tìm đến bạn trên từng bước chân.',
      passCategory: 'THẺ DI SẢN SỐ',
      passTitle: 'Vé Tham Quan 24 Giờ',
      passPrice: '70.000 VNĐ',
      passValidity: 'Kích hoạt khi bắt đầu',
      feat1: 'Thuyết minh GPS',
      feat2: '6 điểm Thần đạo',
      feat3: 'Nghe offline',
      collectionTag: 'BỘ SƯU TẬP VĂN MIẾU',
      collectionCode: 'HN — 001',
      unlockBtn: 'Mở Khóa & Bắt Đầu Trải Nghiệm',
      continueBtn: 'Tiếp tục khám phá di tích →',
      connectingBtn: 'Đang kết nối không gian di sản...',
      activatedBtn: '✓ Vé di sản đã kích hoạt',
      securityFooter: 'Thanh toán bảo mật · Không cần tải app'
    },
    map: {
      gpsAudioOn: 'Âm thanh GPS đang bật',
      gpsAudioOff: 'Âm thanh GPS đang tắt',
      recenterAria: 'Căn giữa vị trí của bạn',
      scale: '20 m'
    },
    bottomNav: {
      explore: 'Bản đồ',
      stops: 'Lộ trình',
      pass: 'Vé của tôi'
    },
    player: {
      tag: 'CÂU CHUYỆN THEO BƯỚC CHÂN BẠN',
      closeAria: 'Đóng bảng thuyết minh',
      playAria: 'Phát hoặc tạm dừng thuyết minh',
      badge: 'DI SẢN NGHÌN NĂM'
    },
    stopsTab: {
      badge: 'LỘ TRÌNH THẦN ĐẠO · 6 ĐIỂM',
      title: 'Hành Trình Di Sản',
      subtitle: 'Chạm vào bất kỳ điểm nào để xem vị trí trên bản đồ và lắng nghe thuyết minh',
      statusPlaying: 'ĐANG PHÁT',
      statusNearby: 'Ở GẦN BẠN',
      statusListen: 'NGHE NGAY',
      stopPrefix: 'ĐIỂM DỪNG'
    },
    passTab: {
      badge: 'VÉ THAM QUAN DI SẢN SỐ',
      title: 'Thẻ Vé Của Bạn',
      subtitle: 'Vé số có giá trị nghe thuyết minh không giới hạn trên toàn bộ di tích',
      cardTitle: '24-Hour Temple Pass',
      price: '50.000 VNĐ',
      timeRemaining: 'Thời gian còn lại',
      featuresTitle: 'QUYỀN LỢI ĐÃ KÍCH HOẠT',
      featGps: 'Tự động kích hoạt thuyết minh qua GPS',
      featAudio: '6 Điểm di sản Văn Miếu — Quốc Tử Giám',
      featLang: 'Đa ngôn ngữ: Tiếng Việt, English, 中文, 日本語',
      featOffline: 'Lưu trữ nghe Offline khi mất mạng (PWA)',
      langLabel: 'NGÔN NGỮ THUYẾT MINH',
      btnDownload: 'Tải trước nội dung Offline',
      btnDownloadDone: '✓ Đã lưu Offline thành công',
      btnRefresh: 'Làm mới trạng thái vé',
      btnLogout: 'Đăng xuất vé / Về màn chào đón'
    },
    modal: {
      exitTitle: 'Xác Nhận Thoát Vé',
      exitMessage: 'Bạn có muốn đăng xuất vé và quay lại màn hình chào đón ban đầu không?',
      confirm: 'Đồng ý',
      cancel: 'Hủy'
    },
    areaPicker: {
      title: 'Chọn Khu Di Tích Thuyết Minh',
      subtitle: 'Khám phá các di tích lịch sử hoặc tìm địa điểm gần bạn',
      badgeHint: 'Khu Di Tích Thuyết Minh',
      nearMeBtn: '🎯 Gần Bạn',
      changeAreaBtn: 'Đổi Khu ▾',
      changeAreaFromStops: 'Đổi Khu Di Tích',
      myLocationPrefix: '📍 Vị trí của bạn: ',
      gpsScanBtn: 'Định vị GPS & Tìm di tích gần bạn',
      gpsScanBtnActive: '⏳ Đang quét vị trí GPS...',
      gpsScanBtnDone: '✓ Đã cập nhật GPS gần bạn',
      gpsScanBtnRetry: '🎯 Thử lại định vị GPS',
      radiusAll: 'Tất cả',
      radius5km: 'Trong 5 km',
      radius15km: 'Trong 15 km',
      radius50km: 'Trong 50 km',
      ticketPrefix: 'Vé: ',
      selected: '✓ Đang chọn',
      selectThis: 'Chọn khu này',
      switchThis: 'Chuyển khu này',
      nearYou: '🟢 Ngay gần bạn',
      distancePrefix: 'Cách ~',
      loading: 'Đang tải danh sách khu di tích...',
      emptyRadius: 'Không tìm thấy khu di tích nào trong bán kính đã chọn.',
      resetRadius: 'Xem tất cả di tích',
      gpsError: 'Không thể lấy vị trí GPS. Vui lòng cho phép quyền truy cập vị trí trên trình duyệt.'
    },
    suggestion: {
      badgeNotUnlocked: '🏛️ KHU DI TÍCH CHƯA MỞ KHÓA',
      poiListTitle: '🎧 CÁC ĐIỂM SẼ ĐƯỢC THUYẾT MINH',
      loadingPois: 'Đang tải các điểm thuyết minh...',
      priceBoxTitle: 'GIÁ VÉ THAM QUAN',
      ttlHoursPrefix: 'Hiệu lực ',
      ttlHoursSuffix: ' giờ',
      btnDismiss: 'Để Sau',
      btnChangeAndGoToBuy: 'Thay Đổi & Trở Về Giao Diện Mua Vé'
    }
  },

  en: {
    langName: 'English',
    arrival: {
      brandTitle: 'MultiGuide',
      editionBadge: 'HANOI EDITION',
      tag: 'TEMPLE OF LITERATURE · HANOI',
      title: 'Every step,<br>a story.',
      subtitle: 'Wander at your own pace. Let the stories find you, automatically, as you explore.',
      passCategory: 'DIGITAL HERITAGE PASS',
      passTitle: '24-Hour Temple Pass',
      passPrice: '$5.00 USD',
      passValidity: 'Starts when you activate',
      feat1: 'GPS Geofence Audio',
      feat2: '6 Sacred Axis POIs',
      feat3: 'Offline Ready',
      collectionTag: 'VAN MIEU COLLECTION',
      collectionCode: 'HN — 001',
      unlockBtn: 'Unlock & Begin Audio Tour',
      continueBtn: 'Continue exploring →',
      connectingBtn: 'Connecting to your heritage experience...',
      activatedBtn: '✓ Your heritage pass is activated',
      securityFooter: 'Secure checkout · No app download needed'
    },
    map: {
      gpsAudioOn: 'GPS audio on',
      gpsAudioOff: 'GPS audio off',
      recenterAria: 'Recenter your location',
      scale: '20 m'
    },
    bottomNav: {
      explore: 'Explore',
      stops: 'All stops',
      pass: 'My pass'
    },
    player: {
      tag: 'STORIES, IN STEP WITH YOU',
      closeAria: 'Close story panel',
      playAria: 'Play or pause audio narration',
      badge: 'THOUSAND-YEAR HERITAGE'
    },
    stopsTab: {
      badge: 'SACRED AXIS ROUTE · 6 STOPS',
      title: 'Heritage Journey',
      subtitle: 'Tap any stop to view on the map and listen to audio narration',
      statusPlaying: 'NOW PLAYING',
      statusNearby: 'NEARBY',
      statusListen: 'LISTEN NOW',
      stopPrefix: 'STOP'
    },
    passTab: {
      badge: 'DIGITAL HERITAGE PASS',
      title: 'Your Heritage Pass',
      subtitle: 'Digital pass valid for unlimited audio guide playback across the monument',
      cardTitle: '24-Hour Temple Pass',
      price: '$5.00 USD',
      timeRemaining: 'Time remaining',
      featuresTitle: 'ACTIVE PRIVILEGES',
      featGps: 'Automatic GPS Geofence Audio Trigger',
      featAudio: '6 Sacred Axis Heritage POIs',
      featLang: 'Multi-language: Tiếng Việt, English, 中文, 日本語',
      featOffline: 'Offline Audio Caching (PWA Ready)',
      langLabel: 'AUDIO LANGUAGE',
      btnDownload: 'Download offline audio pack',
      btnDownloadDone: '✓ Offline pack saved successfully',
      btnRefresh: 'Refresh pass status',
      btnLogout: 'Exit Pass / Return to Welcome'
    },
    modal: {
      exitTitle: 'Exit Pass Confirmation',
      exitMessage: 'Do you want to exit your active pass and return to the welcome screen?',
      confirm: 'Exit',
      cancel: 'Cancel'
    },
    areaPicker: {
      title: 'Select Heritage Site',
      subtitle: 'Explore historical sites or discover heritage near you',
      badgeHint: 'Heritage Site Tour',
      nearMeBtn: '🎯 Near Me',
      changeAreaBtn: 'Change ▾',
      changeAreaFromStops: 'Change Heritage Site',
      myLocationPrefix: '📍 Your location: ',
      gpsScanBtn: 'Scan GPS & Find nearby heritage',
      gpsScanBtnActive: '⏳ Scanning GPS location...',
      gpsScanBtnDone: '✓ GPS location updated',
      gpsScanBtnRetry: '🎯 Retry GPS Scan',
      radiusAll: 'All',
      radius5km: 'Within 5 km',
      radius15km: 'Within 15 km',
      radius50km: 'Within 50 km',
      ticketPrefix: 'Pass: ',
      selected: '✓ Selected',
      selectThis: 'Select this',
      switchThis: 'Switch to this',
      nearYou: '🟢 Near you',
      distancePrefix: 'Distance ~',
      loading: 'Loading heritage sites...',
      emptyRadius: 'No heritage sites found within selected radius.',
      resetRadius: 'View all sites',
      gpsError: 'Unable to acquire GPS location. Please allow location access in your browser.'
    },
    suggestion: {
      badgeNotUnlocked: '🏛️ HERITAGE SITE LOCKED',
      poiListTitle: '🎧 AUDIO GUIDE STOPS INCLUDED',
      loadingPois: 'Loading audio stops...',
      priceBoxTitle: 'TICKET PRICE',
      ttlHoursPrefix: 'Valid for ',
      ttlHoursSuffix: ' hours',
      btnDismiss: 'Later',
      btnChangeAndGoToBuy: 'Change & Return to Ticket Screen'
    }
  },

  zh: {
    langName: '中文',
    arrival: {
      brandTitle: 'MultiGuide',
      editionBadge: 'HANOI EDITION',
      tag: '国家级特别遗迹 · 河内文庙',
      title: '步履所至，<br>皆为故事。',
      subtitle: '漫步于千年古刹，让古老历史随您的足迹自然娓娓道来。',
      passCategory: '数字文博通行证',
      passTitle: '24小时文庙通票',
      passPrice: '50.000 越南盾',
      passValidity: '激活后即刻生效',
      feat1: 'GPS语音导览',
      feat2: '6大神道古迹',
      feat3: '支持离线收听',
      collectionTag: '河内文庙典藏系列',
      collectionCode: 'HN — 001',
      unlockBtn: '解锁通行证 · 开启语音之旅',
      continueBtn: '继续探索古迹 →',
      connectingBtn: '正在连接文化遗产体验...',
      activatedBtn: '✓ 文博通行证已激活',
      securityFooter: '安全便捷 · 无需下载任何应用程序'
    },
    map: {
      gpsAudioOn: 'GPS语音导览已开启',
      gpsAudioOff: 'GPS语音导览已关闭',
      recenterAria: '回到我的当前位置',
      scale: '20米'
    },
    bottomNav: {
      explore: '探索',
      stops: '全部景点',
      pass: '我的通票'
    },
    player: {
      tag: '步履随行 · 历史娓娓道来',
      closeAria: '关闭讲解面板',
      playAria: '播放或暂停语音讲解',
      badge: '千年文博遗存'
    },
    stopsTab: {
      badge: '神道轴核心路线 · 6大景点',
      title: '古迹游踪',
      subtitle: '轻点任一景点即可在地图定位并收听专属讲解',
      statusPlaying: '正在播放',
      statusNearby: '就在附近',
      statusListen: '立即收听',
      stopPrefix: '景点'
    },
    passTab: {
      badge: '数字文博通行证',
      title: '我的数字通票',
      subtitle: '可在全景区内无限次收听高品质文博讲解音频',
      cardTitle: '24小时文庙通票',
      price: '50.000 越南盾',
      timeRemaining: '有效剩余时间',
      featuresTitle: '已激活特权',
      featGps: 'GPS位置智能感应触发语音',
      featAudio: '文庙—国子监6大核心景点解密',
      featLang: '多语种支持：Tiếng Việt, English, 中文, 日本語',
      featOffline: '支持离线下载随时随听 (PWA)',
      langLabel: '语音导览语种',
      btnDownload: '离线下载语音资源包',
      btnDownloadDone: '✓ 离线资源包已成功缓存',
      btnRefresh: '刷新通票状态',
      btnLogout: '退出通行证 / 返回欢迎页'
    },
    modal: {
      exitTitle: '退出通行证确认',
      exitMessage: '您确定要退出当前通行证并返回欢迎页面吗？',
      confirm: '确定',
      cancel: '取消'
    },
    areaPicker: {
      title: '选择语音导览古迹',
      subtitle: '探索历史遗迹或发现附近的文化遗产',
      badgeHint: '古迹语音导览',
      nearMeBtn: '🎯 附近古迹',
      changeAreaBtn: '更换 ▾',
      changeAreaFromStops: '更换景区',
      myLocationPrefix: '📍 您的位置: ',
      gpsScanBtn: 'GPS定位并查找附近古迹',
      gpsScanBtnActive: '⏳ 正在扫描GPS定位...',
      gpsScanBtnDone: '✓ GPS定位已更新',
      gpsScanBtnRetry: '🎯 重试GPS定位',
      radiusAll: '全部',
      radius5km: '5公里以内',
      radius15km: '15公里以内',
      radius50km: '50公里以内',
      ticketPrefix: '门票: ',
      selected: '✓ 当前选择',
      selectThis: '选择此景区',
      switchThis: '切换到此景区',
      nearYou: '🟢 就在附近',
      distancePrefix: '距离 ~',
      loading: '正在加载古迹列表...',
      emptyRadius: '所选范围内未找到古迹景区。',
      resetRadius: '查看全部古迹',
      gpsError: '无法获取GPS位置，请开启浏览器定位权限。'
    },
    suggestion: {
      badgeNotUnlocked: '🏛️ 尚未解锁此景区',
      poiListTitle: '🎧 购票后包含的语音导览点',
      loadingPois: '正在加载语音讲解点...',
      priceBoxTitle: '门票价格',
      ttlHoursPrefix: '有效期 ',
      ttlHoursSuffix: ' 小时',
      btnDismiss: '稍后再说',
      btnChangeAndGoToBuy: '更换并返回购票界面'
    }
  },

  ja: {
    langName: '日本語',
    arrival: {
      brandTitle: 'MultiGuide',
      editionBadge: 'HANOI EDITION',
      tag: '国家特別遺跡 · ハノイ文廟',
      title: '歩むごとに、<br>語られる物語。',
      subtitle: '自分のペースで散策を。歩くごとに、歴史の物語が自然と語りかけてきます。',
      passCategory: 'デジタル遺産パス',
      passTitle: '24時間文廟パス',
      passPrice: '¥750 JPY',
      passValidity: '利用開始時から有効',
      feat1: 'GPS音声ガイド',
      feat2: '6大史跡巡り',
      feat3: 'オフライン対応',
      collectionTag: '文廟コレクション',
      collectionCode: 'HN — 001',
      unlockBtn: 'パスを有効化して音声ツアーを開始',
      continueBtn: '史跡の探索を続ける →',
      connectingBtn: '遺産体験に接続しています...',
      activatedBtn: '✓ 遺産パスが有効になりました',
      securityFooter: '安心の決済 · アプリダウンロード不要'
    },
    map: {
      gpsAudioOn: 'GPS音声ガイド稼働中',
      gpsAudioOff: 'GPS音声ガイド停止中',
      recenterAria: '現在地を中心に表示',
      scale: '20 m'
    },
    bottomNav: {
      explore: '探索',
      stops: '全停留所',
      pass: 'マイパス'
    },
    player: {
      tag: '歩みと共に響く物語',
      closeAria: '解説パネルを閉じる',
      playAria: '音声解説の再生・一時停止',
      badge: '千年の歴史遺産'
    },
    stopsTab: {
      badge: '神道軸ルート · 6つの停留所',
      title: '遺産の旅路',
      subtitle: '停留所をタップして地図で位置を確認し、音声解説を聴く',
      statusPlaying: '再生中',
      statusNearby: 'すぐ近く',
      statusListen: '今すぐ聴く',
      stopPrefix: '停留所'
    },
    passTab: {
      badge: 'デジタル遺産パス',
      title: 'あなたの文廟パス',
      subtitle: '境内全域で何度でも音声ガイドをお楽しみいただけます',
      cardTitle: '24時間文廟パス',
      price: '¥750 JPY',
      timeRemaining: '残り時間',
      featuresTitle: '有効な特典',
      featGps: 'GPS位置連動による自動音声再生',
      featAudio: '文廟・国子監の6大名所完全ガイド',
      featLang: '多言語対応：Tiếng Việt, English, 中文, 日本語',
      featOffline: '電波がなくても聴けるオフライン対応 (PWA)',
      langLabel: '音声言語の選択',
      btnDownload: 'オフライン音声を事前保存',
      btnDownloadDone: '✓ オフライン保存完了',
      btnRefresh: 'パス状態を更新',
      btnLogout: 'パスを終了 / ようこそ画面へ'
    },
    modal: {
      exitTitle: 'パス終了の確認',
      exitMessage: 'パスを終了して最初のようこそ画面に戻りますか？',
      confirm: '終了する',
      cancel: 'キャンセル'
    },
    areaPicker: {
      title: '史跡を選択',
      subtitle: '歴史遺産を探索、または近くの史跡を検出',
      badgeHint: '史跡音声ガイド',
      nearMeBtn: '🎯 近くの史跡',
      changeAreaBtn: '変更 ▾',
      changeAreaFromStops: '史跡を変更',
      myLocationPrefix: '📍 現在地: ',
      gpsScanBtn: 'GPS検出＆近くの史跡を探す',
      gpsScanBtnActive: '⏳ GPS位置情報をスキャン中...',
      gpsScanBtnDone: '✓ GPS位置を更新しました',
      gpsScanBtnRetry: '🎯 GPSを再スキャン',
      radiusAll: 'すべて',
      radius5km: '5km以内',
      radius15km: '15km以内',
      radius50km: '50km以内',
      ticketPrefix: 'チケット: ',
      selected: '✓ 選択中',
      selectThis: 'この史跡を選択',
      switchThis: 'この史跡に切替',
      nearYou: '🟢 近くにあります',
      distancePrefix: '約 ~',
      loading: '史跡一覧を読み込み中...',
      emptyRadius: '指定した範囲内に史跡が見つかりません。',
      resetRadius: 'すべての史跡を表示',
      gpsError: 'GPS位置情報を取得できませんでした。ブラウザの位置情報利用を許可してください。'
    },
    suggestion: {
      badgeNotUnlocked: '🏛️ 未アンロックの史跡',
      poiListTitle: '🎧 音声ガイドに含まれるスポット',
      loadingPois: '解説スポットを読み込み中...',
      priceBoxTitle: 'チケット料金',
      ttlHoursPrefix: '有効期限 ',
      ttlHoursSuffix: ' 時間',
      btnDismiss: '後で',
      btnChangeAndGoToBuy: '変更してチケット購入画面へ'
    }
  }
};

/**
 * Lấy chuỗi bản dịch theo key phân cấp (ví dụ: 'arrival.title')
 */
export function t(keyPath, lang = null) {
  const currentLang = lang || store.get().selectedLang || 'vi';
  const dict = TRANSLATIONS[currentLang] || TRANSLATIONS['vi'];
  
  const keys = keyPath.split('.');
  let result = dict;
  for (const k of keys) {
    if (result && result[k] !== undefined) {
      result = result[k];
    } else {
      // Fallback sang tiếng Anh hoặc giữ nguyên key
      let fallback = TRANSLATIONS['en'];
      for (const fk of keys) {
        if (fallback && fallback[fk] !== undefined) fallback = fallback[fk];
        else { fallback = keyPath; break; }
      }
      return fallback;
    }
  }
  return result;
}

/**
 * Quét toàn bộ DOM và áp dụng bản dịch theo data-i18n & data-i18n-html
 */
export function applyTranslations(lang = null) {
  const currentLang = lang || store.get().selectedLang || 'vi';

  // 1. Áp dụng cho các phần tử văn bản thuần
  document.querySelectorAll('[data-i18n]').forEach((el) => {
    const key = el.getAttribute('data-i18n');
    if (key) {
      el.textContent = t(key, currentLang);
    }
  });

  // 2. Áp dụng cho các phần tử có thẻ HTML (<br>, <strong>...)
  document.querySelectorAll('[data-i18n-html]').forEach((el) => {
    const key = el.getAttribute('data-i18n-html');
    if (key) {
      el.innerHTML = t(key, currentLang);
    }
  });

  // 3. Áp dụng cho aria-label
  document.querySelectorAll('[data-i18n-aria]').forEach((el) => {
    const key = el.getAttribute('data-i18n-aria');
    if (key) {
      el.setAttribute('aria-label', t(key, currentLang));
    }
  });

  // 4. Đồng bộ nút ngôn ngữ trên các thanh chọn
  document.querySelectorAll('.lang-btn, .lang-pill').forEach((btn) => {
    const btnLang = btn.dataset.lang || btn.getAttribute('data-lang');
    const isActive = (btnLang === currentLang);
    btn.classList.toggle('is-active', isActive);
    btn.setAttribute('aria-checked', isActive ? 'true' : 'false');
  });

  // 5. Cập nhật Badge ngôn ngữ trên Mini Player
  const langBadge = document.getElementById('playerLangBadge');
  if (langBadge) {
    langBadge.textContent = currentLang.toUpperCase();
  }

  // 6. Cập nhật nhãn tab Navigation đáy
  const navExplore = document.querySelector('#btnTabExplore .nav-label');
  const navStops = document.querySelector('#btnTabStops .nav-label');
  const navPass = document.querySelector('#btnTabPass .nav-label');
  if (navExplore) navExplore.textContent = t('bottomNav.explore', currentLang);
  if (navStops) navStops.textContent = t('bottomNav.stops', currentLang);
  if (navPass) navPass.textContent = t('bottomNav.pass', currentLang);

  // 7. Đồng bộ danh sách khu di tích và modal nếu có
  if (typeof window._reRenderAreaList === 'function') {
    window._reRenderAreaList();
  }
}

/**
 * Khởi tạo bộ máy i18n và đăng ký lắng nghe state
 */
export function initI18n() {
  const initialLang = store.get().selectedLang || 'vi';
  applyTranslations(initialLang);

  // Tự động cập nhật DOM mỗi khi selectedLang trong store thay đổi
  let lastLangI18n = store.get().selectedLang;
  store.subscribe((state) => {
    if (state.selectedLang && state.selectedLang !== lastLangI18n) {
      lastLangI18n = state.selectedLang;
      applyTranslations(state.selectedLang);
    }
  });

  console.log(`[MultiGuide i18n] Engine khởi tạo thành công với ngôn ngữ: ${initialLang}`);
}

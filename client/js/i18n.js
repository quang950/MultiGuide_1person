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
      passPrice: '50.000 VNĐ',
      passValidity: 'Kích hoạt khi bắt đầu',
      feat1: 'Thuyết minh GPS',
      feat2: '6 điểm Thần đạo',
      feat3: 'Nghe offline',
      collectionTag: 'BỘ SƯU TẬP VĂN MIẾU',
      collectionCode: 'HN — 001',
      unlockBtn: 'Mở Khóa & Bắt Đầu Trải Nghiệm',
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
      btnRefresh: 'Làm mới trạng thái vé'
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
      btnRefresh: 'Refresh pass status'
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
      passPrice: '¥35 CNY',
      passValidity: '激活后即刻生效',
      feat1: 'GPS语音导览',
      feat2: '6大神道古迹',
      feat3: '支持离线收听',
      collectionTag: '河内文庙典藏系列',
      collectionCode: 'HN — 001',
      unlockBtn: '解锁通行证 · 开启语音之旅',
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
      price: '¥35 CNY',
      timeRemaining: '有效剩余时间',
      featuresTitle: '已激活特权',
      featGps: 'GPS位置智能感应触发语音',
      featAudio: '文庙—国子监6大核心景点解密',
      featLang: '多语种支持：Tiếng Việt, English, 中文, 日本語',
      featOffline: '支持离线下载随时随听 (PWA)',
      langLabel: '语音导览语种',
      btnDownload: '离线下载语音资源包',
      btnDownloadDone: '✓ 离线资源包已成功缓存',
      btnRefresh: '刷新通票状态'
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
      btnRefresh: 'パス状態を更新'
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
}

/**
 * Khởi tạo bộ máy i18n và đăng ký lắng nghe state
 */
export function initI18n() {
  const initialLang = store.get().selectedLang || 'vi';
  applyTranslations(initialLang);

  // Tự động cập nhật DOM mỗi khi selectedLang trong store thay đổi
  store.subscribe((state) => {
    if (state.selectedLang) {
      applyTranslations(state.selectedLang);
    }
  });

  console.log(`[MultiGuide i18n] Engine khởi tạo thành công với ngôn ngữ: ${initialLang}`);
}

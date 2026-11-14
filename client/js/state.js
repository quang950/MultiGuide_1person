/**
 * MultiGuide - Reactive State Management
 */
import { CONFIG } from './config.js?v=3.6';

const TOKENS_BY_AREA_KEY = 'mg_tokens_by_area';

function getStoredTokensMap() {
  try {
    const raw = sessionStorage.getItem(TOKENS_BY_AREA_KEY);
    return raw ? JSON.parse(raw) : {};
  } catch {
    return {};
  }
}

function saveTokensMap(map) {
  try {
    sessionStorage.setItem(TOKENS_BY_AREA_KEY, JSON.stringify(map));
  } catch {}
}

class StateStore {
  constructor() {
    const initialArea = CONFIG.DEFAULT_AREA_CODE;
    const tokens = getStoredTokensMap();
    const curAreaTokenInfo = tokens[initialArea] || null;

    this.state = {
      apiBaseUrl: CONFIG.API_BASE_URL,
      areaCode: initialArea,
      areaData: null,
      accessToken: curAreaTokenInfo?.token || localStorage.getItem('mg_access_token') || null,
      passData: curAreaTokenInfo?.passData || null,
      userLocation: {
        lat: CONFIG.MAP_CENTER.lat,
        lng: CONFIG.MAP_CENTER.lng,
        isSimulated: true
      },
      scanRadius: CONFIG.DEFAULT_SCAN_RADIUS,
      selectedLang: localStorage.getItem('mg_lang') || CONFIG.DEFAULT_LANG,
      nearbyPois: [],
      activePoi: null,
      isPlaying: false,
      playbackProgress: 0,
      activeTab: 'explore', // 'explore' | 'stops' | 'pass'
      isGpsAudioOn: true
    };

    this.listeners = new Set();
  }

  get() {
    return this.state;
  }

  set(partialState) {
    this.state = { ...this.state, ...partialState };
    this.notify();
  }

  subscribe(listener) {
    this.listeners.add(listener);
    return () => this.listeners.delete(listener);
  }

  notify() {
    for (const listener of this.listeners) {
      listener(this.state);
    }
  }

  setLanguage(langCode) {
    localStorage.setItem('mg_lang', langCode);
    this.set({ selectedLang: langCode });
  }

  setAreaAccessToken(areaCode, token, passData = null) {
    const targetCode = areaCode || this.state.areaCode || CONFIG.DEFAULT_AREA_CODE;
    const tokens = getStoredTokensMap();
    if (token) {
      tokens[targetCode] = {
        token,
        passData,
        areaCode: targetCode,
        savedAt: Date.now()
      };
      saveTokensMap(tokens);
      if (this.state.areaCode === targetCode) {
        localStorage.setItem('mg_access_token', token);
        this.set({ accessToken: token, passData });
      }
    } else {
      delete tokens[targetCode];
      saveTokensMap(tokens);
      if (this.state.areaCode === targetCode) {
        localStorage.removeItem('mg_access_token');
        this.set({ accessToken: null, passData: null });
      }
    }
  }

  getAreaAccessToken(areaCode) {
    const targetCode = areaCode || this.state.areaCode || CONFIG.DEFAULT_AREA_CODE;
    const tokens = getStoredTokensMap();
    return tokens[targetCode] || null;
  }

  setAccessToken(token, passData = null) {
    const curArea = this.state.areaCode || CONFIG.DEFAULT_AREA_CODE;
    this.setAreaAccessToken(curArea, token, passData);
  }
}

export const store = new StateStore();

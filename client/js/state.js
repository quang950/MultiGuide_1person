/**
 * MultiGuide - Reactive State Management
 */
import { CONFIG } from './config.js?v=3.6';

class StateStore {
  constructor() {
    this.state = {
      apiBaseUrl: CONFIG.API_BASE_URL,
      areaCode: CONFIG.DEFAULT_AREA_CODE,
      areaData: null,
      accessToken: localStorage.getItem('mg_access_token') || null,
      passData: null,
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

  setAccessToken(token, passData = null) {
    if (token) {
      localStorage.setItem('mg_access_token', token);
    } else {
      localStorage.removeItem('mg_access_token');
    }
    this.set({ accessToken: token, passData });
  }
}

export const store = new StateStore();

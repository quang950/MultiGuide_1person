/**
 * MultiGuide - Reactive State Management
 */
import { CONFIG } from './config.js?v=3.6';

class StateStore {
  constructor() {
    const token = localStorage.getItem('mg_access_token') || null;
    let savedPass = null;
    try {
      const saved = JSON.parse(localStorage.getItem('mg_pass_data') || 'null');
      if (token && saved?.token === token) savedPass = saved.data;
    } catch { /* Ignore corrupted or old cached pass metadata. */ }
    this.state = {
      apiBaseUrl: CONFIG.API_BASE_URL,
      areaCode: CONFIG.DEFAULT_AREA_CODE,
      areaData: null,
      accessToken: token,
      passData: savedPass,
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
    if (token && token === this.state.accessToken && !passData) passData = this.state.passData;
    if (token) {
      localStorage.setItem('mg_access_token', token);
      if (passData) localStorage.setItem('mg_pass_data', JSON.stringify({ token, data: passData }));
      else localStorage.removeItem('mg_pass_data');
    } else {
      localStorage.removeItem('mg_access_token');
      localStorage.removeItem('mg_pass_data');
    }
    this.set({ accessToken: token, passData });
  }
}

export const store = new StateStore();

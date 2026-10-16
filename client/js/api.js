/**
 * MultiGuide - API Service
 */
import { CONFIG } from './config.js';
import { store } from './state.js';

export const apiService = {
  get baseUrl() {
    return store.get().apiBaseUrl;
  },

  async checkHealth() {
    try {
      const res = await fetch(`${this.baseUrl}/health`);
      return res.ok;
    } catch {
      return false;
    }
  },

  async getArea(areaCode) {
    const res = await fetch(`${this.baseUrl}/areas/${areaCode}`);
    if (!res.ok) throw new Error(`Lỗi tải khu (${res.status})`);
    return await res.json();
  },

  async purchasePass(areaCode, amount = CONFIG.PASS_PRICE_VND, durationHours = CONFIG.PASS_DURATION_HOURS) {
    const idempotencyKey = crypto.randomUUID ? crypto.randomUUID() : 'idemp_' + Date.now();
    const res = await fetch(`${this.baseUrl}/areas/${areaCode}/passes`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Idempotency-Key': idempotencyKey
      },
      body: JSON.stringify({ amount, durationHours })
    });
    if (!res.ok) {
      const err = await res.json().catch(() => ({}));
      throw new Error(err.detail || err.title || `Lỗi mua vé (${res.status})`);
    }
    return await res.json();
  },

  async getCurrentPass() {
    const token = store.get().accessToken;
    if (!token) return null;
    const res = await fetch(`${this.baseUrl}/passes/current`, {
      headers: { 'Authorization': `Bearer ${token}` }
    });
    if (!res.ok) return null;
    return await res.json();
  },

  async getNearbyPois(lat, lng, radius, lang) {
    const token = store.get().accessToken;
    if (!token) return [];
    const url = `${this.baseUrl}/pois/nearby?lat=${lat}&lng=${lng}&radius=${radius}&lang=${lang}`;
    const res = await fetch(url, {
      headers: { 'Authorization': `Bearer ${token}` }
    });
    if (!res.ok) throw new Error(`Lỗi quét POI (${res.status})`);
    return await res.json();
  },

  async fetchNearbyPois(lat, lng, radius, lang) {
    return this.getNearbyPois(lat, lng, radius, lang);
  },

  async getPoiDetail(poiId, lang) {
    const token = store.get().accessToken;
    if (!token) throw new Error('Cần vé tham quan để nghe thuyết minh');
    const res = await fetch(`${this.baseUrl}/pois/${poiId}?lang=${lang}`, {
      headers: { 'Authorization': `Bearer ${token}` }
    });
    if (!res.ok) throw new Error(`Lỗi tải chi tiết POI (${res.status})`);
    return await res.json();
  },

  async fetchPoiDetail(poiId, lang) {
    return this.getPoiDetail(poiId, lang);
  },

  async sendPlayLog(poiId, durationSeconds) {
    const token = store.get().accessToken;
    if (!token || !poiId || durationSeconds <= 0) return;
    try {
      await fetch(`${this.baseUrl}/play-logs`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${token}`
        },
        body: JSON.stringify({ poiId, durationSeconds })
      });
    } catch (e) {
      console.warn('Lỗi gửi play-log:', e);
    }
  },

  async submitPlayLog(poiId, durationSeconds) {
    return this.sendPlayLog(poiId, durationSeconds);
  }
};

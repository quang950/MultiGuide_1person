/**
 * MultiGuide - API Service
 */
import { CONFIG } from './config.js?v=3.6';
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
    let token = store.get().accessToken;
    if (!token) {
      try {
        const pass = await this.purchasePass(store.get().areaCode || 'van-mieu');
        token = pass.accessToken || pass.token;
        store.setAccessToken(token, pass);
      } catch {}
    }
    if (!token) return [];
    const url = `${this.baseUrl}/pois/nearby?lat=${lat}&lng=${lng}&radius=${radius}&lang=${lang}`;
    let res = await fetch(url, {
      headers: { 'Authorization': `Bearer ${token}` }
    });
    if (res.status === 401 || res.status === 403) {
      try {
        const pass = await this.purchasePass(store.get().areaCode || 'van-mieu');
        token = pass.accessToken || pass.token;
        store.setAccessToken(token, pass);
        res = await fetch(url, { headers: { 'Authorization': `Bearer ${token}` } });
      } catch {}
    }
    if (!res.ok) throw new Error(`Lỗi quét POI (${res.status})`);
    return await res.json();
  },

  async fetchNearbyPois(lat, lng, radius, lang) {
    return this.getNearbyPois(lat, lng, radius, lang);
  },

  async getPoiDetail(poiId, lang) {
    let token = store.get().accessToken;
    if (!token) {
      try {
        console.log('[MultiGuide API] Chưa có vé tham quan, đang tự động kích hoạt vé trải nghiệm...');
        const pass = await this.purchasePass(store.get().areaCode || 'van-mieu');
        token = pass.accessToken || pass.token;
        store.setAccessToken(token, pass);
      } catch (err) {
        throw new Error('Cần vé tham quan để nghe thuyết minh');
      }
    }

    let res = await fetch(`${this.baseUrl}/pois/${poiId}?lang=${lang}`, {
      headers: { 'Authorization': `Bearer ${token}` }
    });

    // Tự động phục hồi (Self-Healing): Nếu vé hết hạn hoặc vé cũ không hợp lệ (401/403)
    if (res.status === 401 || res.status === 403) {
      console.warn('[MultiGuide API] Vé hiện tại đã hết hạn hoặc không hợp lệ (401/403). Đang tự động cấp vé mới...');
      try {
        const pass = await this.purchasePass(store.get().areaCode || 'van-mieu');
        token = pass.accessToken || pass.token;
        store.setAccessToken(token, pass);
        res = await fetch(`${this.baseUrl}/pois/${poiId}?lang=${lang}`, {
          headers: { 'Authorization': `Bearer ${token}` }
        });
      } catch (err) {
        console.error('[MultiGuide API] Không thể tự động cấp lại vé:', err);
      }
    }

    if (!res.ok) throw new Error(`Lỗi tải chi tiết POI (${res.status})`);
    return await res.json();
  },

  async fetchPoiDetail(poiId, lang) {
    return this.getPoiDetail(poiId, lang);
  },

  async sendPlayLog(poiId, durationSeconds, completed = false) {
    const token = store.get().accessToken;
    if (!token || !poiId || durationSeconds <= 0) return;
    const POI_GUID_MAP = {
      1: 'c1875fb2-84ad-4a5a-936a-173fdbf4f7f3',
      2: '3ede2134-c305-4270-9413-39c9743f9cb8',
      3: '70dabf88-5450-48e7-9603-b753f5350047',
      4: 'cc63b968-096f-4b06-b4f8-19f8f829ef42',
      5: '0c23b2e0-73d1-4f37-90a0-f95201abf43b',
      6: 'cd3c98eb-79fe-4980-a870-6f690f9e40cd'
    };
    const guid = POI_GUID_MAP[poiId] || poiId;
    const lang = store.get().selectedLang || 'vi';
    try {
      await fetch(`${this.baseUrl}/play-logs`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${token}`
        },
        body: JSON.stringify({
          poiId: guid,
          languageCode: lang,
          listenedSec: Math.max(1, Math.round(durationSeconds)),
          completed: Boolean(completed)
        })
      });
    } catch (e) {
      console.warn('Lỗi gửi play-log:', e);
    }
  },

  async submitPlayLog(poiId, durationSeconds) {
    return this.sendPlayLog(poiId, durationSeconds);
  }
};

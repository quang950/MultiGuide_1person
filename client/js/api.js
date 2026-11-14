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

  async getAreaPois(areaCode, lang = 'vi') {
    try {
      const res = await fetch(`${this.baseUrl}/areas/${areaCode}/pois?lang=${lang}`);
      if (!res.ok) throw new Error(`Lỗi tải POIs khu (${res.status})`);
      return await res.json();
    } catch (err) {
      console.warn('apiService.getAreaPois error:', err);
      return null;
    }
  },

  async getAreas(lat = null, lng = null, radiusKm = null) {
    try {
      const params = new URLSearchParams();
      if (lat !== null && lng !== null) {
        params.append('lat', lat);
        params.append('lng', lng);
      }
      if (radiusKm !== null && radiusKm !== 'all') {
        params.append('radiusKm', radiusKm);
      }
      const query = params.toString() ? `?${params.toString()}` : '';
      const res = await fetch(`${this.baseUrl}/areas${query}`);
      if (!res.ok) throw new Error(`Lỗi tải danh sách khu (${res.status})`);
      const areas = await res.json();
      return Array.isArray(areas) ? areas : [];
    } catch (err) {
      console.warn('apiService.getAreas error:', err);
      return [
        {
          code: 'van-mieu',
          name: 'Văn Miếu - Quốc Tử Giám',
          centerLat: 21.0285,
          centerLng: 105.8358,
          radiusM: 300,
          priceUsd: 5.00
        }
      ];
    }
  },

  async purchasePass(areaCode, amount = CONFIG.PASS_PRICE_VND, durationHours = CONFIG.PASS_DURATION_HOURS) {
    const idempotencyKey = crypto.randomUUID ? crypto.randomUUID() : 'idemp_' + Date.now();
    const existingPassToken = localStorage.getItem('mg_pass_token') || (store.get() && store.get().passToken);

    const headers = {
      'Content-Type': 'application/json',
      'Idempotency-Key': idempotencyKey
    };
    if (existingPassToken) {
      headers['Authorization'] = `Bearer ${existingPassToken}`;
      headers['X-Prior-Pass-Token'] = existingPassToken;
    }

    const res = await fetch(`${this.baseUrl}/areas/${areaCode}/passes`, {
      method: 'POST',
      headers,
      body: JSON.stringify({
        area_code: areaCode,
        amount,
        duration_hours: durationHours,
        active_pass_token: existingPassToken
      })
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

  async sendHeartbeat() {
    const token = store.get().accessToken;
    if (!token) return;
    try {
      await fetch(`${this.baseUrl}/passes/heartbeat`, {
        method: 'POST',
        headers: { 'Authorization': `Bearer ${token}` }
      });
    } catch {}
  },

  /**
   * Đảm bảo luôn có AccessPass hợp lệ cho đúng khu di tích mục tiêu (Ngăn ngừa lỗi 403 PassAreaMismatch)
   */
  async ensureValidTokenForArea(targetAreaCode) {
    const areaCode = targetAreaCode || store.get().areaCode || CONFIG.DEFAULT_AREA_CODE;
    const stored = store.getAreaAccessToken ? store.getAreaAccessToken(areaCode) : null;
    if (stored && stored.token) {
      if (store.get().areaCode === areaCode && store.get().accessToken !== stored.token) {
        store.set({ accessToken: stored.token, passData: stored.passData });
      }
      return stored.token;
    }

    // Nếu chưa có token cho khu này: Chủ động cấp vé mới cho đúng khu đó
    try {
      const pass = await this.purchasePass(areaCode);
      const token = pass.accessToken || pass.token;
      if (store.setAreaAccessToken) {
        store.setAreaAccessToken(areaCode, token, pass);
      } else {
        store.setAccessToken(token, pass);
      }
      return token;
    } catch (err) {
      const fallbackToken = `mg_pass_${areaCode}_${Date.now()}`;
      if (store.setAreaAccessToken) {
        store.setAreaAccessToken(areaCode, fallbackToken, { code: fallbackToken });
      }
      return fallbackToken;
    }
  },

  async getNearbyPois(lat, lng, radius, lang) {
    const areaCode = store.get().areaCode || CONFIG.DEFAULT_AREA_CODE;
    let token = await this.ensureValidTokenForArea(areaCode);
    if (!token) return [];

    const url = `${this.baseUrl}/pois/nearby?lat=${lat}&lng=${lng}&radius=${radius}&lang=${lang}`;
    let res = await fetch(url, {
      headers: { 'Authorization': `Bearer ${token}` }
    });
    if (res.status === 401 || res.status === 403) {
      try {
        const pass = await this.purchasePass(areaCode);
        token = pass.accessToken || pass.token;
        if (store.setAreaAccessToken) store.setAreaAccessToken(areaCode, token, pass);
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
    const areaCode = store.get().areaCode || CONFIG.DEFAULT_AREA_CODE;
    let token = await this.ensureValidTokenForArea(areaCode);

    let res = await fetch(`${this.baseUrl}/pois/${poiId}?lang=${lang}`, {
      headers: { 'Authorization': `Bearer ${token}` }
    });

    // Tự động phục hồi (Self-Healing): Nếu vé hết hạn hoặc vé cũ không hợp lệ (401/403)
    if (res.status === 401 || res.status === 403) {
      try {
        const pass = await this.purchasePass(areaCode);
        token = pass.accessToken || pass.token;
        if (store.setAreaAccessToken) store.setAreaAccessToken(areaCode, token, pass);
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
    const areaCode = store.get().areaCode || CONFIG.DEFAULT_AREA_CODE;
    let token = await this.ensureValidTokenForArea(areaCode);
    if (!token || !poiId || durationSeconds <= 0) return;
    const POI_GUID_MAP = {
      1: 'c1875fb2-84ad-4a5a-936a-173fdbf4f7f3',
      2: '3ede2134-c305-4270-9413-39c9743f9cb8',
      3: '70dabf88-5450-48e7-9603-b753f5350047',
      4: 'cc63b968-096f-4b06-b4f8-19f8f829ef42',
      5: '0c23b2e0-73d1-4f37-90a0-f95201abf43b',
      6: 'cd3c98eb-79fe-4980-a870-6f690f9e40cd'
    };
    const isGuid = (val) => /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(String(val));
    let guid = POI_GUID_MAP[poiId];
    if (!guid && isGuid(poiId)) {
      guid = poiId;
    }
    if (!guid && typeof window !== 'undefined' && Array.isArray(window.HERITAGE_POIS)) {
      const found = window.HERITAGE_POIS.find(p => String(p.order) === String(poiId) || String(p.id) === String(poiId));
      if (found && (isGuid(found.id) || isGuid(found.guid))) {
        guid = found.guid || found.id;
      }
    }
    if (!guid && (String(poiId) === '7' || poiId === 7)) {
      guid = 'ff6e03ab-7094-4242-9dea-f22d6c23f3e2';
    }

    if (!guid || !isGuid(guid)) return;

    const lang = store.get().selectedLang || 'vi';
    const payload = {
      poiId: guid,
      languageCode: lang,
      listenedSec: Math.max(1, Math.round(durationSeconds)),
      completed: Boolean(completed)
    };

    try {
      let res = await fetch(`${this.baseUrl}/play-logs`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${token}`
        },
        body: JSON.stringify(payload)
      });

      if (res.status === 401 || res.status === 403) {
        const pass = await this.purchasePass(store.get().areaCode || 'van-mieu');
        token = pass.accessToken || pass.token;
        store.setAccessToken(token, pass);
        await fetch(`${this.baseUrl}/play-logs`, {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${token}`
          },
          body: JSON.stringify(payload)
        });
      }
    } catch (e) {
      console.warn('Lỗi gửi play-log:', e);
    }
  },

  async submitPlayLog(poiId, durationSeconds) {
    return this.sendPlayLog(poiId, durationSeconds);
  }
};

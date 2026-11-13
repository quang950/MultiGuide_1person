// Save optional translations without swallowing server validation/storage errors.
export async function savePoiTranslations({ poiId, drafts, topic, generateContent, request }) {
  for (const lang of ['vi', 'en', 'zh', 'ja']) {
    const draft = drafts[lang] || {};
    const fallback = generateContent(topic, lang);
    const title = (draft.title || '').trim() || fallback.title;
    const description = (draft.description || '').trim() || (draft.summary || '').trim() || fallback.script;
    const payload = JSON.stringify({ title, description });
    const endpoint = `/admin/pois/${poiId}/translations`;
    let response;
    if (draft.id) {
      response = await request(`${endpoint}/${draft.id}`, { method: 'PUT', body: payload });
    } else {
      response = await request(endpoint, {
        method: 'POST', body: JSON.stringify({ language_code: lang, title, description })
      });
      if (response.status === 409) {
        const list = await request(endpoint);
        await requireSuccess(list, lang);
        const existing = (await list.json()).find(t => (t.languageCode || t.language_code) === lang);
        if (!existing) throw new Error(`${lang.toUpperCase()}: Không tìm thấy bản dịch để cập nhật.`);
        draft.id = existing.id;
        response = await request(`${endpoint}/${draft.id}`, { method: 'PUT', body: payload });
      }
    }
    await requireSuccess(response, lang);
  }
}

async function requireSuccess(response, lang) {
  if (response.ok) return;
  const problem = await response.json().catch(() => ({}));
  throw new Error(`${lang.toUpperCase()}: ${problem.detail || problem.title || `Lưu bản dịch thất bại (HTTP ${response.status}).`}`);
}

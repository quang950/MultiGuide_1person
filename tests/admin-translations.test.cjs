const {test} = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const vm = require('node:vm');
const path = require('node:path');
const source = fs.readFileSync(path.join(__dirname, '../client/js/admin-translations.js'), 'utf8');
const save = vm.runInNewContext(source.replace('export async function', 'async function') + '\nsavePoiTranslations;');
const response = (status, data = {}) => ({status, ok:status >= 200 && status < 300, json:async()=>data});
const generateContent = (topic, lang) => ({title:topic, script:lang === 'zh' ? '' : 'Narration'});

test('An optional empty Chinese translation is skipped instead of sending an invalid POST', async () => {
  const sent = [];
  await save({poiId:'poi', drafts:{}, topic:'New POI', generateContent,
    request:async(url, options) => {sent.push(JSON.parse(options.body).language_code); return response(201,{id:'saved'});}});
  assert.deepEqual(sent, ['vi','en','ja']);
});

test('422 and 500 responses must reject the save operation', async () => {
  for (const status of [422,500]) {
    await assert.rejects(save({poiId:'poi', drafts:{vi:{id:'vi'}}, topic:'POI', generateContent,
      request:async()=>response(status,{detail:'Cannot persist translation'})}), /Cannot persist translation/);
  }
});

test('A failed conflict-recovery PUT is reported, not treated as success', async () => {
  let calls=0;
  await assert.rejects(save({poiId:'poi', drafts:{}, topic:'POI', generateContent,
    request:async()=>[response(409),response(200,[{id:'existing',languageCode:'vi'}]),response(500)][calls++]}), /HTTP 500/);
});

test('IDs of successful translations survive a later failure for safe retries', async () => {
  const drafts={};
  await assert.rejects(save({poiId:'poi', drafts, topic:'POI', generateContent,
    request:async(url, options)=>JSON.parse(options.body).language_code==='vi'
      ? response(201,{id:'saved-vi'}) : response(500)}));
  assert.equal(drafts.vi.id, 'saved-vi');
});

test('An existing Chinese translation cannot be silently cleared', async () => {
  await assert.rejects(save({poiId:'poi', drafts:{zh:{id:'existing-zh'}}, topic:'POI', generateContent,
    request:async()=>response(200)}), /ZH:/);
});

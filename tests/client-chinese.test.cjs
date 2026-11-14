const {test} = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const vm = require('node:vm');
const path = require('node:path');
const root = path.resolve(__dirname, '..');

function engine(detail, selectedLang = 'zh') {
  const state = { selectedLang };
  const source = fs.readFileSync(path.join(root, 'client/js/audio.js'), 'utf8')
    .replace(/^import .*;\r?\n/gm, '').replaceAll('export const ', 'const ').replaceAll('export class ', 'class ');
  const elements = {
    sheetPoiTitle: {textContent: ''},
    storyHeading: {textContent: ''}
  };
  const context = {
    store: {get: () => state},
    apiService: {getPoiDetail: typeof detail === 'function' ? detail : async () => detail},
    localStorage: {getItem: () => null},
    window: {HERITAGE_POIS: []},
    document: {getElementById: id => elements[id] || null, querySelector: () => null},
    navigator: {},
    setTimeout,
    console: {log(){}, warn(){}}
  };
  const result = vm.runInNewContext(source + '\n({audioEngine, POI_STORIES});', context);
  const playedSources = [];
  result.audioEngine.audioElement = {
    src: '', paused: true, currentTime: 0, volume: 1,
    load(){},
    async play() {
      playedSources.push(this.src);
      this.paused = false;
    },
    pause() { this.paused = true; }
  };
  return {...result, elements, playedSources, state};
}

test('A slow response for the previous pin cannot replace the last selected pin', async () => {
  const pending = new Map();
  const {audioEngine, elements, playedSources} = engine(id => new Promise(resolve => pending.set(id, resolve)), 'vi');
  const first = audioEngine.playPoi(1);
  const second = audioEngine.playPoi(2);
  pending.get(POI_ID(2))({language:'vi', title:'B', audioUrl:'b.mp3'});
  await second;
  pending.get(POI_ID(1))({language:'vi', title:'A', audioUrl:'a.mp3'});
  await first;
  assert.equal(audioEngine.currentPoiId, 2);
  assert.equal(elements.sheetPoiTitle.textContent, 'B');
  assert.equal(audioEngine.audioElement.src, 'b.mp3');
  assert.deepEqual(playedSources, ['b.mp3']);
});

test('An old language response cannot overwrite the current language', async () => {
  const pending = new Map();
  const {audioEngine, state, elements} = engine((id, lang) => new Promise(resolve => pending.set(lang, resolve)), 'en');
  const english = audioEngine.loadAudioForPoi(1);
  state.selectedLang = 'zh';
  const chinese = audioEngine.loadAudioForPoi(1);
  pending.get('zh')({language:'zh', title:'中文', audioUrl:'zh.mp3'});
  await chinese;
  pending.get('en')({language:'en', title:'English', audioUrl:'en.mp3'});
  assert.equal(await english, false);
  assert.equal(elements.sheetPoiTitle.textContent, '中文');
  assert.equal(audioEngine.audioElement.src, 'zh.mp3');
});

test('A failed old request cannot start its fallback over a newer pin', async () => {
  let rejectOld;
  const {audioEngine, playedSources} = engine(id => id === POI_ID(1)
    ? new Promise((resolve, reject) => { rejectOld = reject; })
    : Promise.resolve({language:'vi', title:'B', audioUrl:'b.mp3'}), 'vi');
  const first = audioEngine.playPoi(1);
  await audioEngine.playPoi(2);
  rejectOld(new Error('late network error'));
  await first;
  assert.deepEqual(playedSources, ['b.mp3']);
});

function POI_ID(stop) {
  return ['c1875fb2-84ad-4a5a-936a-173fdbf4f7f3', '3ede2134-c305-4270-9413-39c9743f9cb8'][stop - 1];
}

test('Selecting another pin during fade immediately changes the current selection', async () => {
  const requested = [];
  const {audioEngine, elements} = engine(async id => {
    requested.push(id);
    return {language:'vi', title:'B', audioUrl:'b.mp3'};
  }, 'vi');
  audioEngine.audioElement.paused = false;
  audioEngine.audioElement.currentTime = 1;
  const first = audioEngine.playPoi(1);
  const second = audioEngine.playPoi(2);
  assert.equal(audioEngine.currentPoiId, 2);
  assert.equal(elements.sheetPoiTitle.textContent, 'Khuê Văn Các');
  await Promise.all([first, second]);
  assert.deepEqual(requested, [POI_ID(2)]);
  assert.equal(audioEngine.audioElement.src, 'b.mp3');
  assert.equal(audioEngine.audioElement.paused, false);
});

test('Chinese narration stays Chinese when the API falls back to English', async () => {
  const {audioEngine, POI_STORIES} = engine({language:'en', title:'English fallback', audioUrl:'https://example.com/en.mp3'});
  for (let stop=1;stop<=6;stop++) {
    await audioEngine.loadAudioForPoi(stop);
    const src = audioEngine.audioElement.src;
    assert.ok(src.endsWith('_zh.mp3'));
    assert.ok(POI_STORIES[stop].zh.paragraph.length > 20);
    assert.ok(fs.statSync(path.join(root, 'client', src)).size > 1000);
  }
});

test('A Chinese audio URL returned by the backend takes precedence', async () => {
  const expected = 'https://example.com/zh.mp3';
  const {audioEngine} = engine({language:'zh', audioUrl:expected});
  await audioEngine.loadAudioForPoi(1);
  assert.equal(audioEngine.audioElement.src, expected);
});

const standardPoiIds = [
  'c1875fb2-84ad-4a5a-936a-173fdbf4f7f3',
  '3ede2134-c305-4270-9413-39c9743f9cb8',
  '70dabf88-5450-48e7-9603-b753f5350047',
  'cc63b968-096f-4b06-b4f8-19f8f829ef42',
  '0c23b2e0-73d1-4f37-90a0-f95201abf43b',
  'cd3c98eb-79fe-4980-a870-6f690f9e40cd'
];
const firstStopTitles = {
  vi: 'Cổng Văn Miếu',
  en: 'Van Mieu Gate',
  ja: '文廟門 (大中門)',
  zh: '文庙门 (大门)'
};

for (const lang of ['vi', 'en', 'ja', 'zh']) {
  test(`Selecting a POI starts local ${lang} narration when the API is unavailable`, async () => {
    const requestedPois = [];
    const {audioEngine, POI_STORIES, elements, playedSources} = engine(async (poiId, requestedLang) => {
      requestedPois.push({poiId, lang: requestedLang});
      throw new Error('Backend unavailable');
    }, lang);

    for (const [index, poiId] of standardPoiIds.entries()) {
      await audioEngine.playPoi(poiId);
      const suffix = lang === 'vi' ? '' : `_${lang}`;
      const expectedSrc = `assets/audio/pois/${poiId}${suffix}.mp3`;

      assert.equal(audioEngine.currentPoiId, poiId);
      assert.equal(audioEngine.audioElement.src, expectedSrc);
      assert.equal(audioEngine.audioElement.paused, false);
      assert.equal(playedSources.length, index + 1, 'Each POI selection must request playback');
      assert.equal(playedSources[index], expectedSrc);
      assert.ok(fs.statSync(path.join(root, 'client', expectedSrc)).size > 1000);
      assert.equal(elements.sheetPoiTitle.textContent, POI_STORIES[index + 1][lang].title);
      assert.equal(elements.storyHeading.textContent, POI_STORIES[index + 1][lang].heading);
      if (index === 0) assert.equal(elements.sheetPoiTitle.textContent, firstStopTitles[lang]);
    }

    assert.deepEqual(requestedPois, standardPoiIds.map(poiId => ({poiId, lang})));
  });
}

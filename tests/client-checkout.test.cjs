const {test} = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');

const read = name => fs.readFileSync(path.join(__dirname, '../client/js', name), 'utf8')
  .replace(/^import .*;\r?\n/gm, '').replace(/export (const|function|class|async function)/g, '$1');
const response = (status, data) => ({status, ok:status >= 200 && status < 300, json:async()=>data});
const paidPass = (code, token = `paid-${code}`) => ({
  areaCode:code, accessToken:token, expiresAt:new Date(Date.now() + 3600000).toISOString()
});
function memoryStorage() {
  const values = new Map();
  return {getItem:key=>values.get(key) ?? null, setItem:(key,value)=>values.set(key,String(value)), removeItem:key=>values.delete(key)};
}
function element() {
  return {textContent:'', disabled:false, style:{},
    classList:{add(){}, remove(){}},
    querySelector(){return null;}, querySelectorAll(){return [];},
    addEventListener(event, callback){this[event] = callback;}};
}
function session() {
  const elements = Object.fromEntries([
    'btnActivatePass', 'modalAreaSuggestion', 'btnAcceptSuggestionPurchase', 'btnDismissSuggestion'
  ].map(id=>[id,element()]));
  let checkoutLabel = element();
  elements.btnActivatePass.querySelector = () => checkoutLabel;
  const alerts = [], requests = [], timers = [];
  let transitions = 0, switches = 0, purchase = async code => response(201,paidPass(code));
  const context = vm.createContext({
    CONFIG:{DEFAULT_AREA_CODE:'van-mieu',DEFAULT_LANG:'vi',MAP_CENTER:{lat:21,lng:105},API_BASE_URL:'https://api.test'},
    localStorage:memoryStorage(), sessionStorage:memoryStorage(),
    window:{}, navigator:{},
    document:{getElementById:id=>elements[id] || null, querySelector:()=>null, querySelectorAll:()=>[]},
    modalService:{alert:options=>{alerts.push(options);return Promise.resolve();}},
    console:{log(){},warn(){},error(){}},
    setTimeout:fn=>{timers.push(fn);return timers.length;},clearTimeout(){},
    crypto:{randomUUID:()=>'test-idempotency-key'},URLSearchParams,
    fetch:async (url, options={})=>{
      requests.push({url,options});
      if (options.method === 'POST' && /\/areas\/[^/]+\/passes$/.test(url)) {
        return purchase(url.match(/\/areas\/([^/]+)\/passes$/)[1]);
      }
      if (url.includes('/pois?')) return response(200,[]);
      throw new Error('Unexpected request: ' + url);
    }
  });
  const store = vm.runInContext(read('state.js')+'\nstore;',context);
  const api = vm.runInContext(read('api.js')+'\napiService;',context);
  vm.runInContext(read('i18n.js'),context);
  vm.runInContext(read('ui.js'),context);
  context.onTransition = ()=>transitions++;
  vm.runInContext('transitionToMainApp = onTransition; showSwitchAreaToast = () => {};',context);
  context.window._selectAreaByName = area=>{
    switches++;
    const cached = store.getAreaAccessToken(area.code);
    store.set({areaCode:area.code,accessToken:cached?.token,passData:cached?.passData});
  };
  return {
    store, api, elements, alerts, requests, timers, context,
    transitions:()=>transitions, switches:()=>switches,
    setPurchase:fn=>{purchase=fn;},
    purchaseRequests:()=>requests.filter(r=>r.options.method==='POST'),
    isUnlocked:code=>vm.runInContext(`isAreaUnlocked(${JSON.stringify(code)})`,context),
    initCheckout:()=>vm.runInContext('initPassCheckout()',context),
    replaceCheckoutLabel:()=>{checkoutLabel=element();return checkoutLabel;},
    openModal:async callback=>{
      context.afterUnlock=callback;
      return vm.runInContext('openAreaPurchaseModal({code:"museum-b",name:"Museum B",priceUsd:5}, afterUnlock)',context);
    }
  };
}

test('Continue with an existing pass unlocks the selected area without buying again', async () => {
  const s=session();s.store.setAccessToken('paid-existing',paidPass('van-mieu','paid-existing'));
  s.initCheckout();
  await s.elements.btnActivatePass.click();
  assert.equal(s.transitions(),1);
  assert.equal(s.isUnlocked('van-mieu'),true);
  assert.equal(s.purchaseRequests().length,0);
});

test('A pass for another area cannot skip purchase of the selected area', async () => {
  const s=session();s.store.setAccessToken('paid-a',paidPass('van-mieu','paid-a'));
  s.store.set({areaCode:'museum-b'});
  s.initCheckout();
  await s.elements.btnActivatePass.click();
  assert.equal(s.purchaseRequests().length,1);
  assert.ok(s.purchaseRequests()[0].url.endsWith('/museum-b/passes'));
  assert.equal(s.store.getAreaAccessToken('museum-b').token,'paid-museum-b');
});


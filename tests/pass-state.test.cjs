const {test} = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const read = name => fs.readFileSync(path.join(__dirname, '../client/js', name), 'utf8')
  .replace(/^import .*;\r?\n/gm, '').replace(/export (const|function|class|async function)/g, '$1');

function session() {
  const values = new Map();
  const localStorage = {getItem:key=>values.get(key) ?? null, setItem:(key,value)=>values.set(key,value), removeItem:key=>values.delete(key)};
  const CONFIG = {API_BASE_URL:'https://api.test', MAP_CENTER:{lat:0,lng:0}};
  const stateContext = vm.createContext({localStorage,CONFIG});
  const {store,StateStore} = vm.runInContext(read('state.js')+'\n({store,StateStore});',stateContext);
  const apiContext = vm.createContext({store,CONFIG});
  const apiService = vm.runInContext(read('api.js')+'\napiService;',apiContext);
  const clock={textContent:''}, badge={textContent:''}, code={textContent:''};
  const refreshLabel={key:'passTab.btnRefresh', textContent:'',setAttribute(key,value){this.key=value;}};
  const refreshButton={disabled:false,querySelector:()=>refreshLabel,addEventListener(event,fn){this.click=fn;}};
  const document={getElementById:id=>({passCountdownClock:clock,passCardCode:code,btnRefreshPass:refreshButton})[id] || null,
    querySelector:selector=>selector==='.royal-pass-status-badge'?badge:null};
  let tick;
  const uiContext=vm.createContext({store,apiService,document,window:{},t:key=>key,localStorage,
    setInterval:fn=>{tick=fn;return 1;},clearInterval(){}});
  vm.runInContext(read('ui.js'),uiContext);
  return {store,StateStore,values,apiContext,apiService,uiContext,clock,badge,refreshButton,refreshLabel,tick:()=>tick()};
}

test('Reload restores the same expiry and does not renew a pass', () => {
  const s=session();const expiresAt=new Date(Date.now()+3600000).toISOString();
  s.store.setAccessToken('token',{expiresAt});
  assert.equal(new s.StateStore().get().passData.expiresAt,expiresAt);
  s.store.setAccessToken('other-token');
  assert.equal(new s.StateStore().get().passData,null);
});

test('Corrupt or mismatched cached metadata does not attach to the current token', () => {
  const s=session();s.values.set('mg_access_token','new');
  for(const data of ['{bad',JSON.stringify({token:'old',data:{expiresAt:'2099-01-01'}})]) {
    s.values.set('mg_pass_data',data);
    assert.equal(new s.StateStore().get().passData,null);
  }
});

test('A restored expired pass shows zero instead of another 24 hours', () => {
  const s=session();s.store.setAccessToken('token',{expiresAt:new Date(Date.now()-60000).toISOString()});
  vm.runInContext('initPassCountdown()',s.uiContext);
  assert.equal(s.clock.textContent,'00:00:00');
  assert.equal(s.badge.textContent,'passTab.statusExpired');
});

test('A token without expiry metadata is shown as unknown', () => {
  const s=session();s.store.setAccessToken('token');
  vm.runInContext('initPassCountdown()',s.uiContext);
  assert.equal(s.clock.textContent,'--:--:--');
  assert.equal(s.badge.textContent,'passTab.statusUnknown');
});

test('Refresh calls the API and updates both the saved expiry and the countdown', async () => {
  const s=session();s.store.setAccessToken('token',{expiresAt:new Date(Date.now()-1000).toISOString()});
  let requestedUrl;
  const expiresAt=new Date(Date.now()+7200000).toISOString();
  s.apiContext.fetch=async url=>{requestedUrl=url;return {ok:true,status:200,json:async()=>({id:'pass-id',status:'active',expiresAt})};};
  vm.runInContext('initPassActions()',s.uiContext);
  await s.refreshButton.click();
  assert.equal(requestedUrl,'https://api.test/passes/current');
  assert.equal(s.refreshLabel.key,'passTab.btnPassActive');
  assert.equal(new s.StateStore().get().passData.expiresAt,expiresAt);
  assert.match(s.clock.textContent,/^01:59:5\d$|^02:00:00$/);
});

test('A revoked or expired server response clears the saved pass and shows zero', async () => {
  const s=session();s.store.setAccessToken('token',{expiresAt:'2099-01-01'});
  s.apiContext.fetch=async()=>({ok:false,status:403});
  vm.runInContext('initPassActions()',s.uiContext);
  await s.refreshButton.click();
  assert.equal(s.refreshLabel.key,'passTab.btnPassInvalid');
  assert.equal(s.store.get().accessToken,null);
  assert.equal(s.values.has('mg_pass_data'),false);
  assert.equal(s.clock.textContent,'00:00:00');
});

test('Server failures preserve cached expiry and do not report an active pass', async () => {
  const s=session();s.store.setAccessToken('token',{expiresAt:'2099-01-01'});
  s.apiContext.fetch=async()=>({ok:false,status:500});
  vm.runInContext('initPassActions()',s.uiContext);
  await s.refreshButton.click();
  assert.equal(s.refreshLabel.key,'passTab.btnPassCheckFailed');
  assert.equal(s.store.get().passData.expiresAt,'2099-01-01');
  assert.equal(s.refreshButton.disabled,false);
});

test('A stale validation response cannot clear a newly purchased pass', async () => {
  const s=session();s.store.setAccessToken('old');
  let resolve;s.apiContext.fetch=()=>new Promise(done=>{resolve=done;});
  const pending=s.apiService.getCurrentPass();
  s.store.setAccessToken('new',{expiresAt:'2099-01-01'});
  resolve({ok:false,status:403});await pending;
  assert.equal(s.store.get().accessToken,'new');
});

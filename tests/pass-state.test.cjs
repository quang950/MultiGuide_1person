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


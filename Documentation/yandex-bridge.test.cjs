const { test } = require('node:test');
const assert = require('node:assert/strict');
const vm = require('node:vm');
const fs = require('node:fs');
const source = fs.readFileSync('Assets/WebGLTemplates/MineArena/yandex-sdk.js', 'utf8');
function setup({ local = false, loadError = false, saveError = false, playerName = 'Игрок Яндекса' } = {}) {
  const responses = [], calls = [], listeners = {}, storage = new Map();
  let callbacks, clock = 10000;
  const sdk = {
    getPlayer: async () => ({
      getName: () => playerName,
      getData: async () => { if (loadError) throw Error('offline'); return { progress: '{"level":3}' }; },
      setData: async (data, flush) => { calls.push(['save', data, flush]); if (saveError) throw Error('offline'); }
    }),
    getPayments: async () => ({
      getCatalog: async () => [{ id: 'gold', price: '10', priceCurrencyCode: 'YAN' }],
      getPurchases: async () => [{ productID: 'gold', purchaseToken: 'receipt' }],
      purchase: async () => ({ productID: 'gold', purchaseToken: 'receipt' }),
      consumePurchase: async token => calls.push(['consume', token])
    }),
    adv: { showRewardedVideo: v => { callbacks = v.callbacks; }, showFullscreenAdv: v => { callbacks = v.callbacks; } },
    features: { LoadingAPI: { ready: () => calls.push(['ready']) }, GameplayAPI: { start: () => calls.push(['start']), stop: () => calls.push(['stop']) } },
    on: (event, callback) => { listeners[event] = callback; }, environment: { i18n: { lang: 'ru' } }
  };
  const context = { location: { hostname: local ? 'localhost' : 'games.yandex.ru' },
    document: { hidden: false, addEventListener: (name, callback) => { listeners[name] = callback; }, getElementById: () => ({ remove: () => calls.push(['removeLoading']) }) },
    localStorage: { getItem: k => storage.get(k), setItem: (k,v) => storage.set(k,v) },
    TextEncoder, Date: { now: () => clock }, setTimeout: (fn, delay) => { if (delay >= 30000) return 0; clock += delay; return setTimeout(fn, 0); }, clearTimeout,
    YaGames: local ? undefined : { init: async () => sdk }
  };
  context.window = context;
  vm.runInNewContext(source, context);
  const api = context.MineArenaPlatform;
  api.attach({ SendMessage: (_, method, json) => { if (method === 'OnPlatformResult') responses.push(JSON.parse(json)); else calls.push([method,json]); } });
  api.bind('MineArenaPlatform');
  let id = 0;
  async function request(op, key = '', data = '') { const requestId = ++id; await api.request(JSON.stringify({ id: requestId, op, key, data })); return responses.find(r => r.id === requestId); }
  return { api, request, calls, listeners, context, responses, get callbacks() { return callbacks; } };
}
const tick = () => new Promise(resolve => setImmediate(resolve));
test('profile name is optional and does not require an authorization dialog', async () => {
  assert.equal((await setup().request('playerName')).data, 'Игрок Яндекса');
  assert.equal((await setup({ playerName: '' }).request('playerName')).data, '');
  assert.equal((await setup({ local: true }).request('playerName')).data, '');
});
test('cloud load then durable save, no save before load', async () => {
  const s = setup();
  assert.equal((await s.request('save','progress','{}')).ok, false);
  assert.equal((await s.request('load','progress')).data, '{"level":3}');
  assert.equal((await s.request('save','progress','{"level":4}')).ok, true);
  assert.equal(s.calls.find(c => c[0] === 'save')[2], true);
});
test('cloud read failure never unlocks writes; failed writes reject', async () => {
  const s = setup({loadError:true});
  assert.equal((await s.request('load','progress')).ok,false);
  assert.equal((await s.request('save','progress','{}')).ok,false);
  assert.equal(s.calls.filter(c=>c[0]==='save').length,0);
  const failed = setup({saveError:true}); await failed.request('load','progress');
  assert.equal((await failed.request('save','progress','{}')).ok,false);
});
test('reward only after SDK reward callback, close without reward returns false', async () => {
  const s=setup(); const first=s.request('rewarded'); await tick();
  s.callbacks.onClose(); assert.equal((await first).data,'false');
  const second=s.request('rewarded'); await tick(); s.callbacks.onRewarded(); s.callbacks.onClose();
  assert.equal((await second).data,'true');
});
test('ad errors and overlapping requests cannot duplicate rewards', async () => {
  const s=setup(); const first=s.request('rewarded'); await tick();
  assert.equal((await s.request('rewarded')).data,'false');
  s.callbacks.onError(); s.callbacks.onRewarded(); s.callbacks.onClose();
  assert.equal((await first).data,'false'); assert.equal(s.responses.length,2);
});
test('visibility and ad pauses nest without resuming a hidden game', async () => {
  const s=setup(); await s.request('ready'); await s.request('gameplay','','true');
  const ad=s.request('rewarded'); await tick();
  s.context.document.hidden=true; s.listeners.visibilitychange(); s.callbacks.onClose(); await ad;
  assert.deepEqual(s.calls.filter(c=>c[0]==='OnPlatformPause').at(-1),['OnPlatformPause','1']);
  s.context.document.hidden=false;s.listeners.visibilitychange();
  assert.deepEqual(s.calls.at(-1),['start']);
});
test('ready is sent once, only on explicit Unity readiness',async()=>{
  const s=setup();await s.api.init();assert.equal(s.calls.some(c=>c[0]==='ready'),false);
  await s.request('ready');await s.request('ready');assert.equal(s.calls.filter(c=>c[0]==='ready').length,1);
});
test('local preview persists but never simulates paid or rewarded success',async()=>{
  const s=setup({local:true});await s.request('load','progress');await s.request('save','progress','abc');
  assert.equal((await s.request('load','progress')).data,'abc');
  assert.equal((await s.request('rewarded')).data,'false');assert.equal((await s.request('purchase','gold')).ok,false);
});
test('payment bridge preserves receipt; purchase does not auto-consume',async()=>{
  const s=setup();const r=await s.request('purchase','gold');assert.equal(JSON.parse(r.data).purchaseToken,'receipt');
  assert.equal(s.calls.some(c=>c[0]==='consume'),false);await s.request('consume','receipt');
  assert.equal(s.calls.filter(c=>c[0]==='consume').length,1);
});
test('rapid saves coalesce to latest snapshot, acknowledging all callers',async()=>{
  const s=setup();await s.request('load','progress');
  const results=await Promise.all(Array.from({length:20},(_,i)=>s.request('save','progress',String(i))));
  assert.ok(results.every(r=>r.ok));const saves=s.calls.filter(c=>c[0]==='save');
  assert.equal(saves.length,1);assert.equal(saves[0][1].progress,'19');
});
test('startup callback waits until the Unity instance attaches',async()=>{
  const s=setup();s.api.attach(null);await s.request('load','progress');
  assert.equal(s.responses.length,0);const delivered=[];
  s.api.attach({SendMessage:(_,method,data)=>delivered.push([method,data])});
  assert.equal(delivered.filter(m=>m[0]==='OnPlatformResult').length,1);
  assert.equal(JSON.parse(delivered.find(m=>m[0]==='OnPlatformResult')[1]).ok,true);
});
test('a confirmed reward survives a later SDK error callback',async()=>{
  const s=setup();const ad=s.request('rewarded');await tick();s.callbacks.onRewarded();s.callbacks.onError();
  assert.equal((await ad).data,'true');
});

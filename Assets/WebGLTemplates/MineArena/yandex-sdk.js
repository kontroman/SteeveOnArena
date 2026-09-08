/* MineArena bridge. SDK objects never cross the Unity JSON boundary. */
(function (global) {
  'use strict';
  const local = /^(localhost|127\.0\.0\.1|\[::1\])$/.test(location.hostname);
  let sdk, player, payments, unity, receiver, initialized, ready = false;
  let loaded = false, adBusy = false, gameplay = false, lastSave = 0, reportedGameplay = null;
  let saving = false;
  const pendingSaves = new Map();
  const pauses = new Set();
  function bounded(promise, ms = 30000) {
    let timer;
    return Promise.race([promise, new Promise((_, reject) => {
      timer = setTimeout(() => reject(new Error('Connection timed out')), ms);
    })]).finally(() => clearTimeout(timer));
  }
  const messages = [];
  const send = (method, value) => {
    if (unity && receiver) unity.SendMessage(receiver, method, value);
    else messages.push([method, value]);
  };
  function flushMessages() {
    if (unity && receiver) while (messages.length) { const m = messages.shift(); send(m[0], m[1]); }
  }
  function pause(reason, active) {
    active ? pauses.add(reason) : pauses.delete(reason);
    send('OnPlatformPause', pauses.size ? '1' : '0');
    updateGameplay();
  }
  function updateGameplay() {
    const api = sdk && sdk.features && sdk.features.GameplayAPI;
    if (!api) return;
    const active = ready && gameplay && !pauses.size;
    if (reportedGameplay === active) return;
    reportedGameplay = active;
    if (active) api.start(); else api.stop();
  }
  async function init() {
    if (initialized) return initialized;
    initialized = (async () => {
      if (local && !global.YaGames) return;
      if (!global.YaGames) throw new Error('Yandex SDK did not load');
      sdk = await bounded(global.YaGames.init());
      player = await bounded(sdk.getPlayer());
      if (sdk.on) {
        sdk.on('game_api_pause', () => pause('platform', true));
        sdk.on('game_api_resume', () => pause('platform', false));
      }
    })().catch(error => { initialized = null; throw error; });
    return initialized;
  }
  async function getPayments() {
    await init();
    if (!sdk) throw new Error('Payments are unavailable in local preview');
    if (!payments) payments = await sdk.getPayments({ signed: false });
    return payments;
  }
  function save(key, data) {
    if (!loaded) return Promise.reject(new Error('Cannot save before successful load'));
    if (new TextEncoder().encode(JSON.stringify({ [key]: data })).length > 200 * 1024)
      return Promise.reject(new Error('Cloud save exceeds 200 KB'));
    return new Promise((resolve, reject) => {
      const pending = pendingSaves.get(key) || { data, waiters: [] };
      pending.data = data;
      pending.waiters.push({ resolve, reject });
      pendingSaves.set(key, pending);
      drainSaves();
    });
  }
  async function drainSaves() {
    if (saving) return;
    saving = true;
    while (pendingSaves.size) {
      await new Promise(resolve => setTimeout(resolve, Math.max(0, 3100 - (Date.now() - lastSave))));
      const [key, pending] = pendingSaves.entries().next().value;
      pendingSaves.delete(key);
      try {
        lastSave = Date.now();
        if (player) await player.setData({ [key]: pending.data }, true);
        else if (local) localStorage.setItem(key, pending.data);
        else throw new Error('Cloud player unavailable');
        pending.waiters.forEach(w => w.resolve(''));
      } catch (error) { pending.waiters.forEach(w => w.reject(error)); }
    }
    saving = false;
  }
  function showAd(rewarded) {
    if (!sdk || adBusy) return Promise.resolve('false');
    adBusy = true;
    pause('ad', true);
    return new Promise(resolve => {
      let earned = false, finished = false;
      const finish = value => {
        if (finished) return;
        finished = true; adBusy = false; pause('ad', false); resolve(String(value));
      };
      try {
        if (rewarded) sdk.adv.showRewardedVideo({ callbacks: {
          onOpen: () => pause('ad', true), onRewarded: () => { earned = true; },
          onClose: () => finish(earned), onError: () => finish(earned)
        } });
        else sdk.adv.showFullscreenAdv({ callbacks: {
          onOpen: () => pause('ad', true), onClose: shown => finish(!!shown),
          onError: () => finish(false), onOffline: () => finish(false)
        } });
      } catch (_) { finish(false); }
    });
  }
  const api = {
    init,
    attach(instance) { unity = instance; flushMessages(); },
    bind(name) { receiver = name; flushMessages(); pause('hidden', document.hidden); },
    async request(raw) {
      const r = JSON.parse(raw);
      try {
        await init();
        let data = '';
        switch (r.op) {
          case 'load': {
            const values = player ? await bounded(player.getData([r.key])) : {};
            data = player ? (values[r.key] || '') : (localStorage.getItem(r.key) || '');
            if (typeof data !== 'string') throw new Error('Unsupported cloud save format');
            loaded = true;
            break;
          }
          case 'save': data = await save(r.key, r.data); break;
          case 'rewarded': data = await showAd(true); break;
          case 'interstitial': data = await showAd(false); break;
          case 'catalog': data = JSON.stringify({ items: await (await getPayments()).getCatalog() }); break;
          case 'purchases': data = JSON.stringify({ items: await (await getPayments()).getPurchases() }); break;
          case 'purchase':
            if (adBusy || pauses.size) throw new Error('Another platform dialog is active');
            pause('purchase', true);
            try { data = JSON.stringify(await (await getPayments()).purchase({ id: r.key })); }
            finally { pause('purchase', false); }
            break;
          case 'consume': await (await getPayments()).consumePurchase(r.key); break;
          case 'environment': data = sdk ? sdk.environment.i18n.lang : 'ru'; break;
          case 'ready':
            if (!ready) {
              document.getElementById('loadingScreen')?.remove();
              if (sdk) sdk.features.LoadingAPI.ready();
              ready = true;
            }
            updateGameplay(); break;
          case 'gameplay': gameplay = r.data === 'true'; updateGameplay(); break;
          case 'error': global.mineArenaLoadingError?.(); break;
          default: throw new Error('Unknown platform operation');
        }
        send('OnPlatformResult', JSON.stringify({ id: r.id, ok: true, data }));
      } catch (error) {
        if (r.op === 'load') global.mineArenaLoadingError?.();
        send('OnPlatformResult', JSON.stringify({ id: r.id, ok: false, error: String(error.message || error) }));
      }
    }
  };
  document.addEventListener('visibilitychange', () => pause('hidden', document.hidden));
  global.MineArenaPlatform = api;
})(window);

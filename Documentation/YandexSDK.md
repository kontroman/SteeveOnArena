# MineArena — Yandex Games

WebGL template: `Assets/WebGLTemplates/MineArena`. Selected in Player Settings as `PROJECT:MineArena`.
The template loads the official `/sdk.js` supplied by Yandex Games; no SDK copy is bundled.

## Connected flows

- `YandexPlatform.cs` ↔ `MineArenaPlatform.jslib` ↔ `yandex-sdk.js`, using JSON request IDs and Unity callbacks.
- Cloud progress uses the existing `SaveService_GameProgress` key. Startup waits for progress before initializing managers and the HUD. Failed reads block startup and offer reload; they never fall back to a fresh cloud save. Invalid JSON also blocks overwrite.
- Writes are coalesced, serialized and spaced at least 3.1 seconds apart. Promises resolve only after `player.setData(..., true)` succeeds. Autosave every 30 seconds and when the platform pauses the game, in addition to existing progress save events.
- Rewarded ads: fortune spin, revive, and double expedition rewards. Reward comes only from SDK `onRewarded`, reported to Unity on close. Cancel/error produces no reward.
- Interstitial: ordinary expedition completion, outside tutorial, at least 120 seconds between attempts and no automatic ad on startup. Double-reward completion does not show a second ad.
- Visibility, SDK pause/resume and advertising/payment overlays pause time and audio with nested reasons. GameplayAPI stops during these pauses and scene loads.
- LoadingAPI.ready is called once from Unity after game initialization and after removing the HTML loading screen, not merely after downloading the Unity build.
- Store products use platform prices and restore unprocessed purchases at startup / shop open / Restore Purchases. Existing resource-currency offers remain available.

## Configure real-money products

Populate `Assets/Resources/UI/YandexPurchaseCatalog.asset` in the Inspector. Each entry needs the exact `ProductId` from the Yandex developer console, an existing stackable `Item`, and its `Amount`. Only entries also present in the SDK catalog are displayed. The catalog is intentionally empty until the merchant product IDs and rewards are supplied. No fake prices or product IDs are used.

Current integration implements **consumable stackable item packs**, not permanent entitlements or subscriptions. Each verified purchase adds its receipt token to `PurchasesProgress.GrantedTokens`, grants the items, saves both to the cloud, then consumes the token. Failed saving/consumption retains the token for recovery without duplicate granting. Unknown product IDs are left unconsumed. Processing is client-side (`signed: false`); server-side signed verification requires a separate backend.

## Development and builds

- Localhost / 127.0.0.1 preview works without SDK, with localStorage saves. Ads return no reward and purchases fail explicitly in this preview. Other hosts require the Yandex `/sdk.js` endpoint; use the platform draft for actual SDK testing.
- `MineArena → Yandex → Validate Integration` checks assets, catalog mapping and receipt serialization.
- `MineArena → Yandex → Build WebGL` builds enabled scenes into `output/YandexWebGL`. WebGL `DEVOTION_GODMODE` has been removed from release defines; Editor F10 remains available.
- JS tests: `node --test Documentation/yandex-bridge.test.cjs`.
- Preview screenshots: `yandex-loading-desktop.png`, `yandex-loading-mobile.png` (mocked Unity download progress; these do not verify actual WebGL execution).
- Before publication, test the uploaded draft with actual SDK: cloud reload, ad cancellation and reward, app hide/resume, payment cancellation, successful purchase, reload before consumption, restored purchase and localized price. Local mocks cannot establish merchant account permissions or real ad availability.

## Official references

- [SDK connection](https://yandex.ru/dev/games/doc/ru/sdk/sdk-about)
- [Player data and limits](https://yandex.ru/dev/games/doc/ru/sdk/sdk-player)
- [Advertising callbacks](https://yandex.ru/dev/games/doc/ru/sdk/sdk-adv)
- [Purchases and consumption order](https://yandex.ru/dev/games/doc/ru/sdk/sdk-purchases)
- [Loading and gameplay events](https://yandex.ru/dev/games/doc/ru/sdk/sdk-game-events)

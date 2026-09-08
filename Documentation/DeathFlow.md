# Death and revival

Implemented in PlayerDeathFlow (added automatically to the persistent Player).

- Death cancels attacks, disables input/interactions and opens the choice window.
- Return to village restores full health, animation and controls at the village spawn.
- Confirmed advertising reward revives in place with full health and 3 seconds of damage protection.
- Living enemies resume movement/attacks. Dead enemies remain dead.
- Failed/cancelled advertising leaves the choice available. Missing provider disables advertising.
- Duplicate/stale advertising callbacks cannot grant another life. A 120-second realtime timeout releases the UI if the provider never responds.
- Death does not award level completion rewards or consume the exit portal trigger.

## Advertising integration

No advertising SDK implementation is present in this repository. Implement `MineArena.PlayerSystem.IReviveAdsProvider` on an active MonoBehaviour belonging to the SDK adapter. `ShowReviveAd(Action<bool>)` must call the callback on Unity's main thread, with `true` only for the SDK's confirmed reward event, and `false` on failure or closing without a reward. Existing level double-reward advertising is a separate placement and is not reused.

## Verification

Runtime scripts compile using the generated Unity project and installed Unity reference assemblies (dotnet build). Actual Play Mode and advertising SDK behavior still require verification:

1. Die during sword windup / bow draw: no delayed melee hit or arrow release; one death window.
2. Decline revival: village loads, full HP, idle animation and movement/attack work; enter another expedition.
3. Rewarded ad succeeds: revive in place, enemies resume, damage is ignored for 3 seconds and works afterwards.
4. Close/fail ad, throw in provider, or omit callback: no free life; retry/return remains possible (timeout for missing callback).
5. Send success twice or send an old success after retry/scene change: no extra revival.
6. Die again after reviving; die beside the exit portal; verify the portal is usable after revival.
7. Check Russian text and layout at desktop/mobile aspect ratios.

## UI and health bar revision

The death screen now instantiates `Assets/Resources/UI/DeathWindow.prefab`, built with the shared GameUiBuilder window, ribbon, font and button helpers. Its close button follows the same decline action as returning to the village. Preview: `Documentation/UI/DeathWindow.png`.

Fixed PlayerHealthBar losing its health subscription after the HUD was hidden. Reopening immediately synchronizes the current health before resuming animated updates. Zero health is displayed immediately; other animations use unscaled time and explicit convergence tolerances. Optional hit VFX no longer prevents damage when GameRoot has not been created.

Executed 10 Unity Edit Mode checks covering actual TakeDamage, numeric/fill updates, HUD hide/reopen/repeated reopen, zero health, revival and actual PlayingWindow prefab references/fill configuration. Results: `Documentation/death-ui-validation.txt`. These checks do not constitute a full gameplay or advertising SDK Play Mode test.

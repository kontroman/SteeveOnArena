# Mob audio and creeper explosion

Mob.SetPresetParameters attaches/configures MobFeedback for every mob, including pooled spawns. No scene or prefab wiring is required. AudioManager owns the six-source MobSoundBus and routes it through the existing effects mixer group, so the Effects and Master sliders apply.

- Attack audio fires at the guarded damage/projectile keyframe (or its fallback), once per attack. The previous skeleton/witch one-shot path was removed to avoid doubling.
- Nonlethal health loss plays Hurt; lethal damage plays only Death. Healing and preset resets are silent.
- Creeper wind-up plays a fuse stretched to the configured delay. Cancellation, death, disable and pool release stop it. Detonation replaces it and suppresses the ordinary death sound.
- Terminal sounds have independent lifetimes, so returning an owner to the pool does not cut them off. Scene replacement stops the bus.

Limits: six total voices, at most three ordinary attack/hurt voices, one voice per owner, 70 ms global ordinary cooldown, 160 ms same-clip cooldown (120 ms for explosions), linear 3D attenuation from 2 to 22 units. Priority: explosion > death > fuse > hurt > attack. Lower-priority requests cannot interrupt higher-priority voices.

The 32 original synthesized mono PCM WAVs live in Assets/Resources/MobFeedback (about 544 KiB total). Each species has a distinct base timbre and attack/hurt/death clips; playback adds small pitch variation. These are stylized synthesized effects, not sampled creature recordings. Generate them again with MineArena > Audio > Build Mob Feedback Assets. The same command validates assets and audio lifecycle/admission behavior using real Unity AudioSources in the editor.

CreeperExplosion.prefab contains a flash, sparks and square smoke particles (62 particles total, non-looping). It spawns at the detonation position, scales with blast radius, expires after two seconds and is capped at four simultaneous instances.

Validation results: mob-feedback-validation.txt. Runtime and editor C# builds also pass. These automated checks do not replace listening to the final balance during an actual crowded gameplay encounter.

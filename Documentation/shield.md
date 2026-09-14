# Shield

- Model: `Assets/Art/Shield/Shield.prefab`; texture: `shield_base_nopattern.png`.
- Crafting: Smithy level 1, 6 planks + 1 iron ingot, 7 seconds. Excluded from the general Items category.
- Drag into the off-hand inventory slot to equip; drag back into the inventory grid to unequip. Equipment is saved.
- Hold RMB to raise the shield and face the cursor continuously, including while moving. Releasing RMB restores movement-facing. Blocks a 120-degree frontal sector after approximately 0.12 seconds.
- Blocks local melee attacks, arrows, witch potion splash and explosions with a known source direction. Rear hits and damage without a direction pass through.
- Cannot start attacks while blocking; cannot raise while attacking. Hidden when the bow is selected.
- 336 durability; each successful block costs 1 + floor(incoming damage). Unblocked hits do not wear the shield.
- Durability persists through unequipping and saves. Green-to-red pixel bars appear below inventory, equipment and quick-slot icons.
- The final hit is blocked before breakage. Breaking removes the worn copy, clears equipment/quick slots and removes the hand model immediately; a reserve copy stays pristine in the bag.
- Original synthesized block and break sounds: `Assets/Resources/Audio/ShieldBlock.wav` and `ShieldBreak.wav`; playback uses the effects mixer.
- No passive armor or stamina cost.
- Equipment slots use a compact column with no shield text label. Shield positioning keeps clearance from the animated left arm.

Validation: `MineArena/Validation/Shield`; report: `Temp/shield-validation-result.txt`.
Asset rebuild: `MineArena/Build Shield`.
Server-authoritative PvP health updates are not covered by this local block mechanic.

# Chest collection quest

Quest 2000 (Treasure Hunter) covers all three existing world chests: villageChest, desertChest, forestChest. The mine currently has no chest. Open each chest once across any number of expeditions, then claim the reward in the quest journal.

Reward: TreasureKeeperSword, 110 damage, based on the existing netherite sword visuals. It has no crafting recipe and is not included in shop or random reward catalogs.

WorldChest now grants its configured loot and persists unique chest IDs in AchievementProgress. Collected chests are hidden on subsequent expeditions. Old saves initialize with an empty collection. The collection is independent of resource pickup quests. Journal reward claims are persisted after inventory updates.

The interaction command implements Execute(Component), the actual entry point used by InteractableObject. It restores movement and attack in finally and awards loot before the completion callback destroys the chest.

Validation: MineArena > Validation > Chest Quest. Uses an isolated preview scene, transient player progress and message bus, restoring the previous game state afterward. Results: chest-quest-validation.txt. Tests cover placed IDs, loot, duplicate prevention, cancellation, serialization, legacy saves, journal binding, equipment configuration and one-time reward claims. Full animation and map traversal still require a manual playthrough.

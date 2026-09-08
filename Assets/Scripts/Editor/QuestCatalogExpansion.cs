using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Achievements;
using Devotion.SDK.Controllers;
using Devotion.SDK.Managers;
using Devotion.SDK.Services.SaveSystem.Progress;
using MineArena.AI;
using MineArena.Items;
using MineArena.Managers;
using MineArena.Messages;
using MineArena.UI;
using MineArena.Structs;
using MineArena.Messages.MessageService;
using Structs;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MineArena.Editor
{
    public static partial class GameUiBuilder
    {
        private const string QuestAtlas = "Assets/Art/UI/Quests/quest-icons.png";
        private sealed class QuestFamily
        {
            public string Icon, Target, Description;
            public bool Combat;
            public int[] Goals, Amounts;
            public string[] Titles, Rewards;
            public QuestFamily(string icon, string target, bool combat, int[] goals, string[] titles, string description, string[] rewards, int[] amounts)
            { Icon = icon; Target = target; Combat = combat; Goals = goals; Titles = titles; Description = description; Rewards = rewards; Amounts = amounts; }
        }
        private static QuestFamily[] QuestFamilies() => new[]
        {
            new QuestFamily("oak", "WoodOak", false, new[]{16,96,320}, new[]{"Запас дров", "Лесной поставщик", "Хозяин лесосеки"}, "Соберите дубовую древесину: {0}.\nПомогите деревне запасти стройматериалы.", new[]{"Stone","IronOre","GoldIngot"}, new[]{12,8,6}),
            new QuestFamily("stone", "Stone", false, new[]{24,160,640}, new[]{"Первый фундамент", "Каменное дело", "Опора деревни"}, "Соберите камень: {0}.\nКрепким постройкам нужен надёжный фундамент.", new[]{"WoodOak","IronIngot","GoldIngot"}, new[]{12,4,8}),
            new QuestFamily("coal", "CoalOre", false, new[]{8,48,160}, new[]{"Разжечь горн", "Чёрное золото", "Жар большой кузни"}, "Соберите угольную руду: {0}.\nКузнице нужен запас топлива.", new[]{"IronOre","GoldOre","GoldIngot"}, new[]{4,6,10}),
            new QuestFamily("iron", "IronOre", false, new[]{6,32,96}, new[]{"Железная жила", "Рудный караван", "Стальной характер"}, "Соберите железную руду: {0}.\nОбеспечьте мастеров материалом для снаряжения.", new[]{"CoalItem","GoldIngot","DiamondOre"}, new[]{8,5,4}),
            new QuestFamily("gold", "GoldOre", false, new[]{3,18,60}, new[]{"Золотая находка", "Старатель", "Золотой запас"}, "Соберите золотую руду: {0}.\nИщите ценные залежи в экспедициях.", new[]{"IronIngot","DiamondOre","HealingPotion"}, new[]{3,3,6}),
            new QuestFamily("diamond", "DiamondOre", false, new[]{1,8,24}, new[]{"Синий огонёк", "Алмазный след", "Сокровища глубин"}, "Соберите алмазы: {0}.\nИсследуйте редкие залежи в глубине шахты.", new[]{"GoldIngot","HealingPotion","DiamondOre"}, new[]{4,4,8}),
            new QuestFamily("zombie", "Zombie", true, new[]{3,20,75}, new[]{"Ночной дозор", "Страж деревни", "Гроза нежити"}, "Победите зомби: {0}.\nЗащитите окрестности от нежити.", new[]{"HealingPotion","IronIngot","GoldIngot"}, new[]{1,5,12}),
            new QuestFamily("skeleton", "Skeleton", true, new[]{3,18,60}, new[]{"Костяной патруль", "Сломанные стрелы", "Тишина в склепе"}, "Победите скелетов: {0}.\nСократите ряды вражеских стрелков.", new[]{"IronOre","GoldIngot","DiamondOre"}, new[]{5,6,5}),
            new QuestFamily("spider", "Spider", true, new[]{3,18,60}, new[]{"Паутина на тропе", "Чистые тропы", "Конец паучьей стаи"}, "Победите пауков: {0}.\nОсвободите лесные тропы от опасных обитателей.", new[]{"HealingPotion","IronIngot","GoldIngot"}, new[]{1,5,10}),
            new QuestFamily("wolf", "Wolf", true, new[]{2,12,40}, new[]{"Волчий след", "Охотник на стаю", "Хозяин чащи"}, "Победите волков: {0}.\nСдержите хищников в диких землях.", new[]{"HealingPotion","GoldIngot","DiamondOre"}, new[]{1,6,5}),
            new QuestFamily("raider", "Pillager", true, new[]{2,12,40}, new[]{"Дать отпор", "Сорвать набег", "Щит поселения"}, "Победите разбойников с топорами: {0}.\nЛучники-разбойники не входят в эту цель.", new[]{"IronIngot","HealingPotion","DiamondOre"}, new[]{3,4,6}),
            new QuestFamily("witch", "Witch", true, new[]{1,6,20}, new[]{"Первое заклятие", "Охота на ведьм", "Рассеять проклятие"}, "Победите ведьм: {0}.\nОстерегайтесь их зелий в опасных экспедициях.", new[]{"HealingPotion","DiamondOre","GoldIngot"}, new[]{2,4,16}),
        };

        [InitializeOnLoadMethod]
        private static void WatchQuestExpansion()
        {
            EditorApplication.update += () =>
            {
                if (!File.Exists("Temp/expand-quests.request") || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
                File.Delete("Temp/expand-quests.request");
                try { ExpandQuestCatalog(); ValidateQuestExpansion(); }
                catch (Exception ex) { File.WriteAllText("Documentation/quest-validation.txt", "FAIL " + ex); Debug.LogException(ex); }
            };
        }

        [MenuItem("MineArena/Quests/Build Expanded Catalog")]
        public static void ExpandQuestCatalog()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var families = QuestFamilies();
            AssetDatabase.ImportAsset(QuestAtlas);
            var importer = (TextureImporter)AssetImporter.GetAtPath(QuestAtlas);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.alphaIsTransparency = true; importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048;
            importer.GetSourceTextureWidthAndHeight(out var width, out var height);
            float cellWidth = width / 4f, cellHeight = height / 3f;
            // Generated art occupies source rows 82–350, 405–675 and 720–1002.
            // Offset square sprite rectangles within transparent gutters so wolf ears are not clipped.
            float[] rowTop = { 20f, 350f, 704f };
            importer.spritesheet = families.Select((f, i) => new SpriteMetaData { name = "quest-" + f.Icon, rect = new Rect(i % 4 * cellWidth, height - rowTop[i / 4] * height / 1086f - cellHeight, cellWidth, cellHeight), alignment = (int)SpriteAlignment.Center, pivot = new Vector2(.5f,.5f) }).ToArray();
            importer.SaveAndReimport();
            var sprites = AssetDatabase.LoadAllAssetsAtPath(QuestAtlas).OfType<Sprite>().ToDictionary(s => s.name);
            var config = AssetDatabase.LoadAssetAtPath<GameConfig>("Assets/ScriptableObjects/GameConfig.asset");
            var so = new SerializedObject(config); var definitions = so.FindProperty("_dataAchievements");
            var doc = new List<string> { "# Каталог квестов", "", "36 новых заданий + 4 прежних. Три уровня каждой цели накапливают прогресс параллельно, за несколько экспедиций. Сложность означает длительность задания внутри его группы; доступность врагов и ресурсов зависит от открытых локаций.", "", "Считаются подбор добычи и убийства. Покупки, крафт и получение наград прогресс не добавляют. Награды выдаются один раз через журнал. Старые ID 0–3 сохранены, новые ID 1000–1035 постоянны.", "", "| ID | Сложность | Квест | Цель | Количество | Награда |", "|---|---|---|---|---:|---|" };
            for (int family = 0; family < families.Length; family++)
            {
                var f = families[family];
                var targetPath = f.Combat ? "Assets/Scripts/AI/MobPresets/" + f.Target + ".asset" : "Assets/ScriptableObjects/Configs/Drops/" + f.Target + ".asset";
                var target = AssetDatabase.LoadAssetAtPath<ScriptableObject>(targetPath);
                if (target == null) throw new Exception("Missing quest target " + targetPath);
                for (int tier = 0; tier < 3; tier++)
                {
                    int id = 1000 + family * 3 + tier;
                    int index = -1;
                    for (int i = 0; i < definitions.arraySize; i++) if (definitions.GetArrayElementAtIndex(i).FindPropertyRelative("_stableId").intValue == id) { index = i; break; }
                    if (index < 0) { index = definitions.arraySize; definitions.arraySize++; }
                    var data = definitions.GetArrayElementAtIndex(index);
                    data.FindPropertyRelative("_stableId").intValue = id;
                    data.FindPropertyRelative("_title").stringValue = f.Titles[tier];
                    data.FindPropertyRelative("_description").stringValue = f.Description;
                    data.FindPropertyRelative("_difficulty").enumValueIndex = tier;
                    data.FindPropertyRelative("_questIcon").objectReferenceValue = sprites["quest-" + f.Icon];
                    data.FindPropertyRelative("_nameAchievementKey").stringValue = "quest." + id + ".title";
                    data.FindPropertyRelative("_textTaskKey").stringValue = "quest." + id + ".description";
                    data.FindPropertyRelative("_itemTarget").objectReferenceValue = target;
                    data.FindPropertyRelative("_maxValueOnTask").intValue = f.Goals[tier];
                    var rewardItem = config.ItemDatabase.GetItemConfig(f.Rewards[tier]);
                    if (rewardItem == null) throw new Exception("Missing reward " + f.Rewards[tier]);
                    var prize = data.FindPropertyRelative("_itemPrize");
                    prize.FindPropertyRelative("_itemConfig").objectReferenceValue = rewardItem;
                    prize.FindPropertyRelative("distributionType").enumValueIndex = 0;
                    prize.FindPropertyRelative("_amountInStack").intValue = f.Amounts[tier];
                    doc.Add($"| {id} | {new[]{"Лёгкое","Среднее","Сложное"}[tier]} | {f.Titles[tier]} | {f.Target} | {f.Goals[tier]} | {rewardItem.DisplayName} ×{f.Amounts[tier]} |");
                }
            }
            // Give the four original quests matching art without altering IDs, goals or rewards.
            for (int i = 0; i < Math.Min(4, definitions.arraySize); i++)
            {
                var data = definitions.GetArrayElementAtIndex(i);
                data.FindPropertyRelative("_stableId").intValue = i;
                var item = data.FindPropertyRelative("_itemTarget").objectReferenceValue as ItemConfig;
                int f = Array.FindIndex(families, x => !x.Combat && x.Target == item?.Name);
                if (f >= 0) data.FindPropertyRelative("_questIcon").objectReferenceValue = sprites["quest-" + families[f].Icon];
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            BuildQuestJournal();
            AssetDatabase.SaveAssets();
            File.WriteAllLines("Documentation/QuestCatalog.md", doc);
            var quests = config.DataAchievements.Select((d,i) => new Achievement(d, d.StableId >= 0 ? d.StableId : i)).ToList();
            Render(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Windows/WindowAchievements.prefab"), "Documentation/UI/Quests-Expanded.png", root => root.GetComponent<global::Windows.WindowAchievements>().Bind(quests));
            var combat = quests.Where(q => q.Data.ItemTarget is MobPreset).ToList();
            Render(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Windows/WindowAchievements.prefab"), "Documentation/UI/Quests-Combat.png", root => root.GetComponent<global::Windows.WindowAchievements>().Bind(combat));
        }

        [MenuItem("MineArena/Quests/Validate Expanded Catalog")]
        public static void ValidateQuestExpansion()
        {
            if (Application.isPlaying) throw new Exception("Use Edit Mode.");
            var previousRoot = GameRoot.Instance;
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var checks = new List<string>();
            void Check(bool ok, string message) { if (!ok) throw new Exception(message); checks.Add("PASS " + message); }
            var config = AssetDatabase.LoadAssetAtPath<GameConfig>("Assets/ScriptableObjects/GameConfig.asset");
            var data = config.DataAchievements;
            Check(data.Count == 40, "40 quests registered (4 original + 36 new)");
            Check(data.Select(d => d.StableId).Distinct().Count() == 40, "Persistent quest IDs are unique");
            Check(data.Take(4).Select(d => d.StableId).SequenceEqual(new[]{0,1,2,3}), "Original quest IDs remain 0 through 3");
            Check(data.Skip(4).GroupBy(d => d.Difficulty).All(g => g.Count() == 12), "12 new quests per difficulty");
            foreach (var d in data)
            {
                Check(d.ItemTarget != null && d.MaxValueOnTask > 0 && d.QuestIcon != null, "Target, goal and generated icon: " + d.StableId);
                Check(d.ItemPrize?.ItemConfig != null && d.Amount > 0 && config.ItemDatabase.GetItemConfig(d.ItemPrize.Name) == d.ItemPrize.ItemConfig, "Reward exists in inventory database: " + d.StableId);
                var q = new Achievement(d, d.StableId);
                Check(!string.IsNullOrWhiteSpace(QuestJournalRow.Title(q)) && !QuestJournalRow.Description(q).Contains("{0}"), "Title and formatted description: " + d.StableId);
                if (d.StableId >= 1000 && d.ItemTarget is ItemConfig item)
                    Check(config.Levels.Any(l => l.AvailableResources.Contains(item)), "Resource is available in an expedition: " + item.Name);
                if (d.ItemTarget is MobPreset mob)
                    Check(config.Levels.Any(l => l.EncounterWaves.Any(w => w.MobTypes.Contains(mob.MobType))), "Enemy occurs in expedition waves: " + mob.MobType);
            }
            var busField = typeof(MessageService).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic);
            var oldBus = busField.GetValue(null); busField.SetValue(null, new MessageService());
            var scene = EditorSceneManager.NewPreviewScene();
            var singleton = typeof(GameRoot).GetProperty("Instance");
            try
            {
                var go = new GameObject("Quest validation"); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, scene); go.SetActive(false);
                var root = go.AddComponent<GameRoot>(); singleton.SetValue(null, root);
                var progress = new PlayerProgress("quest-validation-transient");
                typeof(GameRoot).GetField("gameConfig", flags).SetValue(root, config);
                typeof(GameRoot).GetField("playerProgress", flags).SetValue(root, progress);
                progress.AchievementProgress.Achievements.Add(0, new AchievementSaveData(0, 1, false, false));
                var inventory = go.AddComponent<InventoryManager>();
                var managers = (Dictionary<Type, BaseManager>)typeof(GameRoot).GetField("_managers", flags).GetValue(root);
                managers[typeof(InventoryManager)] = inventory;
                var manager = go.AddComponent<global::Managers.AchievementManager>();
                managers[typeof(global::Managers.AchievementManager)] = manager;
                MessageService.Subscribe(manager);
                var quests = manager.GetQuests();
                Check(quests.Count == 40 && manager.GetQuests().Count == 40, "Manager initialization is idempotent");
                Check(quests.Single(q => q.ID == 0).CurrentValueProgress == 1, "Legacy saved progress survives catalog expansion");
                var q = quests.Single(x => x.ID == 1000);
                string reward = q.Data.ItemPrize.Name;
                int before = inventory.GetItemAmount(reward);
                q.TransferPrize();
                Check(!q.IsCompleted && inventory.GetItemAmount(reward) == before, "Premature claim grants nothing");
                AchievementMessages.AchievementTargetTaken.Publish((q.Data.ItemTarget, 7));
                Check(q.CurrentValueProgress == 7 && quests.Single(x => x.ID == 1001).CurrentValueProgress == 7, "Resource pickup advances all tiers exactly once");
                var other = quests.Single(x => x.ID == 1003);
                Check(other.CurrentValueProgress == 0, "Unrelated resource quests do not advance");
                AchievementMessages.AchievementTargetTaken.Publish((q.Data.ItemTarget, int.MaxValue));
                Check(q.CanTakePrize && q.CurrentValueProgress == q.MaxValueProgress, "Large progress is clamped without overflow");
                q.TransferPrize(); q.TransferPrize();
                Check(q.IsCompleted && inventory.GetItemAmount(reward) == before + q.Data.Amount, "Full reward is granted exactly once");
                Check(other.CurrentValueProgress == 0, "Quest reward does not advance resource collection quests");
                progress.AchievementProgress.SaveProgress(q);
                var restored = new Achievement(q.Data, q.ID); restored.LoadData(progress.AchievementProgress.Achievements[q.ID]);
                restored.TransferPrize();
                Check(restored.IsCompleted && inventory.GetItemAmount(reward) == before + q.Data.Amount, "Reloaded completed quest cannot grant a second reward");
                var combat = quests.Single(x => x.ID == 1018);
                AchievementMessages.AchievementTargetTaken.Publish((combat.Data.ItemTarget, 1));
                Check(combat.CurrentValueProgress == 1 && quests.Single(x => x.ID == 1019).CurrentValueProgress == 1, "Mob target events advance combat tiers");
                Check(progress.AchievementProgress.Achievements[combat.ID].CurrentValue == 1, "Combat progress is saved using its stable ID");
                File.WriteAllLines("Documentation/quest-validation.txt", checks);
                Debug.Log("[Quests] " + checks.Count + " checks passed.");
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); singleton.SetValue(null, previousRoot); busField.SetValue(null, oldBus); }
        }
    }
}

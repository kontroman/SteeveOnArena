using System;
using System.IO;
using System.Linq;
using System.Reflection;
using MineArena.AI;
using MineArena.Levels;
using MineArena.PlayerSystem;
using UnityEditor;
using UnityEngine;

namespace MineArena.Editor
{
    public static class ArenaExperienceValidation
    {
        [InitializeOnLoadMethod]
        private static void Watch() => EditorApplication.update += () =>
        {
            const string request = "Temp/arena-experience.request";
            if (!File.Exists(request) || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
            File.Delete(request);
            try { Validate(); }
            catch (Exception error) { File.WriteAllText("Documentation/arena-experience-validation.txt", "FAIL " + error); Debug.LogException(error); }
        };

        [MenuItem("MineArena/Validation/Arena Experience Balance")]
        public static void Validate()
        {
            var report = new System.Text.StringBuilder();
            void Check(bool ok, string message) { if (!ok) throw new Exception(message); report.AppendLine("PASS " + message); }
            string[] names = { "Level1_Village", "Level2_Sand", "Level3_Mine", "Level4_Forest" };
            int[] expected = { 45, 75, 120, 180 };
            for (int i = 0; i < names.Length; i++)
            {
                var config = AssetDatabase.LoadAssetAtPath<LevelConfig>("Assets/ScriptableObjects/Levels/" + names[i] + ".asset");
                int count = config.EncounterWaves.Sum(w => Mathf.Max(0, w.MobCount));
                var rewards = Enumerable.Range(0, count).Select(index => PlayerExperience.ArenaMonsterReward(config.ExperiencePerClear, count, index)).ToArray();
                Check(rewards.Sum() == expected[i] && rewards.All(x => x > 0), names[i] + ": " + count + " monsters award exactly " + expected[i] + " XP");
                Check(rewards.Reverse().Sum() == expected[i], "Kill order does not affect total");
                if (i == 0)
                {
                    var xp = new PlayerExperience();
                    foreach (int reward in rewards) xp.AddExperience(reward);
                    Check(xp.CurrentLevel == 1 && xp.CurrentExperience == 45 && xp.CurrentExperience / (float)xp.ExperiencePerLevel == .75f, "First full arena = 75% to level 2");
                    Check(rewards.Take(9).Sum() < 45, "Early portal exit does not award full-clear XP");
                    foreach (int reward in rewards) xp.AddExperience(reward);
                    Check(xp.CurrentLevel == 2 && xp.CurrentExperience == 30, "Second full cycle awards another 45 XP with correct level remainder");
                    Check(PlayerExperience.ArenaMonsterReward(45, count, 0) == 3, "Tutorial enemy receives only one normal monster share");
                }
            }
            Check(PlayerExperience.ArenaMonsterReward(45, 0, 0) == 0 && PlayerExperience.ArenaMonsterReward(45, 12, 12) == 0, "Empty or extra spawn cannot exceed budget");
            var go = new GameObject("Experience pool validation");
            try
            {
                var mob = go.AddComponent<MobHealth>();
                mob.SetExperienceReward(4); Check(mob.ExperienceReward == 4, "Arena reward overrides health-based reward");
                typeof(MobHealth).GetMethod("OnEnable", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(mob, null);
                Check(mob.ExperienceReward == PlayerExperience.MonsterReward(0), "Pool reuse clears previous arena reward");
                mob.SetExperienceReward(0); Check(mob.ExperienceReward == 0, "Zero budget does not fall back to normal reward");
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
            File.WriteAllText("Documentation/arena-experience-validation.txt", report.ToString());
            Debug.Log("[ArenaExperience] Balance validation passed.");
        }
    }
}

using System;
using System.IO;
using MineArena.AI;
using UnityEditor;
using UnityEngine;

namespace MineArena.Editor
{
    public static class MobFeedbackAssets
    {
        private const string Folder = "Assets/Resources/MobFeedback";
        [InitializeOnLoadMethod]
        private static void Watch()
        {
            EditorApplication.update += () =>
            {
                const string request = "Temp/mob-feedback.request";
                if (!File.Exists(request) || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
                File.Delete(request);
                try { Build(); }
                catch (Exception e) { File.WriteAllText("Documentation/mob-feedback-validation.txt", e.ToString()); Debug.LogException(e); }
            };
        }

        [MenuItem("MineArena/Audio/Build Mob Feedback Assets")]
        public static void Build()
        {
            Directory.CreateDirectory(Folder);
            foreach (MobTypes type in Enum.GetValues(typeof(MobTypes)))
                foreach (string action in new[] { "Attack", "Hurt", "Death" })
                    WriteSound(type + action, action == "Death" ? 0.55f : 0.24f, (int)type, action);
            WriteSound("CreeperFuse", 1f, 8, "Fuse");
            WriteSound("CreeperExplosion", 1.3f, 8, "Explosion");
            AssetDatabase.Refresh();
            foreach (var guid in AssetDatabase.FindAssets("t:AudioClip", new[] { Folder }))
            {
                var importer = (AudioImporter)AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid));
                var settings = importer.defaultSampleSettings;
                settings.loadType = AudioClipLoadType.DecompressOnLoad;
                settings.compressionFormat = AudioCompressionFormat.PCM;
                settings.preloadAudioData = true;
                importer.defaultSampleSettings = settings;
                importer.SaveAndReimport();
            }
            BuildExplosion();
            AssetDatabase.SaveAssets();
            Validate();
        }

        private static void WriteSound(string name, float duration, int species, string action)
        {
            const int rate = 22050;
            int count = (int)(duration * rate);
            var rng = new System.Random(117 + species * 31 + action.Length);
            var samples = new float[count];
            float low = 0, peak = 0;
            double phase = 0;
            float[] frequencies = { 95, 310, 145, 170, 65, 220, 260, 480, 130, 180 };
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)rate, u = t / duration;
                float noise = (float)rng.NextDouble() * 2 - 1;
                low += (noise - low) * (action == "Explosion" ? 0.055f : 0.2f);
                float frequency = frequencies[species] * (action == "Death" ? 1 - u * 0.65f : 1.25f - u * 0.5f);
                phase += 2 * Math.PI * frequency / rate;
                float body = (float)(Math.Sin(phase) + 0.25 * Math.Sin(phase * 2.07));
                float envelope = Mathf.Sin(Mathf.PI * Mathf.Min(1, t / 0.008f) * 0.5f) * Mathf.Pow(1 - u, 2);
                float value = (body * 0.45f + low * 0.8f) * envelope;
                if (action == "Attack") value = (noise * 0.3f + low + body * 0.12f) * envelope;
                if (species == 1 && action != "Attack") value = (body * Mathf.Exp(-t * 22) + noise * 0.12f) * envelope;
                if (action == "Fuse") value = (noise - low) * (0.25f + u * 0.6f) * Mathf.Min(1, t / 0.02f) * Mathf.Min(1, (duration - t) / 0.025f);
                if (action == "Explosion") value = (low * 3 + noise * Mathf.Exp(-t * 35) * 0.5f + (float)Math.Sin(2 * Math.PI * (65 * t - 15 * t * t)) * 0.55f) * envelope * Mathf.Exp(-t * 2);
                samples[i] = value;
                peak = Mathf.Max(peak, Mathf.Abs(value));
            }
            using (var writer = new BinaryWriter(File.Create(Folder + "/" + name + ".wav")))
            {
                writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); writer.Write(36 + count * 2);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); writer.Write(16);
                writer.Write((short)1); writer.Write((short)1); writer.Write(rate); writer.Write(rate * 2);
                writer.Write((short)2); writer.Write((short)16);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("data")); writer.Write(count * 2);
                foreach (float sample in samples) writer.Write((short)(sample / Mathf.Max(peak, 0.001f) * 0.8f * short.MaxValue));
            }
        }

        private static void BuildExplosion()
        {
            var root = new GameObject("CreeperExplosion");
            try
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(Folder + "/ExplosionParticles.mat");
                if (material == null)
                {
                    material = new Material(Shader.Find("Particles/Standard Unlit"));
                    material.SetFloat("_Mode", 2);
                    material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    material.SetFloat("_ZWrite", 0);
                    material.EnableKeyword("_ALPHABLEND_ON");
                    material.renderQueue = 3000;
                    AssetDatabase.CreateAsset(material, Folder + "/ExplosionParticles.mat");
                }
                Burst(root, material, "Flash", 12, 0.22f, 5f, 0.5f, new Color(1, 0.72f, 0.2f), false);
                Burst(root, material, "Block smoke", 28, 1.35f, 2.3f, 0.65f, new Color(0.32f, 0.34f, 0.31f), true);
                Burst(root, material, "Sparks", 22, 0.65f, 6f, 0.12f, new Color(1, 0.43f, 0.08f), false);
                PrefabUtility.SaveAsPrefabAsset(root, Folder + "/CreeperExplosion.prefab");
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        private static void Burst(GameObject root, Material material, string name, short count, float lifetime, float speed, float size, Color color, bool smoke)
        {
            var child = new GameObject(name);
            child.transform.SetParent(root.transform, false);
            var ps = child.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.duration = 1.5f; main.loop = false; main.playOnAwake = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime * 0.7f, lifetime);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.5f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.5f, size);
            main.startColor = color; main.maxParticles = count; main.gravityModifier = smoke ? -0.12f : 0.5f;
            var emission = ps.emission; emission.rateOverTime = 0; emission.SetBursts(new[] { new ParticleSystem.Burst(0, count) });
            var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Sphere; shape.radius = 0.2f;
            var fade = ps.colorOverLifetime; fade.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(0.8f, 0.4f), new GradientAlphaKey(0, 1) });
            fade.color = gradient;
            var scale = ps.sizeOverLifetime; scale.enabled = true;
            scale.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Linear(0, 1, 1, smoke ? 2 : 0.2f));
            ps.GetComponent<ParticleSystemRenderer>().sharedMaterial = material;
        }

        private static void Validate()
        {
            int clips = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:AudioClip", new[] { Folder }))
            {
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(guid));
                if (clip == null || clip.length <= 0 || clip.channels != 1) throw new Exception("Invalid mono clip: " + guid);
                var data = new float[clip.samples];
                if (!clip.GetData(data, 0)) throw new Exception("Cannot inspect " + clip.name);
                foreach (float value in data) if (Mathf.Abs(value) >= 0.99f || float.IsNaN(value)) throw new Exception("Clipping: " + clip.name);
                clips++;
            }
            if (clips != 32) throw new Exception("Expected 32 clips, got " + clips);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "/CreeperExplosion.prefab");
            var particles = prefab.GetComponentsInChildren<ParticleSystem>();
            if (particles.Length != 3) throw new Exception("Explosion layers missing");
            foreach (var ps in particles)
                if (ps.main.loop || ps.GetComponent<ParticleSystemRenderer>().sharedMaterial.shader == null) throw new Exception("Invalid particle setup");
            ValidateVoiceLifecycle();
            File.WriteAllText("Documentation/mob-feedback-validation.txt", "PASS: 32 mono clips, nonempty PCM data, no clipping.\nPASS: explosion prefab, 3 non-looping particle layers and assigned shader.\nPASS: six spatial sources, fuse cancellation, explosion survives pool release, owner replacement stops playback.\nPASS: 30 simultaneous attacks throttled to one voice, explosion preempts full bus, duplicate explosions suppressed, disabled bus stops.\nUnity editor compilation completed. Gameplay listening still requires a playtest.\n");
        }

        private static void ValidateVoiceLifecycle()
        {
            var root = new GameObject("Mob audio validation");
            try
            {
                var bus = root.AddComponent<MineArena.Managers.MobSoundBus>();
                bus.Initialize(null);
                var sources = root.GetComponentsInChildren<AudioSource>();
                if (sources.Length != 6) throw new Exception("Voice cap must be six");
                foreach (var source in sources)
                    if (source.spatialBlend != 1 || source.loop || source.maxDistance != 22) throw new Exception("Invalid spatial voice");
                var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
                var voices = (Array)typeof(MineArena.Managers.MobSoundBus).GetField("_voices", flags).GetValue(bus);
                var voice = voices.GetValue(0);
                var type = voice.GetType();
                type.GetField("Owner").SetValue(voice, 41);
                type.GetField("Priority").SetValue(voice, 2);
                sources[0].clip = AssetDatabase.LoadAssetAtPath<AudioClip>(Folder + "/CreeperFuse.wav");
                sources[0].Play();
                if (!sources[0].isPlaying) throw new Exception("Editor audio unavailable for lifecycle check");
                bus.StopFuse(41);
                if (sources[0].isPlaying) throw new Exception("Cancelled fuse still playing");
                type.GetField("Priority").SetValue(voice, 4);
                sources[0].Play();
                bus.StopOwner(41, false);
                if (!sources[0].isPlaying) throw new Exception("Pooling cut off explosion");
                bus.StopOwner(41, true);
                if (sources[0].isPlaying) throw new Exception("Owner replacement did not stop voice");
                var listener = UnityEngine.Object.FindObjectOfType<AudioListener>();
                if (listener == null) listener = root.AddComponent<AudioListener>();
                typeof(MineArena.Managers.MobSoundBus).GetField("_listener", flags).SetValue(bus, listener);
                var position = listener.transform.position;
                // Crowd burst: 30 simultaneous requests must not create 30 one-shots.
                for (int i = 0; i < 30; i++) bus.Play(100 + i, position, ((MobTypes)(i % 10)) + "Attack", 0);
                int playing = 0;
                foreach (var source in sources) if (source.isPlaying) playing++;
                if (playing != 1) throw new Exception("Crowd throttle failed: " + playing);
                // Fill all slots with lower-priority voices; explosion must replace exactly one.
                for (int i = 0; i < voices.Length; i++)
                {
                    var slot = voices.GetValue(i);
                    type.GetField("Owner").SetValue(slot, 200 + i);
                    type.GetField("Priority").SetValue(slot, 2);
                    sources[i].clip = sources[0].clip;
                    sources[i].Play();
                }
                bus.Play(999, position, "CreeperExplosion", 4);
                int explosions = 0;
                foreach (var source in sources) if (source.isPlaying && source.clip.name == "CreeperExplosion") explosions++;
                if (explosions != 1) throw new Exception("Explosion priority failed");
                bus.Play(1000, position, "CreeperExplosion", 4);
                explosions = 0;
                foreach (var source in sources) if (source.isPlaying && source.clip.name == "CreeperExplosion") explosions++;
                if (explosions != 1) throw new Exception("Explosion duplicate suppression failed");
                // MonoBehaviour lifecycle callbacks are not dispatched in edit mode without ExecuteAlways.
                typeof(MineArena.Managers.MobSoundBus).GetMethod("OnDisable", flags).Invoke(bus, null);
                foreach (var source in sources) if (source.isPlaying) throw new Exception("Disabled bus still plays");
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Devotion.SDK.Services.Localization
{
    /// <summary>Translates presentation without changing source text used by UI logic.</summary>
    public sealed class LocalizedTextRenderer : ITextPreprocessor
    {
        private static readonly LocalizedTextRenderer Instance = new();
        private static readonly Dictionary<string, string> Sources = new();
        private static readonly Dictionary<string, string> NormalizedSources = new();
        private static readonly Regex Whitespace = new(@"\s+");
        private static readonly Dictionary<string, string> Cache = new();
        private static readonly List<(Regex Pattern, string Translation)> Templates = new();
        private static Regex fragments;
        private static readonly Regex Placeholder = new(@"\{(\d+)(?:[^{}]*)\}");
        private static readonly HashSet<TMP_Text> Labels = new();
        private static readonly HashSet<TMP_Text> Pending = new();
        private static int bindingCount;
        public static int Revision { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            Labels.Clear();
            Pending.Clear();
            TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(OnTextChanged);
            TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnTextChanged);
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            Canvas.preWillRenderCanvases -= BindPending;
            Canvas.preWillRenderCanvases += BindPending;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            foreach (var root in scene.GetRootGameObjects())
                foreach (var label in root.GetComponentsInChildren<TMP_Text>(true)) Bind(label);
        }

        private static void OnTextChanged(UnityEngine.Object target)
        {
            if (target is TMP_Text label && label.textPreprocessor == null) Pending.Add(label);
        }

        private static void BindPending()
        {
            foreach (var label in Pending) Bind(label);
            Pending.Clear();
        }

        public static void Bind(TMP_Text label)
        {
            if (label == null || label.textPreprocessor != null) return;
            label.textPreprocessor = Instance;
            Labels.Add(label);
            if (++bindingCount % 128 == 0) Labels.RemoveWhere(item => item == null);
            label.havePropertiesChanged = true;
            label.SetVerticesDirty();
        }

        public static void Rebuild(IReadOnlyDictionary<string, string> russian, IReadOnlyDictionary<string, string> translated)
        {
            Revision++;
            Sources.Clear(); NormalizedSources.Clear(); Cache.Clear(); Templates.Clear();
            foreach (var pair in russian)
            {
                var value = translated.TryGetValue(pair.Key, out var translation) && !string.IsNullOrWhiteSpace(translation)
                    ? translation : pair.Value;
                Sources[pair.Value] = value;
                NormalizedSources[Whitespace.Replace(pair.Value, " ").Trim()] = value;
            }
            foreach (var pair in Sources.OrderByDescending(p => p.Key.Length))
            {
                var matches = Placeholder.Matches(pair.Key);
                if (matches.Count == 0) continue;
                var pattern = new StringBuilder("^");
                int position = 0;
                var seen = new HashSet<string>();
                foreach (Match match in matches)
                {
                    pattern.Append(Regex.Escape(pair.Key.Substring(position, match.Index - position)));
                    var group = "p" + match.Groups[1].Value;
                    pattern.Append(seen.Add(group) ? "(?<" + group + ">.*?)" : @"\k<" + group + ">");
                    position = match.Index + match.Length;
                }
                pattern.Append(Regex.Escape(pair.Key.Substring(position))).Append("$");
                Templates.Add((new Regex(pattern.ToString(), RegexOptions.Singleline, TimeSpan.FromMilliseconds(20)), pair.Value));
            }
            var literals = Sources.Keys.Where(s => s.Length > 1 && !Placeholder.IsMatch(s))
                .OrderByDescending(s => s.Length).Select(s =>
                    (char.IsLetterOrDigit(s[0]) ? @"(?<![\p{L}\p{N}_])" : "") + Regex.Escape(s) +
                    (char.IsLetterOrDigit(s[s.Length - 1]) ? @"(?![\p{L}\p{N}_])" : ""));
            var alternatives = string.Join("|", literals);
            fragments = alternatives.Length == 0 ? null : new Regex(alternatives, RegexOptions.None, TimeSpan.FromMilliseconds(20));
            Labels.RemoveWhere(label => label == null);
            foreach (var label in Labels)
            {
                label.havePropertiesChanged = true;
                label.SetVerticesDirty();
                label.SetLayoutDirty();
            }
        }

        public string PreprocessText(string text) => Translate(text);

        public static string Translate(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            if (Sources.TryGetValue(text, out var exact)) return exact;
            if (Cache.TryGetValue(text, out var cached)) return cached;
            var result = NormalizedSources.TryGetValue(Whitespace.Replace(text, " ").Trim(), out var normalized)
                ? normalized : TranslateInternal(text);
            if (Cache.Count >= 2048) Cache.Clear();
            Cache[text] = result;
            return result;
        }

        private static string TranslateInternal(string text)
        {
            try
            {
                foreach (var template in Templates)
                {
                    var match = template.Pattern.Match(text);
                    if (!match.Success) continue;
                    return Placeholder.Replace(template.Translation, token => TranslateFragments(match.Groups["p" + token.Groups[1].Value].Value));
                }
                return TranslateFragments(text);
            }
            catch (RegexMatchTimeoutException) { return text; }
        }

        private static string TranslateFragments(string text) => fragments == null ? text :
            fragments.Replace(text, match => Sources[match.Value]);
    }
}

using System;
using System.Collections.Generic;
using Devotion.SDK.Async;
using Devotion.SDK.Confgs;
using Devotion.SDK.Controllers;
using Devotion.SDK.Interfaces;
using UnityEngine;

namespace Devotion.SDK.Services.Localization
{
    public class LocalizationService : BaseService
    {
        private static LocalizationConfig _config;
        private static readonly Dictionary<string, string> _localizationDictionary = new Dictionary<string, string>();
        private static readonly Dictionary<string, string> _russianDictionary = new();
        private const string LanguagePreference = "Localization.Language";

        public static SystemLanguage CurrentLanguage { get; private set; }

        public override IPromise Initialize()
        {
            if (GameRoot.GameConfig == null)
            {
                Debug.LogError("LocalizationService: GameConfig is missing.");
                return Promise.ResolveAndReturn();
            }

            return Initialize(GameRoot.GameConfig.LocalizationConfig);
        }

        public static IPromise Initialize(LocalizationConfig config)
        {
            if (config == null)
            {
                Debug.LogError("LocalizationService: LocalizationConfig is null.");
                return Promise.ResolveAndReturn();
            }

            _config = config;

            var languageToLoad = config.defaultLanguage;
            if (Enum.TryParse(PlayerPrefs.GetString(LanguagePreference, string.Empty), out SystemLanguage savedLanguage))
                languageToLoad = savedLanguage;
            if (Array.IndexOf(_config.supportedLanguages, languageToLoad) < 0 && _config.supportedLanguages.Length > 0)
            {
                languageToLoad = _config.supportedLanguages[0];
                Debug.LogWarning($"LocalizationService: Default language {config.defaultLanguage} is not in supportedLanguages. Falling back to {languageToLoad}.");
            }

            SetLanguage(languageToLoad);

            return Promise.ResolveAndReturn();
        }

        public static bool TryGetLocalizedText(string key, out string text)
        {
            text = null;
            return !string.IsNullOrEmpty(key) && _localizationDictionary.TryGetValue(key, out text) && !string.IsNullOrWhiteSpace(text);
        }

        public static string GetLocalizedText(string key)
        {
            if (_localizationDictionary.TryGetValue(key, out string value))
            {
                return value;
            }

            Debug.LogError($"Localization key not found: {key} (language: {CurrentLanguage})");

            return $"<BAD_KEY:{key}>";
        }

        public static void SetLanguage(SystemLanguage language)
        {
            if (_config == null)
            {
                Debug.LogError("LocalizationService: Cannot set language before initialization.");
                return;
            }

            if (Array.IndexOf(_config.supportedLanguages, language) < 0)
            {
                Debug.LogError($"Language {language} is not supported!");
                return;
            }

            CurrentLanguage = language;
            LoadLocalizationData(language);
            PlayerPrefs.SetString(LanguagePreference, language.ToString());
            PlayerPrefs.Save();

            MineArena.Messages.Game.LanguageChanged.Publish(language);
        }

        private static void LoadLocalizationData(SystemLanguage language)
        {
            _localizationDictionary.Clear();
            _russianDictionary.Clear();
            ReadLanguage(SystemLanguage.Russian, _russianDictionary);
            foreach (var pair in _russianDictionary) _localizationDictionary[pair.Key] = pair.Value;
            if (language != SystemLanguage.Russian) ReadLanguage(language, _localizationDictionary);
            LocalizedTextRenderer.Rebuild(_russianDictionary, _localizationDictionary);
        }

        private static void ReadLanguage(SystemLanguage language, Dictionary<string, string> target)
        {

            string normalizedRoot = string.IsNullOrWhiteSpace(_config.localizationFilesPath) ? string.Empty : _config.localizationFilesPath.TrimEnd('/', '\\');
            string path = string.IsNullOrEmpty(normalizedRoot) ? language.ToString() : $"{normalizedRoot}/{language}";
            TextAsset jsonFile = Resources.Load<TextAsset>(path);

            if (jsonFile == null)
            {
                Debug.LogError($"Localization file for {language} not found at path: {path}!");
                return;
            }

            LocalizationData data = JsonUtility.FromJson<LocalizationData>(jsonFile.text);
            if (data == null)
            {
                Debug.LogError($"Localization file for {language} failed to deserialize.");
                return;
            }

            foreach (var pair in data.ToDictionary())
            {
                if (!string.IsNullOrWhiteSpace(pair.Value)) target[pair.Key] = pair.Value;
            }
        }
    }
}

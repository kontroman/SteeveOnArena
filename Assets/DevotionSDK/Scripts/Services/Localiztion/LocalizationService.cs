using System.Collections.Generic;
using System;
using System.IO;
using Devotion.SDK.Async;
using Devotion.SDK.Confgs;
using UnityEngine;
using Devotion.SDK.Interfaces;
using MineArena.Helpers;
using Devotion.SDK.Controllers;

namespace Devotion.SDK.Services.Localization
{
    public class LocalizationService : BaseService, ILocalizationService
    {
        private static LocalizationService _instance;
        public static LocalizationService Instance => _instance.IsNullOrDead() ? _instance = new LocalizationService() : _instance;

        public SystemLanguage CurrentLanguage { get; private set; }

        private LocalizationConfig _config;

        private Dictionary<string, string> _localizationDictionary = new Dictionary<string, string>();

        public override IPromise Initialize()
        {
            ServiceLocator.Register<ILocalizationService>(this);
            _config = GameRoot.GameConfig.LocalizationConfig;
            CurrentLanguage = Application.systemLanguage;
            LoadLocalizationData(CurrentLanguage);
            return Promise.ResolveAndReturn();
        }

        private void LoadLocalizationData(SystemLanguage language)
        {
            string path = Path.Combine(_config.localizationFilesPath, language.ToString());
            TextAsset jsonFile = Resources.Load<TextAsset>(path);

            if (jsonFile == null)
            {
                Debug.LogError($"Localization file for {language} not found at path: {path}!");

                _localizationDictionary.Clear();

                return;
            }

            LocalizationData data = JsonUtility.FromJson<LocalizationData>(jsonFile.text);
            _localizationDictionary = data?.ToDictionary() ?? new Dictionary<string, string>();
        }

        public string GetLocalizedText(string key)
        {
            if (_localizationDictionary.TryGetValue(key, out string value))
                return value;

            Debug.LogError($"Localization key not found: {key}");

            return $"<BAD_KEY:{key}>";
        }

        public void SetLanguage(SystemLanguage language)
        {
            if (Array.IndexOf(_config.supportedLanguages, language) < 0)
            {
                Debug.LogError($"Language {language} is not supported!");
                return;
            }

            CurrentLanguage = language;
            LoadLocalizationData(language);

            MineArena.Messages.Game.LanguageChanged.Publish(language);
        }
    }
}

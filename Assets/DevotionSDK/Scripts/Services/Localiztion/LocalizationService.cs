using System.Collections.Generic;
using System;
using UnityEngine;
using System.IO;
using Devotion.SDK.Confgs;

namespace Devotion.SDK.Services.Localization
{
    public class LocalizationService : BaseService, ILocalizationService
    {
        public SystemLanguage CurrentLanguage { get; private set; }

        [SerializeField] private LocalizationConfig _config;

        private Dictionary<string, string> _localizationDictionary = new Dictionary<string, string>();

        private void Awake()
        {
            ServiceLocator.Register<ILocalizationService>(this);

            DontDestroyOnLoad(gameObject);

            Initialize();
        }

        public override void Initialize()
        {
            CurrentLanguage = Application.systemLanguage;

            LoadLocalizationData(CurrentLanguage);
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

            Messages.Game.LanguageChanged.Publish(language);
        }
    }
}

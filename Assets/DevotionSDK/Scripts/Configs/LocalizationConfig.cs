using UnityEngine;

namespace Devotion.SDK.Confgs
{
    [CreateAssetMenu(fileName = "LocalizationConfig", menuName = "Localization/Config")]
    public class LocalizationConfig : ScriptableObject
    {
        public SystemLanguage defaultLanguage = SystemLanguage.Russian;
        public string localizationFilesPath = "Localization/";

        public SystemLanguage[] supportedLanguages = 
        {
            SystemLanguage.Russian,
            SystemLanguage.English,
            SystemLanguage.Turkish,
            SystemLanguage.German,
            SystemLanguage.Spanish,
            SystemLanguage.Japanese,
            SystemLanguage.ChineseSimplified
        };
    }
}
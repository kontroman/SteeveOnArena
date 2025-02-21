using UnityEngine;

public interface ILocalizationService
{
    string GetLocalizedText(string key);
    SystemLanguage CurrentLanguage { get; }
    void SetLanguage(SystemLanguage language);
}
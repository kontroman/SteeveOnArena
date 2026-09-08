using Devotion.SDK.Base;
using Devotion.SDK.Controllers;
using MineArena.Managers;
using UnityEngine;
using UnityEngine.UI;

namespace MineArena.Windows
{
    public sealed class SettingsWindow : BaseWindow
    {
        [SerializeField] private Slider master;
        [SerializeField] private Slider music;
        [SerializeField] private Slider effects;
        [SerializeField] private Slider sensitivity;
        [SerializeField] private TMPro.TMP_Text sensitivityValue;
        private void Awake()
        {
            master.onValueChanged.AddListener(value => { AudioListener.volume = value; PlayerPrefs.SetFloat("UI.MasterVolume", value); });
            music.onValueChanged.AddListener(value => { GameRoot.GetManager<AudioManager>()?.SetMusicVolume(value); PlayerPrefs.SetFloat("UI.MusicVolume", value); });
            effects.onValueChanged.AddListener(value => { GameRoot.GetManager<AudioManager>()?.SetEffectVolume(value); PlayerPrefs.SetFloat("UI.EffectsVolume", value); });
            if (sensitivity != null) sensitivity.onValueChanged.AddListener(SetSensitivity);
        }
        private void OnEnable()
        {
            master.SetValueWithoutNotify(PlayerPrefs.GetFloat("UI.MasterVolume", 1));
            music.SetValueWithoutNotify(PlayerPrefs.GetFloat("UI.MusicVolume", 1));
            effects.SetValueWithoutNotify(PlayerPrefs.GetFloat("UI.EffectsVolume", 1));
            if (sensitivity != null) sensitivity.SetValueWithoutNotify(MineArena.UI.CameraSensitivity.Value);
            RefreshSensitivityLabel();
        }
        private void SetSensitivity(float value)
        {
            PlayerPrefs.SetFloat(MineArena.UI.CameraSensitivity.PreferenceKey, Mathf.Clamp(value, MineArena.UI.CameraSensitivity.Minimum, MineArena.UI.CameraSensitivity.Maximum));
            RefreshSensitivityLabel();
        }
        private void RefreshSensitivityLabel() { if (sensitivityValue != null) sensitivityValue.text = MineArena.UI.CameraSensitivity.Value.ToString("0.00") + "×"; }
        private void OnDisable() => PlayerPrefs.Save();
        public override void CloseWindow() => GameRoot.UIManager.CloseWindow<SettingsWindow>();
    }
}

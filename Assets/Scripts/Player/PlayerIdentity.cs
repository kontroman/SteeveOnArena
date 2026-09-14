using System;
using MineArena.Platform;
using UnityEngine;

namespace MineArena.PlayerSystem
{
    public static class PlayerIdentity
    {
        private static string _name;
        private static bool _requested;
        private static string _temporaryName;
        public static string DisplayName => !string.IsNullOrWhiteSpace(_name) ? _name :
            PlayerDevelopment.Progress?.FallbackName ?? (_temporaryName ??= "Player" + UnityEngine.Random.Range(100, 1000));

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() { _name = null; _requested = false; _temporaryName = null; }
        public static void RequestName()
        {
            if (_requested || !YandexPlatform.IsWebPlatform) return;
            _requested = true;
            YandexPlatform.Instance.Request("playerName").Then(value =>
            {
                if (string.IsNullOrWhiteSpace(value)) return;
                var clean = value.Trim().Replace("\n", " ").Replace("\r", " ");
                _name = clean.Length > 40 ? clean.Substring(0, 40) : clean;
            }).Catch(_ => { });
        }
    }
}

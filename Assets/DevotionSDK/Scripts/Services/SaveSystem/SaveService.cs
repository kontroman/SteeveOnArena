using System;
using System.Reflection;
using Devotion.SDK.Async;
using Devotion.SDK.Controllers;
using Devotion.SDK.DataStructures;
using Devotion.SDK.Interfaces;
using Devotion.SDK.Messages;
using Devotion.SDK.Services.SaveSystem.Progress;
using MineArena.Helpers;
using MineArena.Messages.MessageService;
using UnityEngine;

namespace Devotion.SDK.Services.SaveSystem
{
    public class SaveService : BaseService, ISaveService,
        IMessageSubscriber<Messages.Player.SavePlayerProgress>
    {
        private static SaveService _instance;
        public static SaveService Instance => _instance.IsNullOrDead() ? _instance = new SaveService() : _instance;

        private static readonly FieldInfo PlayerProgressField =
            typeof(GameRoot).GetField("playerProgress", BindingFlags.Instance | BindingFlags.NonPublic);

        private ISaveProvider _platformProvider;


        public override IPromise Initialize()
        {
            MessageService.Subscribe(this);

            ServiceLocator.Register<ISaveService>(this);

#if DEVOTION_YANDEX
            _platformProvider = new YandexSaveProvider();
#elif DEVOTION_CRAZYGAMES
            _platformProvider = new CrazyGamesProvider();
#elif DEVOTION_GOOGLEPLAY
            _platformProvider = new GooglePlayProvider();
#else
            _platformProvider = new DesktopSaveProvider();
#endif

            return base.Initialize().Then(() => Load());
        }

        public IPromise Load()
        {
            var loadPromise = _platformProvider.Load(Constants.PlayerSaveKey);

            loadPromise.Catch(ex =>
            {
                Debug.LogError($"[SaveService] Failed to load player progress: {ex}");
            });

            return loadPromise.Then(rawData =>
            {
                ApplyLoadedProgress(rawData);
                return Promise.ResolveAndReturn();
            });
        }

        public IPromise Save()
        {
            var progress = EnsurePlayerProgress();
            if (progress == null)
            {
                return Promise.RejectAndReturn(new Exception("GameRoot is not initialized."));
            }

            var serialized = JsonUtility.ToJson(progress, false);
            return _platformProvider.Save(Constants.PlayerSaveKey, serialized);
        }

        public void OnMessage(Messages.Player.SavePlayerProgress message)
        {
            Save().Catch(ex =>
            {
                Debug.LogError($"[SaveService] Failed to save player progress: {ex}");
            });
        }

        private PlayerProgress EnsurePlayerProgress()
        {
            var gameRoot = GameRoot.Instance;
            if (gameRoot.IsNullOrDead())
            {
                Debug.LogError("[SaveService] GameRoot instance is not initialized yet.");
                return null;
            }

            if (PlayerProgressField == null)
            {
                Debug.LogError("[SaveService] Unable to access player progress field on GameRoot.");
                return null;
            }

            var progress = PlayerProgressField.GetValue(gameRoot) as PlayerProgress;
            if (progress == null)
            {
                progress = new PlayerProgress(Constants.PlayerSaveKey);
                PlayerProgressField.SetValue(gameRoot, progress);
            }

            return progress;
        }

        private void ApplyLoadedProgress(string rawData)
        {
            var progress = EnsurePlayerProgress();
            if (progress == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(rawData))
            {
                return;
            }

            try
            {
                JsonUtility.FromJsonOverwrite(rawData, progress);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveService] Failed to deserialize player progress: {ex}");
            }
        }
    }
}

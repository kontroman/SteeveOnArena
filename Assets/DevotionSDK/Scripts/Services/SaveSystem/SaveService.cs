using Devotion.SDK.Async;
using Devotion.SDK.Controllers;
using Devotion.SDK.DataStructures;
using Devotion.SDK.Interfaces;
using MineArena.Helpers;
using MineArena.Messages.MessageService;
using System;

namespace Devotion.SDK.Services.SaveSystem
{
    public class SaveService : BaseService, ISaveService,
        IMessageSubscriber<Messages.Player.SavePlayerProgress>
    {
        private ISaveProvider _platformProvider;

        public override IPromise Initialize()
        {
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

            _platformProvider.Load();

            return Promise.ResolveAndReturn();
        }

        public void Load()
        {
            
        }

        public void OnMessage(Messages.Player.SavePlayerProgress message)
        {
            Save();
        }

        private IPromise Save()
        {
            return GameRoot.PlayerProgress.IsNullOrDead()
                            ? Promise.RejectAndReturn(new Exception("No player progress"))
                            : _platformProvider.Save(Constants.PlayerSaveKey, GameRoot.PlayerProgress.ToJson());
        }
    }
}
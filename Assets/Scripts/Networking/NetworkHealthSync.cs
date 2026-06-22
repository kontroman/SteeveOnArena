using UnityEngine;

namespace MineArena.Networking
{
    [DisallowMultipleComponent]
    public class NetworkHealthSync : MonoBehaviour
    {
        private NetworkPlayerView view;

        public void Bind(NetworkPlayerView playerView)
        {
            view = playerView;
        }

        public void PlayerHealthUpdate(HealthUpdate update)
        {
            if (update == null || view == null)
                return;

            view.ApplyHealth(update.health, update.isAlive);
        }

        public void PlayerDamaged(DamageEventMessage damage)
        {
            if (damage == null || view == null)
                return;

            view.ApplyHealth(damage.healthAfter, damage.healthAfter > 0);
        }

        public void PlayerDied(DeathEventMessage death)
        {
            if (death == null || view == null)
                return;

            view.ApplyDeath(death.killerPlayerId);
        }

        public void PlayerRespawned(RespawnEventMessage respawn)
        {
            if (respawn == null || view == null)
                return;

            view.ApplyRespawn(respawn.transform, respawn.health);
        }
    }
}

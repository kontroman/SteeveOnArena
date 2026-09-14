using UnityEngine;

namespace MineArena
{
    public class Arrow : Projectile
    {
        private TrailRenderer _flightTrail;
        private Material _trailMaterial;

        protected override void OnPlayerFlightStarted()
        {
            if (_flightTrail == null)
            {
                var shader = Shader.Find("Sprites/Default");
                if (shader == null) return;
                _trailMaterial = new Material(shader);
                _flightTrail = gameObject.AddComponent<TrailRenderer>();
                _flightTrail.sharedMaterial = _trailMaterial;
                _flightTrail.time = 0.065f;
                _flightTrail.startWidth = 0.045f;
                _flightTrail.endWidth = 0f;
                _flightTrail.startColor = new Color(1f, 0.9f, 0.65f, 0.6f);
                _flightTrail.endColor = new Color(1f, 0.9f, 0.65f, 0f);
                _flightTrail.minVertexDistance = 0.1f;
                _flightTrail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                _flightTrail.receiveShadows = false;
            }
            _flightTrail.Clear();
            _flightTrail.emitting = true;
        }

        protected override void OnPlayerCollision(bool hitTarget)
        {
            if (_flightTrail != null) _flightTrail.emitting = false;
            if (Devotion.SDK.Controllers.GameRoot.Instance != null)
                Devotion.SDK.Controllers.GameRoot.GetManager<Managers.AudioManager>()?.PlayEffect(
                    hitTarget ? "ArrowHitTarget" : "ArrowHitSurface", 0.65f);
        }

        protected override void ResetFlightFeedback()
        {
            if (_flightTrail == null) return;
            _flightTrail.emitting = false;
            _flightTrail.Clear();
        }

        private void OnDestroy()
        {
            if (_trailMaterial == null) return;
            if (Application.isPlaying) Destroy(_trailMaterial);
            else DestroyImmediate(_trailMaterial);
        }
    }
}

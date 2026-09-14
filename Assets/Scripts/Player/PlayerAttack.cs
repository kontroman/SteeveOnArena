using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MineArena.Interfaces;
using MineArena.Structs;
using MineArena.Commands;
using Devotion.SDK.Helpers;
using MineArena.Managers;
using Devotion.SDK.Controllers;
using MineArena.Messages.MessageService;
using MineArena.Messages;
using MineArena.Controllers;
using MineArena.Items;
using MineArena.ObjectPools;
using UnityEngine.EventSystems;
using MineArena.Networking;
using MineArena.Basics;

namespace MineArena.PlayerSystem
{
    [RequireComponent(typeof(Animator))]
    public class PlayerAttack : MonoBehaviour,
        IMessageSubscriber<GameMessages.NewSwordEquiped>
    {
        [SerializeField] private PlayerAnimatorController _animatorController;
        [SerializeField] private string _attackStateName = "Attack";
        [SerializeField] private int _attackLayerIndex = 1;
        [SerializeField] private float _attackStateFailSafe = 1.5f;
        [SerializeField] private string _bowStateName = "BowShooting";
        [SerializeField] private int _bowLayerIndex = 1;
        [SerializeField] private float _bowStateFailSafe = 1.5f;

        [Header("Configuration")]
        [SerializeField] private AttackConfig _config;
        [SerializeField] private PlayerEquipment _equipment;
        [SerializeField, Min(0f)] private float _minimumNetworkAttackCooldown = 0.45f;
        [SerializeField, Min(0f)] private float _maximumNetworkMeleeRange = 4f;

        [Header("Melee Area")]
        [SerializeField, Min(0f)] private float attackRange = 3f;
        [SerializeField, Min(0f)] private float startHalfWidth = 0.8f;
        [SerializeField, Min(0f)] private float endHalfWidth = 1.8f;
        [SerializeField] private bool includeBehindPlayerCircle = true;
        [SerializeField, Min(0f)] private float nearPlayerRadius = 0.8f;

        [Header("Bow")]
        [SerializeField] private AttackConfig _defaultBowAttack;
        [SerializeField] private GameObject _arrowProjectilePrefab;
        [SerializeField] private Transform _bowFirePoint;
        [SerializeField] private string _arrowResourcePath = "Prefabs/Objects/Projectile/Arrow";
        [SerializeField] private float _bowFallbackDamage = 25f;
        [SerializeField] private float _bowFallbackCooldown = 0.8f;
        [SerializeField] private float _bowFallbackAnimationDelay = 0.2f;
        [SerializeField] private float _bowFallbackRange = 60f;
        [SerializeField] private LayerMask _bowFallbackAttackableLayers = 1 << 8;
        [SerializeField] private float _bowAimRaycastDistance = 500f;
        [SerializeField, Range(0.1f, 1f)] private float _meleeMovementMultiplier = 0.7f;
        [SerializeField, Range(0f, 0.3f)] private float _bowInputBuffer = 0.15f;
        private float _bufferedBowClickUntil = float.NegativeInfinity;
        private Transform _bowRecoilArm;
        private Quaternion _armRotationBeforeRecoil;
        private bool _recoilApplied;
        private float _bowReleaseTime = float.NegativeInfinity;

        private float _nextAttackTime;
        private ICommand _damageCommand;
        private bool _isEnabled;
        private bool _isAttacking;
        private bool _meleeActive;
        private bool _meleeHitResolved;
        private bool _meleeSoundPlayed;
        private IPlayerAnimator _animator;
        private Animator _rawAnimator;
        private GameObject _cachedArrowProjectilePrefab;
        private AttackConfig _runtimeBowAttackConfig;
        private AttackConfig _pendingBowConfig;
        private Vector3 _pendingBowTargetPoint;
        private bool _hasPendingBowShot;
        private bool _bowShotReleased;

        public bool IsAttacking => _isAttacking;
        public float MovementMultiplier => _meleeActive ? _meleeMovementMultiplier : 1f;

        private void Awake()
        {
            MessageService.Subscribe(this);
            _isEnabled = true;

            _damageCommand = ScriptableObject.CreateInstance<DamageCommand>();

            _animator = _animatorController ?? GetComponent<IPlayerAnimator>();
            _rawAnimator = (_animator as PlayerAnimatorController)?.Animator ?? GetComponent<Animator>();

            if (_equipment == null)
                _equipment = GetComponent<PlayerEquipment>();
        }

        private void OnDestroy()
        {
            MessageService.Unsubscribe(this);
        }

        private void OnDisable()
        {
            RestoreBowRecoil();
            _bowReleaseTime = float.NegativeInfinity;
            StopAllCoroutines();
            _isAttacking = false;
            _meleeActive = false;
            ClearPendingBowShot();
            _bufferedBowClickUntil = float.NegativeInfinity;
        }

        private void Update()
        {
            RestoreBowRecoil();
            bool bowSelected = _equipment != null && _equipment.LastActiveHandItem == HandItemType.Bow && IsBowSelectedInQuickSlot();
            if ((GetComponent<PlayerShield>()?.WantsToBlock ?? false) || !_isEnabled || MineArena.Managers.TutorialService.BlocksInput ||
                (MineArena.Managers.TutorialService.Active && MineArena.Managers.TutorialService.Progress.Step != MineArena.Managers.TutorialStep.Kill) || IsPointerOverUi())
            {
                _bufferedBowClickUntil = float.NegativeInfinity;
                return;
            }

            if (!bowSelected) _bufferedBowClickUntil = float.NegativeInfinity;
            else if (Inputs.LKMPressed) _bufferedBowClickUntil = Time.time + _bowInputBuffer;

            if (_hasPendingBowShot && !_bowShotReleased && bowSelected)
                UpdateBowAim();

            if (_isAttacking) return;

            if ((!Inputs.LKMPressed && !(bowSelected && Time.time <= _bufferedBowClickUntil)) || Time.time < _nextAttackTime)
                return;

            if (PotionEffects.SelectedPotion != null)
            {
                PotionEffects.TryDrinkSelected();
                return;
            }

            if (_equipment != null && _equipment.LastActiveHandItem == HandItemType.Bow)
            {
                if (IsBowSelectedInQuickSlot())
                {
                    _bufferedBowClickUntil = float.NegativeInfinity;
                    StartCoroutine(BowAttackRoutine());
                }

                return;
            }

            Vector3 targetPoint = GetAttackClickTargetPoint();

            StartCoroutine(AttackRoutine(targetPoint));
        }

        private static bool IsPointerOverUi()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }

        private Vector3 GetAttackClickTargetPoint()
        {
            var camera = Camera.main;
            if (camera == null)
                return transform.position + transform.forward;

            return ResolveAttackAim(camera.ScreenPointToRay(Input.mousePosition), transform.position, 500f);
        }

        private Vector3 ResolveAttackAim(Ray ray, Vector3 origin, float maxDistance)
        {
            var hits = Physics.RaycastAll(ray, maxDistance, ~0, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));
            foreach (var hit in hits)
            {
                if (hit.collider == null || IsOwnerCollider(hit.collider) || ShouldIgnoreAttackClick(hit.collider)) continue;
                // Scenery must not bend screen-space aiming toward a roof or wall.
                if (hit.collider.GetComponentInParent<MineArena.AI.MobHealth>() != null ||
                    hit.collider.GetComponentInParent<NetworkPlayerView>() != null)
                    return hit.collider.bounds.center;
            }
            var plane = new Plane(Vector3.up, origin);
            if (plane.Raycast(ray, out float distance)) return ray.GetPoint(distance);
            return origin + transform.forward * maxDistance;
        }
        private IEnumerator AttackRoutine(Vector3 targetPoint)
        {
            _isAttacking = true;

            Vector3 attackDirection = targetPoint - transform.position;
            attackDirection.y = 0f;
            attackDirection = attackDirection.sqrMagnitude > 0.0001f ? attackDirection.normalized : transform.forward;

            float rotationDuration = 0.07f;
            var rotationController = Player.Instance.GetComponentFromList<RotationController>();

            if (rotationController != null)
            {
                rotationController.RotateToDirection(attackDirection, 2, rotationDuration);

            }

            if (_equipment != null)
            {
                _equipment.SetActiveHandItem(HandItemType.Sword);
                var swordAttack = _equipment.GetSwordAttackConfig();
                if (swordAttack != null)
                    _config = swordAttack;
            }

            yield return PerformAttack();
        }

        private IEnumerator PerformAttack()
        {
            var activeConfig = _config ?? _equipment?.GetSwordAttackConfig();
            if (activeConfig == null)
            {
                _isAttacking = false;
                yield break;
            }

            _config = activeConfig;

            _nextAttackTime = Time.time + GetAttackCooldown(_config);

            _meleeActive = true;
            _meleeHitResolved = false;
            _meleeSoundPlayed = false;
            try
            {
                _animator?.TriggerAttack();
                if (_rawAnimator == null || _rawAnimator.runtimeAnimatorController == null)
                {
                    HandleMeleeSoundKeyframe();
                    yield return new WaitForSeconds(activeConfig.AnimationDelay);
                    HandleMeleeHitKeyframe();
                }
                yield return WaitForAttackAnimation(activeConfig);
            }
            finally
            {
                _isAttacking = false;
                _meleeActive = false;
            }
        }

        private IEnumerator BowAttackRoutine()
        {
            _isAttacking = true;

            var bowConfig = GetBowAttackConfig();
            if (bowConfig == null)
            {
                _isAttacking = false;
                yield break;
            }

            _nextAttackTime = Time.time + GetAttackCooldown(bowConfig);

            var firePoint = GetBowFirePoint();
            var targetPoint = GetBowTargetPoint(firePoint.position, bowConfig);
            var shotDirection = targetPoint - firePoint.position;

            if (shotDirection.sqrMagnitude <= 0.0001f)
                shotDirection = transform.forward;

            var horizontalDirection = shotDirection;
            horizontalDirection.y = 0f;
            if (horizontalDirection.sqrMagnitude > 0.0001f)
                Player.Instance.GetComponentFromList<RotationController>()?.RotateToDirection(horizontalDirection, 2, 0.08f);

            PreparePendingBowShot(bowConfig, targetPoint);
            _animator?.TriggerBowShoot();

            try
            {
                yield return WaitForAnimationState(
                    _bowStateName,
                    _bowLayerIndex,
                    Mathf.Max(_bowStateFailSafe, bowConfig.AnimationDelay),
                    waitForNewPlayback: true
                );
            }
            finally
            {
                ClearPendingBowShot();
                _isAttacking = false;
            }
        }

        public void HandleMeleeHitKeyframe()
        {
            if (!_isEnabled || !_isAttacking || !_meleeActive || _meleeHitResolved) return;
            _meleeHitResolved = true;
            DetectHits();
        }

        // The clip event leads contact by 90 ms at the authored attack speed (2.2).
        // SwordAttack's measured acoustic peak then coincides with the damage event.
        public void HandleMeleeSoundKeyframe()
        {
            if (!_isEnabled || !_isAttacking || !_meleeActive || _meleeSoundPlayed) return;
            _meleeSoundPlayed = true;
            GameRoot.GetManager<AudioManager>()?.PlayEffect(Constants.AudioNames.SwrdAttack);
        }

        private void UpdateBowAim()
        {
            var origin = GetBowFirePoint().position;
            _pendingBowTargetPoint = GetBowTargetPoint(origin, _pendingBowConfig);
            GetComponent<RotationController>()?.RotateToDirection(_pendingBowTargetPoint - origin, 2, 0.08f);
        }

        private void LateUpdate()
        {
            float elapsed = Time.time - _bowReleaseTime;
            if (!_isEnabled || _bowRecoilArm == null || elapsed < 0f || elapsed >= 0.14f) return;
            _armRotationBeforeRecoil = _bowRecoilArm.localRotation;
            _bowRecoilArm.localRotation *= Quaternion.Euler(-4f * Mathf.Sin(elapsed / 0.14f * Mathf.PI), 0f, 0f);
            _recoilApplied = true;
        }

        private void RestoreBowRecoil()
        {
            if (_recoilApplied && _bowRecoilArm != null)
                _bowRecoilArm.localRotation = _armRotationBeforeRecoil;
            _recoilApplied = false;
        }

        public void HandleBowShootKeyframe()
        {
            if (!_isAttacking || !_hasPendingBowShot || _bowShotReleased)
                return;

            _bowShotReleased = true;

            // Resolve once more at the animation event, after the last input update.
            if (_equipment != null && Camera.main != null && !IsPointerOverUi())
                UpdateBowAim();
            if (GameRoot.Instance != null)
                GameRoot.GetManager<AudioManager>()?.PlayEffect(Constants.AudioNames.BowAttack);
            _bowRecoilArm = FindChildTransform("bow")?.parent;
            _bowReleaseTime = Time.time;

            var firePoint = GetBowFirePoint();
            var shotDirection = _pendingBowTargetPoint - firePoint.position;
            if (shotDirection.sqrMagnitude <= 0.0001f)
                shotDirection = transform.forward;

            SpawnBowProjectile(_pendingBowConfig, firePoint.position, shotDirection.normalized);
        }

        private void DetectHits()
        {
            Vector3 attackOrigin = GetAttackOrigin();
            Vector3 attackForward = GetFlatAttackForward();

            // Remote players use the regular Player layer, while PvE targets use AttackableLayers.
            // Query all nearby colliders, then keep the existing mask for non-network targets.
            Collider[] hits = Physics.OverlapSphere(
                attackOrigin,
                GetMeleeCandidateRadius(),
                ~0,
                QueryTriggerInteraction.Collide);
            var networkTargets = new HashSet<int>();
            var localTargets = new HashSet<IDamageable>();

            foreach (Collider hit in hits)
            {
                if (hit == null || IsOwnerCollider(hit))
                    continue;

                if (!IsWithinMeleeArea(hit, attackOrigin, attackForward))
                    continue;

                var networkView = hit.GetComponentInParent<NetworkPlayerView>();
                if (networkView != null)
                {
                    var withinServerRange = Vector3.Distance(transform.position, networkView.transform.position) <=
                                            _maximumNetworkMeleeRange;
                    if (!networkView.IsLocalPlayer && networkView.IsAlive && withinServerRange &&
                        networkTargets.Add(networkView.PlayerId))
                    {
                        SendNetworkDamage(
                            networkView,
                            GetMeleeDamage(_config),
                            GetSelectedWeaponId("sword"),
                            hit.ClosestPoint(attackOrigin));
                    }

                    continue;
                }

                var damageable = hit.GetComponentInParent<IDamageable>();
                int targetLayer = damageable is Component targetComponent ? targetComponent.gameObject.layer : hit.gameObject.layer;
                if ((_config.AttackableLayers.value & (1 << targetLayer)) == 0)
                    continue;
                if (damageable is Component body && !MineArena.AI.CombatTargeting.HasLineOfSight(
                    attackOrigin, hit.bounds.center, transform, body.transform)) continue;
                if (damageable != null && localTargets.Add(damageable))
                    _damageCommand.Execute(new DamageData(GetMeleeDamage(_config), damageable));
            }
        }

        private float GetMeleeCandidateRadius()
        {
            float range = Mathf.Max(0f, attackRange);
            float farCornerDistance = Mathf.Sqrt(range * range + endHalfWidth * endHalfWidth);
            return Mathf.Max(farCornerDistance, startHalfWidth, nearPlayerRadius);
        }

        private Vector3 GetFlatAttackForward()
        {
            Vector3 forward = transform.forward;
            forward.y = 0f;

            return forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector3.forward;
        }

        private bool IsWithinMeleeArea(Collider hit, Vector3 attackOrigin, Vector3 attackForward)
        {
            if (hit == null)
                return false;

            return IsPointWithinMeleeArea(hit.transform.position, attackOrigin, attackForward) ||
                   IsPointWithinMeleeArea(hit.ClosestPoint(attackOrigin), attackOrigin, attackForward);
        }

        private bool IsPointWithinMeleeArea(Vector3 worldPoint, Vector3 attackOrigin, Vector3 attackForward)
        {
            // Truncated cone / wide cone attack area in the XZ plane.
            Vector3 toTarget = worldPoint - attackOrigin;
            toTarget.y = 0f;

            if (includeBehindPlayerCircle && toTarget.sqrMagnitude <= nearPlayerRadius * nearPlayerRadius)
                return true;

            float range = Mathf.Max(0.0001f, attackRange);
            float distanceForward = Vector3.Dot(toTarget, attackForward);
            if (distanceForward < 0f || distanceForward > range)
                return false;

            Vector3 sideOffset = toTarget - attackForward * distanceForward;
            float allowedHalfWidth = Mathf.Lerp(startHalfWidth, endHalfWidth, distanceForward / range);
            return sideOffset.sqrMagnitude <= allowedHalfWidth * allowedHalfWidth;
        }

        private bool IsBowSelectedInQuickSlot()
        {
            var progress = GameRoot.PlayerProgress?.InventoryProgress;
            if (progress == null)
                return false;

            var selectedItemId = progress.GetQuickSlotItemId(progress.SelectedQuickSlotIndex);
            if (string.IsNullOrWhiteSpace(selectedItemId))
                return false;

            var itemConfig = GameRoot.GameConfig?.ItemDatabase?.GetItemConfig(selectedItemId);
            if (itemConfig is WeaponItemConfig weaponConfig)
                return weaponConfig.Kind == WeaponItemKind.Bow;

            return selectedItemId.IndexOf("Bow", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private AttackConfig GetBowAttackConfig()
        {
            var progress = GameRoot.PlayerProgress?.InventoryProgress;
            var selectedItemId = progress != null ? progress.GetQuickSlotItemId(progress.SelectedQuickSlotIndex) : null;
            var itemConfig = GameRoot.GameConfig?.ItemDatabase?.GetItemConfig(selectedItemId);

            if (itemConfig is WeaponItemConfig weaponConfig && weaponConfig.Kind == WeaponItemKind.Bow && weaponConfig.AttackConfig != null)
                return weaponConfig.AttackConfig;

            var equipmentBowConfig = _equipment != null ? _equipment.GetBowAttackConfig() : null;
            if (equipmentBowConfig != null)
                return equipmentBowConfig;

            if (_defaultBowAttack != null)
                return _defaultBowAttack;

            _runtimeBowAttackConfig ??= CreateRuntimeBowAttackConfig();
            return _runtimeBowAttackConfig;
        }

        private AttackConfig CreateRuntimeBowAttackConfig()
        {
            var config = ScriptableObject.CreateInstance<AttackConfig>();
            config.BaseDamage = _bowFallbackDamage;
            config.Radius = _bowFallbackRange;
            config.Angle = 1f;
            config.Cooldown = _bowFallbackCooldown;
            config.AnimationDelay = _bowFallbackAnimationDelay;
            config.AttackableLayers = _bowFallbackAttackableLayers;
            return config;
        }

        private Transform GetBowFirePoint()
        {
            if (_bowFirePoint != null)
                return _bowFirePoint;

            var bow = FindChildTransform("bow");
            if (bow != null)
                return bow;

            return transform;
        }

        private Vector3 GetBowTargetPoint(Vector3 origin, AttackConfig bowConfig)
        {
            var maxDistance = Mathf.Max(1f, bowConfig.Radius, _bowAimRaycastDistance);
            var camera = Camera.main;

            if (camera == null)
                return origin + transform.forward * maxDistance;

            return ResolveBowAim(camera.ScreenPointToRay(Input.mousePosition), origin, maxDistance);
        }

        private Vector3 ResolveBowAim(Ray ray, Vector3 origin, float maxDistance)
        {
            var hits = Physics.RaycastAll(ray, maxDistance, ~0, QueryTriggerInteraction.Collide);
            System.Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));
            foreach (var hit in hits)
            {
                var collider = hit.collider;
                if (collider == null || IsOwnerCollider(collider) || ShouldIgnoreAttackClick(collider) ||
                    collider.GetComponentInParent<Projectile>() != null) continue;

                // Keep low targets hittable, but stop aiming through solid scenery.
                if (collider.GetComponentInParent<MineArena.AI.MobHealth>() != null ||
                    collider.GetComponentInParent<NetworkPlayerView>() != null)
                    return collider.bounds.center;

                // Enemy hurtboxes may be triggers (for example Zombie). Other
                // triggers are interaction/area volumes, not aiming surfaces.
                if (!collider.isTrigger) return hit.point;
            }

            // Empty space uses foot height, never the animated bow's height.
            float groundHeight = TryGetComponent<CharacterController>(out var body)
                ? body.bounds.min.y : transform.position.y;
            var groundPlane = new Plane(Vector3.up, new Vector3(origin.x, groundHeight, origin.z));
            if (groundPlane.Raycast(ray, out float distance) && distance <= maxDistance)
                return ray.GetPoint(distance);
            return origin + transform.forward * maxDistance;
        }
        private void SpawnBowProjectile(AttackConfig bowConfig, Vector3 origin, Vector3 direction)
        {
            var projectilePrefab = GetArrowProjectilePrefab();
            if (projectilePrefab == null)
            {
                ApplyBowRaycastDamage(bowConfig, origin, direction);
                return;
            }

            var projectileObject = GetProjectileObject(projectilePrefab);
            if (projectileObject == null)
            {
                ApplyBowRaycastDamage(bowConfig, origin, direction);
                return;
            }

            projectileObject.transform.position = origin;

            var projectile = projectileObject.GetComponent<Projectile>();
            if (projectile == null)
            {
                Destroy(projectileObject);
                ApplyBowRaycastDamage(bowConfig, origin, direction);
                return;
            }

            projectile.SetParameters(
                direction,
                new DamageData(GetBowDamage(bowConfig), null),
                bowConfig.AttackableLayers,
                transform,
                GetSelectedWeaponId("bow")
            );
        }

        private void PreparePendingBowShot(AttackConfig bowConfig, Vector3 targetPoint)
        {
            _pendingBowConfig = bowConfig;
            _pendingBowTargetPoint = targetPoint;
            _hasPendingBowShot = true;
            _bowShotReleased = false;
        }

        private void ClearPendingBowShot()
        {
            _pendingBowConfig = null;
            _pendingBowTargetPoint = default;
            _hasPendingBowShot = false;
            _bowShotReleased = false;
        }

        private GameObject GetArrowProjectilePrefab()
        {
            if (_arrowProjectilePrefab != null)
                return _arrowProjectilePrefab;

            if (_cachedArrowProjectilePrefab != null)
                return _cachedArrowProjectilePrefab;

            if (string.IsNullOrWhiteSpace(_arrowResourcePath))
                return null;

            _cachedArrowProjectilePrefab = Resources.Load<GameObject>(_arrowResourcePath);
            return _cachedArrowProjectilePrefab;
        }

        private static GameObject GetProjectileObject(GameObject projectilePrefab)
        {
            var projectile = projectilePrefab.GetComponent<Projectile>();
            if (projectile == null)
                return null;

            var projectileType = projectile.GetType();
            if (ObjectPoolsManager.Instance != null && ObjectPoolsManager.Instance.HasPool(projectileType))
                return ObjectPoolsManager.Instance.Get(projectileType);

            return Instantiate(projectilePrefab);
        }

        private void ApplyBowRaycastDamage(AttackConfig bowConfig, Vector3 origin, Vector3 direction)
        {
            var hits = Physics.RaycastAll(origin, direction, bowConfig.Radius, ~0, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));

            foreach (var hit in hits)
            {
                if (hit.collider == null || IsOwnerCollider(hit.collider))
                    continue;

                var networkView = hit.collider.GetComponentInParent<NetworkPlayerView>();
                if (networkView != null)
                {
                    if (!networkView.IsLocalPlayer && networkView.IsAlive)
                    {
                        SendNetworkDamage(
                            networkView,
                            GetBowDamage(bowConfig),
                            GetSelectedWeaponId("bow"),
                            hit.point);
                    }

                    return;
                }

                if ((bowConfig.AttackableLayers.value & (1 << hit.collider.gameObject.layer)) != 0)
                {
                    var damageable = hit.collider.GetComponentInParent<IDamageable>();
                    if (damageable != null)
                        _damageCommand.Execute(new DamageData(GetBowDamage(bowConfig), damageable));
                }

                // The first non-owner collider blocks the shot even when it is not damageable.
                return;
            }
        }

        private static void SendNetworkDamage(NetworkPlayerView target, float damage, string weaponId, Vector3 hitPoint)
        {
            var manager = NetworkClientManager.Instance;
            if (manager == null || !manager.IsConnected || target == null)
                return;

            manager.SendDamageRequest(
                target.PlayerId,
                Mathf.Max(1, Mathf.RoundToInt(damage)),
                weaponId,
                hitPoint);
        }

        private static string GetSelectedWeaponId(string fallback)
        {
            var progress = GameRoot.PlayerProgress?.InventoryProgress;
            var selectedItemId = progress != null
                ? progress.GetQuickSlotItemId(progress.SelectedQuickSlotIndex)
                : null;

            return string.IsNullOrWhiteSpace(selectedItemId) ? fallback : selectedItemId;
        }

        private float GetAttackCooldown(AttackConfig attackConfig)
        {
            var cooldown = attackConfig != null ? attackConfig.Cooldown : 0f;
            var manager = NetworkClientManager.Instance;
            return manager != null && manager.IsConnected
                ? Mathf.Max(cooldown, _minimumNetworkAttackCooldown)
                : cooldown;
        }

        private float GetMeleeDamage(AttackConfig attackConfig)
        {
            return GetConfiguredDamage(attackConfig);
        }

        private float GetBowDamage(AttackConfig bowConfig)
        {
            return GetConfiguredDamage(bowConfig);
        }

        private float GetConfiguredDamage(AttackConfig attackConfig)
        {
            var development = GetComponent<PlayerDevelopment>();
            var damageToDeal = development != null ? development.ModifyAttack(attackConfig.BaseDamage) : attackConfig.BaseDamage;

#if UNITY_EDITOR || DEVOTION_GODMODE
            var config = GameRoot.GameConfig;
            if (config != null && config.GodModeOneHitKill)
                damageToDeal = 9999999f;
#endif

            return damageToDeal;
        }

        private Transform FindChildTransform(string childName)
        {
            if (string.IsNullOrWhiteSpace(childName))
                return null;

            foreach (var child in GetComponentsInChildren<Transform>(true))
            {
                if (child != null && string.Equals(child.name, childName, System.StringComparison.OrdinalIgnoreCase))
                    return child;
            }

            return null;
        }

        private bool IsOwnerCollider(Collider hitCollider)
        {
            if (hitCollider == null)
                return false;

            var hitTransform = hitCollider.transform;
            return hitTransform == transform || hitTransform.IsChildOf(transform);
        }

        private static bool ShouldIgnoreAttackClick(Collider hitCollider)
        {
            return hitCollider != null && hitCollider.GetComponentInParent<IgnoreAttackClickTarget>() != null;
        }

        private void OnDrawGizmosSelected()
        {
            if (_config == null)
                return;

            Vector3 attackOrigin = GetAttackOrigin();
            Vector3 forward = GetFlatAttackForward();
            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
            float range = Mathf.Max(0f, attackRange);

            Vector3 nearLeft = attackOrigin - right * startHalfWidth;
            Vector3 nearRight = attackOrigin + right * startHalfWidth;
            Vector3 farCenter = attackOrigin + forward * range;
            Vector3 farLeft = farCenter - right * endHalfWidth;
            Vector3 farRight = farCenter + right * endHalfWidth;

            Gizmos.color = new Color(1, 0, 0, 0.3f);
            Gizmos.DrawLine(nearLeft, nearRight);
            Gizmos.DrawLine(farLeft, farRight);
            Gizmos.DrawLine(nearLeft, farLeft);
            Gizmos.DrawLine(nearRight, farRight);
            Gizmos.DrawLine(attackOrigin, farCenter);

            if (includeBehindPlayerCircle && nearPlayerRadius > 0f)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(attackOrigin, nearPlayerRadius);
            }
        }

        private Vector3 GetAttackOrigin()
        {
            if (TryGetComponent<CharacterController>(out var characterController))
            {
                return characterController.bounds.center;
            }

            if (TryGetComponent<Collider>(out var collider))
            {
                return collider.bounds.center;
            }

            return transform.position;
        }

        public void SetComponentEnable(bool value)
        {
            _isEnabled = value;
            if (!value)
            {
                RestoreBowRecoil();
                _bowReleaseTime = float.NegativeInfinity;
                StopAllCoroutines();
                _isAttacking = false;
                _hasPendingBowShot = false;
                _bufferedBowClickUntil = float.NegativeInfinity;
                _meleeActive = false;
            }
        }

        public void OnMessage(GameMessages.NewSwordEquiped message)
        {
            _config = message.Model;
        }

        private IEnumerator WaitForAttackAnimation(AttackConfig activeConfig)
        {
            yield return WaitForAnimationState(
                _attackStateName,
                _attackLayerIndex,
                Mathf.Max(_attackStateFailSafe, activeConfig?.AnimationDelay ?? 0f)
            );
        }

        private IEnumerator WaitForAnimationState(string stateName, int preferredLayerIndex, float failSafeTime, bool waitForNewPlayback = false)
        {
            if (_rawAnimator == null || string.IsNullOrWhiteSpace(stateName))
            {
                yield return null;
                yield break;
            }

            int preferredLayer = Mathf.Clamp(preferredLayerIndex, 0, _rawAnimator.layerCount - 1);
            int activeLayer = -1;

            bool stateStarted = false;
            float elapsed = 0f;
            int previousLayer = waitForNewPlayback ? FindAnimationStateLayer(preferredLayer, stateName) : -1;
            var previousState = previousLayer >= 0 ? _rawAnimator.GetCurrentAnimatorStateInfo(previousLayer) : default;
            bool waitingForRestart = previousLayer >= 0 && previousState.IsName(stateName);

            while (true)
            {
                if (activeLayer < 0)
                    activeLayer = FindAnimationStateLayer(preferredLayer, stateName);

                if (activeLayer >= 0)
                {
                    var info = _rawAnimator.GetCurrentAnimatorStateInfo(activeLayer);
                    bool currentState = info.IsName(stateName);
                    bool nextState = _rawAnimator.IsInTransition(activeLayer) &&
                                     _rawAnimator.GetNextAnimatorStateInfo(activeLayer).IsName(stateName);

                    // SetTrigger is evaluated later by Animator. The outgoing shot can
                    // still be current here; its completion does not finish the new shot.
                    if (waitingForRestart)
                    {
                        if (!currentState || info.normalizedTime < previousState.normalizedTime || nextState)
                            waitingForRestart = false;
                    }

                    if (!waitingForRestart && (currentState || nextState))
                    {
                        if (!stateStarted && waitForNewPlayback) elapsed = 0f;
                        stateStarted = true;

                        if (currentState && info.normalizedTime >= 1f && !_rawAnimator.IsInTransition(activeLayer))
                            break;
                    }
                    else if (stateStarted)
                    {
                        break;
                    }
                }

                elapsed += Time.deltaTime;
                if (elapsed >= failSafeTime)
                    break;

                yield return null;
            }
        }

        private int FindAnimationStateLayer(int preferredLayer, string stateName)
        {
            if (IsAnimationStateActive(preferredLayer, stateName))
                return preferredLayer;

            for (int layer = 0; layer < _rawAnimator.layerCount; layer++)
            {
                if (layer == preferredLayer)
                    continue;

                if (IsAnimationStateActive(layer, stateName))
                    return layer;
            }

            return -1;
        }

        private bool IsAnimationStateActive(int layer, string stateName)
        {
            if (_rawAnimator.GetCurrentAnimatorStateInfo(layer).IsName(stateName))
                return true;

            return _rawAnimator.IsInTransition(layer) &&
                   _rawAnimator.GetNextAnimatorStateInfo(layer).IsName(stateName);
        }
    }
}

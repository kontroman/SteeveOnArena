using MineArena.Structs;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MineArena.PlayerSystem
{
    // LateUpdate overlays the animated left arm without replacing locomotion clips.
    [DefaultExecutionOrder(100)]
    public sealed class PlayerShield : MonoBehaviour
    {
        private PlayerEquipment _equipment;
        private PlayerAttack _attack;
        private Transform _arm, _forearm, _hand, _visual;
        private Items.ArmorConfig _visualConfig;
        private float _raise, _impact;
        private Quaternion _baseArmRotation;
        private bool _posed, _blockHeld;
        private static AudioClip _blockSound, _breakSound;
        public bool WantsToBlock => isActiveAndEnabled && _equipment != null && _equipment.Shield != null
            && _equipment.LastActiveHandItem != HandItemType.Bow && !PlayerMovement.IsPlayerDead
            && Time.timeScale > 0 && !Managers.TutorialService.BlocksInput
            && (MineArena.UI.MobileGameInput.Enabled ? MineArena.UI.MobileGameInput.ShieldHeld : Input.GetMouseButton(1)) && Application.isFocused
            && (MineArena.UI.MobileGameInput.Enabled || EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject())
            && !(Devotion.SDK.Controllers.GameRoot.UIManager?.HasOpenDialog ?? false);
        public bool IsAimingBlock => WantsToBlock && (_attack == null || !_attack.IsAttacking);
        public bool IsBlocking => isActiveAndEnabled && _blockHeld && _equipment != null && _equipment.Shield != null
            && _equipment.LastActiveHandItem != HandItemType.Bow && _raise >= .8f
            && !PlayerMovement.IsPlayerDead && (_attack == null || !_attack.IsAttacking);

        private void Awake()
        {
            _equipment = GetComponent<PlayerEquipment>();
            _attack = GetComponent<PlayerAttack>();
            foreach (var bone in GetComponentsInChildren<Transform>(true))
            {
                if (bone.name == "Arm:Left:Upper") _arm = bone;
                if (bone.name == "Arm:Left:Lower") _forearm = bone;
                if (bone.name == "Arm:Left:Lower_end") _hand = bone;
            }
        }

        private void Update()
        {
            // Remove last frame's overlay before the Animator evaluates its pose.
            if (_posed && _arm != null) _arm.localRotation = _baseArmRotation;
            _posed = false;
            if (!IsAimingBlock) return;
            if (MineArena.UI.MobileGameInput.Enabled)
            {
                GetComponent<RotationController>()?.FaceDirection(MineArena.UI.MobileGameInput.AimDirection(transform), 2);
                return;
            }
            var camera = Camera.main;
            if (camera == null || !TryResolveBlockDirection(camera.ScreenPointToRay(Input.mousePosition), transform.position, out var direction)) return;
            var rotation = GetComponent<RotationController>();
            if (rotation != null) rotation.FaceDirection(direction, 2);
            else transform.rotation = Quaternion.LookRotation(direction);
        }

        public static bool TryResolveBlockDirection(Ray ray, Vector3 position, out Vector3 direction)
        {
            direction = Vector3.zero;
            var plane = new Plane(Vector3.up, position);
            if (!plane.Raycast(ray, out float distance)) return false;
            direction = Vector3.ProjectOnPlane(ray.GetPoint(distance) - position, Vector3.up);
            if (direction.sqrMagnitude < .0001f) return false;
            direction.Normalize();
            return true;
        }

        private void LateUpdate()
        {
            var config = _equipment != null ? _equipment.Shield : null;
            if (config != _visualConfig)
            {
                if (_visual != null) Destroy(_visual.gameObject);
                _visual = null;
                _visualConfig = config;
                if (config != null && config.Prefab != null && _hand != null)
                {
                    _visual = Instantiate(config.Prefab, _hand).transform;
                    _visual.name = "OffHandShield";
                }
            }
            _blockHeld = WantsToBlock;
            bool raising = _blockHeld && (_attack == null || !_attack.IsAttacking);
            _raise = Mathf.MoveTowards(_raise, raising ? 1f : 0f, Time.deltaTime * 7f);
            _impact = Mathf.MoveTowards(_impact, 0, Time.deltaTime * 5f);
            if (_visual == null) return;
            _visual.gameObject.SetActive(_equipment.LastActiveHandItem != HandItemType.Bow);
            if (_arm != null)
            {
                _baseArmRotation = _arm.localRotation;
                _arm.localRotation *= Quaternion.Euler(-65f * _raise, 0, -15f * _raise);
                _posed = true;
            }
            UpdateVisualPose();
        }

        private void UpdateVisualPose()
        {
            // Keep the face aligned to the character while the left hand carries it.
            _visual.rotation = transform.rotation * Quaternion.Euler(0, -15f * (1 - _raise), 8f * _impact);
            _visual.position = Vector3.Lerp(_hand.position + transform.forward * .12f,
                transform.position + Vector3.up * 1.05f + transform.forward * (.48f - .08f * _impact) - transform.right * .12f, _raise);
            // Locomotion and the block overlay can swing the forearm ahead of the wrist.
            // Keep the plate in front of the entire arm volume, including the impact recoil.
            var normal = _visual.forward;
            float front = Vector3.Dot(_hand.position, normal);
            if (_forearm != null) front = Mathf.Max(front, Vector3.Dot(_forearm.position, normal));
            if (_arm != null) front = Mathf.Max(front, Vector3.Dot(_arm.position, normal));
            const float armClearance = .20f; // Arm half-width plus plate half-thickness and a small gap.
            float correction = front + armClearance - Vector3.Dot(_visual.position, normal);
            if (correction > 0) _visual.position += normal * correction;
            _visual.localScale = new Vector3(1f / _hand.lossyScale.x, 1f / _hand.lossyScale.y, 1f / _hand.lossyScale.z);
        }

        public static bool FacesSource(Vector3 forward, Vector3 position, Vector3 source)
        {
            var direction = Vector3.ProjectOnPlane(source - position, Vector3.up);
            return direction.sqrMagnitude > .0001f && Vector3.Dot(forward.normalized, direction.normalized) >= .5f;
        }

        public bool TryBlock(DamageData damage)
        {
            if (damage.Damage <= 0 || float.IsNaN(damage.Damage) || !IsBlocking || !damage.SourcePosition.HasValue ||
                !FacesSource(transform.forward, transform.position, damage.SourcePosition.Value)) return false;
            _impact = 1f;
            var config = _equipment.Shield;
            var progress = Devotion.SDK.Controllers.GameRoot.PlayerProgress?.InventoryProgress;
            int wear = 1 + Mathf.FloorToInt(Mathf.Min(damage.Damage, config.MaxDurability));
            bool broken = progress != null && progress.DamageDurableItem(config.Name, wear, config.MaxDurability);
            if (Devotion.SDK.Controllers.GameRoot.Instance != null)
            {
                if (_blockSound == null) _blockSound = Resources.Load<AudioClip>("Audio/ShieldBlock");
                if (_breakSound == null) _breakSound = Resources.Load<AudioClip>("Audio/ShieldBreak");
                var audio = Devotion.SDK.Controllers.GameRoot.GetManager<Managers.AudioManager>();
                audio?.PlayEffect(_blockSound, .8f);
                if (broken) audio?.PlayEffect(_breakSound, .9f);
            }
            if (broken)
            {
                _equipment.UnequipArmor(Items.ArmorSlot.OffHand);
                _blockHeld = false;
                _raise = 0;
                if (_visual != null)
                {
                    _visual.gameObject.SetActive(false);
                    Destroy(_visual.gameObject);
                    _visual = null;
                }
                _visualConfig = null;
                if (_posed && _arm != null) _arm.localRotation = _baseArmRotation;
                _posed = false;
                Devotion.SDK.Controllers.GameRoot.GetManager<Managers.InventoryManager>()?.InitManager();
            }
            return true;
        }

        private void OnDisable()
        {
            if (_posed && _arm != null) _arm.localRotation = _baseArmRotation;
            _posed = false;
            _raise = 0;
            _blockHeld = false;
            if (_visual != null) _visual.gameObject.SetActive(false);
        }
    }
}

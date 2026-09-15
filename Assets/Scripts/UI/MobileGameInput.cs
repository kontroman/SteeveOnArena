using Devotion.SDK.Controllers;
using MineArena.Managers;
using MineArena.PlayerSystem;
using UnityEngine;
namespace MineArena.UI
{
    public static class MobileGameInput
    {
        public static bool Enabled { get; set; }
        public static bool PreviewInEditor => Application.isEditor && GameRoot.GameConfig != null && GameRoot.GameConfig.MobileControlsInEditor;
        public static Vector2 Move, Aim;
        public static bool AttackHeld, ShieldHeld;
        public static bool Pinching { get; set; }
        private static int jumpFrame = -1, interactFrame = -1;
        public static bool Blocked => !Application.isFocused || Time.timeScale <= 0 || PlayerMovement.IsPlayerDead || TutorialService.BlocksInput || (GameRoot.UIManager?.HasOpenDialog ?? false);
        public static Vector2 Movement => Blocked ? Vector2.zero : Enabled ? Move : new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        public static bool Jump => !Blocked && (Enabled ? Time.frameCount == jumpFrame : Input.GetButtonDown("Jump"));
        public static bool Interact => !Blocked && (Enabled ? Time.frameCount == interactFrame : Input.GetKeyDown(KeyCode.E));
        public static bool Attack => !Blocked && (Enabled ? AttackHeld && PotionEffects.SelectedPotion == null : Devotion.SDK.Helpers.Inputs.LKMPressed);
        public static void JumpPressed() { if (!Blocked) jumpFrame = Time.frameCount + 1; }
        public static void InteractPressed() { if (!Blocked) interactFrame = Time.frameCount + 1; }
        public static void Reset() { Move = Aim = Vector2.zero; AttackHeld = ShieldHeld = Pinching = false; jumpFrame = interactFrame = -1; }
        public static Vector3 AimDirection(Transform player)
        {
            if (Aim.sqrMagnitude < .04f) return player.forward;
            var camera = Camera.main;
            Vector3 forward = camera != null ? Vector3.ProjectOnPlane(camera.transform.forward, Vector3.up) : Vector3.forward;
            if (forward.sqrMagnitude < .001f) forward = Vector3.forward;
            return (Quaternion.LookRotation(forward) * new Vector3(Aim.x, 0, Aim.y)).normalized;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Initialize() { Enabled = false; Reset(); }
    }
}

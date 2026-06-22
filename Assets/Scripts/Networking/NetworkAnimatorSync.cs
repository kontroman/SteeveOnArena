using System;
using System.Collections.Generic;
using UnityEngine;

namespace MineArena.Networking
{
    [DisallowMultipleComponent]
    public class NetworkAnimatorSync : MonoBehaviour
    {
        [SerializeField, Tooltip("Animation packets per second for local player.")]
        private float sendRate = 8f;

        private readonly Dictionary<int, bool> lastBoolValues = new Dictionary<int, bool>();
        private readonly Dictionary<int, int> lastIntValues = new Dictionary<int, int>();
        private readonly Dictionary<int, float> lastFloatValues = new Dictionary<int, float>();
        private readonly List<string> pendingTriggers = new List<string>();

        private NetworkClientManager manager;
        private Animator animator;
        private int playerId;
        private bool isLocal;
        private float nextSendTime;
        private ulong localTick;
        private int lastStateHash;
        private float lastNormalizedTime;
        private int remoteStateHash;
        private bool remoteStateInitialized;

        private void Awake()
        {
            animator = GetComponentInChildren<Animator>();
        }

        public void Bind(NetworkClientManager networkManager, int id, bool local)
        {
            manager = networkManager;
            playerId = id;
            isLocal = local;

            if (animator == null)
                animator = GetComponentInChildren<Animator>();
        }

        public void NotifyTrigger(string triggerName)
        {
            if (!string.IsNullOrEmpty(triggerName))
                pendingTriggers.Add(triggerName);
        }

        public bool TryBuildAnimationUpdate(out AnimationUpdate update)
        {
            update = null;

            if (!isLocal || animator == null || Time.unscaledTime < nextSendTime)
                return false;

            var state = animator.GetCurrentAnimatorStateInfo(0);
            var parameters = BuildChangedParameters();
            var stateChanged = state.fullPathHash != lastStateHash ||
                               Mathf.Abs(state.normalizedTime - lastNormalizedTime) > 0.05f;

            if (!stateChanged && parameters.Count == 0 && pendingTriggers.Count == 0)
                return false;

            localTick++;
            nextSendTime = Time.unscaledTime + 1f / Mathf.Max(1f, sendRate);
            lastStateHash = state.fullPathHash;
            lastNormalizedTime = state.normalizedTime;

            update = new AnimationUpdate
            {
                playerId = playerId,
                stateHash = state.fullPathHash,
                normalizedTime = Mathf.Repeat(state.normalizedTime, 1f),
                changedOnly = true,
                parameters = parameters.ToArray(),
                triggers = pendingTriggers.ToArray(),
                clientTick = localTick
            };

            pendingTriggers.Clear();
            return true;
        }

        public void ApplyRemoteState(AnimationStateDto state)
        {
            if (animator == null || state == null)
                return;

            if (state.stateHash != 0 && ShouldPlayRemoteState(state))
            {
                animator.Play(state.stateHash, 0, Mathf.Clamp01(state.normalizedTime));
                remoteStateHash = state.stateHash;
                remoteStateInitialized = true;
            }

            ApplyParameters(state.parameters);
            ApplyTriggers(state.triggers);
        }

        private bool ShouldPlayRemoteState(AnimationStateDto state)
        {
            if (!remoteStateInitialized)
                return true;

            if (state.stateHash != remoteStateHash)
                return true;

            // Do not restart the same animation every snapshot. Only resync on large drift.
            var current = animator.GetCurrentAnimatorStateInfo(0);
            var currentNormalized = Mathf.Repeat(current.normalizedTime, 1f);
            var incomingNormalized = Mathf.Repeat(state.normalizedTime, 1f);
            var drift = Mathf.Abs(Mathf.DeltaAngle(currentNormalized * 360f, incomingNormalized * 360f)) / 360f;

            return drift > 0.35f;
        }

        private List<AnimationParameterDto> BuildChangedParameters()
        {
            var changed = new List<AnimationParameterDto>();
            var parameters = animator.parameters;

            for (var i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                switch (parameter.type)
                {
                    case AnimatorControllerParameterType.Bool:
                        var boolValue = animator.GetBool(parameter.nameHash);
                        if (!lastBoolValues.TryGetValue(parameter.nameHash, out var lastBool) || lastBool != boolValue)
                        {
                            lastBoolValues[parameter.nameHash] = boolValue;
                            changed.Add(new AnimationParameterDto
                            {
                                name = parameter.name,
                                type = "bool",
                                boolValue = boolValue
                            });
                        }
                        break;
                    case AnimatorControllerParameterType.Int:
                        var intValue = animator.GetInteger(parameter.nameHash);
                        if (!lastIntValues.TryGetValue(parameter.nameHash, out var lastInt) || lastInt != intValue)
                        {
                            lastIntValues[parameter.nameHash] = intValue;
                            changed.Add(new AnimationParameterDto
                            {
                                name = parameter.name,
                                type = "int",
                                intValue = intValue
                            });
                        }
                        break;
                    case AnimatorControllerParameterType.Float:
                        var floatValue = animator.GetFloat(parameter.nameHash);
                        if (!lastFloatValues.TryGetValue(parameter.nameHash, out var lastFloat) ||
                            Mathf.Abs(lastFloat - floatValue) > 0.01f)
                        {
                            lastFloatValues[parameter.nameHash] = floatValue;
                            changed.Add(new AnimationParameterDto
                            {
                                name = parameter.name,
                                type = "float",
                                floatValue = floatValue
                            });
                        }
                        break;
                }
            }

            return changed;
        }

        private void ApplyParameters(AnimationParameterDto[] parameters)
        {
            if (parameters == null)
                return;

            for (var i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                if (parameter == null || string.IsNullOrEmpty(parameter.name))
                    continue;

                switch (parameter.type)
                {
                    case "bool":
                        animator.SetBool(parameter.name, parameter.boolValue);
                        break;
                    case "int":
                        animator.SetInteger(parameter.name, parameter.intValue);
                        break;
                    case "float":
                        animator.SetFloat(parameter.name, parameter.floatValue);
                        break;
                }
            }
        }

        private void ApplyTriggers(string[] triggers)
        {
            if (triggers == null)
                return;

            for (var i = 0; i < triggers.Length; i++)
            {
                if (!string.IsNullOrEmpty(triggers[i]))
                    animator.SetTrigger(triggers[i]);
            }
        }
    }
}

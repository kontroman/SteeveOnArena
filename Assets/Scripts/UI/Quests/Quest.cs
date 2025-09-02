using System;
using MineArena.Game.UI;
using MineArena.Messages.MessageService;
using UnityEngine;

namespace UI.Quests
{
    public class Quest : MonoBehaviour, IProgressBar, IMessageSubscriber
    {
        private readonly float _initialValue = 0;

        public event Action<float, float> OnValueChanged;

        public float MaxValue { get; private set; }
        public float CurrentValue { get; private set; }

        public void Construct(int dataMaxValueOnTask)
        {
            MaxValue = dataMaxValueOnTask;
            CurrentValue = _initialValue;
            OnValueChanged?.Invoke(CurrentValue, MaxValue);
        }
    }
}
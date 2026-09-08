using System.Linq;
using Devotion.SDK.Controllers;
using TMPro;
using UnityEngine;

namespace MineArena.UI
{
    public sealed class QuestReadyBadge : MonoBehaviour
    {
        [SerializeField] private GameObject badge;
        [SerializeField] private TMP_Text amount;
        private float _next;
        private void Update()
        {
            if (Time.unscaledTime < _next) return;
            _next = Time.unscaledTime + 0.5f;
            var saved = GameRoot.PlayerProgress?.AchievementProgress?.Achievements;
            int count = saved == null ? 0 : saved.Values.Count(q => q.CanTakePrize && !q.IsCompleted);
            badge.SetActive(count > 0); amount.text = count > 99 ? "99+" : count.ToString();
        }
    }
}

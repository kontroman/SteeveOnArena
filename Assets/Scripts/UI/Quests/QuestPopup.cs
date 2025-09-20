using System.Collections;
using System.Collections.Generic;
using MineArena.Game.UI;
using MineArena.Messages;
using MineArena.Messages.MessageService;
using TMPro;
using UnityEngine;

namespace UI.Quests
{
    public class QuestPopup : MonoBehaviour,
        IProgressBar,
        IMessageSubscriber<QuestMessages.ItemTaken>,
        IMessageSubscriber<QuestMessages.QuestBegun>
    {
        [SerializeField] private TextMeshProUGUI _nameQuest;
        [SerializeField] private ProgressQuestBar _progressBarQuest;
        [SerializeField] private float _timer;
        [SerializeField] private float _speed;

        private readonly Vector3[] _positions = new Vector3[2];
        private readonly float _minDistance = 1f;
        private int _spot;

        private List<Quest> _quests = new();
        private Coroutine _coroutine;
        private RectTransform _rectTransform;

        public float MaxValue { get; private set; }
        public float CurrentValue { get; private set; }

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
        }

        private void Start()
        {
            _positions[0] = _rectTransform.position + new Vector3(0, -100, 0);
            _positions[1] = _rectTransform.position;
        }

        public void OnMessage(QuestMessages.QuestBegun message)
        {
            MaxValue = message.Model.MaxValueProgress;
            CurrentValue = message.Model.CurrentValueProgress;
            _nameQuest.text = message.Model.Data.NameQuest;
            _spot = 0;
            _coroutine = StartCoroutine(ShowInformation());
        }

        public void OnMessage(QuestMessages.ItemTaken message)
        {
            // Debug.LogError("Пришло сообщение в попап");
            // _quests = GameRoot.GetManager<QuestManager>().GiveQuests();
            //
            // foreach (Quest quest in _quests)
            // {
            //     Debug.LogError("ЗАшли в цикл обработки квестов");
            //     if (quest.CurrentValueProgress != 0)
            //     {
            //         MaxValue = quest.MaxValueProgress;
            //         CurrentValue = quest.CurrentValueProgress;
            //         _nameQuest.text = quest.Data.NameQuest;
            //         _spot = 0;
            //         _coroutine = StartCoroutine(ShowInformation());
            //     }
            // }
        }

        private IEnumerator ShowInformation()
        {
            var waitForSeconds = new WaitForSeconds(_timer);

            while (Vector3.Distance(_rectTransform.position, _positions[_spot]) > _minDistance)
            {
                _rectTransform.position = Vector3.MoveTowards(_rectTransform.position, _positions[_spot],
                    _speed * Time.deltaTime);
                yield return null;
            }

            _spot++;

            yield return waitForSeconds;

            if (_spot == _positions.Length)
                StopCoroutine(_coroutine);
            else
                StartCoroutine(ShowInformation());
        }

        private void OnEnable() =>
            MessageService.Subscribe(this);

        private void OnDisable() =>
            MessageService.Unsubscribe(this);

        private void OnDestroy() =>
            MessageService.Unsubscribe(this);
    }
}
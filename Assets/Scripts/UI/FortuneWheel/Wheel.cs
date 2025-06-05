using System.Collections.Generic;
using Devotion.SDK.Controllers;
using UnityEngine;

namespace UI.FortuneWheel
{
    public class Wheel : MonoBehaviour
    {
        private const int MaxNumberTurns = 10;
        private const int MinNumberTurns = 5;
        private const float MinValueTimer = 3.0f;
        private const float MaxValueTimer = 5.0f;

        [SerializeField] private List<WheelPrize> _items = new();
        [SerializeField] private WheelConstructor _constructor;
        [SerializeField] private Transform _wheelContainer;
        [SerializeField] private AnimationCurve _curve;
        [SerializeField] private GameObject _winPanel;

        private bool _isStarted;
        private float _startAngle;
        private int _randomRewardIndex;
        private int _endAngle;
        private float _currentRotationTime;
        private float _maxRotationTime;

        private void Start()
        {
            CreatListPrizeItems();
            _constructor.Create(_wheelContainer, _items);
        }

        private void Update()
        {
            StartWheelRotation();
        }

        public void TernWheel()
        {
            if (_isStarted)
                return;

            if (_winPanel != null)
                _winPanel.SetActive(false);

            _isStarted = true;
            _startAngle = _wheelContainer.localEulerAngles.z;
            int totalSlots = _items.Count;
            _randomRewardIndex = Random.Range(0, totalSlots);
            int rotationCount = Random.Range(MinNumberTurns, MaxNumberTurns);
            _endAngle = -(rotationCount * 360 + _randomRewardIndex * 360 / totalSlots);
            _currentRotationTime = 0.0f;
            _maxRotationTime = Random.Range(MinValueTimer, MaxValueTimer);
        }

        private void StartWheelRotation()
        {
            if (!_isStarted)
                return;

            float t = _currentRotationTime / _maxRotationTime;
            t = _curve.Evaluate(t);
            float angle = Mathf.Lerp(_startAngle, _endAngle, t);
            _wheelContainer.eulerAngles = new Vector3(0, 0, angle);

            if (angle <= _endAngle)
            {
                _isStarted = false;
                ShowResult(_randomRewardIndex);
            }

            _currentRotationTime += Time.deltaTime;
        }

        private void ShowResult(int randomRewardIndex)
        {
            if (_winPanel != null)
                _winPanel.SetActive(true);

            _constructor.SettingSector(_winPanel, _items[randomRewardIndex]);
        }

        private void CreatListPrizeItems()
        {
            for (int i = 0; i < GameRoot.GameConfig.Prizes.Count; i++)
            {
                _items.Add(GameRoot.GameConfig.Prizes[i]);
            }
        }
    }
}
using System.Collections;
using System.Collections.Generic;
using Devotion.SDK.Controllers;
using MineArena.Basics;
using UnityEngine;

namespace MineArena.UI.FortuneWheel
{
    public class Wheel : MonoBehaviour
    {
        [SerializeField] private List<ItemPrize> _items;
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

            int rotationCount =
                Random.Range(Constants.FortuneWheel.MinNumberTurns, Constants.FortuneWheel.MaxNumberTurns);
            _endAngle = -(rotationCount * 360 + _randomRewardIndex * 360 / totalSlots);

            _currentRotationTime = 0.0f;
            _maxRotationTime = Random.Range(Constants.FortuneWheel.MinValueTimer,
                Constants.FortuneWheel.MaxValueTimer);

            StartCoroutine(StartWheelRotation());
        }

        private IEnumerator StartWheelRotation()
        {
            while (_currentRotationTime < _maxRotationTime)
            {
                float t = _currentRotationTime / _maxRotationTime;
                t = _curve.Evaluate(t);

                float angle = Mathf.Lerp(_startAngle, _endAngle, t);
                _wheelContainer.eulerAngles = new Vector3(0, 0, angle);

                _currentRotationTime += Time.deltaTime;
                yield return null;
            }

            _wheelContainer.eulerAngles = new Vector3(0, 0, _endAngle);
            _isStarted = false;
            ShowResult(_randomRewardIndex);
        }

        private void ShowResult(int randomRewardIndex)
        {
            if (_winPanel != null)
                _winPanel.SetActive(true);

            _constructor.SettingSector(_winPanel, _items[randomRewardIndex]);

            _items[randomRewardIndex].Construct();
            _items[randomRewardIndex].GiveTo();
        }

        private void CreatListPrizeItems()
        {
            foreach (var t in GameRoot.GameConfig.Prizes)
                _items.Add(t);
        }
    }
}
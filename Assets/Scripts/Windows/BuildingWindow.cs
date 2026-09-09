using Devotion.SDK.Base;
using Devotion.SDK.Controllers;
using Devotion.SDK.Helpers;
using MineArena.Buildings;
using MineArena.Managers;
using MineArena.Windows.Elements;
using TMPro;
using UnityEngine;

namespace MineArena.Windows
{
    public class BuildingWindow : BaseWindow
    {
        [SerializeField] private TextMeshProUGUI _buildingName;
        [SerializeField] private Transform _priceTransform;
        [SerializeField] private Transform _opensTransform;
        [SerializeField] private BuildingPriceElement _pricePrefab;
        [SerializeField] private UnityEngine.UI.Button _buildButton;
        public Transform TutorialTarget => _buildButton != null ? _buildButton.transform : null;
        [SerializeField] private TMP_Text _feedback;
        [SerializeField] private UnityEngine.UI.Image _preview;
        [SerializeField] private TMP_Text _previewCaption;
        [SerializeField] private TMP_Text _unlocksEmpty;
        private InventoryManager _inventory;
        private BuildingConfig _buildingConfig;
        private Transform _buildingPlace;
        private BuildingLevelConfig _targetLevel;
        private bool _upgrade;
        private bool _busy;

        private void OnEnable()
        {
            if (GameRoot.Instance == null) return;
            _inventory = GameRoot.GetManager<InventoryManager>();
            if (_inventory != null) _inventory.InventoryUpdated += RefreshBuildState;
            RefreshBuildState();
        }
        private void OnDisable()
        {
            if (_inventory != null) _inventory.InventoryUpdated -= RefreshBuildState;
        }
        private void RefreshBuildState()
        {
            if (_priceTransform != null)
                foreach (var price in _priceTransform.GetComponentsInChildren<BuildingPriceElement>()) price.RefreshCost();
            bool ready = !_busy && _targetLevel != null && _inventory != null && _inventory.CanAfford(_targetLevel.RequiredResources);
            if (_buildButton != null) _buildButton.interactable = ready;
            if (_feedback != null) _feedback.text = _buildingConfig == null ? "" : _targetLevel == null
                ? "Достигнут максимальный уровень" : ready ? "Все ресурсы собраны" : "Нужно добыть ещё ресурсов";
        }
        public void InitializeBuilding(BuildingConfig config, Transform buildingPlace)
        {
            ClearSavedData();
            _buildingConfig = config;
            _buildingPlace = buildingPlace;
            _targetLevel = null;
            _busy = false;
            if (config == null) { RefreshBuildState(); return; }
            var manager = GameRoot.GetManager<BuildingManager>();
            int current = manager != null ? manager.GetBuildingLevel(config) : 0;
            _upgrade = current > 0;
            if (_upgrade) config.TryGetNextLevel(current, out _targetLevel);
            else _targetLevel = config.GetCurrentLevel();
            var shown = _targetLevel ?? config.GetLevelByNumber(current);
            foreach (var text in GetComponentsInChildren<TMP_Text>(true))
                if (text.name == "UnlocksTitle") text.text = shown != null && shown.Production.Count > 0 ? "УРОЖАЙ" : "ОТКРЫВАЕТ";
            _buildingName.text = config.BuildingName;
            if (_preview != null)
            {
                _preview.sprite = shown != null ? shown.Preview : null;
                _preview.enabled = _preview.sprite != null;
            }
            if (_previewCaption != null) _previewCaption.text = _targetLevel == null ? "Уровень " + current
                : _upgrade ? "Улучшение: " + current + " → " + _targetLevel.Level : "Уровень " + _targetLevel.Level;
            if (_previewCaption != null && shown != null)
            {
                if (shown.CraftOutputBonus > 0) _previewCaption.text += "\n+" + shown.CraftOutputBonus + " к выходу каждого крафта";
                if (shown.Production.Count > 0) _previewCaption.text += "\nУрожай каждые " + shown.ProductionSeconds + " с";
                if (shown.ExpeditionRewardBonusPercent > 0) _previewCaption.text += "\nНаграды экспедиции +" + shown.ExpeditionRewardBonusPercent + "%";
            }
            if (_buildButton != null)
            {
                var label = _buildButton.GetComponentInChildren<TMP_Text>(true);
                if (label != null) label.text = _upgrade ? "Улучшить" : "Построить";
            }
            if (_unlocksEmpty != null)
            {
                _unlocksEmpty.gameObject.SetActive(_targetLevel == null || _targetLevel.Unlocks == null || _targetLevel.Unlocks.Count == 0);
                _unlocksEmpty.text = MineArena.Cosmetics.SkinCatalog.Load()?.Building == config
                    ? "Коллекция скинов и гардероб. Покупай образы за валюту, получай за задания и награды. Скины меняют только внешний вид."
                    : "У этого здания нет новых рецептов.";
            }
            if (_targetLevel != null)
            {
                foreach (var cost in _targetLevel.RequiredResources) Instantiate(_pricePrefab, _priceTransform).Setup(cost);
                if (_targetLevel.Unlocks != null)
                    foreach (var item in _targetLevel.Unlocks)
                        if (item != null) Instantiate(_pricePrefab, _opensTransform).Setup(item);
            }
            if (shown != null && shown.Production.Count > 0)
            {
                if (_unlocksEmpty != null) _unlocksEmpty.gameObject.SetActive(false);
                foreach (var output in shown.Production)
                    if (output.Item != null) Instantiate(_pricePrefab, _opensTransform).Setup(output.Item, output.Amount);
            }
            RefreshBuildState();
        }
        public void ClearSavedData()
        {
            foreach (Transform t in _priceTransform) Destroy(t.gameObject);
            foreach (Transform t in _opensTransform) Destroy(t.gameObject);
        }
        public void OnTryBuildClick()
        {
            if (_busy || _targetLevel == null) return;
            _busy = true;
            var manager = GameRoot.GetManager<BuildingManager>();
            bool success = _upgrade ? manager.TryUpgrade(_buildingConfig, _buildingPlace) : manager.TryBuild(_buildingConfig, _buildingPlace);
            _busy = false;
            if (success) CloseWindow();
            else RefreshBuildState();
        }
        public override void CloseWindow() => this.CloseWindow<BuildingWindow>();
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape)) CloseWindow();
            if (_buildingConfig == null || _feedback == null || GameRoot.Instance == null) return;
            var manager = GameRoot.GetManager<BuildingManager>();
            var current = manager != null ? _buildingConfig.GetLevelByNumber(manager.GetBuildingLevel(_buildingConfig)) : null;
            if (current?.Production == null || current.Production.Count == 0) return;
            long next = GameRoot.PlayerProgress.InventoryProgress.FarmNextProductionUtcTicks;
            float remaining = Mathf.Max(0, (float)System.TimeSpan.FromTicks(next - System.DateTime.UtcNow.Ticks).TotalSeconds);
            string outputs = string.Join(", ", System.Linq.Enumerable.Select(current.Production, x => $"{x.Item.DisplayName} ×{x.Amount}"));
            _feedback.text = $"Урожай через {remaining:0} с: {outputs}";
        }
    }
}

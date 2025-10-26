using System;
using System.Collections.Generic;
using MineArena.Buildings;
using MineArena.Windows.Elements;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MineArena.Windows.Crafting
{
    public class CraftingDetailsPanel : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private Image _icon;
        [SerializeField] private TextMeshProUGUI _itemName;
        [SerializeField] private TextMeshProUGUI _description;
        [SerializeField] private Transform _costContainer;
        [SerializeField] private BuildingPriceElement _costPrefab;
        [SerializeField] private Button _craftButton;

        private readonly List<GameObject> _spawnedCosts = new();
        private Action _onCraft;

        public void Hide()
        {
            if (_root != null)
            {
                _root.SetActive(false);
            }

            _onCraft = null;
        }

        public void Show(Sprite icon, string itemName, string description, IReadOnlyList<ResourceRequired> costs, bool canCraft, Action onCraft)
        {
            if (_root != null)
            {
                _root.SetActive(true);
            }

            if (_icon != null)
            {
                _icon.sprite = icon;
            }

            if (_itemName != null)
            {
                _itemName.text = itemName;
            }

            if (_description != null)
            {
                _description.text = description ?? string.Empty;
            }

            RebuildCostList(costs);

            _onCraft = onCraft;

            if (_craftButton != null)
            {
                _craftButton.onClick.RemoveListener(HandleCraftClicked);
                _craftButton.onClick.AddListener(HandleCraftClicked);
                _craftButton.interactable = canCraft && _onCraft != null;
            }
        }

        public void SetCraftInteractable(bool canCraft)
        {
            if (_craftButton != null)
            {
                _craftButton.interactable = canCraft && _onCraft != null;
            }
        }

        private void RebuildCostList(IReadOnlyList<ResourceRequired> costs)
        {
            foreach (var element in _spawnedCosts)
            {
                if (element != null)
                {
                    Destroy(element);
                }
            }

            _spawnedCosts.Clear();

            if (_costContainer == null || _costPrefab == null || costs == null)
                return;

            foreach (var cost in costs)
            {
                if (cost.Resource == null || cost.Amount <= 0)
                    continue;

                var element = Instantiate(_costPrefab, _costContainer);
                element.Setup(cost);
                _spawnedCosts.Add(element.gameObject);
            }
        }

        private void HandleCraftClicked()
        {
            _onCraft?.Invoke();
        }

        private void OnDestroy()
        {
            if (_craftButton != null)
            {
                _craftButton.onClick.RemoveListener(HandleCraftClicked);
            }
        }
    }
}

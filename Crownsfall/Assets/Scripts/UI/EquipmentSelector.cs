using System;
using System.Collections.Generic;
using Crownsfall.Characters;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Crownsfall.UI
{
    public class EquipmentSelector : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private Button previousButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private TextMeshProUGUI labelText;
        [SerializeField] private TextMeshProUGUI itemNameText;

        private readonly List<EquipmentItemSO> items = new List<EquipmentItemSO>();
        private int currentIndex;

        public event Action<EquipmentItemSO> OnSelectionChanged;

        public EquipmentItemSO CurrentItem =>
            items.Count > 0 ? items[currentIndex] : null;

        public int CurrentIndex => currentIndex;

        private void Awake()
        {
            if (previousButton != null)
            {
                previousButton.onClick.AddListener(SelectPrevious);
            }

            if (nextButton != null)
            {
                nextButton.onClick.AddListener(SelectNext);
            }
        }

        public void Init(IReadOnlyList<EquipmentItemSO> equipmentItems, string slotLabel = null)
        {
            items.Clear();
            if (equipmentItems != null)
            {
                items.AddRange(equipmentItems);
            }

            currentIndex = 0;

            if (labelText != null && !string.IsNullOrEmpty(slotLabel))
            {
                labelText.text = slotLabel;
            }

            RefreshDisplay();
        }

        public void SelectPrevious()
        {
            if (items.Count == 0)
            {
                return;
            }

            currentIndex = (currentIndex - 1 + items.Count) % items.Count;
            RefreshDisplay();
            NotifySelectionChanged();
        }

        public void SelectNext()
        {
            if (items.Count == 0)
            {
                return;
            }

            currentIndex = (currentIndex + 1) % items.Count;
            RefreshDisplay();
            NotifySelectionChanged();
        }

        private void RefreshDisplay()
        {
            var hasItems = items.Count > 0;
            var item = hasItems ? items[currentIndex] : null;

            if (iconImage != null)
            {
                iconImage.enabled = hasItems && item?.icon != null;
                iconImage.sprite = item?.icon;
                iconImage.preserveAspect = true;
            }

            if (itemNameText != null)
            {
                itemNameText.text = hasItems ? item.itemName : "None";
            }

            if (previousButton != null)
            {
                previousButton.interactable = items.Count > 1;
            }

            if (nextButton != null)
            {
                nextButton.interactable = items.Count > 1;
            }
        }

        private void NotifySelectionChanged()
        {
            OnSelectionChanged?.Invoke(CurrentItem);
        }
    }
}

using UnityEngine;

namespace Crownsfall.Characters
{
    public enum EquipmentType
    {
        Head,
        Body,
        Weapon,
        Mount
    }

    public enum EquipmentRarity
    {
        Common,
        Rare,
        Epic,
        Legendary,
        Mythic
    }

    public abstract class EquipmentItemSO : ScriptableObject
    {
        [Header("Identity")]
        public string id;
        public string itemName;

        [Header("Visual")]
        public Sprite icon;

        [Header("Type")]
        public EquipmentType equipmentType;

        [Header("Rarity")]
        public EquipmentRarity rarity = EquipmentRarity.Common;

        [Header("Gameplay")]
        public int attack;
        public int defense;
        public int speed;

        [Header("Unlock System")]
        public int unlockLevel;
        public bool unlockedByDefault;

        [Header("Shop")]
        public int purchaseCost;

        [Header("Description")]
        public string description;

        // Returns the rarity name for UI labels (e.g. "Rare").
        public string GetRarityDisplayName()
        {
            return rarity.ToString();
        }

        // Color used for rarity text in the Character Builder and future UI.
        public Color GetRarityColor()
        {
            switch (rarity)
            {
                case EquipmentRarity.Rare:
                    return new Color(0.2f, 0.6f, 1f);
                case EquipmentRarity.Epic:
                    return new Color(0.65f, 0.3f, 0.95f);
                case EquipmentRarity.Legendary:
                    return new Color(1f, 0.55f, 0.1f);
                case EquipmentRarity.Mythic:
                    return new Color(0.95f, 0.2f, 0.2f);
                default:
                    return new Color(0.6f, 0.6f, 0.6f);
            }
        }

        // Higher rarity boosts how much this item's stats count in battle.
        public float GetPowerMultiplier()
        {
            switch (rarity)
            {
                case EquipmentRarity.Rare:
                    return 1.15f;
                case EquipmentRarity.Epic:
                    return 1.35f;
                case EquipmentRarity.Legendary:
                    return 1.6f;
                case EquipmentRarity.Mythic:
                    return 2.0f;
                default:
                    return 1.0f;
            }
        }
    }
}

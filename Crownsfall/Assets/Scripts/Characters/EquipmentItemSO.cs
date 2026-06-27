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
        Uncommon,
        Rare,
        Epic,
        Legendary
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
        public EquipmentRarity rarity;

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
    }
}

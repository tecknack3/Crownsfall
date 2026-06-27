using System.Collections.Generic;
using Crownsfall.Characters;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Crownsfall.UI
{
    /// <summary>
    /// Holds the fighter choices made on the Character Builder screen.
    /// Other scenes can read these values later (e.g. before a battle).
    /// </summary>
    public static class FighterSessionData
    {
        public static string FighterName { get; set; }
        public static HeadSO SelectedHead { get; set; }
        public static BodySO SelectedBody { get; set; }
        public static WeaponSO SelectedWeapon { get; set; }
        public static MountSO SelectedMount { get; set; }
    }

    /// <summary>
    /// Controls the Character Builder UI: equipment lists, preview images,
    /// name labels, Create Fighter button, and the Fighter Card confirmation panel.
    /// Wire all public fields in the Inspector to your scene objects.
    /// </summary>
    public class CharacterBuilderManager : MonoBehaviour
    {
        [Header("Equipment Lists")]
        [Tooltip("Drag Head ScriptableObjects here from the Project window.")]
        public List<HeadSO> heads = new List<HeadSO>();

        [Tooltip("Drag Body ScriptableObjects here from the Project window.")]
        public List<BodySO> bodies = new List<BodySO>();

        [Tooltip("Drag Weapon ScriptableObjects here from the Project window.")]
        public List<WeaponSO> weapons = new List<WeaponSO>();

        [Tooltip("Drag Mount ScriptableObjects here from the Project window.")]
        public List<MountSO> mounts = new List<MountSO>();

        [Header("Fighter Name")]
        public TMP_InputField fighterNameInput;

        [Header("Preview Images")]
        [Tooltip("Images that show the currently selected equipment icon.")]
        public Image headPreviewImage;
        public Image bodyPreviewImage;
        public Image weaponPreviewImage;
        public Image mountPreviewImage;

        [Header("Previous / Next Buttons")]
        public Button headPreviousButton;
        public Button headNextButton;
        public Button bodyPreviousButton;
        public Button bodyNextButton;
        public Button weaponPreviousButton;
        public Button weaponNextButton;
        public Button mountPreviousButton;
        public Button mountNextButton;

        [Header("Item Name Labels")]
        public TMP_Text headNameText;
        public TMP_Text bodyNameText;
        public TMP_Text weaponNameText;
        public TMP_Text mountNameText;

        [Header("Actions")]
        public Button createFighterButton;

        [Header("Fighter Card Panel")]
        [Tooltip("Full-screen overlay shown after Create Fighter. Wire to Canvas/MainLayout/FighterCardPanel.")]
        public GameObject fighterCardPanel;

        [Tooltip("Card preview stack (back to front): mount, body, weapon, head.")]
        public Image cardMountImage;
        public Image cardBodyImage;
        public Image cardWeaponImage;
        public Image cardHeadImage;

        public TMP_Text cardFighterNameText;
        public TMP_Text cardAttackText;
        public TMP_Text cardDefenseText;
        public TMP_Text cardSpeedText;
        public TMP_Text cardHealthText;

        public Button startBattleButton;
        public Button backEditButton;

        // Tracks which item is currently selected in each list (starts at 0).
        private int headIndex;
        private int bodyIndex;
        private int weaponIndex;
        private int mountIndex;

        // The fighter created on the last Create Fighter click.
        private PlayerFighter _currentFighter;

        private void Start()
        {
            // Hook up button clicks so the manager responds to player input.
            WireButtonListeners();

            // Fighter card starts hidden until the player creates a fighter.
            if (fighterCardPanel != null)
            {
                fighterCardPanel.SetActive(false);
            }

            // Begin on the first item in every list.
            headIndex = 0;
            bodyIndex = 0;
            weaponIndex = 0;
            mountIndex = 0;

            // Refresh all UI so the screen matches the starting selection.
            RefreshHeadDisplay();
            RefreshBodyDisplay();
            RefreshWeaponDisplay();
            RefreshMountDisplay();
        }

        /// <summary>
        /// Connects each Previous/Next, Create, and Fighter Card button to its handler.
        /// </summary>
        private void WireButtonListeners()
        {
            if (headPreviousButton != null)
            {
                headPreviousButton.onClick.AddListener(SelectPreviousHead);
            }

            if (headNextButton != null)
            {
                headNextButton.onClick.AddListener(SelectNextHead);
            }

            if (bodyPreviousButton != null)
            {
                bodyPreviousButton.onClick.AddListener(SelectPreviousBody);
            }

            if (bodyNextButton != null)
            {
                bodyNextButton.onClick.AddListener(SelectNextBody);
            }

            if (weaponPreviousButton != null)
            {
                weaponPreviousButton.onClick.AddListener(SelectPreviousWeapon);
            }

            if (weaponNextButton != null)
            {
                weaponNextButton.onClick.AddListener(SelectNextWeapon);
            }

            if (mountPreviousButton != null)
            {
                mountPreviousButton.onClick.AddListener(SelectPreviousMount);
            }

            if (mountNextButton != null)
            {
                mountNextButton.onClick.AddListener(SelectNextMount);
            }

            if (createFighterButton != null)
            {
                createFighterButton.onClick.AddListener(OnCreateFighterClicked);
            }

            if (startBattleButton != null)
            {
                startBattleButton.onClick.AddListener(OnStartBattleClicked);
            }

            if (backEditButton != null)
            {
                backEditButton.onClick.AddListener(OnBackEditClicked);
            }
        }

        // --- Head selection ---

        private void SelectPreviousHead()
        {
            if (heads == null || heads.Count == 0)
            {
                return;
            }

            // Wrap to the last item when going back from the first.
            headIndex = (headIndex - 1 + heads.Count) % heads.Count;
            RefreshHeadDisplay();
        }

        private void SelectNextHead()
        {
            if (heads == null || heads.Count == 0)
            {
                return;
            }

            // Wrap to the first item when going forward from the last.
            headIndex = (headIndex + 1) % heads.Count;
            RefreshHeadDisplay();
        }

        private void RefreshHeadDisplay()
        {
            var head = GetItemAt(heads, headIndex);
            UpdatePreviewImage(headPreviewImage, head?.icon);
            UpdateNameText(headNameText, head);
            UpdateNavigationButtons(headPreviousButton, headNextButton, heads);
        }

        // --- Body selection ---

        private void SelectPreviousBody()
        {
            if (bodies == null || bodies.Count == 0)
            {
                return;
            }

            bodyIndex = (bodyIndex - 1 + bodies.Count) % bodies.Count;
            RefreshBodyDisplay();
        }

        private void SelectNextBody()
        {
            if (bodies == null || bodies.Count == 0)
            {
                return;
            }

            bodyIndex = (bodyIndex + 1) % bodies.Count;
            RefreshBodyDisplay();
        }

        private void RefreshBodyDisplay()
        {
            var body = GetItemAt(bodies, bodyIndex);
            UpdatePreviewImage(bodyPreviewImage, body?.icon);
            UpdateNameText(bodyNameText, body);
            UpdateNavigationButtons(bodyPreviousButton, bodyNextButton, bodies);
        }

        // --- Weapon selection ---

        private void SelectPreviousWeapon()
        {
            if (weapons == null || weapons.Count == 0)
            {
                return;
            }

            weaponIndex = (weaponIndex - 1 + weapons.Count) % weapons.Count;
            RefreshWeaponDisplay();
        }

        private void SelectNextWeapon()
        {
            if (weapons == null || weapons.Count == 0)
            {
                return;
            }

            weaponIndex = (weaponIndex + 1) % weapons.Count;
            RefreshWeaponDisplay();
        }

        private void RefreshWeaponDisplay()
        {
            var weapon = GetItemAt(weapons, weaponIndex);
            UpdatePreviewImage(weaponPreviewImage, weapon?.icon);
            UpdateNameText(weaponNameText, weapon);
            UpdateNavigationButtons(weaponPreviousButton, weaponNextButton, weapons);
        }

        // --- Mount selection ---

        private void SelectPreviousMount()
        {
            if (mounts == null || mounts.Count == 0)
            {
                return;
            }

            mountIndex = (mountIndex - 1 + mounts.Count) % mounts.Count;
            RefreshMountDisplay();
        }

        private void SelectNextMount()
        {
            if (mounts == null || mounts.Count == 0)
            {
                return;
            }

            mountIndex = (mountIndex + 1) % mounts.Count;
            RefreshMountDisplay();
        }

        private void RefreshMountDisplay()
        {
            var mount = GetItemAt(mounts, mountIndex);
            UpdatePreviewImage(mountPreviewImage, mount?.icon);
            UpdateNameText(mountNameText, mount);
            UpdateNavigationButtons(mountPreviousButton, mountNextButton, mounts);
        }

        // --- Create Fighter ---

        private void OnCreateFighterClicked()
        {
            // Read the name the player typed (or use a default if empty).
            var fighterName = fighterNameInput != null
                ? fighterNameInput.text.Trim()
                : string.Empty;

            if (string.IsNullOrEmpty(fighterName))
            {
                fighterName = "Unnamed Fighter";
            }

            // Pull the currently selected equipment from each list.
            var head = GetItemAt(heads, headIndex);
            var body = GetItemAt(bodies, bodyIndex);
            var weapon = GetItemAt(weapons, weaponIndex);
            var mount = GetItemAt(mounts, mountIndex);

            // Build the fighter and calculate stats from equipment.
            _currentFighter = new PlayerFighter();
            _currentFighter.fighterName = fighterName;
            _currentFighter.head = head;
            _currentFighter.body = body;
            _currentFighter.weapon = weapon;
            _currentFighter.mount = mount;
            _currentFighter.CalculateStats();

            // Store choices so other scenes can use them later.
            FighterSessionData.FighterName = fighterName;
            FighterSessionData.SelectedHead = head;
            FighterSessionData.SelectedBody = body;
            FighterSessionData.SelectedWeapon = weapon;
            FighterSessionData.SelectedMount = mount;

            // Show the confirmation card with preview images and stats.
            ShowFighterCardPanel(_currentFighter);
        }

        /// <summary>
        /// Fills the fighter card UI and shows the overlay panel.
        /// </summary>
        private void ShowFighterCardPanel(PlayerFighter fighter)
        {
            if (fighter == null)
            {
                return;
            }

            // Card preview uses the same equipment icons as the builder preview.
            UpdatePreviewImage(cardMountImage, fighter.mount?.icon);
            UpdatePreviewImage(cardBodyImage, fighter.body?.icon);
            UpdatePreviewImage(cardWeaponImage, fighter.weapon?.icon);
            UpdatePreviewImage(cardHeadImage, fighter.head?.icon);

            if (cardFighterNameText != null)
            {
                cardFighterNameText.text = fighter.fighterName;
            }

            if (cardAttackText != null)
            {
                cardAttackText.text = $"Attack: {fighter.attack}";
            }

            if (cardDefenseText != null)
            {
                cardDefenseText.text = $"Defense: {fighter.defense}";
            }

            if (cardSpeedText != null)
            {
                cardSpeedText.text = $"Speed: {fighter.speed}";
            }

            if (cardHealthText != null)
            {
                cardHealthText.text = $"Health: {fighter.maxHealth}";
            }

            if (fighterCardPanel != null)
            {
                fighterCardPanel.SetActive(true);
            }
        }

        /// <summary>
        /// Hides the fighter card and returns to the builder screen.
        /// </summary>
        private void OnBackEditClicked()
        {
            if (fighterCardPanel != null)
            {
                fighterCardPanel.SetActive(false);
            }
        }

        /// <summary>
        /// Placeholder for future battle flow — logs only for now.
        /// </summary>
        private void OnStartBattleClicked()
        {
            var fighterName = _currentFighter != null
                ? _currentFighter.fighterName
                : FighterSessionData.FighterName ?? "Unknown";

            Debug.Log($"Start Battle clicked for fighter: {fighterName}");
        }

        // --- Shared UI helpers ---

        /// <summary>
        /// Safely returns the item at an index, or null if the list is empty.
        /// </summary>
        private static T GetItemAt<T>(List<T> list, int index) where T : class
        {
            if (list == null || list.Count == 0)
            {
                return null;
            }

            return list[index];
        }

        /// <summary>
        /// Sets a preview image sprite and keeps the icon's proportions.
        /// </summary>
        private static void UpdatePreviewImage(Image image, Sprite sprite)
        {
            if (image == null)
            {
                return;
            }

            image.preserveAspect = true;
            image.sprite = sprite;
            image.enabled = sprite != null;
        }

        /// <summary>
        /// Shows the item name, or "None" when nothing is available.
        /// </summary>
        private static void UpdateNameText(TMP_Text nameText, EquipmentItemSO item)
        {
            if (nameText == null)
            {
                return;
            }

            nameText.text = item != null ? item.itemName : "None";
        }

        /// <summary>
        /// Enables Previous/Next only when there are 2+ items to cycle through.
        /// Disables both when the list is empty or has a single item.
        /// </summary>
        private static void UpdateNavigationButtons<T>(Button previousButton, Button nextButton, List<T> list)
        {
            var canCycle = list != null && list.Count > 1;

            if (previousButton != null)
            {
                previousButton.interactable = canCycle;
            }

            if (nextButton != null)
            {
                nextButton.interactable = canCycle;
            }
        }
    }
}

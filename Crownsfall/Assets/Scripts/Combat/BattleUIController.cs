using System.Text;
using Crownsfall.Characters;
using TMPro;
using UnityEngine;

namespace Crownsfall.Combat
{
    /// <summary>
    /// Battle HUD overlay on a Screen Space Canvas.
    /// Shows stat cards for player and enemy, a VS label, and a scrolling-style battle log.
    /// BattleManager fills this at scene start; combat scripts can call AddLogLine later.
    /// </summary>
    public class BattleUIController : MonoBehaviour
    {
        [Header("Player Stat Card (bottom-left)")]
        [SerializeField] private TMP_Text playerNameText;
        [SerializeField] private TMP_Text playerAttackText;
        [SerializeField] private TMP_Text playerDefenseText;
        [SerializeField] private TMP_Text playerSpeedText;
        [SerializeField] private TMP_Text playerHealthText;

        [Header("Enemy Stat Card (bottom-right)")]
        [SerializeField] private TMP_Text enemyNameText;
        [SerializeField] private TMP_Text enemyAttackText;
        [SerializeField] private TMP_Text enemyDefenseText;
        [SerializeField] private TMP_Text enemySpeedText;
        [SerializeField] private TMP_Text enemyHealthText;

        [Header("Center")]
        [SerializeField] private TMP_Text vsText;

        [Header("Battle Log")]
        [SerializeField] private TMP_Text battleLogText;

        [Tooltip("Maximum log lines kept on screen before older lines drop off.")]
        [SerializeField] private int maxLogLines = 12;

        private readonly StringBuilder _logBuilder = new StringBuilder();
        private int _logLineCount;

        /// <summary>
        /// Fills the bottom-left player stat card from a PlayerFighter.
        /// </summary>
        public void SetPlayerStats(PlayerFighter fighter)
        {
            if (fighter == null)
            {
                SetStatTexts(playerNameText, playerAttackText, playerDefenseText, playerSpeedText, playerHealthText,
                    "—", 0, 0, 0, 0);
                return;
            }

            SetStatTexts(
                playerNameText,
                playerAttackText,
                playerDefenseText,
                playerSpeedText,
                playerHealthText,
                fighter.fighterName,
                fighter.attack,
                fighter.defense,
                fighter.speed,
                fighter.maxHealth);
        }

        /// <summary>
        /// Fills the bottom-right enemy stat card from a PlayerFighter.
        /// </summary>
        public void SetEnemyStats(PlayerFighter fighter)
        {
            if (fighter == null)
            {
                SetStatTexts(enemyNameText, enemyAttackText, enemyDefenseText, enemySpeedText, enemyHealthText,
                    "—", 0, 0, 0, 0);
                return;
            }

            SetStatTexts(
                enemyNameText,
                enemyAttackText,
                enemyDefenseText,
                enemySpeedText,
                enemyHealthText,
                fighter.fighterName,
                fighter.attack,
                fighter.defense,
                fighter.speed,
                fighter.maxHealth);
        }

        /// <summary>
        /// Appends one line to the battle log panel (newest at the bottom).
        /// </summary>
        public void AddLogLine(string message)
        {
            if (battleLogText == null || string.IsNullOrEmpty(message))
            {
                return;
            }

            if (_logLineCount >= maxLogLines && _logBuilder.Length > 0)
            {
                TrimOldestLogLine();
            }

            if (_logBuilder.Length > 0)
            {
                _logBuilder.AppendLine();
            }

            _logBuilder.Append(message);
            _logLineCount++;
            battleLogText.text = _logBuilder.ToString();
        }

        /// <summary>
        /// Clears all battle log text.
        /// </summary>
        public void ClearLog()
        {
            _logBuilder.Clear();
            _logLineCount = 0;

            if (battleLogText != null)
            {
                battleLogText.text = string.Empty;
            }
        }

        /// <summary>
        /// Optional: set the large VS label (defaults to "VS" in the setup tool).
        /// </summary>
        public void SetVsLabel(string label)
        {
            if (vsText != null)
            {
                vsText.text = label;
            }
        }

        /// <summary>
        /// Writes name and stat numbers into five TMP fields on one stat card.
        /// </summary>
        private static void SetStatTexts(
            TMP_Text nameText,
            TMP_Text attackText,
            TMP_Text defenseText,
            TMP_Text speedText,
            TMP_Text healthText,
            string fighterName,
            int attack,
            int defense,
            int speed,
            int health)
        {
            SetText(nameText, fighterName);
            SetText(attackText, $"ATK {attack}");
            SetText(defenseText, $"DEF {defense}");
            SetText(speedText, $"SPD {speed}");
            SetText(healthText, $"HP {health}");
        }

        private static void SetText(TMP_Text textField, string value)
        {
            if (textField != null)
            {
                textField.text = value;
            }
        }

        /// <summary>
        /// Removes the first line from the log when we hit maxLogLines.
        /// </summary>
        private void TrimOldestLogLine()
        {
            var fullText = _logBuilder.ToString();
            var firstNewline = fullText.IndexOf('\n');

            _logBuilder.Clear();

            if (firstNewline >= 0 && firstNewline + 1 < fullText.Length)
            {
                _logBuilder.Append(fullText.Substring(firstNewline + 1));
            }

            _logLineCount = Mathf.Max(0, _logLineCount - 1);
        }
    }
}

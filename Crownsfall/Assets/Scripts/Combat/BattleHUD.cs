using System.Text;
using Crownsfall.Characters;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Crownsfall.Combat
{
    /// <summary>
    /// Production mobile battle HUD on a Screen Space Canvas.
    /// Top bar (title, wave, score), fighter area, battle log, and bottom stat summaries.
    /// BattleManager fills this at scene start; combat scripts can update it later.
    /// </summary>
    public class BattleHUD : MonoBehaviour
    {
        [Header("Top Bar")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text waveText;
        [SerializeField] private TMP_Text scoreText;

        [Header("Battle Log")]
        [SerializeField] private TMP_Text battleLogText;

        [Header("Bottom Stats")]
        [SerializeField] private TMP_Text playerStatsText;
        [SerializeField] private TMP_Text enemyStatsText;

        [Header("Health Bars (Filled Image)")]
        [Tooltip("The green/red fill child inside PlayerHealthBar.")]
        [SerializeField] private Image playerHealthBarFill;

        [Tooltip("The green/red fill child inside EnemyHealthBar.")]
        [SerializeField] private Image enemyHealthBarFill;

        [Tooltip("Maximum log lines kept on screen before older lines drop off.")]
        [SerializeField] private int maxLogLines = 12;

        private readonly StringBuilder _logBuilder = new StringBuilder();
        private int _logLineCount;

        /// <summary>
        /// Resets wave, score, and log to battle-start defaults.
        /// Call once when the battle scene loads.
        /// </summary>
        public void InitializeForBattle()
        {
            SetWave(1);
            SetScore(0);
            ClearLog();
            AddLogLine("Battle begins!");
        }

        /// <summary>
        /// Updates the wave label in the top bar (e.g. "Wave 3").
        /// </summary>
        public void SetWave(int wave)
        {
            SetText(waveText, $"Wave {wave}");
        }

        /// <summary>
        /// Updates the score label in the top bar.
        /// </summary>
        public void SetScore(int score)
        {
            SetText(scoreText, $"Score: {score}");
        }

        /// <summary>
        /// Fills the bottom-left player stat block: name, ATK, DEF, SPD, HP.
        /// </summary>
        public void SetPlayerStats(PlayerFighter fighter)
        {
            SetText(playerStatsText, BuildStatsBlock(fighter));
        }

        /// <summary>
        /// Fills the bottom-right enemy stat block: name, ATK, DEF, SPD, HP.
        /// </summary>
        public void SetEnemyStats(PlayerFighter fighter)
        {
            SetText(enemyStatsText, BuildStatsBlock(fighter));
        }

        /// <summary>
        /// Sets the player health bar fill (0 = empty, 1 = full).
        /// </summary>
        public void SetPlayerHealth(float current, float max)
        {
            SetHealthFill(playerHealthBarFill, current, max);
        }

        /// <summary>
        /// Sets the enemy health bar fill (0 = empty, 1 = full).
        /// </summary>
        public void SetEnemyHealth(float current, float max)
        {
            SetHealthFill(enemyHealthBarFill, current, max);
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
        /// Optional: override the title (defaults to scene value, e.g. "CROWNSFALL").
        /// </summary>
        public void SetTitle(string title)
        {
            SetText(titleText, title);
        }

        /// <summary>
        /// Builds a multi-line stat string for one fighter.
        /// </summary>
        private static string BuildStatsBlock(PlayerFighter fighter)
        {
            if (fighter == null)
            {
                return "—";
            }

            // Three-line mobile layout: name, ATK/DEF row, SPD/HP row (extra spacing for readability).
            return $"{fighter.fighterName}\n" +
                   $"ATK {fighter.attack}   DEF {fighter.defense}\n" +
                   $"SPD {fighter.speed}   HP {fighter.currentHealth}/{fighter.maxHealth}";
        }

        /// <summary>
        /// Converts current/max HP into a 0–1 fill amount on a Filled Image.
        /// </summary>
        private static void SetHealthFill(Image fillImage, float current, float max)
        {
            if (fillImage == null)
            {
                return;
            }

            var fillAmount = max > 0f ? Mathf.Clamp01(current / max) : 0f;
            fillImage.fillAmount = fillAmount;
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

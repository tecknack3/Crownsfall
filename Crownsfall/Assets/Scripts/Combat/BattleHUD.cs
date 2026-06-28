using System.Text;
using Crownsfall.Characters;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
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

        [Header("Game Over Panel")]
        [Tooltip("Full-screen overlay shown when the player is defeated. Hidden at battle start.")]
        [SerializeField] private GameObject gameOverPanel;

        [SerializeField] private TMP_Text gameOverTitleText;
        [SerializeField] private TMP_Text gameOverFighterNameText;
        [SerializeField] private TMP_Text gameOverFinalScoreText;
        [SerializeField] private TMP_Text gameOverWaveReachedText;
        [SerializeField] private TMP_Text gameOverEnemiesDefeatedText;
        [SerializeField] private TMP_Text gameOverHighestWaveText;

        [SerializeField] private Button playAgainButton;
        [SerializeField] private Button characterBuilderButton;

        private readonly StringBuilder _logBuilder = new StringBuilder();
        private int _logLineCount;

        private void Awake()
        {
            // Panel starts hidden; BattleManager shows it only after player death.
            HideGameOver();

            if (playAgainButton != null)
            {
                playAgainButton.onClick.AddListener(OnPlayAgainClicked);
            }

            if (characterBuilderButton != null)
            {
                characterBuilderButton.onClick.AddListener(OnCharacterBuilderClicked);
            }
        }

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
            SetWave(wave, false);
        }

        /// <summary>
        /// Updates the wave label; boss waves show "Wave 10 - BOSS".
        /// </summary>
        public void SetWave(int wave, bool isBoss)
        {
            var label = isBoss ? $"Wave {wave} - BOSS" : $"Wave {wave}";
            SetText(waveText, label);
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
        /// Fills the bottom-right enemy stat block from an EnemyFighter (wave enemies from EnemyFactory).
        /// </summary>
        public void SetEnemyStats(EnemyFighter enemy)
        {
            SetText(enemyStatsText, BuildEnemyStatsBlock(enemy));
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
        /// Hides the game over overlay (called when a new battle begins).
        /// </summary>
        public void HideGameOver()
        {
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(false);
            }
        }

        /// <summary>
        /// Shows the game over overlay with run stats after the player is defeated.
        /// </summary>
        public void ShowGameOver(
            string fighterName,
            int finalScore,
            int waveReached,
            int enemiesDefeated,
            int highestWaveReached)
        {
            SetText(gameOverTitleText, "YOU HAVE FALLEN");
            SetText(gameOverFighterNameText, fighterName);
            SetText(gameOverFinalScoreText, $"Final Score: {finalScore}");
            SetText(gameOverWaveReachedText, $"Wave Reached: {waveReached}");
            SetText(gameOverEnemiesDefeatedText, $"Enemies Defeated: {enemiesDefeated}");
            SetText(gameOverHighestWaveText, $"Highest Wave: {highestWaveReached}");

            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(true);
            }
        }

        /// <summary>
        /// Reloads BattleScene. GameSession keeps the same fighter across the load.
        /// </summary>
        private void OnPlayAgainClicked()
        {
            SceneManager.LoadScene("BattleScene");
        }

        /// <summary>
        /// Returns to Character Builder so the player can edit or rebuild their fighter.
        /// </summary>
        private void OnCharacterBuilderClicked()
        {
            SceneManager.LoadScene("CharacterBuilder");
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

            return BuildStatsBlock(
                fighter.fighterName,
                fighter.attack,
                fighter.defense,
                fighter.speed,
                fighter.currentHealth,
                fighter.maxHealth);
        }

        /// <summary>
        /// Builds a multi-line stat string for one wave enemy.
        /// </summary>
        private static string BuildEnemyStatsBlock(EnemyFighter enemy)
        {
            if (enemy == null)
            {
                return "—";
            }

            return BuildStatsBlock(
                enemy.enemyName,
                enemy.attack,
                enemy.defense,
                enemy.speed,
                enemy.currentHealth,
                enemy.maxHealth);
        }

        /// <summary>
        /// Shared three-line mobile layout: name, ATK/DEF row, SPD/HP row.
        /// </summary>
        private static string BuildStatsBlock(
            string name,
            int attack,
            int defense,
            int speed,
            int currentHealth,
            int maxHealth)
        {
            return $"{name}\n" +
                   $"ATK {attack}   DEF {defense}\n" +
                   $"SPD {speed}   HP {currentHealth}/{maxHealth}";
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

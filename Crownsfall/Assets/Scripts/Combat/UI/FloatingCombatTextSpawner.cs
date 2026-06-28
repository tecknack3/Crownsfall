using Crownsfall.Combat;
using Crownsfall.Combat.Events;
using UnityEngine;

namespace Crownsfall.Combat.UI
{
    /// <summary>
    /// Listens to CombatEventBus and spawns floating numbers above fighter rigs.
    /// Wire player/enemy FighterRig references and the FloatingCombatText prefab in the Inspector,
    /// or run Tools → Fighter Tools → Setup Battle Scene Production UI to auto-wire the battle scene.
    /// </summary>
    public class FloatingCombatTextSpawner : MonoBehaviour
    {
        public const string FloatingTextLayerName = "FloatingTextLayer";
        public const int FloatingTextSortingOrder = 999;

        private static readonly Color DamageColor = Color.white;
        private static readonly Color HealColor = new Color(0.267f, 1f, 0.4f, 1f);
        private static readonly Color CritColor = new Color(1f, 0.69f, 0.125f, 1f);

        private const float VerticalSpawnOffset = 120f;
        private const float SpawnScale = 1.2f;

        [Header("Prefab")]
        [SerializeField] private FloatingCombatText floatingCombatTextPrefab;

        [Header("Canvas")]
        [SerializeField] private Canvas targetCanvas;

        [Header("Fighter Rigs")]
        [SerializeField] private FighterRig playerFighterRig;
        [SerializeField] private FighterRig enemyFighterRig;

        private Canvas _canvas;
        private RectTransform _floatingTextLayerRect;

        private void Awake()
        {
            FindSceneReferencesIfNeeded();
            ResolveCanvas();
            GetOrCreateFloatingTextLayer();
        }

        private void OnEnable()
        {
            CombatEventBus.OnCombatEvent += HandleCombatEvent;
        }

        private void OnDisable()
        {
            CombatEventBus.OnCombatEvent -= HandleCombatEvent;
        }

        private void OnDestroy()
        {
            CombatEventBus.OnCombatEvent -= HandleCombatEvent;
        }

        /// <summary>
        /// Ensures a dedicated overlay layer exists under the canvas for floating combat text.
        /// </summary>
        public static RectTransform EnsureFloatingTextLayer(Canvas canvas)
        {
            if (canvas == null)
            {
                return null;
            }

            var canvasTransform = canvas.transform;
            var existing = canvasTransform.Find(FloatingTextLayerName);
            RectTransform layerRect;

            if (existing != null)
            {
                layerRect = existing as RectTransform;
            }
            else
            {
                var layerObject = new GameObject(FloatingTextLayerName, typeof(RectTransform));
                layerObject.transform.SetParent(canvasTransform, false);
                layerRect = layerObject.GetComponent<RectTransform>();
                layerRect.anchorMin = Vector2.zero;
                layerRect.anchorMax = Vector2.one;
                layerRect.offsetMin = Vector2.zero;
                layerRect.offsetMax = Vector2.zero;
            }

            var layerCanvas = layerRect.GetComponent<Canvas>();
            if (layerCanvas == null)
            {
                layerCanvas = layerRect.gameObject.AddComponent<Canvas>();
            }

            layerCanvas.overrideSorting = true;
            layerCanvas.sortingOrder = FloatingTextSortingOrder;

            layerRect.SetAsLastSibling();
            return layerRect;
        }

        private void HandleCombatEvent(CombatEvent combatEvent)
        {
            if (combatEvent == null || floatingCombatTextPrefab == null)
            {
                return;
            }

            switch (combatEvent.eventType)
            {
                case CombatEventType.DamageDealt:
                    HandleDamageDealt(combatEvent);
                    break;

                case CombatEventType.HealingReceived:
                    SpawnForFighter(
                        playerFighterRig,
                        $"+{combatEvent.amount} HP",
                        HealColor,
                        combatEvent.eventType);
                    break;

                case CombatEventType.SkillTriggered:
                    HandleSkillTriggered(combatEvent);
                    break;
            }
        }

        private void HandleDamageDealt(CombatEvent combatEvent)
        {
            if (combatEvent.amount <= 0)
            {
                return;
            }

            var text = $"-{combatEvent.amount}";

            if (IsDamageToEnemy(combatEvent))
            {
                SpawnForFighter(enemyFighterRig, text, DamageColor, combatEvent.eventType);
                return;
            }

            if (IsDamageToPlayer(combatEvent))
            {
                SpawnForFighter(playerFighterRig, text, DamageColor, combatEvent.eventType);
            }
        }

        /// <summary>
        /// v1 only shows a crit label for Critical Strike ("CRITICAL HIT!").
        /// The actual crit damage number comes from the following DamageDealt event.
        /// </summary>
        private void HandleSkillTriggered(CombatEvent combatEvent)
        {
            if (!IsCriticalHitMessage(combatEvent.message))
            {
                return;
            }

            SpawnForFighter(enemyFighterRig, "CRIT!", CritColor, combatEvent.eventType);
        }

        private void SpawnForFighter(FighterRig fighterRig, string text, Color color, CombatEventType eventType)
        {
            if (fighterRig == null)
            {
                Debug.LogWarning($"[FloatingCombatTextSpawner] Cannot spawn '{text}' ({eventType}): FighterRig reference is missing.");
                return;
            }

            if (_canvas == null)
            {
                ResolveCanvas();
            }

            if (_canvas == null)
            {
                Debug.LogWarning($"[FloatingCombatTextSpawner] Cannot spawn '{text}' ({eventType}): Canvas not found in scene.");
                return;
            }

            GetOrCreateFloatingTextLayer();

            if (_floatingTextLayerRect == null)
            {
                Debug.LogWarning($"[FloatingCombatTextSpawner] Cannot spawn '{text}' ({eventType}): FloatingTextLayer is missing.");
                return;
            }

            if (!TryResolveSpawnPosition(fighterRig, out var anchoredPosition, out var usedFallback))
            {
                Debug.LogWarning($"[FloatingCombatTextSpawner] Cannot spawn '{text}' ({eventType}): failed to resolve spawn position for {fighterRig.name}.");
                return;
            }

            anchoredPosition += new Vector2(0f, VerticalSpawnOffset);

            var instance = Instantiate(floatingCombatTextPrefab, _floatingTextLayerRect);
            var rectTransform = instance.transform as RectTransform;

            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = anchoredPosition;
                rectTransform.localScale = Vector3.one * SpawnScale;
                rectTransform.SetAsLastSibling();
            }

            instance.Initialize(text, color);

            var fallbackNote = usedFallback ? " (fallback rig position)" : string.Empty;
            Debug.Log(
                $"[FloatingCombatTextSpawner] Spawned floating text: {text} at anchoredPosition {anchoredPosition}{fallbackNote}");
        }

        private bool TryResolveSpawnPosition(FighterRig fighterRig, out Vector2 anchoredPosition, out bool usedFallback)
        {
            anchoredPosition = Vector2.zero;
            usedFallback = false;

            var damageAnchor = fighterRig.DamageAnchor;
            Transform sourceTransform;

            if (damageAnchor != null)
            {
                sourceTransform = damageAnchor;
            }
            else
            {
                Debug.LogWarning(
                    $"[FloatingCombatTextSpawner] {fighterRig.name} has no DamageAnchor — using rig root + Y offset {VerticalSpawnOffset}.");
                sourceTransform = fighterRig.transform;
                usedFallback = true;
            }

            return TryWorldPointToLayerLocal(sourceTransform.position, out anchoredPosition);
        }

        private bool TryWorldPointToLayerLocal(Vector3 worldPoint, out Vector2 localPoint)
        {
            localPoint = Vector2.zero;

            var eventCamera = _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;
            var screenPoint = RectTransformUtility.WorldToScreenPoint(eventCamera, worldPoint);

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _floatingTextLayerRect,
                    screenPoint,
                    eventCamera,
                    out localPoint))
            {
                Debug.LogWarning(
                    $"[FloatingCombatTextSpawner] ScreenPointToLocalPointInRectangle failed for screen point {screenPoint} "
                    + $"(layer: {FloatingTextLayerName}, canvas: {_canvas.name}, renderMode: {_canvas.renderMode}).");
                return false;
            }

            return true;
        }

        private void GetOrCreateFloatingTextLayer()
        {
            _floatingTextLayerRect = EnsureFloatingTextLayer(_canvas);
        }

        private void ResolveCanvas()
        {
            _canvas = targetCanvas;

            if (_canvas == null)
            {
                _canvas = GetComponentInParent<Canvas>();
            }

            if (_canvas == null)
            {
                _canvas = FindObjectOfType<Canvas>();
            }

            if (_canvas == null)
            {
                Debug.LogWarning("[FloatingCombatTextSpawner] Canvas not found. Floating combat text will not spawn.");
                _floatingTextLayerRect = null;
            }
        }

        /// <summary>
        /// BattleManager always sets message on DamageDealt — use those prefixes first.
        /// </summary>
        private static bool IsDamageToEnemy(CombatEvent combatEvent)
        {
            var message = combatEvent.message;

            if (!string.IsNullOrEmpty(message))
            {
                if (message.StartsWith("Enemy dealt"))
                {
                    return false;
                }

                if (message.Contains("Player dealt")
                    || message.Contains("Burn dealt")
                    || message.Contains("Poison dealt"))
                {
                    return true;
                }
            }

            // Fallback when message is missing: player-side sources are fighter names, not skill/enemy labels.
            return !IsLikelyEnemySource(combatEvent.sourceName);
        }

        private static bool IsDamageToPlayer(CombatEvent combatEvent)
        {
            var message = combatEvent.message;

            if (!string.IsNullOrEmpty(message))
            {
                return message.StartsWith("Enemy dealt");
            }

            return IsLikelyEnemySource(combatEvent.sourceName);
        }

        private static bool IsLikelyEnemySource(string sourceName)
        {
            if (string.IsNullOrEmpty(sourceName))
            {
                return false;
            }

            var lower = sourceName.ToLowerInvariant();
            return lower.Contains("enemy") || lower.Contains("goblin") || lower.Contains("boss");
        }

        private static bool IsCriticalHitMessage(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return false;
            }

            return message.IndexOf("CRITICAL HIT", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        /// Finds rigs by scene object name when Inspector references are empty.
        /// </summary>
        private void FindSceneReferencesIfNeeded()
        {
            if (playerFighterRig == null)
            {
                var playerObject = GameObject.Find("PlayerFighterRig");
                if (playerObject != null)
                {
                    playerFighterRig = playerObject.GetComponent<FighterRig>();
                }
            }

            if (enemyFighterRig == null)
            {
                var enemyObject = GameObject.Find("EnemyFighterRig");
                if (enemyObject != null)
                {
                    enemyFighterRig = enemyObject.GetComponent<FighterRig>();
                }
            }
        }
    }
}

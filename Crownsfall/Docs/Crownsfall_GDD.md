# Crownsfall — Game Design Document

**Version:** 0.1 (Prototype)  
**Platform:** Android / iOS (mobile)  
**Genre:** Single-player endless auto-battler  
**Unity Project:** `Crownsfall/` (subfolder of repo root)  
**Last Updated:** June 2025

---

## Table of Contents

1. [Game Vision](#1-game-vision)
2. [Core Gameplay Loop](#2-core-gameplay-loop)
3. [Scene Flow](#3-scene-flow)
4. [Character Builder](#4-character-builder)
5. [Fighter Rig System](#5-fighter-rig-system)
6. [PlayerFighter Data Model](#6-playerfighter-data-model)
7. [Battle System](#7-battle-system)
8. [Enemy Progression](#8-enemy-progression)
9. [Scoring System](#9-scoring-system)
10. [Crown Holder System](#10-crown-holder-system)
11. [Hall of Kings](#11-hall-of-kings)
12. [Firebase Plan](#12-firebase-plan)
13. [AdMob Plan](#13-admob-plan)
14. [Firebase Remote Config Plan](#14-firebase-remote-config-plan)
15. [Analytics Events](#15-analytics-events)
16. [UI/UX Direction](#16-uiux-direction)
17. [Art Direction](#17-art-direction)
18. [Technical Architecture](#18-technical-architecture)
19. [Development Phases](#19-development-phases)
20. [Release Checklist](#20-release-checklist)
- [Appendix A: Cursor Prompt Templates](#appendix-a-cursor-prompt-templates)

---

## 1. Game Vision

### Elevator Pitch

**Crownsfall** is a mobile auto-battler where players assemble a unique fighter from modular equipment—Head, Body, Weapon, and Mount—then send them into an endless gauntlet of AI enemies. Every kill earns score. When the fighter dies, the run ends and the score is saved. The top player worldwide holds the **Crown**; their reign is tracked as **Longest Crown Reign**. Past champions are immortalized in the **Hall of Kings**.

### Player Fantasy

- **Build identity:** Name your fighter and mix gear to create a distinct look and stat profile.
- **Survive the gauntlet:** Watch your fighter auto-fight wave after wave without manual input during combat.
- **Climb the ranks:** Beat your personal best and compete on a global Firebase leaderboard.
- **Claim the Crown:** Become the reigning Crown Holder until someone surpasses your score.

### Design Pillars

| Pillar | Description |
|--------|-------------|
| **Modular fighters** | Four equipment slots drive both visuals and stats. Same rig system in builder preview, fighter card, and battle. |
| **Endless escalation** | Enemies scale in difficulty; no fixed endpoint—runs are measured by score and wave depth. |
| **Passive combat** | Auto-battle suits mobile sessions: build, launch, watch, react to results. |
| **Social competition** | Leaderboard, Crown Holder, and Hall of Kings create long-term goals beyond a single run. |
| **Mobile-first UI** | Screen Space Canvas, large touch targets, readable stat blocks, UI Image–based fighter rigs (not SpriteRenderer in production layout). |

### Target Audience

Casual-to-midcore mobile players who enjoy character customization, incremental difficulty, and asynchronous competition (leaderboards) without real-time PvP.

### Prototype Status (v0.1)

| Area | Status |
|------|--------|
| Character Builder scene + UI | Implemented |
| Fighter Card confirmation panel | Implemented |
| `PlayerFighter` data model | Implemented |
| `GameSession` singleton (cross-scene persistence) | Implemented |
| Battle scene prototype (`BattleScene`) | Implemented |
| `FighterRig` (UI Image layers) | Implemented |
| `BattleHUD` overlay | Implemented |
| Equipment ScriptableObjects + importer tools | Implemented |
| Combat (damage, turns, waves) | **Not implemented** |
| Firebase / AdMob / Remote Config | **Planned** |
| Crown Holder / Hall of Kings | **Planned** |

---

## 2. Core Gameplay Loop

```
┌─────────────────┐
│  Character      │  Pick Head, Body, Weapon, Mount; name fighter
│  Builder        │  → Create Fighter → Fighter Card
└────────┬────────┘
         │ Start Battle
         ▼
┌─────────────────┐
│  Battle Scene   │  Auto-combat vs escalating enemies
│  (Endless)      │  Score += on each enemy kill
└────────┬────────┘
         │ Player HP → 0
         ▼
┌─────────────────┐
│  Run End        │  Save score (local + Firebase)
│  Results        │  Check Crown / leaderboard rank
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│  Meta Loop      │  Retry with new build, view Hall of Kings,
│                 │  chase Crown Holder score
└─────────────────┘
```

### Session Flow (Player Actions)

1. **Customize** — Cycle equipment with Previous/Next; see live preview icons and stat implications.
2. **Commit** — Tap **Create Fighter**; review Fighter Card (name, stacked preview, ATK/DEF/SPD/HP).
3. **Enter battle** — Tap **Start Battle**; `GameSession` stores fighter; `BattleScene` loads.
4. **Watch** — Combat resolves automatically; HUD shows wave, score, health bars, battle log.
5. **Die** — Run ends; final score persisted; optional ad/retry/leaderboard UI (future).
6. **Return** — Back to Character Builder or Main Menu (Main Menu scene planned).

### Engagement Hooks

- **Short runs:** Early waves are fast; players can retry quickly with a new build.
- **Score chase:** Visible score counter during battle reinforces kill-by-kill progress.
- **Build experimentation:** Different gear combinations change survivability and kill speed.
- **Crown aspiration:** Global #1 is a clear, shareable goal.

---

## 3. Scene Flow

### Scenes (Current & Planned)

| Scene | Path | Status | Purpose |
|-------|------|--------|---------|
| `CharacterBuilder` | `Assets/Scenes/CharacterBuilder/CharacterBuilder.unity` | **Live** | Equipment selection, fighter creation, Fighter Card |
| `BattleScene` | `Assets/Scenes/BattleScene/BattleScene.unity` | **Live** | Battle HUD, FighterRigs, combat (combat pending) |
| `MainMenu` | `Assets/Scenes/MainMenu/` | Folder exists, scene pending | Title, Play, Leaderboard, Settings |
| `Results` | Planned | — | Post-death score, rank, Crown status |
| `HallOfKings` | Planned | — | Historical Crown Holder gallery |

### Scene Transition Diagram

```
MainMenu (future)
    │
    ├─► CharacterBuilder
    │         │
    │         │ Start Battle (SceneManager.LoadScene("BattleScene"))
    │         ▼
    │     BattleScene
    │         │
    │         │ On Player Death
    │         ▼
    │     Results (future)
    │         │
    │         ├─► CharacterBuilder (retry)
    │         ├─► MainMenu
    │         └─► HallOfKings (future)
    │
    └─► HallOfKings (future)
```

### Cross-Scene Data

| Mechanism | Namespace | Usage |
|-----------|-----------|-------|
| `GameSession.Instance` | `Crownsfall.Core` | Primary: `SetCurrentFighter(PlayerFighter)` before battle load; `DontDestroyOnLoad` |
| `FighterSessionData` | `Crownsfall.UI` | Static fallback: equipment refs + `CurrentFighter`; used if `GameSession` empty |
| `GameManager.Instance` | `Crownsfall.Core` | Global app shell (minimal today; expand for Firebase init) |

### Implementation Notes

- **Battle scene direct play:** `BattleManager` falls back to Inspector test equipment or `FighterSessionData` when no session fighter exists—useful for rapid iteration without running Character Builder every time.
- **Scene build order:** Ensure `CharacterBuilder` and `BattleScene` are in **File → Build Settings** before device builds.
- **Future:** Results scene should read final score from a `RunResult` object on `GameSession` rather than mutating `CurrentFighter` in place.

---

## 4. Character Builder

### Purpose

Let the player assemble a fighter from four equipment categories, name it, preview visuals and stats, confirm on a Fighter Card, and launch into battle.

### UI Layout (Implemented)

- **Equipment lists** — Wired in `CharacterBuilderManager` Inspector: `heads`, `bodies`, `weapons`, `mounts` (`List<HeadSO>`, etc.).
- **Preview images** — Four `Image` components show selected slot icons (`headPreviewImage`, etc.).
- **Navigation** — Previous/Next buttons per category; disabled when list has ≤1 item.
- **Name input** — `TMP_InputField`; defaults to `"Unnamed Fighter"` if empty.
- **Create Fighter** — Builds `PlayerFighter`, calculates stats, populates `FighterSessionData`, shows Fighter Card panel.
- **Fighter Card panel** — Full-screen overlay: stacked card images (mount → body → weapon → head), stat text, **Start Battle**, **Back / Edit**.

### Key Script

- `Assets/Scripts/UI/CharacterBuilderManager.cs` — `Crownsfall.UI`

### Equipment Selection Rules

- One item per slot; cycling wraps index modulo list count.
- Null/empty lists show `"None"` and hide preview image.
- Stats are not shown on builder screen during preview (only on Fighter Card after Create)—consider adding live stat summary in a future UX pass.

### Editor Tooling

| Menu Item | Script | Purpose |
|-----------|--------|---------|
| `Tools/Fighter Tools/Import All Equipment` | `EquipmentImporter` | Scan art folders → create/update ScriptableObjects |
| `Tools/Fighter Tools/...` (scene setup) | `CharacterBuilderSceneSetup`, `CharacterBuilderUIGenerator`, `CharacterBuilderAutoWire`, `CharacterBuilderFighterCardSetup` | Automate scene wiring |
| `Equipment Sprite Auto Assign` | `EquipmentSpriteAutoAssignWindow` | Batch assign icons to SOs |
| `Body ScriptableObject Generator` | `BodyScriptableObjectGenerator` | Category-specific SO generation |

### Art → SO Pipeline

```
Assets/Art/Heads/     → Assets/ScriptableObjects/Heads/
Assets/Art/Bodies/    → Assets/ScriptableObjects/Bodies/
Assets/Art/Weapons/   → Assets/ScriptableObjects/Weapons/
Assets/Art/Mounts/    → Assets/ScriptableObjects/Mounts/
```

Default stat templates on import (tune via Remote Config later):

| Category | Default ATK | DEF | SPD |
|----------|-------------|-----|-----|
| Body | 2 | 12 | 0 |
| Head | 1 | 3 | 1 |
| Weapon | 10 | 0 | 2 |
| Mount | 0 | 2 | 8 |

### Future Enhancements

- Unlock gating via `unlockLevel` / `unlockedByDefault` on `EquipmentItemSO`
- Shop / `purchaseCost` economy
- Rarity visual treatment on card and builder
- Filter/sort equipment lists
- Save favorite builds locally

---

## 5. Fighter Rig System

### Purpose

Display a fighter as a **stacked UI composition** on a Screen Space Canvas. Used in Battle scene production layout; mirrors preview logic from Character Builder and Fighter Card.

### Production Decision

**Use `UnityEngine.UI.Image` layers on RectTransforms—not `SpriteRenderer`** in the battle production layout. This keeps fighters inside the mobile HUD canvas, simplifies safe-area scaling, and avoids world-space camera setup.

### Component

- `Assets/Scripts/Combat/FighterRig.cs` — `Crownsfall.Combat`

### Layer Stack (back → front)

| Layer | Child Image | Offset (default) | Source |
|-------|-------------|------------------|--------|
| Mount | `mountImage` | `(0, -80)` | `fighter.mount?.icon` |
| Body | `bodyImage` | `(0, 0)` | `fighter.body?.icon` |
| Weapon | `weaponImage` | `(45, 10)` | `fighter.weapon?.icon` |
| Head | `headImage` | `(0, 75)` | `fighter.head?.icon` |
| Crown | `crownImage` | `(0, 100)` | Hidden until Crown system adds sprite |

### Public API

| Method | Behavior |
|--------|----------|
| `DisplayFighter(PlayerFighter)` | Assign sprites; hide layers with null gear |
| `SetFacing(bool faceRight)` | Flip `localScale.x`; player right, enemy left |
| `SetColorTint(Color)` | Tint all layers (enemy uses reddish tint) |
| `ApplyLayerOffsets()` | Apply configured anchored positions |
| `ClearAllLayers()` | Remove all sprites |

### Battle Scene Wiring

- `PlayerFighterRig` — player side, white tint, faces right
- `EnemyFighterRig` — enemy side, tint `Color(1, 0.32, 0.32)`, faces left
- `BattleManager.DisplayFighters()` orchestrates both rigs

### Enemy Placeholder Behavior

Training Dummy has fixed stats and no equipment. `enemyMirrorPlayerAppearance` (default **true**) borrows player equipment **sprites only** for visibility during prototype—stats remain dummy values.

### Legacy / Alternate

- `BattleFighterView.cs` — older/alternate view path; production uses `FighterRig`
- `FighterPreview.cs` / `EquipmentSelector.cs` — builder-specific preview helpers

### Future

- Crown sprite when player is Crown Holder or when displaying reigning king in Hall of Kings
- Attack animations via DOTween/Animator on rig root or weapon layer
- Hit flash tint without losing facing direction

---

## 6. PlayerFighter Data Model

### Class

- `Assets/Scripts/Characters/PlayerFighter.cs` — `Crownsfall.Characters`
- Plain C# class (no `MonoBehaviour`) — safe for logic layer and future server snapshots

### Fields

| Field | Type | Description |
|-------|------|-------------|
| `fighterId` | `string` | GUID on construction |
| `fighterName` | `string` | Player-chosen name |
| `head`, `body`, `weapon`, `mount` | `HeadSO`, `BodySO`, `WeaponSO`, `MountSO` | Equipment references |
| `attack`, `defense`, `speed` | `int` | Sum of equipment stats |
| `maxHealth`, `currentHealth` | `int` | Derived from defense |
| `currentScore`, `highestScore` | `int` | Run and personal best |
| `isAlive` | `bool` | Set false on `Die()` |

### Stat Formulas (Current)

```
attack   = sum(head, body, weapon, mount).attack
defense  = sum(...).defense
speed    = sum(...).speed
maxHealth = 100 + defense * 10
currentHealth = maxHealth  (on CalculateStats / ResetHealth)
```

### Methods

| Method | Purpose |
|--------|---------|
| `CalculateStats()` | Recompute ATK/DEF/SPD/HP from equipment |
| `ResetHealth()` | Full heal to `maxHealth` |
| `AddScore(int)` | Increment `currentScore`; update `highestScore` |
| `TakeDamage(int)` | Reduce HP; call `Die()` at 0 |
| `Heal(int)` | Clamp heal to max |
| `Die()` | `isAlive = false` |

### Equipment ScriptableObjects

Base: `EquipmentItemSO` (`Crownsfall.Characters`)

| Type | Class | Key Fields |
|------|-------|------------|
| Head | `HeadSO` | `id`, `itemName`, `icon`, `attack`, `defense`, `speed`, `rarity`, `unlockLevel`, `purchaseCost` |
| Body | `BodySO` | same |
| Weapon | `WeaponSO` | same |
| Mount | `MountSO` | same |

Enums: `EquipmentType`, `EquipmentRarity` (Common → Legendary)

### Enemy Representation (Prototype)

Enemies reuse `PlayerFighter` for simplicity. Future: `EnemyFighter` subclass or `IFighter` interface with `EnemyDefinitionSO` for procedural scaling without equipment SOs.

### Serialization for Firebase

Recommended leaderboard payload (future):

```json
{
  "fighterId": "guid",
  "fighterName": "Sir Lancelot",
  "score": 1250,
  "wave": 18,
  "equipment": {
    "headId": "head_01",
    "bodyId": "body_03",
    "weaponId": "sword_02",
    "mountId": "horse_01"
  },
  "timestamp": 1719500000
}
```

---

## 7. Battle System

### Current State

`BattleManager` loads fighters, displays rigs, populates `BattleHUD`. **No combat loop exists yet.**

### Target Architecture

```
BattleManager (orchestrator)
    ├── CombatController (turn/tick loop)      [NEW]
    ├── EnemyWaveManager (spawn + scaling)     [NEW]
    ├── DamageResolver (ATK/DEF math)          [NEW]
    ├── BattleHUD (presentation)
    └── FighterRig x2 (presentation)
```

Namespace: `Crownsfall.Combat`

### Auto-Battle Loop (Spec)

1. **Initialize** — Clone player from `GameSession`; spawn wave 1 enemy; HUD `InitializeForBattle()`.
2. **Combat tick** — Repeat until player or enemy dead:
   - Determine action order by `speed` (higher speed acts first; tie → player priority).
   - Attacker deals damage: `damage = max(1, attacker.attack - defender.defense)` (baseline formula—tune via Remote Config).
   - Update health bars, stat text, battle log line.
   - Brief delay (0.5–1.0s) for readability on mobile.
3. **Enemy defeated** — `AddScore(points)`, increment wave, spawn next enemy with scaled stats, `SetWave(n)`.
4. **Player defeated** — End run, persist score, transition to Results (future).

### BattleHUD Integration (Existing API)

| Method | When to call |
|--------|--------------|
| `InitializeForBattle()` | Scene start |
| `SetWave(int)` | Each new enemy |
| `SetScore(int)` | After each kill |
| `SetPlayerHealth / SetEnemyHealth` | After each damage |
| `SetPlayerStats / SetEnemyStats` | After stat changes |
| `AddLogLine(string)` | Each action |

### Training Dummy (Wave 1 Placeholder)

| Stat | Value |
|------|-------|
| Name | Training Dummy |
| ATK | 5 |
| DEF | 2 |
| SPD | 1 |
| HP | 100 |

Replace with `EnemyWaveManager` procedural enemies after combat MVP.

### Non-Goals (v0.1 Combat MVP)

- Manual player input during fight
- Skills / abilities
- Status effects
- Multi-enemy waves (1v1 only for MVP)

---

## 8. Enemy Progression

### Design Goal

Each kill should feel slightly harder than the last. Difficulty scales infinitely but smoothly so early runs teach the loop and late runs test build optimization.

### Wave Index

- `wave` starts at 1 on battle start (`BattleHUD.SetWave(1)`).
- Increment after each enemy death.

### Scaling Model (Recommended)

```
enemyAttack  = baseAttack  + (wave - 1) * attackPerWave
enemyDefense = baseDefense + (wave - 1) * defensePerWave
enemySpeed   = baseSpeed   + (wave - 1) * speedPerWave
enemyMaxHP   = baseHP      + (wave - 1) * hpPerWave
```

**Default bases** (match Training Dummy): ATK 5, DEF 2, SPD 1, HP 100.

**Default per-wave increments** (starting tuning):

| Stat | Per Wave |
|------|----------|
| ATK | +2 |
| DEF | +1 |
| SPD | +0.5 (round) |
| HP | +15 |

All coefficients should be Remote Config keys for live tuning without app update.

### Enemy Naming

Prototype: `"Training Dummy"` or `"Wave {n} Challenger"`. Future: procedural name tables or themed factions per wave bracket.

### Visual Progression

- Continue tinting enemies (redder or darker per wave tier)
- Optional: swap to higher-tier equipment mirror sets at wave brackets (10, 25, 50…)

### Score vs Difficulty

Higher waves should yield more score per kill (see Scoring System) so deep runs reward risk.

---

## 9. Scoring System

### Principles

- Score is earned **only on enemy kills** during a run.
- Score is **run-scoped** (`currentScore`); resets each battle.
- On death, run score is **saved** locally and submitted to Firebase leaderboard.
- `highestScore` on `PlayerFighter` tracks personal best across runs (persist locally).

### Base Formula (Recommended)

```
killScore = baseKillPoints + wave * waveMultiplier
```

Example: `baseKillPoints = 10`, `waveMultiplier = 5` → Wave 1 = 15 pts, Wave 10 = 60 pts.

### Implementation Hook

Call `playerFighter.AddScore(killScore)` then `battleHUD.SetScore(playerFighter.currentScore)`.

### Run End Persistence

On player death:

1. Capture `finalScore = playerFighter.currentScore`, `finalWave = currentWave`.
2. Update local personal best if `finalScore > savedBest`.
3. Submit to Firebase leaderboard (async).
4. Evaluate Crown Holder rules (score only for MVP).

### Anti-Cheat (Future)

- Server-side validation via Cloud Functions
- Rate-limit submissions
- Flag impossibly high scores for review

### UI Surfaces

- Battle HUD top bar: live score
- Results screen: final score, personal best delta, global rank
- Leaderboard: top N entries

---

## 10. Crown Holder System

### Concept

The player with the **highest global leaderboard score** is the **Crown Holder**. Only one reigning king at a time. When surpassed, the crown transfers to the new top scorer.

### Data (Firebase)

**Document:** `meta/crownHolder`

| Field | Type | Description |
|-------|------|-------------|
| `fighterName` | string | Display name |
| `score` | int | Score that earned the crown |
| `fighterId` | string | Unique fighter id |
| `equipmentIds` | map | head/body/weapon/mount ids for display |
| `crownedAt` | timestamp | When they took the crown |
| `playerId` | string | Firebase Auth uid or anonymous id |

### Longest Crown Reign

Track duration: `now - crownedAt` while holder retains #1.

When crown transfers:

1. Compute reign duration for outgoing holder.
2. If duration > `hallOfKings` longest entry, record in Hall of Kings.
3. Update `crownedAt` for new holder.

### Client Behavior

- Fetch crown holder on Main Menu / Results / Hall of Kings entry
- If local player is crown holder: show crown on `FighterRig.crownImage` in builder and battle
- Push notification (future): "Your crown was taken!"

### MVP Scope

- Read crown holder from Firestore
- Display name + score + reign timer on Results if player wins crown or is dethroned
- Full reign history → Hall of Kings (Phase 2)

---

## 11. Hall of Kings

### Concept

A gallery honoring fighters who achieved the **longest Crown Reign**—not just highest score, but longest time spent as #1.

### Entry Schema

| Field | Description |
|-------|-------------|
| `fighterName` | Champion name |
| `reignDurationSeconds` | Length of reign |
| `peakScore` | Score when crowned / dethroned |
| `crownedAt`, `dethronedAt` | Timestamps |
| `equipmentSnapshot` | ids for FighterRig display |
| `rank` | Sort by `reignDurationSeconds` |

### UI (Planned Scene)

- Scrollable list: rank, name, reign duration, peak score
- Tap entry → detail card with FighterRig preview
- "Current Crown Holder" banner at top with live reign timer

### Firebase Collection

`hallOfKings` — ordered query by `reignDurationSeconds` desc, limit 50.

### Relation to Leaderboard

| System | Measures |
|--------|----------|
| Leaderboard | Highest single-run score |
| Crown Holder | Who is #1 right now |
| Hall of Kings | Longest time at #1 historically |

---

## 12. Firebase Plan

### Services

| Service | Use |
|---------|-----|
| **Firebase Auth** | Anonymous auth for leaderboard identity (upgrade to Google/Apple later) |
| **Firestore** | Leaderboard entries, crown holder doc, hall of kings |
| **Realtime Database** | Optional for live crown timer; Firestore sufficient for MVP |
| **Firebase Analytics** | Event tracking (see Analytics section) |
| **Remote Config** | Balance tuning (see Remote Config section) |
| **Cloud Functions** | Validate scores, crown transfer, hall of kings writes |

### Package Setup (Unity)

1. Add Firebase Unity SDK via Package Manager or `FirebaseApp.unitypackage`.
2. Place `google-services.json` (Android) and `GoogleService-Info.plist` (iOS) in project.
3. Initialize in `GameManager` or dedicated `FirebaseBootstrap` (`Crownsfall.Services`).

### Firestore Structure

```
leaderboards/
  global/
    entries/{entryId}
      - fighterName, score, wave, fighterId, equipmentIds, playerId, timestamp

meta/
  crownHolder (single document)

hallOfKings/
  {entryId}
    - fighterName, reignDurationSeconds, peakScore, crownedAt, dethronedAt, equipmentSnapshot
```

### Leaderboard Query

- Collection: `leaderboards/global/entries`
- Order: `score` descending
- Limit: 100 for UI
- Client writes on run end; Cloud Function validates

### Namespace Plan

`Assets/Scripts/Services/` — `Crownsfall.Services`

Suggested classes:

- `FirebaseService` — init
- `LeaderboardService` — submit + fetch
- `CrownService` — fetch holder, check reign
- `HallOfKingsService` — fetch gallery

### Offline / Error Handling

- Queue failed submissions in PlayerPrefs; retry on next launch
- Show cached leaderboard with stale indicator
- Never block run end UI on network failure

---

## 13. AdMob Plan

### Monetization Model (Recommended for MVP)

| Ad Type | Placement | Trigger |
|---------|-----------|---------|
| **Interstitial** | Between runs | After Results screen, before Character Builder (frequency cap: 1 per 3 runs) |
| **Rewarded** | Optional revive | On death: "Watch ad to continue once" (future) |
| **Banner** | Low priority | Bottom of Main Menu only; avoid battle HUD |

### Unity Integration

- Google Mobile Ads Unity plugin
- Initialize after Firebase init in `GameManager`
- Test ad units in development; production units in release builds

### Namespace

`Crownsfall.Services.Ads` — `AdMobService`

### Compliance

- GDPR / COPPA considerations if targeting broad audiences
- ATT prompt on iOS before personalized ads
- Clear close buttons; no ads during active combat

### Remote Config Hooks

- `ads_enabled` — master kill switch
- `interstitial_frequency` — runs between ads
- `rewarded_revive_enabled` — feature flag

---

## 14. Firebase Remote Config Plan

### Purpose

Tune combat balance, enemy scaling, and scoring without shipping a new binary.

### Recommended Keys

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `combat_damage_formula` | string | `"max(1, atk - def)"` | Future: expression or enum |
| `base_kill_points` | int | 10 | Score per kill base |
| `wave_score_multiplier` | int | 5 | Extra points per wave tier |
| `enemy_attack_per_wave` | int | 2 | Scaling |
| `enemy_defense_per_wave` | int | 1 | Scaling |
| `enemy_hp_per_wave` | int | 15 | Scaling |
| `enemy_speed_per_wave` | float | 0.5 | Scaling |
| `health_per_defense` | int | 10 | HP formula multiplier |
| `base_health` | int | 100 | HP formula base |
| `ads_enabled` | bool | true | AdMob master switch |
| `interstitial_frequency` | int | 3 | Runs between interstitials |

### Client Flow

1. `FirebaseRemoteConfig.FetchAsync()` on app start
2. `ActivateAsync()` after fetch
3. `BalanceConfig` static class reads values with defaults fallback
4. `PlayerFighter.CalculateStats()` and combat use `BalanceConfig` instead of hardcoded literals (refactor when implementing)

### Namespace

`Crownsfall.Services` — `RemoteConfigService`, `BalanceConfig`

---

## 15. Analytics Events

### Firebase Analytics Event Catalog

| Event | Parameters | When |
|-------|------------|------|
| `app_open` | `session_id` | App launch |
| `character_builder_open` | — | Scene load |
| `fighter_created` | `head_id`, `body_id`, `weapon_id`, `mount_id`, `attack`, `defense`, `speed` | Create Fighter tapped |
| `battle_started` | `fighter_id`, equipment ids | Start Battle |
| `battle_ended` | `score`, `wave`, `duration_sec`, `reason` (`death`/`quit`) | Run end |
| `enemy_killed` | `wave`, `kill_score` | Each kill (sample or batch every 5 waves to reduce volume) |
| `leaderboard_view` | — | Leaderboard opened |
| `leaderboard_submit` | `score`, `success` | After Firebase submit |
| `crown_status` | `is_holder`, `holder_score` | Results screen |
| `hall_of_kings_view` | — | Gallery opened |
| `ad_impression` | `ad_type`, `placement` | Ad shown |
| `ad_reward` | `reward_type` | Rewarded complete |

### Implementation

- `AnalyticsService.LogEvent(string name, Dictionary<string, object> params)`
- Call from UI managers and combat controller—not from data classes

### Privacy

- No PII in event params
- Use `fighterId` / anonymous `playerId` only

---

## 16. UI/UX Direction

### Platform Constraints

- Portrait orientation primary (landscape optional later)
- Safe area padding for notched devices
- Minimum touch target: 48×48 dp equivalent
- Readable at arm's length: stat text ≥ 14pt effective

### Visual Hierarchy (Battle)

1. Top bar — Title, wave, score (always visible)
2. Center — Fighter rigs (player left, enemy right)
3. Mid — Battle log (scrollable text, max 12 lines in `BattleHUD`)
4. Bottom — Player/enemy stat blocks + health bar fills

### Character Builder UX

- Clear category labels (Head / Body / Weapon / Mount)
- Previous/Next affordances at thumb reach
- Fighter Card as commitment step—prevents accidental battle entry
- **Back / Edit** returns to builder without losing selections

### Typography

- TextMeshPro throughout (`TMP_Text`, `TMP_InputField`)
- Title: bold, high contrast (e.g. "CROWNSFALL" in battle HUD)

### Color Language

| Role | Color |
|------|-------|
| Player | White / neutral tint on rig |
| Enemy | Red tint `RGB(1, 0.32, 0.32)` |
| Health fill | Green (player), red (enemy) |
| Crown / gold accents | Future meta screens |

### Feedback (Future)

- Damage numbers pop on rig
- Health bar lerp animation
- Screen shake on heavy hits (subtle)
- Haptic on kill (mobile)

### Accessibility

- Color is not sole indicator—pair tint with labels
- Sufficient contrast on log text vs background
- Optional: reduce motion setting

---

## 17. Art Direction

### Style

- **Modular 2D equipment icons** stacked into a readable fighter silhouette
- Consistent icon size and pivot alignment across categories
- Fantasy/medieval tone fitting "Crowns" and "Kings" theming

### Asset Organization

```
Assets/Art/
  Heads/
  Bodies/
  Weapons/
  Mounts/
Assets/ScriptableObjects/
  Heads/
  Bodies/
  Weapons/
  Mounts/
```

### Equipment Icon Requirements

| Rule | Detail |
|------|--------|
| Format | PNG with transparency |
| Pivot | Consistent visual center for body; head above, mount below |
| Resolution | 256–512px source; UI scales with `preserveAspect` |
| Naming | Match `id` field in ScriptableObject for importer |

### UI Chrome

- Dark or parchment battle backdrop—fighters must pop
- Minimal frame clutter on mobile battle screen
- Fighter Card: framed "card" with stat panel

### Crown Asset

- Dedicated crown sprite for `FighterRig.crownImage`
- Used for Crown Holder display only (not equipment slot)

### Production Pipeline

1. Artist drops sprites in `Assets/Art/{category}/`
2. Run `Tools/Fighter Tools/Import All Equipment`
3. Tune stats in Inspector or spreadsheet → `Apply Stats` tooling
4. `Equipment Sprite Auto Assign` for bulk icon fixes

---

## 18. Technical Architecture

### Namespaces

| Namespace | Folder | Responsibility |
|-----------|--------|----------------|
| `Crownsfall.Core` | `Scripts/Core/` | `GameSession`, `GameManager`, app lifecycle |
| `Crownsfall.Characters` | `Scripts/Characters/` | `PlayerFighter`, equipment SOs |
| `Crownsfall.Combat` | `Scripts/Combat/` | Battle, rigs, HUD, combat (expanding) |
| `Crownsfall.UI` | `Scripts/UI/` | Character Builder, menus |
| `Crownsfall.Editor` | `Scripts/Editor/` | Import, scene setup, tooling |
| `Crownsfall.Services` | `Scripts/Services/` | Firebase, Ads, Analytics (planned) |
| `Crownsfall.AI` | `Scripts/AI/` | Enemy behavior (planned) |

### Layering

```
Presentation (UI, FighterRig, BattleHUD)
        ↓
Application (BattleManager, CharacterBuilderManager)
        ↓
Domain (PlayerFighter, combat rules)
        ↓
Data (ScriptableObjects, Firebase)
```

### Key Patterns

- **Singleton session:** `GameSession.Instance` with `DontDestroyOnLoad`
- **ScriptableObject equipment:** Designer-friendly, importer-driven
- **Plain C# fighter model:** No UI dependency in `PlayerFighter`
- **Clone for battle:** `BattleManager.CloneFighter()` avoids mutating session fighter during combat

### Dependencies (Current)

- Unity UI (`UnityEngine.UI`)
- TextMeshPro
- Unity SceneManagement

### Dependencies (Planned)

- Firebase Unity SDK
- Google Mobile Ads Unity SDK
- Optional: DOTween for UI animation

### Editor Menu Convention

`Tools/Fighter Tools/*` — all fighter/equipment/battle setup tools grouped together.

### Testing Strategy

- Play `BattleScene` directly with Inspector test equipment
- Full flow: `CharacterBuilder` → Create → Start Battle
- Unit tests (future): `PlayerFighter.CalculateStats`, damage formula, wave scaling math in pure C#

### Build Targets

- Android APK/AAB
- iOS Xcode export
- Minimum API levels per Unity 6 / project settings

---

## 19. Development Phases

### Phase 0 — Prototype Foundation ✅ (Current)

- [x] Equipment SOs + importer
- [x] Character Builder scene + manager
- [x] Fighter Card panel
- [x] `PlayerFighter` + `GameSession`
- [x] Battle scene layout: `FighterRig`, `BattleHUD`, `BattleManager`
- [x] Scene flow: Builder → Battle

### Phase 1 — Combat MVP

- [ ] `CombatController` auto-battle loop
- [ ] `DamageResolver` with baseline formula
- [ ] `EnemyWaveManager` procedural scaling
- [ ] HUD updates on damage/kill/wave
- [ ] Run end detection on player death
- [ ] Local score persistence (`PlayerPrefs`)

### Phase 2 — Results & Main Menu

- [ ] `MainMenu` scene
- [ ] `Results` scene (score, wave, personal best)
- [ ] Retry → Character Builder
- [ ] Balance values in `BalanceConfig` (hardcoded defaults first)

### Phase 3 — Firebase Core

- [ ] Firebase init + anonymous auth
- [ ] Leaderboard submit + fetch UI
- [ ] Analytics events for core loop
- [ ] Remote Config fetch + balance keys

### Phase 4 — Meta Systems

- [ ] Crown Holder read/display
- [ ] Crown transfer Cloud Function
- [ ] Longest Crown Reign tracking
- [ ] Hall of Kings scene + Firestore collection

### Phase 5 — Monetization & Polish

- [ ] AdMob interstitial on Results
- [ ] Optional rewarded revive
- [ ] Combat animations, SFX, haptics
- [ ] Equipment unlock / shop (if desired)
- [ ] App Store / Play Store release

---

## 20. Release Checklist

### Pre-Release Technical

- [ ] All scenes in Build Settings with correct order
- [ ] `GameSession` bootstrap verified on cold start
- [ ] No missing script references (`MissingScriptFinder` tool)
- [ ] Firebase config files for prod package name / bundle id
- [ ] AdMob prod ad unit IDs
- [ ] Remote Config defaults match shipped balance
- [ ] Cloud Functions deployed for score validation
- [ ] Offline/error paths tested (no network, slow network)

### Store Assets

- [ ] App icon (1024×1024)
- [ ] Screenshots (Character Builder, Battle, Leaderboard, Hall of Kings)
- [ ] Short description + keywords
- [ ] Privacy policy URL (Firebase, Ads, Analytics)
- [ ] Age rating questionnaire

### Platform Compliance

- [ ] Android: target SDK current, 64-bit
- [ ] iOS: ATT if personalized ads
- [ ] GDPR consent flow if EU traffic
- [ ] No debug/test ad units in release build

### QA Scenarios

- [ ] New install → build fighter → battle → die → score saved
- [ ] Leaderboard submit success and failure
- [ ] Crown display when holder known
- [ ] Rotation / safe area on notched phones
- [ ] Low-end device performance (battle log, image stacking)

### Versioning

- [ ] Semantic version in app (`0.1.0` prototype → `1.0.0` release)
- [ ] Changelog for store "What's New"

---

## Appendix A: Cursor Prompt Templates

Use these prompts in Cursor Agent mode with workspace root `Crownsfall/`. Adjust phase as needed.

### A1 — Implement Combat MVP

```
Implement auto-battle combat for Crownsfall BattleScene.

Context:
- Unity project in Crownsfall/ subfolder
- BattleManager already loads player/enemy, displays FighterRigs, fills BattleHUD
- PlayerFighter has TakeDamage, Die, AddScore, CalculateStats
- No combat exists yet

Requirements:
1. Create CombatController.cs in Assets/Scripts/Combat/ (Crownsfall.Combat)
2. Turn order by speed (player wins ties); baseline damage = max(1, atk - def)
3. 0.75s delay between actions for mobile readability
4. On enemy death: AddScore(10 + wave * 5), increment wave, spawn scaled enemy via EnemyWaveManager
5. On player death: log run end, stop combat
6. Update BattleHUD health bars, stats, score, wave, battle log each step
7. Wire CombatController from BattleManager after DisplayFighters/PopulateBattleUI
8. Enemy scaling: base ATK 5 DEF 2 SPD 1 HP 100; +2 ATK, +1 DEF, +15 HP, +0.5 SPD per wave
9. Do not use SpriteRenderer for fighters—keep UI FighterRig only
10. Match existing code style and namespaces

Do not commit unless I ask.
```

### A2 — Local Score Persistence

```
Add local score persistence for Crownsfall runs.

After player dies in combat:
1. Save finalScore and finalWave to PlayerPrefs (personal best score + wave)
2. Create RunResult.cs in Crownsfall.Core with score, wave, timestamp
3. Store last run on GameSession for a future Results scene
4. CharacterBuilder or Battle HUD should be able to read personal best for display later

Files: GameSession, CombatController or BattleManager, new SaveService in Crownsfall.Services
Do not commit unless I ask.
```

### A3 — Results Scene

```
Create a Results scene for Crownsfall after battle death.

Flow: BattleScene → ResultsScene → CharacterBuilder or MainMenu

UI:
- Final score, wave reached, personal best (new record highlight)
- Fighter name + small FighterRig or card preview
- Buttons: Retry (CharacterBuilder), Main Menu (placeholder OK)

Load final run data from GameSession.RunResult
Add scene to Build Settings
Namespace Crownsfall.UI for ResultsUIController

Do not commit unless I ask.
```

### A4 — Firebase Leaderboard

```
Integrate Firebase Firestore leaderboard for Crownsfall.

1. FirebaseBootstrap + LeaderboardService in Assets/Scripts/Services/
2. Anonymous Firebase Auth on first launch
3. On run end: submit entry { fighterName, score, wave, fighterId, equipment ids, timestamp, playerId }
4. LeaderboardUI: fetch top 50, show rank, name, score
5. Graceful failure if offline—queue submit in PlayerPrefs

Use namespace Crownsfall.Services. Do not block UI on network.
Assume google-services.json will be added manually.
Do not commit unless I ask.
```

### A5 — Remote Config Balance

```
Wire Firebase Remote Config for Crownsfall balance tuning.

1. RemoteConfigService fetches on app start
2. BalanceConfig static class with defaults matching GDD:
   base_kill_points=10, wave_score_multiplier=5, enemy_attack_per_wave=2, etc.
3. Refactor PlayerFighter.CalculateStats to use base_health + defense * health_per_defense from config
4. CombatController and EnemyWaveManager read scaling from BalanceConfig

Namespace Crownsfall.Services. Fallback to defaults if fetch fails.
Do not commit unless I ask.
```

### A6 — Crown Holder System

```
Implement Crown Holder read path for Crownsfall.

Firestore doc: meta/crownHolder with fighterName, score, fighterId, equipmentIds, crownedAt, playerId

1. CrownService.FetchCrownHolder()
2. ResultsUI shows current holder name/score and reign duration
3. If local player is holder, enable crown on FighterRig.crownImage in battle (assign crown sprite)

Cloud Function for crown transfer is out of scope—client read only for now.
Do not commit unless I ask.
```

### A7 — Hall of Kings Scene

```
Build Hall of Kings scene for Crownsfall.

Firestore collection hallOfKings ordered by reignDurationSeconds desc, limit 50.

UI: scroll list (rank, name, reign duration, peak score), tap for detail card with FighterRig preview from equipment snapshot.

HallOfKingsService + HallOfKingsUIController in Crownsfall.UI / Services.
Entry from future MainMenu button.
Do not commit unless I ask.
```

### A8 — AdMob Interstitial

```
Add AdMob interstitial ads to Crownsfall Results screen.

1. AdMobService init after Firebase in GameManager
2. Show interstitial when leaving Results toward CharacterBuilder
3. Frequency cap: once per 3 runs (PlayerPrefs counter)
4. Remote Config key ads_enabled and interstitial_frequency—stub if Remote Config not ready

Test ad units in dev, serialized prod unit fields in AdMobService.
Do not commit unless I ask.
```

### A9 — Main Menu Scene

```
Create MainMenu scene for Crownsfall.

Buttons: Play (CharacterBuilder), Leaderboard (panel or scene), Hall of Kings (if exists), Settings (stub)

Show current Crown Holder banner (name + score) if CrownService available else placeholder.

Wire as first scene in Build Settings.
GameSession + GameManager on bootstrap object.
Do not commit unless I ask.
```

### A10 — Battle Scene Production UI Audit

```
Audit and fix BattleScene production UI layout for mobile.

Verify:
- Screen Space Canvas, safe area
- PlayerFighterRig / EnemyFighterRig use FighterRig with UI Images only
- BattleHUD references wired (wave, score, log, health fills, stat blocks)
- BattleManager Inspector refs or FindSceneReferencesIfNeeded works after Tools/Fighter Tools/Setup Battle Scene Production UI

Run editor menu BattleSceneSetup if needed. Document any manual Inspector steps in code comments only if necessary.
Do not commit unless I ask.
```

---

*End of Crownsfall GDD v0.1 (Prototype)*

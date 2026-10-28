# Project Context & Architecture: BlockBreaker

High-density technical context, architectural rules, and subsystem specifications for BlockBreaker.

---

## 1. Project Specifications & Key Paths
- **Product Name / Bundle ID**: `BlockBreaker` / `com.RaminRasulzade.BlockBreaker`
- **Unity Version**: Unity 6 (`6000.6.0f1`), Universal Render Pipeline (`URP 17.6.0`)
- **Active Branch**: `feature/ui-improvements` (Git LFS enabled)
- **Scenes**:
  1. `Assets/Scenes/LV_BlockBreaker_MainMenu.unity` (Build Index 0, Start Scene configured via [PlayModeSceneSetup.cs](Assets/Editor/PlayModeSceneSetup.cs))
  2. `Assets/Scenes/LV_BlockBreaker.unity` (Build Index 1, Primary Gameplay Arena)
- **Key Directories & Assemblies**:
  - Gameplay Scripts: `Assets/Scripts/` (Assembly: `Arcade.Gameplay.asmdef`)
  - Editor Tools: `Assets/Editor/` (Assembly: `Arcade.Editor.asmdef`)
  - Modular Prefabs: `Assets/Prefabs/` (100% prefab-driven suite: Arena, Paddle, Balls, Blocks, Drops, VFX)
  - Materials: `Assets/Materials/BlockBreaker/` (`MT_Master_PBR_URP.mat` master, `MI_*` instances)
  - Level Settings: `Assets/Settings/` (`SO_Level_01` to `SO_Level_30`, `SO_PowerupIcons.asset`, URP Profiles)
  - Localization: `Assets/Localization/` (`Locales/`, `Tables/ArcadeTable.asset`, `LocalizationSettings.asset`)
  - UI Assets: `Assets/UI/` (UXML, USS, Fonts, Icons, procedural gradients under `Textures/Gradients/`, Level Previews under `LevelPreviews/`)
  - Automated Tests: `Assets/Tests/` (Assembly: `Arcade.Tests.asmdef`, 331 EditMode NUnit tests)

---

## 2. Inviolable Architectural Mandates
1. **Preservation of User-Authored Work (STRICT)**: The user directly customizes and hand-tunes scenes (`Assets/Scenes/LV_BlockBreaker.unity`), materials (`MI_*`), prefabs (`PF_Background.prefab`, `PF_Walls.prefab`), volume profiles (`SampleSceneProfile.asset`), gradient textures (`TX_Background_Gradient_*.png`), and UI visuals/icons (`Assets/UI/Icons/`, medals, flags). Agents must **NEVER** revert, overwrite, or discard visual assets or hand-tuned values when diffing, staging, or committing, nor execute automated scene rebuild scripts (e.g. `SetupBlockBreakerScenes.cs`).
2. **Parent Scale Normalization (1.0 Root Rule)**: All prefab root containers (`PF_Paddle.prefab`, `PF_Ball_Standard.prefab`, `PF_LaserBolt.prefab`) strictly maintain `transform.localScale = Vector3.one`. Dimensions and dynamic expansions are driven by child visual sockets (`visuals`, `frost`, `guns`) and root colliders without reciprocal squishing, allowing artists to add child meshes directly at true 1:1 scale.
3. **Zero Runtime Primitives**: DO NOT use `GameObject.CreatePrimitive` at runtime or in scene setups. All entities instantiate directly from `Assets/Prefabs/`.
4. **Decoupled Physics & Visuals**: Physical colliders reside exclusively on root container objects; 3D visual models reside in child GameObjects with all colliders removed to prevent double-collision glitches and phantom catches.
5. **Prefab-Driven Authoring**: Particle System curves (velocities, lifetime, color gradients), materials, transforms, and collider dimensions MUST be authored directly on Prefab assets or Scene GameObjects—NEVER hardcoded in C# overrides (`Awake()`, `Start()`, `Update()`).
6. **Standalone & Mobile Bundling Without Resources**: Every prefab required at runtime (`PF_Drop_Powerup`, `PF_Drop_Hazard`, `PF_LaserBolt`, `PF_ShieldWall`, `PF_VFX_*`) MUST be serialized on a Scene GameObject (`ArcadeGameManager`, `LevelGenerator`, `BlockVFXManager`, or `PF_Paddle`) in `LV_BlockBreaker.unity`.
7. **Naming Conventions**:
   - Materials: `MT_*` (Master), `MI_*` (Instance/Variant under `Assets/Materials/BlockBreaker/`)
   - Textures: `TX_*` (`TX_*_BaseColor`, `TX_*_Normal`, `TX_*_Emissive`)
   - Audio: `AU_*` (e.g. `AU_Pop`, `AU_Break`, `AU_Powerup`, `AU_Shield_Impact`, `AU_Game_Over`)
   - ScriptableObjects: `SO_*` (e.g. `SO_Level_*.asset`, `SO_PowerupIcons.asset`)
   - Prefabs: `PF_*` under `Assets/Prefabs/`
   - Presets: `PR_*` under `Assets/Presets/`
8. **Humanized Localization Copy & Decoupled Tests**: The user refines and customizes English and Turkish translation strings in `ArcadeTable` for natural tone. **NEVER** overwrite existing table translations, and unit tests **MUST NEVER** assert hardcoded literal string values. Tests must verify key presence, non-emptiness, dynamic binding against `LocalizationManager.Get(key)`, and argument formatting, giving the user complete freedom over the actual copy.

---

## 3. Modular Prefab Suite (`Assets/Prefabs/`)

| Category | Prefab Name | Hierarchy & Sockets | Physics & Components |
| :--- | :--- | :--- | :--- |
| **Arena** | `PF_Walls.prefab` | `Side_Left` ($X = -10.25, H = 30.2$), `Side_Right` ($X = +10.25, H = 30.2$), `Top_Wall` ($Y = 24.25, W = 17.4$), `Chamfer_Left` & `Chamfer_Right` (45° wedges at $\pm 9.40, 23.40$), `KillZone` ($Y = -9.0$, Tag `"KillZone"`). | `ArenaWalls.cs`, decoupled BoxColliders with `PM_ArcadeBounce.physicMaterial`. |
| **Arena** | `PF_Background.prefab` | Root container + child `plane` ($40 \times 80$ quad at $Z = 6.0$). | `LevelBackgroundController.cs`, `URP/Unlit` material (`MI_Background_Gradient.mat`), textures `TX_Background_Gradient_A` to `E`. |
| **Arena** | `PF_ShieldWall.prefab` | Root + Child 1 `model` (`SM_ShieldWall.fbx`, `MI_Playfield_Border.mat`, zero colliders) + Child 2 `vfx` (`ParticleSystem` cyan energy barrier). | `ShieldWall.cs`, root `BoxCollider` (`size = (21, 0.5, 2)` with `PM_ArcadeBounce`). Preserves prefab material & emission; emits 12 particles on ball impact via `shieldVfx.Emit(12)`. |
| **Paddle** | `PF_Paddle.prefab` | Root ($W=5.0$, scale 1.0) + child sockets (scale 1.0): `visuals` (`Deck`, `Chassis`, `Keel` from `SM_Paddle.fbx`), `guns` (`Muzzle_Left`, `Muzzle_Right`), `laser_gun` (`Aperture`), `frost`, `dock_point` (`(0, 0.88f, 0)`), `vfx`. | `PaddleController.cs`, `PaddleLaserController.cs`. Root `BoxCollider` ($W=5.0, H=0.24, Z=2.8$ at center $Y=+0.38$). Dynamic expansion scales child visual sockets. |
| **Projectiles** | `PF_LaserBolt.prefab` | Root (scale 1.0) + Child 1 `model` (`Bullet` mesh from `SM_Paddle.fbx` at scale 1.0 with bottom pivot $Y=0$). | `LaserBolt.cs`, kinematic `Rigidbody`, root `CapsuleCollider` trigger ($R=0.10, H=0.54, \text{center } Y=0.27$), continuous volumetric `SphereCastAll`. |
| **Ball** | `PF_Ball_Standard.prefab` | Root (scale 1.0) + Child 1 `model` (Sphere scale $0.70, 0.70, 0.70$) + Child 2 `trail` + Child 3 `vfx`. | `BallController.cs`, root `SphereCollider` ($R=0.35$), `Rigidbody` (interpolate, continuous dynamic, velocity-driven). |
| **Blocks** | `PF_Block_Base.prefab`<br>`PF_Block_Red.prefab`<br>`PF_Block_Green.prefab`<br>`PF_Block_Blue.prefab`<br>`PF_Block_Bomb.prefab`<br>`PF_Block_Glass.prefab` | 4 standardized child sockets:<br>1. `brick`: PBR visual mesh (zero colliders).<br>2. `brick frost`: Ice encasement shell.<br>3. `brick special`: UI Toolkit badge element (`BlockBadge`).<br>4. `brick vfx`: Burst anchor. | `Block.cs`, root `BoxCollider` ($W = 1.15, H = 0.55, Z = 1.0$), `MaterialPropertyBlock` zero-allocation tinting. |
| **Drops** | `PF_Drop_Powerup.prefab`<br>`PF_Drop_Hazard.prefab` | Root container + Child 1 visual mesh (Capsule scale 1.60 / Diamond Cube scale 1.33) + Child 1.1 `Icon_Billboard` (parented directly to visual child, rotates synchronously). | `PowerupCapsule.cs`, trigger `BoxCollider`, tumbling rotation ($120^\circ/\text{s}$), fall speed $4.5\text{ u/s}$ at $Z = -1.0\text{f}$. `Initialize()` swaps sprite directly. |
| **VFX** | `PF_VFX_BlockShatter.prefab`<br>`PF_VFX_BombExplosion.prefab`<br>`PF_VFX_PaddleSpark.prefab`<br>`PF_VFX_PowerupCollect.prefab`<br>`PF_VFX_FallingTrail.prefab`<br>`PF_VFX_ShieldBarrier.prefab` | Consolidated Shuriken `ParticleSystem` prefabs with `playOnAwake = true`, `loop = true`, `emission.enabled = true`, and authored bursts for instant Editor previewing. `PF_VFX_BombExplosion` uses `MI_VFX_Shockwave.mat` (`TX_VFX_Shockwave_Ring.png`). | `BlockVFXManager.cs`, prewarmed instance pool under `_Pool_VFX`, zero-allocation `MaterialPropertyBlock` tinting, dynamic block material inheritance. |

---

## 4. Physics, Deflection Math & Safeguards
- **3-Tier Inverted Stepped Pyramid Paddle**:
  - **Tier 1 (Strike Deck)**: $W = 5.0, H = 0.24, Z = 1.0$ at $Y = -6.0$ with [MI_Paddle_Deck.mat](Assets/Materials/BlockBreaker/MI_Paddle_Deck.mat). Holds main `BoxCollider` ($H = 0.24$, center $Y = +0.38$).
  - **Tier 2 (Mid Chassis)**: $W = 3.6, H = 0.20, Z = 0.88$ with [MI_Paddle.mat](Assets/Materials/BlockBreaker/MI_Paddle.mat).
  - **Tier 3 (Keel / Thrusters)**: $W = 2.2, H = 0.16, Z = 0.72$ with [MI_Paddle_Core.mat](Assets/Materials/BlockBreaker/MI_Paddle_Core.mat).
  - Total vertical profile $0.60$ with inward stepping ($\le 1.4\text{u}$/side) eliminates side catches.
  - Normal Validation: `BallController.IsValidPaddleBounceNormal(normal)` requires `normal.y >= 0.25f`. Brushing balls fall cleanly to the killzone.
- **Deflection & Active Kinetic Strike**:
  - Formula: `Exit Angle = Ray Angle + Hit Offset Steer (-32° to +32°) + Paddle Velocity Steer (-45° to +45°)`, clamped to $[25^\circ, 155^\circ]$.
  - Trajectory strictly excludes vertical deadzone $[85^\circ, 95^\circ]$.
  - **Active Kinetic Strike**: When moving paddle $|V_x| \ge 3.5\text{ u/s}$, strike imparts $+8\%$ speed pop (capped at `maxSpeed = 22f`), higher audio pitch (`1.22f`), $18\%$ squash recoil, and cyan spark burst (`BlockVFXManager.PlayPaddleHitSpark`). Stationary paddle cushions ball and preserves speed.
- **Anti-Trap Trajectory Safeguards**:
  - **Vertical Floor**: $|v_y| \ge \text{currentSpeed} \cdot \sin(20^\circ)$ prevents horizontal traps.
  - **Wall Steepener**: $\ge 2$ consecutive wall bounces steepens trajectory to $\ge 35^\circ$.
  - **Continuous X Movement**: Enforces $|v_x| \ge \text{currentSpeed} \cdot \sin(5^\circ)$.
  - **45° Corner Chamfers**: Angled wedges at $(\pm 9.40, 23.40)$ join top and side walls, preventing top corner wedging.
- **Shield Wall**:
  - Deployed at $Y = -7.75\text{f}$ for 10s on shield pickup with smooth tween overshoot ($0 \to 1.08 \to 1.0$).
  - Bounces balls upward ($\ge 35^\circ$) without life penalty; emits 12 particles on hit.
  - Upward anti-trap one-way passthrough allows balls below to rise freely through collider.

---

## 5. Tactical Brick Interactions (Model A)
- 🔴 **Red (Tier 1, 10 pts) — Kinetic Dampener**: Reduces ball speed by $-1.2\text{ u/s}$ (floored at `baseSpeed = 14f`). Pitch $0.90\times$.
- 🟢 **Green (Tier 2, 20 pts) — Kinetic Turbo**: Boosts ball speed $+10\%$ (capped at `maxSpeed = 22f`). Pitch $1.10\times$.
- 🔵 **Blue (Tier 3, 30 pts) — Optical Scatter**: Rotates exit trajectory by $\pm 18^\circ$ to $\pm 28^\circ$ while respecting anti-trap constraints. Pitch $1.25\times$.
- **Glass Bricks**: 2-hit reinforced blocks (Hit 1: shatters glass shell; Hit 2: breaks brick for $2\times$ score).
- **Bomb Bricks**: Cascades destruction across blocks in a $2.5\text{u}$ radius with compound $1.5^{\text{chainIndex}}$ multiplier.

---

## 6. Pickups, Hazards & Defense Architecture
- **Drops Mechanics**: Fall speed $4.5\text{ u/s}$ at $Z = -1.0\text{f}$. All falling drops destroyed on life loss, shield save, level clear, or game over via `ClearAllFallingCapsules()`.
- **Powerups (Cyan `#00f2fe`, Capsule, scale 0.85)**:
  - **Expander**: Widens paddle $+10\%$ compounding (max $12.0\text{u}$, spring overshoot animation, 10s).
  - **Score Multipliers**: Multiplies break score by $2\times, 3\times, 4\times,$ or $5\times$ (10s).
  - **Multi-Ball**: Spawns 2 extra balls at $\pm 35^\circ$; scales all scored points ($2\times$ or $3\times$).
  - **Extra Heart**: Grants $+1$ life (up to 5 max).
  - **Laser Blaster**: Twin cannons firing ruby bolts ($34\text{ u/s}$) at $0.32\text{s}$ interval (10s).
- **Powerdowns / Hazards (Crimson `#ff1744`, Diamond, scale 0.72)**:
  - **Paddle Shortener**: Shrinks paddle $-18\%$ (min $2.4\text{u}$, 10s).
  - **Paddle Slower**: Adds input drag, reducing speed by $-50\%$ (8s).
  - **Brick Freezer**: Glaciates 5 bricks; each absorbs 1 defrost hit before shattering.
  - **Ball Shrinker**: Shrinks ball radius to $60\%$ ($0.8\text{u} \to 0.48\text{u}$, 10s).
  - **Ball Slower**: Slows ball velocity to $9.5\text{ u/s}$ (8s).
  - **Paddle Freezer**: Immobilizes paddle for $1.2\text{s}$ with sinusoidal tremor (`Mathf.Sin(Time.time * 45f) * 0.07f`).

---

## 7. Level Campaign, Pacing & End-Game
- **30-Level Campaign**: High-density grids (11–14 columns, $13.75\text{u}–17.5\text{u}$ span) with outer flank bumper blocks preventing wall bypass.
- **Pacing Progression**: Speed scales from $0.92\times$ (Level 1) to $1.80\times$ (Level 30). Paddle narrows from $5.0\text{u}$ to $4.2\text{u}$. Hazards escalate from 0 to 20.
- **Onboarding Block Scale (+20% on Levels 1–3)**: Levels 1, 2, and 3 (`SO_Level_01`, `SO_Level_02`, `SO_Level_03`) feature enlarged blocks (`blockSize = 1.20f`) with adjusted horizontal spacing ($1.50\text{u}, 1.45\text{u}, 1.40\text{u}$) to maximize readability and touch comfort while strictly preserving $> 1.0\text{u}$ clearance to arena walls ($X = \pm 10.25$). Levels 4–30 retain standard $1.00\text{u}$ scale.
- **Bomb Placement Sanitization (`SanitizeBombPlacements`)**:
  - **No Isolated Bombs**: Enforces that every bomb has $\ge 1$ orthogonal neighbor; isolated tips (e.g. antenna tips in Invader layouts) are relocated to connected blocks with $\ge 2$ neighbors.
  - **No Clustered/Adjacent Bombs**: Enforces Chebyshev distance $> 1$ ($\max(|x_1-x_2|, |y_1-y_2|) > 1$) between any two bombs, relocating colliding bombs across valid grid spaces.
  - **CheckerboardEmpty Exemption**: In sparse checkerboard designs (`SO_Level_13`, `SO_Level_26`) where blocks inherently lack orthogonal neighbors, `bombCount` is set to 0 and reallocated to glass-reinforced and score multiplier blocks.
- **Lone Block Countdown**: Exactly 1 brick remaining activates 12s countdown ($10\times \to 1\times$ decaying multiplier).
- **Railgun Hyper-Beam**: On countdown expiration, fires vertical beam ($W \approx 3.2$, duration $0.65\text{s}$, shader `Arcade/VFX_LaserHyperBeam`) destroying remaining blocks.
- **Victory Freeze**: When level clears, freeze ball velocities (`FreezeBall()`), lock paddle input, freeze falling capsules, wait $1.0\text{s}$ ($1.5\text{s}$ for Hyper-Beam) before showing scorecard.

---

## 8. Camera, Graphics, UI & Audio
- **Perspective Camera & URP Post-Processing**:
  - Perspective camera: $38^\circ$ FOV at $Z = -32\text{f}$, centered at $Y = 8.5\text{f}$. [ResponsiveCameraController.cs](Assets/Scripts/Core/ResponsiveCameraController.cs) dynamically adjusts distance to keep arena 100% visible across 16:9, 9:16, and 19.5:9 aspect ratios.
  - `Main Camera` requires `UniversalAdditionalCameraData` with `renderPostProcessing = true` and `volumeLayerMask = ~0` for Game view bloom.
  - `RenderSettings.skybox = null` and `defaultReflectionMode = Custom` eliminates default Unity skybox reflections.
- **UI Toolkit Architecture & Motion**:
  - Unity 6 `PanelRenderer` on `UI_HUD` and `UI_MainMenu` (iPhone portrait reference $1170 \times 2532$).
  - Safe area insets calculated by [SafeAreaController.cs](Assets/Scripts/UI/SafeAreaController.cs) using Yoga width-relative percentages (`CalculateYogaInsets`).
  - [UIAnimationHelper.cs](Assets/Scripts/UI/UIAnimationHelper.cs) drives modal springs (`0.82 \to 1.0`), cascading button entrances ($0.035\text{s}$ stagger), and tactile click depressions.
  - Stacked `TAP TO LAUNCH` prompt features enlarged icons ($96\text{px}$ finger, $80\text{px}$ arrows) with continuous animated horizontal swipe (`StartHorizontalSwipe`) projected at $Y = -0.5\text{f}$.
  - Compact tablet powerup badges ($60\text{px}$ height, $36\text{px}$ icons) eliminate block occlusion on squarer aspect ratios (iPad Mini).
  - **Level End Modal Actions**: Icon-only circular buttons (`TX_Icon_Arrow_Next.png`, `TX_Icon_Menu.png`, `TX_Icon_Replay.png`) prevent text truncation across languages.
  - **Logo & Header Branding**: Main Menu header text replaced with `TX_Block_Breaker_3D_Logo.png`.
  - **Star Rating Sprites**: Main Menu and HUD use `TX_Star_Filled.png` and `TX_Star_Empty.png` sprites.
  - **Accolade Medal**: Preserved gold medal badge (`TX_Medal_Gold.png`) on `scorecard-star`.
  - **Highscores Date Cleanliness**: Removed redundant `L{level}` prefix from leaderboard dates.
  - **30-Stage Level Previews**: Pixel-perfect 128x256 rendered thumbnails under `Assets/UI/LevelPreviews/TX_LevelPreview_*.png` with `scale-and-crop` and `overflow: hidden`.
  - **Zero Invalid USS Web Properties**: No `box-shadow`, `pointer-events`, `color: inherit`, or multi-shadow CSS syntax in `.uss` stylesheets.
- **Localization**: Multi-language StringTableCollection [ArcadeTable.asset](Assets/Localization/Tables/ArcadeTable.asset) (`com.unity.localization` 1.5.13) under `Assets/Localization/Tables/` for English (`en`) and Turkish (`tr`).
- **Audio Engine**: [ArcadeAudioManager.cs](Assets/Scripts/Audio/ArcadeAudioManager.cs) with custom clips (`AU_*`), algorithmic synthesis fallback, and looping background music (`AU_Music_Background.wav`) on dedicated `_MusicSource` child transform. Mobile import configuration: 2D flat stereo (`m_3D = false`), `CompressedInMemory` (Vorbis quality 0.7 for Android/Standalone, hardware MP3/AAC for iOS), `loadInBackground = true`, `preloadAudioData = true`.

---

## 9. Subsystem Lookup Map

| Subsystem | Script(s) | Primary Prefab / Asset | Responsibility |
| :--- | :--- | :--- | :--- |
| **Ball Physics** | [BallController.cs](Assets/Scripts/BlockBreaker/BallController.cs) | `PF_Ball_Standard.prefab` | Deflection math, anti-trap floor, wall steepener, speed capping, active strike recoil, color interactions, speed label null guards. |
| **Paddle Control** | [PaddleController.cs](Assets/Scripts/BlockBreaker/PaddleController.cs), [PaddleLaserController.cs](Assets/Scripts/BlockBreaker/PaddleLaserController.cs) | `PF_Paddle.prefab` | Tracking, twin ruby blasters, hyper-beam aperture, freeze tremor, dock point, width expansion. |
| **Arena Boundaries** | [ArenaWalls.cs](Assets/Scripts/BlockBreaker/ArenaWalls.cs), [KillZone.cs](Assets/Scripts/BlockBreaker/KillZone.cs) | `PF_Walls.prefab` | Side/top boundaries, 45° corner chamfers, kill zone trigger at $Y = -9.0$. |
| **Shield Defense** | [ShieldWall.cs](Assets/Scripts/BlockBreaker/ShieldWall.cs) | `PF_ShieldWall.prefab` | Defensive energy wall, tween scale overshoot, anti-trap upward passthrough, impact burst. |
| **Level Generation** | [LevelGenerator.cs](Assets/Scripts/BlockBreaker/LevelGenerator.cs), [Block.cs](Assets/Scripts/BlockBreaker/Block.cs) | `PF_Block_*.prefab` | Spawns modular block prefabs from ScriptableObjects, special badges/frost shells, bomb cascade, `SanitizeBombPlacements` algorithm. |
| **Background Plane** | [LevelBackgroundController.cs](Assets/Scripts/BlockBreaker/LevelBackgroundController.cs) | `PF_Background.prefab` | Cosmic gradient quad at $Z = 6.0\text{f}$ ($40 \times 80$), randomizes textures on level transitions. |
| **Drops & Buffs** | [PowerupCapsule.cs](Assets/Scripts/BlockBreaker/PowerupCapsule.cs) | `PF_Drop_*.prefab` | Tumbling capsule/diamond falls, pickup triggers, duration timers, HUD badge updates. |
| **Game Coordinator** | [ArcadeGameManager.cs](Assets/Scripts/Core/ArcadeGameManager.cs) | Scene Coordinator | Lives, score multipliers, clutch countdown, hyper-beam sequence, victory freeze, scorecard popup. |
| **Responsive Camera**| [ResponsiveCameraController.cs](Assets/Scripts/Core/ResponsiveCameraController.cs) | Main Camera | Calculates distance to fit $23 \times 32.5$ arena across any device aspect ratio (16:9, 9:16, 19.5:9). |
| **Audio Engine** | [ArcadeAudioManager.cs](Assets/Scripts/Audio/ArcadeAudioManager.cs) | `AudioManager` | Custom SFX (`AU_*`) with algorithmic fallback synthesis, looping BGM (`AU_Music_Background`), dedicated `_MusicSource` child, two-way options mute & volume synchronization. |
| **Localization** | [LocalizationManager.cs](Assets/Scripts/Core/LocalizationManager.cs) | `ArcadeTable.asset` | Unity Localization Tables runtime queries, two-way `SelectedLocale` sync. |
| **UI HUD & Menus** | [ArcadeUIManager.cs](Assets/Scripts/UI/ArcadeUIManager.cs), [MainMenuUIManager.cs](Assets/Scripts/UI/MainMenuUIManager.cs) | `BlockBreakerHUD.uxml`, `MainMenuUI.uxml` | PanelRenderer HUD dashboard pods, transparent main menu overlay, menu options parity, mute toggles, 128x256 level preview thumbs, icon-only modals. |
| **VFX Manager** | [BlockVFXManager.cs](Assets/Scripts/BlockBreaker/BlockVFXManager.cs) | `PF_VFX_*.prefab` | Pooled Shuriken particle bursts, 3D mesh particle debris, block material inheritance. |

---

## 10. Verification Commands
- **Run Automated Tests**:
  ```bash
  unity cmd run_tests --mode editor --timeout 90
  ```
- **Recompile Assemblies**:
  ```bash
  unity cmd recompile
  ```
- **Total EditMode Tests**: 331 / 331 passing (100%).

---

## 11. Engineering Milestones & Session Changelog
- **UI Overhaul, Typography & Universal Popups (`feature/ui-improvements`)**: Redesigned Options volume controls as iOS-style pill sliders (`26px` track, cyan fill bar, `52px` circular white thumb) and mute toggles as iOS-style pill switches (`136x68px` track in `152x88px` unclipped container; left crimson muted state, right cyan enabled state). Applied `FT_Montserrat-SemiBold.ttf` across Main Menu and HUD headers/buttons. Refined Turkish localization (`ArcadeTable_tr.asset`) with natural gaming terminology. Removed level names/descriptions from Level Select & Level Settings (preserving user-customized Level Select layouts). Replaced wordy localized ball speed popups with universal numeric multiplier deltas (`+0.1x`, `-0.1x`, `-0.3x`; removed `STRIKE!` popup on paddle smash) and simplified flying score labels to pure math (`+40 (x2)`). Fixed `ShieldWall.cs` to preserve prefab-authored `SM_ShieldWall.fbx` and `MI_Playfield_Border.mat` without runtime material/cyan emission overrides. Bound HUD Clutch Countdown icon (`clutch-status-icon`) to `TX_Powerup_Laser_Clutch.png` via `SO_PowerupIcons.asset` and `ArcadeUIManager.cs`.
- **Background Music Integration & Mobile Import Optimization**: Integrated `AU_Music_Background.wav` looping soundtrack across Main Menu and Gameplay scenes via persistent `ArcadeAudioManager.cs`. Hosted `musicSource` on a dedicated `_MusicSource` child transform to prevent FMOD root voice-channel contention. Optimized audio import for mobile (`m_3D = false`, `loadType = CompressedInMemory`, `loadInBackground = true`, `preloadAudioData = true`, Vorbis 0.7 for Android/Standalone, MP3/AAC for iOS). Synchronized with Main Menu and in-game UI options (mute toggle and volume sliders). Added 2 new NUnit tests (331 total passing EditMode tests).
- **Level Visualizer & Inspector EditorWindow**: Created [LevelVisualizerWindow.cs](Assets/Editor/LevelVisualizerWindow.cs) (`Tools > Arcade > Level Visualizer` / `Window > Arcade > Level Visualizer`) featuring interactive 2D grid matrix rendering, block tier & powerup/hazard distribution preview, real-time Scene View preview toggle, and full level configuration inspection and tuning.
- **Level Preview Thumbnail Generator Tool**: Created [LevelPreviewGenerator.cs](Assets/Editor/LevelPreviewGenerator.cs) (`Tools > Arcade > Generate All Level Previews` & `Sync All Level Previews to Assets and Scenes`) automating 128x256 rendering, sprite import configuration, and two-way synchronization.
- **iOS Build Inclusion & Asset Serialization**: Added serialized `previewSprite` field to [LevelConfiguration.cs](Assets/Scripts/BlockBreaker/LevelConfiguration.cs) directly referencing `TX_LevelPreview_{XX}.png` on all 30 `SO_Level_*.asset` files, and serialized `levelPreviewSprites` arrays in [LV_BlockBreaker_MainMenu.unity](Assets/Scenes/LV_BlockBreaker_MainMenu.unity) and [LV_BlockBreaker.unity](Assets/Scenes/LV_BlockBreaker.unity). Guarantees sprites are bundled into standalone and iOS player builds.
- **Bomb Placement Sanitization**: Enforced orthogonal connectivity ($\ge 1$ neighbor) and Chebyshev separation ($> 1$) in `LevelGenerator.cs`. Zeroed bombs in sparse checkerboard levels (`SO_Level_13`, `SO_Level_26`).
- **Level Preview Thumbnails**: Captured and integrated 30 rendered 128x256 (1:2 ratio) preview sprites under `Assets/UI/LevelPreviews/`.

---

## 12. Next Session Priorities & Backlog
- **Anti-Aliasing & SSAO Optimization (Desktop & Pipeline)**:
  - **Resolution Baseline**: Desktop target configured to windowed $1080 \times 1920$ portrait. Camera stack and cosmic gradient textures remain intact as authored.
  - **FXAA vs SMAA Evaluation**: Evaluate switching anti-aliasing from SMAA to FXAA to eliminate intermediate SMAA render textures (`_EdgeTexture`, `_BlendTexture`, `CameraColorSMAA`).
  - **SSAO Downsample Tuning**: Inspect URP SSAO settings (evaluate Downsample / Half-Resolution SSAO or radius/sample count tuning) to streamline occlusion passes and depth normals overhead while maintaining arcade visual depth.


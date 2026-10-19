# Codex Developer Briefing & Project Handoff

Welcome to the **BlockBreaker** codebase. This file is your operational briefing and entry point, designed to equip OpenAI Codex and other autonomous LLM agents with complete context, project status, architectural mandates, commands, and roadmap items to continue seamlessly.

---

## 1. Quick Facts & Environment

- **Product Name / Bundle ID**: `BlockBreaker` / `com.RaminRasulzade.BlockBreaker`
- **Unity Version**: Unity 6 (`6000.6.0f1`)
- **Render Pipeline**: Universal Render Pipeline (`URP 17.6.0`)
- **Git Branch**: `feature/3d-models-and-scale-fixes`
- **Git Working Tree**: Clean, all changes committed and pushed
- **Latest Commit**: `1446be6`
- **Automated Tests**: **277 / 277 EditMode tests passing** (100% pass rate)
- **Primary Scenes**:
  1. `Assets/Scenes/LV_BlockBreaker_MainMenu.unity` (Build Index 0, Start Scene)
  2. `Assets/Scenes/LV_BlockBreaker.unity` (Build Index 1, Primary Gameplay Arena)

---

## 2. Essential Commands

Execute these commands from the repository root:

```bash
# Run all automated EditMode tests (NUnit)
unity cmd run_tests --mode editor --timeout 90

# Trigger fast script recompilation and verify no C# compilation errors
unity cmd recompile

# Check git status
git status
```

---

## 3. Strict Architectural Mandates (DO NOT VIOLATE)

1. **Preservation of User-Authored Scene & Assets**:
   - The user hand-tunes lighting, materials, post-processing volume profiles, gradient textures, and prefabs in the Unity Editor:
     - `Assets/Scenes/LV_BlockBreaker.unity`
     - `Assets/Materials/BlockBreaker/` (`MI_*.mat`)
     - `Assets/Prefabs/Arena/` (`PF_Background.prefab`, `PF_Walls.prefab`)
     - `Assets/Settings/SampleSceneProfile.asset`
     - `Assets/Textures/Backgrounds/` (`TX_Background_Gradient_*.png`)
   - **NEVER** run automated scene reconstruction scripts (such as `Tools > Arcade > Setup All Block Breaker Scenes` or [SetupBlockBreakerScenes.cs](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/Assets/Editor/SetupBlockBreakerScenes.cs)) over user work. All modifications must be targeted and surgical.

2. **Zero Runtime Primitives**:
   - Never use `GameObject.CreatePrimitive` at runtime or in scene setups. Always instantiate from modular prefabs under `Assets/Prefabs/`.

3. **Decoupled Physics & Visuals**:
   - Physical colliders reside strictly on the root container. Visual meshes reside in child objects with all colliders stripped to prevent double-collision physics bugs.

4. **3D Model Scale Calibration on Sockets**:
   - In `PF_Paddle.prefab`, the root paddle has an arcade collision width of $5.5\text{u}$ ($5.0\text{u}–5.5\text{u}$) and dynamically scales with Expander buffs.
   - Child visual sockets (`visuals`, `frost`, `guns`, `Aperture`) MUST maintain local scale $X = 0.20\text{f}$ (`Vector3(0.20f, 1.0f, 1.0f)`). This mathematical compensation prevents double scaling so child 3D models (`SM_Paddle.fbx`) render at true 1:1 proportions while the root controls collision boundaries and powerup expansion.

---

## 4. Subsystem & Codebase Map

| Subsystem | Primary Script(s) | Primary Asset / Prefab | Responsibility |
| :--- | :--- | :--- | :--- |
| **Ball Physics** | [BallController.cs](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/Assets/Scripts/BlockBreaker/BallController.cs) | `Assets/Prefabs/Balls/PF_Ball_Standard.prefab` | Deflection math, steering, active kinetic strikes, anti-trap trajectory guards (floor, steepeners, deadzones), color physics interactions. |
| **Paddle Control** | [PaddleController.cs](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/Assets/Scripts/BlockBreaker/PaddleController.cs)<br>[PaddleLaserController.cs](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/Assets/Scripts/BlockBreaker/PaddleLaserController.cs) | `Assets/Prefabs/Paddle/PF_Paddle.prefab` | Input tracking (touch/mouse/keyboard), docking, twin laser blaster firing, hyper-beam aperture, $0.20\text{f}$ child socket calibration. |
| **Arena & Boundaries** | [ArenaWalls.cs](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/Assets/Scripts/BlockBreaker/ArenaWalls.cs)<br>[KillZone.cs](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/Assets/Scripts/BlockBreaker/KillZone.cs) | `Assets/Prefabs/Arena/PF_Walls.prefab` | Boundary colliders ($W=17.4, H=30.2$), 45° corner chamfers at $(\pm 9.40, 23.40)$, killzone at $Y = -9.0$. |
| **Shield Defense** | [ShieldWall.cs](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/Assets/Scripts/BlockBreaker/ShieldWall.cs) | `Assets/Prefabs/Arena/PF_ShieldWall.prefab` | Energy wall at $Y = -7.6\text{f}$, upward one-way passthrough anti-trap, 12-particle impact pulse. |
| **Blocks & Campaign** | [Block.cs](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/Assets/Scripts/BlockBreaker/Block.cs)<br>[LevelGenerator.cs](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/Assets/Scripts/BlockBreaker/LevelGenerator.cs)<br>[LevelConfiguration.cs](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/Assets/Scripts/BlockBreaker/LevelConfiguration.cs) | `Assets/Prefabs/Blocks/PF_Block_*.prefab`<br>`Assets/Settings/Levels/SO_Level_*.asset` | 15-level campaign, Model A brick colors (Red dampens, Green boosts, Blue scatters), bomb cascades, glass shell 2-hit bricks, +20% onboarding scale on levels 1-3. |
| **Pickups & Hazards** | [PowerupCapsule.cs](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/Assets/Scripts/BlockBreaker/PowerupCapsule.cs) | `Assets/Prefabs/Powerups/PF_Drop_*.prefab` | Tumbling capsules (cyan buffs) and diamonds (crimson hazards), fall speed $4.5\text{ u/s}$, duration timers, HUD badges. |
| **Game Flow & Scoring** | [ArcadeGameManager.cs](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/Assets/Scripts/Core/ArcadeGameManager.cs) | Scene Coordinator GameObject | Lives (3 start, max 5), volley combo multipliers ($1\times \to 5\times$), clutch 12s countdown, railgun hyper-beam clear, victory freeze. |
| **Responsive Camera** | [ResponsiveCameraController.cs](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/Assets/Scripts/Core/ResponsiveCameraController.cs) | `Main Camera` | Fits $23 \times 32.5$ playfield across 16:9, 9:16, 19.5:9 aspect ratios. URP post-processing host. |
| **UI HUD & Menus** | [BlockBreakerHUD.cs](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/Assets/Scripts/UI/BlockBreakerHUD.cs)<br>[MainMenuUIManager.cs](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/Assets/Scripts/UI/MainMenuUIManager.cs)<br>[SafeAreaController.cs](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/Assets/Scripts/UI/SafeAreaController.cs) | `Assets/UI/BlockBreakerHUD.uxml`<br>`Assets/UI/MainMenuUI.uxml` | 3D extruded gradient buttons, radiant pod glows, animated mute toggles, Dynamic Island / notch safe area insets, iOS gesture deferral. |
| **Localization** | [LocalizationManager.cs](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/Assets/Scripts/Core/LocalizationManager.cs) | `Assets/Localization/Tables/ArcadeTable.asset` | Unity Localization Tables (`com.unity.localization`), runtime `StringDatabase` & `StringTable` authoritative queries, two-way `SelectedLocale` sync. |
| **Audio Engine** | [ArcadeAudioManager.cs](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/Assets/Scripts/Audio/ArcadeAudioManager.cs) | `AudioManager` GameObject | Plays `AU_*` audio clips with procedural fallback synthesis. |

---

## 5. Recent Milestones Completed (Where We Left Off)

1. **3D Custom Paddle Integration & Scale Calibration**:
   - `SM_Paddle.fbx` meshes (`Deck`, `Chassis`, `Keel`) integrated into `PF_Paddle.prefab`.
   - Scale calibration: child sockets (`visuals`, `frost`, `guns`, `Aperture`) set to local scale $X = 0.20\text{f}$.
   - All 272 automated EditMode tests passing, verifying scale calibration, docking points, collider bounds, and powerup expansion.

2. **URP Camera & Lighting Calibrations**:
   - Attached `UniversalAdditionalCameraData` to `Main Camera` with `renderPostProcessing = true` and `volumeLayerMask = ~0`.
   - Cleared default Unity skybox reflections (`RenderSettings.skybox = null`, `RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom`) to prevent metallic reflections of the sky horizon.
   - Refined neon cosmic lighting, bloom, and candy block surface reflections.

3. **Tactile 3D Extruded Gradient UI System**:
   - Implemented procedural gradient sprites (`TX_Grad_Card_Bg`, `TX_Grad_Ruby_Btn`, `TX_Grad_Emerald_Btn`, `TX_Grad_Titanium_Btn`).
   - Tactile 3D extruded button mechanics (7px resting shelf, 5px active click depression).
   - Menu parity between Main Menu and Pause Menu sub-screens, including custom animated checkmark mute toggles for SFX and Music (`68px × 68px`).

4. **Mobile Touch Ergonomics & Safe Area**:
   - `SafeAreaController.cs` dynamically computes Yoga percentage insets from screen dimensions, ensuring clear display below Dynamic Island and notches on iPhone 15/15 Pro.
   - `PlayerSettings.iOS.deferSystemGesturesMode` configured to `All` to prevent accidental iOS Home bar swipes.
   - Minimal touch guideline placed at bottom of screen.

5. **15-Level Campaign & Onboarding Scale**:
   - 15 ScriptableObject levels with high density grids (11–14 columns) and outer bumper flanks.
   - Levels 1–3 feature +20% enlarged blocks (`blockSize = 1.20f`) for touch readability.

---

## 6. How to Test and Verify Code Changes

When you make changes as Codex:
1. Always compile first:
   ```bash
   unity cmd recompile
   ```
2. Run the automated test suite:
   ```bash
   unity cmd run_tests --mode editor --timeout 90
   ```
3. If creating or modifying prefabs, check that the decoupled physics pattern is maintained (colliders on root, models on children).
4. If modifying `PF_Paddle.prefab`, verify child socket local scales remain $X = 0.20\text{f}$ so `PaddlePrefabTests.cs` continues to pass.

---

## 7. Recommended Next Workflows for Codex Testing

Here are suggested areas where Codex can be tested effectively on this codebase:
- **PlayMode / Integration Test Coverage**: Adding automated integration or PlayMode tests simulating ball-paddle strikes and powerup drops.
- **Additional Level Designs / Custom Themes**: Crafting new ScriptableObject level layouts or grid patterns in `Assets/Settings/Levels/`.
- **Performance Profiling & WebGPU Optimization**: Validating frame pacing, draw call batching, and memory allocations under high projectile/particle load.
- **Particle / VFX Polish**: Enhancing block break particle effects, hyper-beam shaders, or laser impact scorch marks.

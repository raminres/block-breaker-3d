# BlockBreaker: Game Design Document (GDD)

**Version**: 5.0  
**Project**: BlockBreaker (`com.RaminRasulzade.BlockBreaker`)  
**Engine**: Unity 6 (`6000.6.0f1`), Universal Render Pipeline (`URP 17.6.0`)  

---

## 1. Overview & Vision

**BlockBreaker** is a physics-driven 3D arcade brick breaker. It combines responsive paddle deflection, tactical brick physics, compounding volley combo multipliers, an escalating hazard system, and a physical shield defense across an expansive 30-level campaign.

### Core Pillars
1. **Kinetic Agency**: Deflection is player-steered via paddle contact point, tangential velocity, active strikes, and anti-trap trajectory safeguards.
2. **Tactile Differentiation**: Bricks behave physically based on color (dampen, boost, scatter).
3. **Escalating Challenge**: Positive buffs balance against tumbling hazard debuffs and environmental hazards.
4. **Dual-Layer Feedback**: Floating world-space popups and clean HUD dashboard pods provide immediate mechanical feedback.
5. **Cross-Platform Delivery**: High-performance execution on PC and iOS with notch / Dynamic Island safe-area adaptation, system gesture deferral, and tactile 3D extruded controls.

---

## 2. Core Game Mechanics

### 2.1 Inverted Stepped Paddle & Kinetic Strike
- **Normalized 1.0 Parent Prefab Architecture**:
  - The root container (`PF_Paddle.prefab`) strictly maintains `transform.localScale = Vector3(1, 1, 1)`.
  - Child sockets (`visuals`, `guns`, `frost`, `laser_gun`, `dock_point`, `vfx`) rest at true 1:1 scale `Vector3(1, 1, 1)`, allowing direct mesh parenting without warped transforms.
  - Dynamic width adjustments (Expander, Shortener) scale the child visual socket horizontally (`visuals.localScale.x = paddleWidth / 5.0f`) and update the root `BoxCollider.size.x`.
- **3-Tier Geometry**:
  - **Tier 1 (Strike Deck)**: $W = 5.0, H = 0.24, Z = 1.0$ at $Y = -6.0$ with cyan emissive rim (`MI_Paddle_Deck.mat`). Holds the primary `BoxCollider` ($H = 0.24$, center $Y = +0.38$).
  - **Tier 2 (Chassis)**: $W = 3.6, H = 0.20$ in dark brushed titanium (`MI_Paddle.mat`).
  - **Tier 3 (Keel / Thrusters)**: $W = 2.2, H = 0.16$ with engine vent glow (`MI_Paddle_Core.mat`).
  - Low profile ($0.60$ total height) and inward stepping ($\le 1.4\text{u}$/side) prevent side clipping.
- **Normal Guard**: Contact normal $Y \ge 0.25$ required for valid bounce; shallow side hits fall cleanly to killzone.
- **Kinetic Active Strike**: Striking with a moving paddle ($|V_x| \ge 3.5\text{ u/s}$) injects an active $+8\%$ speed pop (capped at $22\text{ u/s}$), snappier high-pitch pop audio ($1.22\times$), $18\%$ squash recoil, and cyan spark bursts (`PF_VFX_PaddleSpark`). Stationary paddles cushion and preserve ball speed.
- **Paddle Expansion / Shortening**: Buff widens paddle $+10\%$ compounding (max $12.0\text{u}$) with spring overshoot animation; shortener shrinks paddle $-18\%$ (min $2.4\text{u}$).

### 2.2 Deflection Math & Anti-Trap Physics
- **Steering Formula**:
  - `Exit Angle = Ray Angle + Hit Offset Steer (-32° to +32°) + Paddle Velocity Steer (-45° to +45°)`.
  - Reversal cuts are possible by swiping against incoming ball trajectory. Exit angle clamped to $[25^\circ, 155^\circ]$.
- **Anti-Trap Trajectory Safeguards**:
  - **Vertical Floor**: $|v_y| \ge v \cdot \sin(20^\circ)$ prevents flat horizontal bouncing.
  - **Wall Steepener**: $\ge 2$ consecutive wall bounces force exit trajectory $\ge 35^\circ$.
  - **Vertical Deadzone**: Excludes $[85^\circ, 95^\circ]$ cone; continuous play enforces $|v_x| \ge v \cdot \sin(5^\circ)$.
  - **45° Corner Chamfers**: Angled wedges at $(\pm 9.40, 23.40)$ prevent top corner traps.

### 2.3 Brick Physical Interactions (Model A)
Field blocks provide unique tactile and acoustic responses upon impact:
- 🔴 **Red (Tier 1, 10 pts) — Kinetic Dampener**: Absorbs ball energy, slowing velocity by $-1.2\text{ u/s}$ (floored at $14\text{ u/s}$). Plays low-pitch crunch ($0.90\times$).
- 🟢 **Green (Tier 2, 20 pts) — Kinetic Turbo**: Acts as a spring bumper, imparting $+10\%$ speed boost (capped at $22\text{ u/s}$). Plays bright mid-pitch shatter ($1.10\times$).
- 🔵 **Blue (Tier 3, 30 pts) — Optical Scatter**: Refracts trajectory by $\pm 18^\circ$ to $\pm 28^\circ$ to break repetitive loops while respecting anti-trap constraints. Plays crystalline chime ($1.25\times$).

---

## 3. Scoring & Progression

### 3.1 Volley Combos & Multipliers
```
Awarded Score = Base Brick Score × Capsule Multiplier × Volley Multiplier × Multi-Ball Count
```
- **Volley Combo Multiplier**: Consecutive unreturned rallies increase the multiplier: Hits 1–2 ($1\times$), 3–4 ($2\times$), 5–7 ($3\times$), 8–10 ($4\times$), 11+ ($5\times$ MAX). Banks on paddle hit; resets on life loss. SFX pitches up $+1$ semitone per hit.
- **Bomb Chains**: Explosive bricks detonate within $2.5\text{u}$ radius with escalating chain multipliers ($\text{base} \times 1.5^{\text{chainIndex}}$).
- **Multi-Ball**: Spawns 2 extra balls at $\pm 35^\circ$. All points are multiplied by the count of active live balls ($2\times$ or $3\times$).

### 3.2 Clutch Hyper-Beam & Clear Sequence
- **Lone Block Countdown**: When 1 brick remains, triggers a 12s timer with a decaying score multiplier ($10\times \to 1\times$).
- **Hyper-Beam Railgun**: If the timer expires, an emergency vertical railgun beam sweeps upward from paddle to ceiling ($0.65\text{s}$ surge), vaporizing remaining bricks.
- **Level Clear Transition**: Balls freeze velocity, input locks, and falling drops freeze during a $1.0\text{s}$ standard (or $1.5\text{s}$ hyper-beam) celebration banner before opening the victory scorecard.

### 3.3 Scorecard & High Scores
- **Scorecard Modal**: Tallies blocks destroyed, peak combo, elapsed time vs par, under-par bonus (+500 pts), and flawless life bonus (+1,000 pts). Awards 1 to 3 stars with animated starburst effects.
- **Persistence & Mobile Formatting**: Tracks high scores and best clear times in `PlayerPrefs` via `HighScoreManager`. Date timestamps use compact `dd/MM` formatting (e.g. `22/10`) with invariant culture, guaranteeing pristine display without truncation or wrapping on mobile screens.

### 3.4 Ball Speed Dynamics & 3-Layer Hybrid Visualization System
To ensure ball speed changes from brick interactions (Red Dampen $-1.2$, Green Turbo $+10\%$), active strikes ($+8\%$), and hazards (Ball Slower $9.5\text{ u/s}$) are instantly transparent to players, the game implements a synchronized 3-layer visualization architecture:
1. **Layer 1: Diegetic Visuals (In-World Ball Trail & Emission Shifts)**:
   - Dynamic trail duration scaling: Ball `TrailRenderer` lengths expand with velocity (from $0.55\times$ at $9.5\text{ u/s}$ up to $1.75\times$ at $22.0\text{ u/s}$), providing immediate tactile speed cues.
   - Dynamic 4-tier color shifts:
     - ❄️ **Slow Tier (< 12 u/s)**: Frosted Ice Blue (`#59d9f2`).
     - ⚡ **Normal Tier (12–17 u/s)**: Electric Cyan (`#00f2fe`) blending into Solar Gold.
     - ☀️ **Boost Tier (17–20 u/s)**: Solar Gold (`#ffb300`).
     - 🔥 **Turbo Tier (>= 20 u/s)**: Blazing Plasma Flame (`#ff3d00`).
   - Multi-ball color preservation: Extra balls preserve their distinct multi-ball identity colors (Neon Magenta, Solar Gold, Emerald) while dynamically scaling trail duration.
2. **Layer 2: Event-Driven Floating Text Popups**:
   - Floating arcade popups spawn at impact coordinates and rise vertically with scale punch pops and alpha fades:
     - `TURBO +10%` (Neon Emerald) on Green Turbo brick hits.
     - `DAMPEN -1.2` (Crimson) on Red Kinetic Dampener brick hits.
     - `STRIKE! +8%` (Electric Cyan) on active moving paddle strikes ($|V_x| \ge 3.5\text{ u/s}$).
     - `BALL SLOWED!` (Frosted Blue) on Ball Slower hazard triggers.
     - `SPEED UP! +8%` (Solar Gold) on 10s sustained volley ramps.
3. **Layer 3: Cockpit HUD Speed Pod (`#speed-container`)**:
   - Integrated into `#top-bar` alongside lives, score, and timer.
   - Displays real-time multiplier (`1.0x`, `1.2x`, `1.6x`) formatted with invariant culture.
   - Glows dynamically matching active speed tiers (`.speed-tier-slow`, `.speed-tier-normal`, `.speed-tier-boost`, `.speed-tier-turbo`) and triggers micro punch-pops on speed changes.

---

## 4. Powerups, Hazards & Defense

Drops fall at $4.5\text{ u/s}$ with distinct 3D tumbling meshes and emissive colors:

### 4.1 Positive Buffs (Glowing Cyan `#00f2fe`, 3D Capsules)
- **Paddle Expander**: Widens paddle $+10\%$ compounding (max $12.0\text{u}$, 10s).
- **Score Multipliers**: Multiplies break score by $2\times, 3\times, 4\times,$ or $5\times$ (10s).
- **Multi-Ball**: Spawns 2 additional balls at $\pm 35^\circ$ angles.
- **Extra Heart**: Adds $+1$ life (up to 5 maximum).
- **Laser Blaster**: Mounts twin cannons firing ruby bolts ($34\text{ u/s}$) at $0.32\text{s}$ intervals (10s). Continuous volumetric SphereCast collision sweeps and kinematic Rigidbody ensure multi-hit penetration.

### 4.2 Negative Debuffs / Hazards (Warning Crimson `#ff1744`, 3D Diamonds)
- **Paddle Shortener**: Reduces paddle width by $-18\%$ (min $2.4\text{u}$, 10s).
- **Paddle Slower**: Adds input drag and reduces keyboard velocity by $-50\%$ (8s).
- **Brick Freezer**: Glaciates up to 5 bricks; each absorbs 1 defrost hit before shattering.
- **Ball Shrinker**: Shrinks ball radius to $60\%$ ($0.8\text{u} \to 0.48\text{u}$, 10s).
- **Ball Slower**: Reduces ball speed to $9.5\text{ u/s}$ (8s).
- **Paddle Freezer**: Immobilizes paddle for $1.2\text{s}$. Input triggers a visible tremor animation (`Mathf.Sin(Time.time * 45f) * 0.07f`).

### 4.3 Physical Shield Wall (`ShieldWall.cs`, `PF_ShieldWall.prefab`)
- Modular prefab consisting of a root `BoxCollider` (`size = (1, 1, 1)` with `PM_ArcadeBounce.physicMaterial`), visual `model` child, and `vfx` child (`ParticleSystem` cyan energy barrier).
- Deployed at arena bottom ($Y = -7.75\text{f}$) for 10s upon collecting a Shield drop.
- **Entrance Animation**: Smooth tween scale overshoot ($0 \to 1.08 \to 1.0$) with configurable parameters.
- **Physical Collision & VFX**: Bounces balls upward ($\ge 35^\circ$) to keep them in play without docking or life penalty; impacts trigger `shieldVfx.Emit(12)` particle bursts.
- **Anti-Trap One-Way Passthrough**: If a ball falls beneath the paddle and shield, upward trajectory passes through the shield collider cleanly without trapping.

### 4.4 Environmental Blocks
- **Glass Shells**: Require 2 hits (Hit 1: shatter shell, Hit 2: destroy brick for $2\times$ pts).
- **Bomb Bricks**: Explosive triggers detonating nearby blocks in a $2.5\text{u}$ radius.

### 4.5 Consolidated Shuriken Particle System VFX Suite
- Prefab-driven Shuriken VFX (`Assets/Prefabs/VFX/`) with pre-authored bursts and zero runtime scripting overrides:
  - `PF_VFX_BlockShatter`: Multi-mesh debris bursts (`SM_Block_Large_Particles`, `SM_Block_Small_Particles`, `SM_Block_Frost_Particles`) inheriting block materials.
  - `PF_VFX_BombExplosion`: High-energy shockwave ring (`MI_VFX_Shockwave.mat`) and radial explosive burst.
  - `PF_VFX_PaddleSpark`: Cyan kinetic sparks on active strikes.
  - `PF_VFX_FallingTrail`: Tumbling drop particle tails.
  - `PF_VFX_ShieldBarrier`: Energy impacts on shield defense.
- Direct scene serialization in `LV_BlockBreaker.unity` ensures 100% build bundling without `Resources/` folder.

---

## 5. Progressive 30-Level Campaign Arc

Dense layouts (11–14 columns, $13.75\text{u}–17.5\text{u}$ span) with side bumper flanks eliminate empty wall bypasses. Ball speed scales from $0.92\times$ (Level 1) to $1.80\times$ (Level 30), paddle narrows from $5.0\text{u}$ to $4.2\text{u}$, and hazards escalate progressively from 0 to 20.

| Level | Name | Archetype | Grid | Blocks | Speed | Paddle | Highlights & Hazards | Par | 3-Star |
| :---: | :--- | :---: | :---: | :---: | :---: | :---: | :--- | :---: | :---: |
| **1** | First Flight | `Pyramid` | $11 \times 6$ | 42 | 0.92x | 5.0u | Warmup, +20% enlarged blocks (1.20u), Expander, 0 hazards | 35s | 1,200 |
| **2** | Glass & Gold | `Diamond` | $12 \times 6$ | 42 | 0.96x | 5.0u | +20% enlarged blocks (1.20u), 2X, Glass, Expander, 1x Shortener | 40s | 1,800 |
| **3** | Twin Pillars | `Pillars` | $13 \times 6$ | 42 | 1.00x | 5.0u | +20% enlarged blocks (1.20u), Bombs, Laser, 1x Paddle Slower, 1x Ball Slower | 45s | 2,400 |
| **4** | Kinetic Shield | `Shield` | $13 \times 6$ | 62 | 1.04x | 5.0u | Shield, Heart, 1x Brick Freezer, 1x Ball Shrink | 50s | 3,200 |
| **5** | Multi-Ball Ring | `HollowBox` | $13 \times 6$ | 34 | 1.08x | 5.0u | Multi-Ball, Shield, 1x Paddle Freezer | 45s | 3,800 |
| **6** | Royal Crown | `Crown` | $13 \times 6$ | 70 | 1.12x | 4.8u | 3X, Laser, Multi-Ball, 4x hazards | 60s | 5,000 |
| **7** | Neon Heart | `Heart` | $13 \times 6$ | 52 | 1.16x | 4.8u | Hearts, Shield, 3X, 4x hazards | 50s | 4,200 |
| **8** | Space Invader | `Invader` | $13 \times 6$ | 34 | 1.20x | 4.8u | Multi-Ball, Laser, Bombs, 5x hazards | 45s | 4,500 |
| **9** | Crossfire | `Cross` | $13 \times 6$ | 48 | 1.24x | 4.8u | 4X, Multi-Ball, Bombs, 6x hazards | 50s | 5,200 |
| **10** | The Hourglass | `Hourglass` | $13 \times 6$ | 56 | 1.28x | 4.6u | 4X, 3X, Multi-Ball, 7x hazards | 55s | 6,000 |
| **11** | Chevron Strike | `Chevron` | $13 \times 6$ | 38 | 1.32x | 4.6u | Laser, 2x Multi-Balls, 8x hazards | 45s | 4,800 |
| **12** | Castle Bastion | `Castle` | $14 \times 6$ | 59 | 1.36x | 4.6u | 2x Shields, Multi-Balls, 9x hazards | 60s | 7,000 |
| **13** | Quantum Lattice | `CheckerboardEmpty` | $14 \times 6$ | 45 | 1.40x | 4.6u | 5X, Multi-Balls, 10x hazards | 50s | 6,500 |
| **14** | Striped Vault | `Stripes` | $14 \times 6$ | 44 | 1.44x | 4.5u | 5X, Laser, Shields, 11x hazards | 55s | 7,200 |
| **15** | Chaos Labyrinth | `Custom` | $14 \times 6$ | 66 | 1.48x | 4.5u | Climax, 2x Lasers, Multi-Balls, 13x hazards | 65s | 8,500 |
| **16** | Twin Peaks | `Pyramid` | $14 \times 6$ | 68 | 1.50x | 4.5u | Steep ravines, high speed deflections, 13x hazards | 65s | 13,000 |
| **17** | Inverted Citadel | `InvertedPyramid` | $14 \times 6$ | 60 | 1.52x | 4.5u | Downward fortress funnel, reinforced glass, 13x hazards | 65s | 13,500 |
| **18** | Iron Bastion | `Castle` | $14 \times 6$ | 64 | 1.54x | 4.5u | Fortified ramparts, explosive bombs, 14x hazards | 70s | 14,000 |
| **19** | Quantum Pillars | `Pillars` | $14 \times 6$ | 52 | 1.56x | 4.5u | Close-quartered coliseum, rapid ricochets, 14x hazards | 60s | 14,500 |
| **20** | The Vortex | `Hourglass` | $14 \times 6$ | 62 | 1.58x | 4.5u | Mid-arc bottleneck, blazing speed, 15x hazards | 65s | 15,000 |
| **21** | Double Cross | `Cross` | $14 \times 6$ | 56 | 1.60x | 4.4u | Dual crossfire sectors, narrowed 4.4u paddle, 15x hazards | 65s | 16,000 |
| **22** | Aegis Bastion | `Shield` | $14 \times 6$ | 66 | 1.62x | 4.4u | Shielded heraldic crest, laser and shield drops, 15x hazards | 70s | 17,000 |
| **23** | Alien Swarm | `Invader` | $14 \times 6$ | 48 | 1.64x | 4.4u | 8-bit armada, triple multi-balls, 16x hazards | 65s | 18,000 |
| **24** | Imperial Crown | `Crown` | $14 \times 6$ | 74 | 1.66x | 4.4u | Triple-jeweled diadem, three 5X multipliers, 16x hazards | 70s | 19,000 |
| **25** | Phoenix Wings | `Chevron` | $14 \times 6$ | 46 | 1.68x | 4.4u | Swept chevron wings, extreme lateral angles, 16x hazards | 65s | 20,000 |
| **26** | Void Lattice | `CheckerboardEmpty` | $14 \times 6$ | 52 | 1.70x | 4.2u | Deep quantum permeability, slimmed 4.2u paddle, 17x hazards | 65s | 22,000 |
| **27** | Cosmic Matrix | `FullGrid` | $14 \times 6$ | 84 | 1.72x | 4.2u | Solid 84-brick impenetrable matrix, dense bombs, 17x hazards | 80s | 24,000 |
| **28** | Hyperion Ring | `HollowBox` | $14 \times 6$ | 44 | 1.74x | 4.2u | Double containment ring, hollow kinetic cavity, 18x hazards | 70s | 26,000 |
| **29** | Cyber Grid | `Stripes` | $14 \times 6$ | 52 | 1.76x | 4.2u | Frequency bands, high clearance corridors, 18x hazards | 75s | 28,000 |
| **30** | Singularity Core | `Custom` | $14 \times 6$ | 72 | 1.80x | 4.2u | Grand Master Climax! Hyper-density ASCII core, 1.80x speed, 20x hazards | 85s | 32,000 |

*Volley pacing escalates ball speed +8% every 10s of sustained rally.*

---

## 6. Technical Architecture & UI

- **Arena & Camera**: Perspective camera at $38^\circ$ FOV with dynamic distance scaling (`ResponsiveCameraController`) for 16:9, 9:16, and 9:19.5 visibility. Arena walls: Top $Y = 24.25$, Sides $X = \pm 10.25$, Kill Zone $Y = -9.0$. `Main Camera` incorporates `UniversalAdditionalCameraData` (`renderPostProcessing = true`) for URP Volume Profiles and Bloom, while `RenderSettings.skybox = null` isolates reflections to the dark cosmic arena.
- **Modular Prefab Architecture**: 100% prefab-driven (`Assets/Prefabs/` for `Arena/`, `Paddle/`, `Balls/`, `Blocks/`, `Powerups/`), eliminating runtime procedural primitives (`GameObject.CreatePrimitive`):
  - `PF_Paddle.prefab`: Normalized 1.0 root container architecture. Child sockets (`visuals`, `guns`, `frost`, `laser_gun`, `dock_point`, `vfx`) maintain 1:1 scale without double-scaling issues.
  - `PF_LaserBolt.prefab`: Continuous volumetric sweep raycasting and kinematic `Rigidbody`.
  - `PF_Ball_Standard.prefab`: Normalized 1.0 container with decoupled physics and visuals.
- **UI Toolkit & Screen Harmonization**:
  - Unity 6 `PanelRenderer` HUD and Main Menu scaled to mobile portrait resolution (`1170x2532`).
  - **Dynamic UI Motion & Snappy Transitions (`UIAnimationHelper.cs`)**:
    - Modal card spring-in (`0.82 \to 1.0` with `ease-out-back` over $0.18\text{s}$) and exit fade/scale.
    - Cascading button entrance (`CascadeIn`) with $0.035\text{s}$ stagger across Main Menu, Pause Menu, Scorecard, and Level Grid selection cards.
    - Tactile button micro-depression (`PunchButton`: $0.93 \to 1.0$ over $0.08\text{s}$).
    - Punch pops & movement: Score pod punch pop on score events, combo multiplier badge pop ($1.25\times$), scorecard star bursts ($1.45\times$), and powerup/hazard badge entrance pops.
    - Redesigned dynamic Launch Prompt (`TAP TO LAUNCH`): Vertical layout with enlarged finger ($96\text{px}$) and directional arrows ($80\text{px}$), with continuous animated horizontal swipe (`StartHorizontalSwipe`) indicating left-right paddle steer without occluding the ball or paddle.
  - **Mobile Safe Area & Dynamic Island Adaptation**: `SafeAreaController.cs` calculates Yoga width-relative percentage insets (`CalculateYogaInsets`), preventing Dynamic Island and notch occlusion on iPhone 15/15 Pro in physical builds and Unity Device Simulator. `#safe-area-content` wraps top bar dashboard pods, while modals maintain full-bleed coverage.
  - **Touch Ergonomics & System Gesture Deferral**: `PlayerSettings.iOS.deferSystemGesturesMode = UnityEngine.iOS.SystemGestureDeferMode.All` configures iOS to require a deliberate double-swipe for Home bar navigation, preventing edge gesture drops. In `ArcadeUIManager`, `IsPointerOverUI` flips the $Y$ coordinate to match UI Toolkit top-left panel coordinates and only intercepts touches on interactive buttons, ensuring bottom 15% screen touches reliably drive paddle movement without UI interception. Touch sessions also seamlessly initialize on `pointerHeld` if iOS disambiguation delays frame 0 `pointerDown`.
  - **Tactile 3D Extruded Gradient Design System & Radiant Glow**:
    - Solid opaque obsidian navy backgrounds (`rgb(18, 24, 40)`) with 4 procedural vertical gradient sprites in `Assets/UI/Textures/Gradients/` (`TX_Grad_Card_Bg`, `TX_Grad_Ruby_Btn`, `TX_Grad_Emerald_Btn`, `TX_Grad_Titanium_Btn`).
    - Mechanical 3D extruded button geometry: resting 7px bottom shelf (`border-bottom-width: 7px;`), 2px top/side bevels, and active physical depression (`translate: 0 5px; border-bottom-width: 2px; border-top-width: 4px;`).
    - Radiant neon HUD pod glows (`box-shadow`), neon cyan/magenta dashboard contrast, and radiant `TAP TO LAUNCH` pill banner.
    - Propagated across all modal cards (`.modal-card`, `.level-modal-card`, `.scorecard-card`) and button styles (`.arcade-btn`, `.arcade-button`, `.btn-primary`, `.btn-secondary`, `.btn-default`, `.warning-btn`) across both Main Menu and Gameplay HUD.
  - **Menu Parity**: In-Game Pause Menu sub-screens (How to Play, Credits, Level Select, Options) match Main Menu in layout structure, fonts, button styling, and responsive proportions.
  - **Level Select Screen**: Expanded scroll view and card grid accommodating all 30 campaign stages with responsive touch scrolling and clear progression indicators.
  - **Tablet Screen Real-Estate & Compact Badges**: On squarer tablet displays (iPad Mini 6th gen, 3:2 ratio), active powerup and hazard badges feature compact dimensions (height $60\text{px}$, icons $36\text{px}$, vertical margin $6\text{px}$ with flex-wrapping), eliminating HUD overhang and strictly preventing block occlusion.
  - **Custom Animated Mute Toggles**: Both Main Menu and In-Game Pause Options panels feature custom checkmark toggles for SFX and Music sliders (`68px × 68px`). Toggles dynamically switch between speaker/mute icons with glowing cyan (`#21d4fd`) and crimson (`#ff3b56`) tints.
  - Main Menu features transparent root container with unified camera framing ($Z = -32\text{f}$) and Bloom. Sprites bound via `SO_PowerupIcons.asset`. Inset management via `SafeAreaController.cs`.
- **Unity Localization Tables (`com.unity.localization` 1.5.13)**:
  - Table-based multi-language architecture under `Assets/Localization/Tables/` (`ArcadeTable.asset`) containing English (`en`) and Turkish (`tr`) locales.
  - Translations can be modified directly within the Unity Editor's Localization Tables window or inspector without code changes.
  - Dynamic runtime lookup through `LocalizationManager.cs` with synchronous fallback dictionary for offline/unit test resilience.
- **Audio Engine**: `ArcadeAudioManager.cs` with custom clips (`AU_`) and procedural synthesizer fallback.
- **Controls**: Desktop (mouse 1:1 or A/D / Arrow keys, Space launch), Mobile (touch drag paddle, tap launch).
- **Tests**: 312 EditMode unit and integration tests via `unity cmd run_tests --mode editor`.

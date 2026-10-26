# BlockBreaker: Game Design Document (GDD)

**Version**: 5.1  
**Project**: BlockBreaker (`com.RaminRasulzade.BlockBreaker`)  
**Target Platforms**: iOS, PC (Desktop), WebGPU  
**Engine & Pipeline**: Unity 6 (`6000.6.0f1`), Universal Render Pipeline (`URP 17.6.0`)  

---

## 1. Overview & Vision

**BlockBreaker** is a modern physics-driven 3D arcade brick breaker. It reimagines classic breakout gameplay with tactile brick physics, responsive paddle deflection, active kinetic strikes, an escalating hazard drop system, and a physical shield defense across a progressive 30-level campaign.

### Core Pillars
1. **Kinetic Agency**: Deflection is steered by the player through contact position, paddle swipe velocity, and active kinetic strikes, supported by anti-trap trajectory guards.
2. **Tactile Differentiation**: Bricks behave physically based on color (dampen speed, spring boost, optical scatter).
3. **Escalating Challenge**: Beneficial powerups balance against tumbling hazard debuffs and armored environmental blocks.
4. **Dual-Layer Feedback**: World-space floating arcade popups, dynamic ball speed trails, and cockpit HUD pods provide instant mechanical transparency.
5. **Ergonomic Multi-Platform Experience**: Tailored for touch drag and desktop input with mobile safe-area insets, system gesture protection, and tactile 3D extruded controls.

### Player Lifecycle & Lives
- **Starting Lives**: 3 hearts per game session.
- **Maximum Life Capacity**: 5 hearts (expandable via Extra Heart powerup drops).
- **Life Loss**: If a ball drops past the bottom boundary into the abyss without an active shield, 1 life is lost and active volley combos reset.
- **Game Over**: Triggered when all lives are depleted. Displays the game over scorecard with final score, highest stage reached, and leaderboard placement.

---

## 2. Core Game Mechanics

### 2.1 Inverted Stepped Paddle & Kinetic Strike
- **Stepped Pyramid Geometry**:
  - **Tier 1 (Top Strike Deck)**: Full width ($W = 5.0, H = 0.24$), glowing cyan neon rim. Holds the primary strike collider.
  - **Tier 2 (Mid Chassis)**: $72\%$ width ($W = 3.6, H = 0.20$) in dark brushed titanium.
  - **Tier 3 (Bottom Keel / Thrusters)**: $44\%$ width ($W = 2.2, H = 0.16$) with twin thruster vents.
  - The inward-stepping profile ($0.60$ total height) eliminates phantom side catches.
- **Contact Normal Guard**: Contact normals must point predominantly upward ($N_y \ge 0.25$) for a valid save. Brushing edge hits cleanly fall toward the kill zone.
- **Active Kinetic Strike**:
  - When the paddle strikes the ball while moving horizontally ($|V_x| \ge 3.5\text{ u/s}$), it imparts an active $+8\%$ speed pop (capped at $22\text{ u/s}$), a higher-pitch impact audio ($1.22\times$), an $18\%$ tactile squash recoil, and a radiant cyan spark burst.
  - A stationary or slow-moving paddle cushions the impact, preserving ball speed for precision control.
- **Dynamic Width Scaling**:
  - Collecting a **Paddle Expander** widens the paddle $+10\%$ compounding (max $12.0\text{u}$) with a spring overshoot pulse.
  - Collecting a **Paddle Shortener** hazard shrinks the paddle $-18\%$ (min $2.4\text{u}$).

### 2.2 Deflection Math & Anti-Trap Physics
- **Steering Formula**:
  $$\text{Exit Angle} = \text{Ray Angle} + \text{Hit Offset Steer } (\pm 32^\circ) + \text{Paddle Velocity Steer } (\pm 45^\circ)$$
  - Counter-swiping against an incoming ball reverses horizontal momentum for aggressive directional cuts.
  - Exit angles are clamped to $[25^\circ, 155^\circ]$.
- **Anti-Trap Trajectory Safeguards**:
  - **Vertical Floor**: Trajectory enforces $|v_y| \ge v \cdot \sin(20^\circ)$, preventing shallow horizontal bouncing.
  - **Wall Steepener**: $\ge 2$ consecutive side wall bounces force the exit angle to steepen to $\ge 35^\circ$.
  - **Vertical Deadzone**: Trajectories strictly exclude the near-vertical $[85^\circ, 95^\circ]$ cone; continuous movement enforces $|v_x| \ge v \cdot \sin(5^\circ)$.
  - **45° Corner Chamfers**: Angled wedges at arena top corners $(\pm 9.40, 23.40)$ eliminate corner wedging.

### 2.3 Brick Physical Interactions (Model A)
Bricks respond with distinct mechanical behaviors upon collision:
- 🔴 **Red (Tier 1, 10 pts) — Kinetic Dampener**: Absorbs ball momentum, reducing speed by $-1.2\text{ u/s}$ (floored at $14\text{ u/s}$) with a deep crunch tone ($0.90\times$).
- 🟢 **Green (Tier 2, 20 pts) — Kinetic Turbo**: Spring bumper impulse, boosting speed by $+10\%$ (capped at $22\text{ u/s}$) with a bright shatter tone ($1.10\times$).
- 🔵 **Blue (Tier 3, 30 pts) — Optical Scatter**: Refracts the exit trajectory by $\pm 18^\circ$ to $\pm 28^\circ$ to break repetitive loops while respecting anti-trap constraints, accompanied by a crystalline chime ($1.25\times$).

---

## 3. Scoring, Combos & Progression

### 3.1 Scoring Formula & Volley Multipliers
$$\text{Awarded Points} = \text{Base Brick Score} \times \text{Capsule Multiplier} \times \text{Volley Multiplier} \times \text{Active Ball Count}$$

- **Volley Combo Multiplier**: Consecutive unreturned brick hits build a rally multiplier:
  - Hits 1–2: $1\times$
  - Hits 3–4: $2\times$
  - Hits 5–7: $3\times$
  - Hits 8–10: $4\times$
  - Hits 11+: $5\times$ (MAX)
  - The combo banks into cumulative score upon paddle contact; resets if a life is lost. Audio pitches up $+1$ semitone per consecutive hit.
- **Bomb Chains**: Detonates blocks within a $2.5\text{u}$ radius with a compound chain multiplier ($\text{Base} \times 1.5^{\text{chainIndex}}$).
- **Multi-Ball Synergy**: Spawns 2 extra balls at $\pm 35^\circ$. All points scored while multiple balls are in play are multiplied by the live ball count ($2\times$ or $3\times$).

### 3.2 Clutch Hyper-Beam & Clear Sequence
- **Lone Block Countdown**: When exactly 1 brick remains on the stage, a 12-second countdown begins with a decaying score multiplier ($10\times \to 1\times$, dropping by $1\times$ per second).
- **Railgun Hyper-Beam**: If the countdown expires, an emergency wide vertical beam surges upward from the paddle to the arena ceiling ($0.65\text{s}$ duration), obliterating remaining blocks.
- **Victory Freeze**: When the stage clears, ball velocities freeze, player input locks, and falling drops pause during a $1.0\text{s}$ celebration banner ($1.5\text{s}$ for Hyper-Beam) before revealing the scorecard.

### 3.3 Scorecard & Star Ratings
- **Star Rating Rules**:
  - ⭐ **1 Star**: Stage Clear (awarded automatically upon clearing all bricks).
  - ⭐⭐ **2 Stars**: Reaching the level's Tier 2 score threshold.
  - ⭐⭐⭐ **3 Stars**: Reaching the level's Tier 3 score threshold.
- **End-of-Level Bonuses**:
  - **Decaying Time Bonus**:
    $$\text{Time Bonus} = \max(0, \text{TimeBonusPool} - (\text{ElapsedSeconds} \times 35))$$
  - **Speedrun Under-Par Bonus**: $+500\text{ pts}$ if cleared within par time.
  - **Flawless Stage Bonus**: $+1,000\text{ pts}$ if cleared without losing a life.
- **Leaderboards & Persistence**:
  - Tracks top 10 high scores with compact `dd/MM` date formatting.
  - Persists best clear times and highest star ratings per stage in local save data.

### 3.4 3-Layer Ball Speed Visualization System
To ensure velocity shifts from brick collisions, active strikes, and hazards are instantly clear, the game coordinates three visual feedback layers:
1. **Layer 1: Diegetic Ball Visuals (In-World Trail & Color)**:
   - Dynamic trail duration: Scales from $0.55\times$ (sluggish $9.5\text{ u/s}$) up to $1.75\times$ (blazing $22.0\text{ u/s}$).
   - Dynamic 4-tier color palette:
     - ❄️ **Slow Tier (< 12 u/s)**: Frosted Ice Blue (`#59d9f2`).
     - ⚡ **Normal Tier (12–17 u/s)**: Electric Cyan (`#00f2fe`).
     - ☀️ **Boost Tier (17–20 u/s)**: Solar Gold (`#ffb300`).
     - 🔥 **Turbo Tier (>= 20 u/s)**: Blazing Plasma Flame (`#ff3d00`).
   - Secondary balls retain their distinct identity colors (Neon Magenta, Solar Gold, Emerald) while trail lengths scale dynamically.
2. **Layer 2: Event-Driven Floating Text Popups**:
   - Punchy floating popups spawn at impact coordinates and rise vertically:
     - `TURBO +10%` (Neon Emerald) on Green Turbo brick hits.
     - `DAMPEN -1.2` (Crimson) on Red Kinetic Dampener brick hits.
     - `STRIKE! +8%` (Electric Cyan) on active moving paddle strikes.
     - `BALL SLOWED!` (Frosted Blue) on Ball Slower hazard collections.
     - `SPEED UP! +8%` (Solar Gold) on 10s sustained volley ramps.
3. **Layer 3: Cockpit HUD Speed Pod**:
   - Integrated into the top bar, displaying real-time speed multiplier (`1.0x`, `1.2x`, `1.6x`).
   - Glows dynamically matching active speed tiers and triggers micro punch-pops on changes.

---

## 4. Pickups, Hazards & Defense Architecture

Collectible drops fall downward at $4.5\text{ u/s}$ ($Z = -1.0\text{f}$). All active falling drops are cleared upon life loss, shield saves, level clear, or game over.

### 4.1 Positive Powerup Buffs (Cyan `#00f2fe`, 3D Capsules)
- **Paddle Expander**: Widens paddle $+10\%$ compounding (max $12.0\text{u}$, 10s duration).
- **Score Multipliers**: Multiplies brick break points by $2\times, 3\times, 4\times,$ or $5\times$ (10s duration).
- **Multi-Ball**: Spawns 2 extra balls at $\pm 35^\circ$ angles, activating multi-ball score scaling.
- **Extra Heart**: Grants $+1$ life (up to 5 max).
- **Laser Blaster**: Mounts twin cannons firing penetrating ruby bolts ($34\text{ u/s}$) at $0.32\text{s}$ intervals (10s duration).

### 4.2 Negative Hazard Debuffs (Crimson `#ff1744`, 3D Diamonds)
- **Paddle Shortener**: Shrinks paddle width $-18\%$ (min $2.4\text{u}$, 10s duration).
- **Paddle Slower**: Adds drag and reduces horizontal speed by $-50\%$ (8s duration).
- **Brick Freezer**: Glaciates 5 bricks in ice; each absorbs 1 defrost hit before shattering.
- **Ball Shrinker**: Shrinks ball radius to $60\%$ ($0.8\text{u} \to 0.48\text{u}$, 10s duration).
- **Ball Slower**: Drops ball velocity to a sluggish $9.5\text{ u/s}$ (8s duration).
- **Paddle Freezer**: Immobilizes paddle for $1.2\text{s}$; player input triggers a rapid 45Hz visual struggle tremor.

### 4.3 Physical Shield Wall
- Deployed at the bottom of the arena ($Y = -7.75\text{f}$) for 10s upon collecting a Shield drop.
- **Entrance Animation**: Smooth expanding spring overshoot ($0 \to 1.08 \to 1.0$).
- **Collision & Defense**: Reflects balls upward ($\ge 35^\circ$) without life penalty, emitting a radiant cyan particle impact burst.
- **Anti-Trap Upward Passthrough**: Balls positioned below the shield can pass freely upward through the barrier, preventing trap slams into the kill zone.

### 4.4 Environmental Blocks
- **Glass Bricks**: 2-hit reinforced blocks (Hit 1: shatters glass shell; Hit 2: breaks brick for $2\times$ points).
- **Bomb Bricks**: Explosive triggers detonating all blocks within a $2.5\text{u}$ radius.

---

## 5. Progressive 30-Level Campaign Arc

The campaign features 30 hand-tuned layouts with outer flank bumper blocks eliminating empty wall bypasses. Ball speed scales from $0.92\times$ to $1.80\times$, paddle width narrows from $5.0\text{u}$ to $4.2\text{u}$, and hazards escalate progressively from 0 to 20.

- **Onboarding Block Scale (+20% on Levels 1–3)**: Levels 1, 2, and 3 feature enlarged blocks ($1.20\text{u}$) and wider spacing ($1.50\text{u}, 1.45\text{u}, 1.40\text{u}$) to maximize readability and touch comfort while preserving $> 1.0\text{u}$ clearance to arena walls.
- **Campaign Progression**: Clearing stage $N$ unlocks stage $N+1$. Players can revisit any unlocked stage via the Level Selection modal.

| Level | Name | Archetype | Grid | Blocks | Speed | Paddle | Highlights & Hazards | Par | 2-Star | 3-Star |
| :---: | :--- | :---: | :---: | :---: | :---: | :---: | :--- | :---: | :---: | :---: |
| **1** | First Flight | `Pyramid` | $11 \times 6$ | 42 | 0.92x | 5.0u | Warmup, +20% enlarged blocks (1.20u), Expander, 0 hazards | 35s | 1,200 | 2,200 |
| **2** | Glass & Gold | `Diamond` | $12 \times 6$ | 42 | 0.96x | 5.0u | +20% enlarged blocks (1.20u), 2X, Glass, Expander, 1x Shortener | 40s | 1,800 | 3,200 |
| **3** | Twin Pillars | `Pillars` | $13 \times 6$ | 42 | 1.00x | 5.0u | +20% enlarged blocks (1.20u), Bombs, Laser, 1x Paddle Slower, 1x Ball Slower | 45s | 2,600 | 4,200 |
| **4** | Kinetic Shield | `Shield` | $13 \times 6$ | 62 | 1.04x | 5.0u | Shield, Heart, 1x Brick Freezer, 1x Ball Shrink | 50s | 3,500 | 5,400 |
| **5** | Multi-Ball Ring | `HollowBox` | $13 \times 6$ | 34 | 1.08x | 5.0u | Multi-Ball, Shield, 1x Paddle Freezer | 45s | 4,400 | 6,800 |
| **6** | Royal Crown | `Crown` | $13 \times 6$ | 70 | 1.12x | 4.8u | 3X, Laser, Multi-Ball, 4x hazards | 60s | 5,800 | 8,800 |
| **7** | Neon Heart | `Heart` | $13 \times 6$ | 52 | 1.16x | 4.8u | Hearts, Shield, 3X, 4x hazards | 50s | 7,000 | 10,500 |
| **8** | Space Invader | `Invader` | $13 \times 6$ | 34 | 1.20x | 4.8u | Multi-Ball, Laser, Bombs, 5x hazards | 50s | 8,500 | 12,500 |
| **9** | Crossfire | `Cross` | $13 \times 6$ | 48 | 1.24x | 4.8u | 4X, Multi-Ball, Bombs, 6x hazards | 55s | 10,000 | 14,500 |
| **10** | The Hourglass | `Hourglass` | $13 \times 6$ | 56 | 1.28x | 4.6u | 4X, 3X, Multi-Ball, 7x hazards | 60s | 12,000 | 17,500 |
| **11** | Chevron Strike | `Chevron` | $13 \times 6$ | 38 | 1.32x | 4.6u | Laser, 2x Multi-Balls, 8x hazards | 45s | 13,500 | 19,500 |
| **12** | Castle Bastion | `Castle` | $14 \times 6$ | 59 | 1.36x | 4.6u | 2x Shields, Multi-Balls, 9x hazards | 65s | 16,000 | 23,000 |
| **13** | Quantum Lattice | `CheckerboardEmpty` | $14 \times 6$ | 45 | 1.40x | 4.6u | 5X, Multi-Balls, 10x hazards | 55s | 18,000 | 26,000 |
| **14** | Striped Vault | `Stripes` | $14 \times 6$ | 44 | 1.44x | 4.5u | 5X, Laser, Shields, 11x hazards | 60s | 21,000 | 30,000 |
| **15** | Chaos Labyrinth | `Custom` | $14 \times 6$ | 66 | 1.48x | 4.5u | Climax, 2x Lasers, Multi-Balls, 13x hazards | 70s | 25,000 | 36,000 |
| **16** | Twin Peaks | `Pyramid` | $14 \times 6$ | 56 | 1.50x | 4.5u | Steep ravines, high speed deflections, 13x hazards | 65s | 26,000 | 38,000 |
| **17** | Inverted Citadel | `InvertedPyramid` | $14 \times 6$ | 56 | 1.52x | 4.5u | Downward fortress funnel, reinforced glass, 13x hazards | 65s | 27,000 | 40,000 |
| **18** | Iron Bastion | `Castle` | $14 \times 6$ | 59 | 1.54x | 4.5u | Fortified ramparts, explosive bombs, 14x hazards | 70s | 28,000 | 42,000 |
| **19** | Quantum Pillars | `Pillars` | $14 \times 6$ | 45 | 1.56x | 4.5u | Close-quartered coliseum, rapid ricochets, 14x hazards | 60s | 29,000 | 44,000 |
| **20** | The Vortex | `Hourglass` | $14 \times 6$ | 62 | 1.58x | 4.5u | Mid-arc bottleneck, blazing speed, 15x hazards | 65s | 30,000 | 46,000 |
| **21** | Double Cross | `Cross` | $14 \times 6$ | 54 | 1.60x | 4.4u | Dual crossfire sectors, narrowed 4.4u paddle, 15x hazards | 65s | 32,000 | 48,000 |
| **22** | Aegis Bastion | `Shield` | $14 \times 6$ | 66 | 1.62x | 4.4u | Shielded heraldic crest, laser and shield drops, 15x hazards | 70s | 34,000 | 50,000 |
| **23** | Alien Swarm | `Invader` | $14 \times 6$ | 30 | 1.64x | 4.4u | 8-bit armada, triple multi-balls, 16x hazards | 65s | 36,000 | 53,000 |
| **24** | Imperial Crown | `Crown` | $14 \times 6$ | 76 | 1.66x | 4.4u | Triple-jeweled diadem, three 5X multipliers, 16x hazards | 70s | 38,000 | 56,000 |
| **25** | Phoenix Wings | `Chevron` | $14 \times 6$ | 38 | 1.68x | 4.4u | Swept chevron wings, extreme lateral angles, 16x hazards | 65s | 40,000 | 60,000 |
| **26** | Void Lattice | `CheckerboardEmpty` | $14 \times 6$ | 45 | 1.70x | 4.2u | Deep quantum permeability, slimmed 4.2u paddle, 17x hazards | 65s | 44,000 | 65,000 |
| **27** | Cosmic Matrix | `FullGrid` | $14 \times 6$ | 84 | 1.72x | 4.2u | Solid 84-brick impenetrable matrix, dense bombs, 17x hazards | 80s | 48,000 | 70,000 |
| **28** | Hyperion Ring | `HollowBox` | $14 \times 6$ | 36 | 1.74x | 4.2u | Double containment ring, hollow kinetic cavity, 18x hazards | 70s | 52,000 | 75,000 |
| **29** | Cyber Grid | `Stripes` | $14 \times 6$ | 44 | 1.76x | 4.2u | Frequency bands, high clearance corridors, 18x hazards | 75s | 56,000 | 80,000 |
| **30** | Singularity Core | `Custom` | $14 \times 6$ | 64 | 1.80x | 4.2u | Grand Master Finale! Hyper-dense core, 1.80x speed, 20x hazards | 85s | 64,000 | 95,000 |

*Volley pacing escalates ball speed $+8\%$ every 10s of sustained rally.*

---

## 6. User Interface, Presentation & Audio

### 6.1 Visual Aesthetics & UI Motion
- **Tactile 3D Extruded Gradient Design**:
  - Dark obsidian navy backdrop with vertical gradients for cards, primary buttons, secondary buttons, and warning actions.
  - Mechanical 3D extruded buttons featuring physical depression on click/touch for tactile feedback.
  - Radiant neon pod glows and dashboard contrast.
- **Dynamic UI Motion**:
  - Modals feature punchy spring pop-in transitions ($0.82 \to 1.0$) and quick exit fades.
  - Cascading button entrance reveals buttons sequentially from top to bottom.
  - Micro-depression feedback on button tap.
  - Dynamic `TAP TO LAUNCH` prompt: stacked layout with enlarged icons ($96\text{px}$ finger, $80\text{px}$ arrows) and animated horizontal swipe indicating steering direction without occluding the paddle or ball.
- **Screen Adaptation & Tablet Support**:
  - Insets protect top dashboard pods from camera cutouts, notches, and home indicator bars across mobile displays.
  - Compact powerup and hazard badges ($60\text{px}$ height, $36\text{px}$ icons) prevent block occlusion on squarer tablet displays (iPad Mini, 3:2 ratios).
  - Main Menu and Pause Menu sub-screens (How to Play, Credits, Level Select, Options) share 1:1 design and proportion parity.
- **Bilingual Localization**:
  - English and Turkish supported with instant runtime switching.
  - Custom animated checkmark mute toggles with glowing cyan/crimson audio feedback.

### 6.2 Controls
| Platform | Action | Input |
| :--- | :--- | :--- |
| **Desktop** | Move Paddle | Mouse Movement (1:1 tracking) or `A` / `D` / Arrow Keys |
| | Launch Ball | Left Mouse Button or `Space` |
| | Pause / Options | `Esc` or UI Button |
| **Mobile / Touch** | Move Paddle | Horizontal Finger Drag anywhere on screen |
| | Launch Ball | Screen Tap when docked |
| | UI Interaction | Touch interaction ($\ge 54\text{px}$ touch targets) |

### 6.3 Audio Soundscape
- **Pop**: Paddle and boundary wall impacts.
- **Break**: Brick destruction, pitch-scaled upwards by $+1$ semitone per combo streak and modulated by color tier (Red heavier, Green bright, Blue crystalline).
- **Chimes & Fanfares**: Banking combo chimes, celebratory scorecard starburst chimes, and ascending victory fanfares.
- **Powerup / Powerdown**: Bright ascending harmonic chime for buffs; gritty buzzing tone for hazard debuffs.
- **Special SFX**: High-pitched crystal shatter for glass bricks, resonant low rumble for bomb explosions, and sci-fi shield deflection chirps.
- **Fallback Procedural Synthesis**: Out-of-the-box procedural audio synthesis ensures rich arcade audio even without custom audio clips loaded.

# BlockBreaker 3D

[![Unity Version](https://img.shields.io/badge/Unity-6%20(6000.6.0f1)-black.svg?style=flat&logo=unity)](https://unity.com/)
[![Render Pipeline](https://img.shields.io/badge/Render%20Pipeline-URP-blue.svg)](https://unity.com/srp/universal-render-pipeline)
[![Automated Tests](https://img.shields.io/badge/Tests-312%2F312%20Passing%20(100%25)-brightgreen.svg)]()
[![Platforms](https://img.shields.io/badge/Platforms-iOS%20%7C%20WebGPU%20%7C%20PC-purple.svg)]()
[![Git LFS](https://img.shields.io/badge/Git-LFS%20Enabled-orange.svg)](https://git-lfs.github.com/)

**BlockBreaker 3D** is a physics-driven arcade brick breaker built with **Unity 6** and the **Universal Render Pipeline (URP)**. It features stepped pyramid paddle geometry with kinetic active strikes, tactile brick interactions, anti-trap trajectory safeguards, a 30-level campaign, collectible powerups and hazards, and a modern UI Toolkit interface adapted for desktop and mobile.

Also serves as a production testbed for **Google Antigravity**, **Unity MCP (Model Context Protocol)**, and **Unity CLI** agentic workflows.

---

## 🎮 Key Features

- **Kinetic Deflection & Active Strikes**: Moving paddle strikes impart tangential steer ($\pm 45^\circ$) and trigger a $+8\%$ kinetic speed pop with high-pitch audio and spark bursts.
- **Anti-Trap Safeguards**: Mathematical trajectory floors, wall steepening on repeated bounces, and $45^\circ$ corner chamfers prevent horizontal and vertical deadlocks.
- **Tactile Brick Physics**:
  - 🔴 **Red**: Kinetic dampener ($-1.2\text{ u/s}$, pitch $0.90\times$)
  - 🟢 **Green**: Kinetic turbo ($+10\%$ speed impulse, pitch $1.10\times$)
  - 🔵 **Blue**: Optical scatter ($\pm 18^\circ$ to $\pm 28^\circ$ refraction)
  - 🛡️ **Glass**: 2-hit reinforced brick (shell shatter $\to$ break)
  - 💣 **Bomb**: Radial explosion cascade ($2.5\text{u}$ radius)
- **Powerups & Hazards**: Tumbling drops including Paddle Expander/Shortener, Multi-Ball, Physical Shield Wall, Laser Blaster, Brick Freezer, Ball Shrinker, and Slowers.
- **30-Level Campaign**: High-density layouts scaling from $0.92\times$ to $1.80\times$ ball speed, with onboarding scale comfort on early stages and clutch Railgun Hyper-Beam clears.
- **Modern UI & Multi-Platform Ergonomics**: Responsive camera framing, mobile Safe Area / Dynamic Island insets, 3D extruded buttons, and bilingual localization (English / Turkish).

---

## 🕹️ Controls

| Platform | Action | Input |
| :--- | :--- | :--- |
| **Desktop** | Move Paddle | Mouse Movement or `A` / `D` / Arrow Keys |
| | Launch Ball | Left Mouse Button or `Space` |
| | Pause / Options | `Esc` or UI Button |
| **Mobile / Touch** | Move Paddle | Horizontal Finger Drag anywhere on screen |
| | Launch Ball | Screen Tap when docked |
| | UI Navigation | Touch interaction ($\ge 54\text{px}$ touch targets) |

---

## 🏗️ Technical Stack

- **Engine**: Unity 6 (`6000.6.0f1`), Universal Render Pipeline (`URP 17.6.0`)
- **UI System**: UI Toolkit (`PanelRenderer`) with responsive Yoga layouts and safe-area insets
- **Audio**: Custom audio clips (`AU_*`) with procedural synthesis fallback
- **Localization**: Unity Localization Tables (`com.unity.localization` 1.5.13)
- **Architecture**: 100% modular prefabs under `Assets/Prefabs/` with normalized 1.0 root parent scales
- **Tests**: 312 EditMode NUnit unit and integration tests

---

## 🧪 Running Tests

Run the automated test suite via Unity CLI:

```bash
unity cmd run_tests --mode editor --timeout 90
```

---

## 📚 Documentation

- [Game Design Document (GDD)](GDD.md) — Comprehensive game design specifications, level layouts, and scoring mechanics.
- [Agent Specifications (AGENTS.md)](AGENTS.md) — Architectural rules, prefab standards, deflection math, and subsystem lookup.

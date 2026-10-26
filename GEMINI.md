# BlockBreaker: System Rules & Architectural Pointer

> **Primary Source of Truth**: For complete subsystem maps, mathematical constraints, and prefab hierarchies, see [AGENTS.md](AGENTS.md).

---

## 1. Non-Negotiable Core Rules
1. **Preserve User-Authored Work (STRICT)**: The user hand-tunes scenes (`Assets/Scenes/LV_BlockBreaker.unity`), materials (`Assets/Materials/BlockBreaker/MI_*`), prefabs (`Assets/Prefabs/Arena/PF_*`), post-processing (`Assets/Settings/SampleSceneProfile.asset`), and gradient textures (`Assets/Textures/Backgrounds/TX_Background_Gradient_*.png`). **NEVER** overwrite these files or run automated scene rebuild menus (`Tools > Arcade > Setup All Block Breaker Scenes`).
2. **Normalized 1.0 Parent Prefab Scale**: All root prefab containers (`PF_Paddle.prefab`, `PF_Ball_Standard.prefab`, `PF_LaserBolt.prefab`) strictly maintain `transform.localScale = Vector3.one`. Dynamic expansions adjust child visual sockets (`visuals.localScale.x = paddleWidth / 5.0f`) and root colliders without reciprocal squishing.
3. **Zero Runtime Primitives**: Never use `GameObject.CreatePrimitive` at runtime or in scene setups. All entities instantiate directly from `Assets/Prefabs/`.
4. **Decoupled Physics & Visuals**: Physical colliders belong exclusively on root containers; child visual meshes have all colliders removed.
5. **Prefab-Driven Authoring**: Author particle curves, materials, transforms, and colliders directly on prefabs or scenes—never hardcode runtime overrides in C# (`Awake`/`Start`/`Update`).
6. **Standalone & Mobile Bundling Without Resources**: Every runtime prefab must be referenced by a Scene GameObject in `LV_BlockBreaker.unity` so Unity linker bundles it without a `Resources/` folder.
7. **No Warning Suppression**: Never suppress compiler warnings, use hacks, or bypass type systems.

---

## 2. Key Commands & Verification
- **Run Automated Tests**:
  ```bash
  unity cmd run_tests --mode editor --timeout 90
  ```
- **Recompile Assemblies**:
  ```bash
  unity cmd recompile
  ```
- **Active Branch**: `feature/visual-and-object-refinements` (Git LFS enabled).
- **Engine / Pipeline**: Unity 6 (`6000.6.0f1`), Universal Render Pipeline (`URP 17.6.0`).

# Agent Skills & Playbook: Unity URP & Cinemachine Automation

Automation recipes, MCP tool patterns, conventions, and C# editor techniques for Unity 6 and URP.

---

## 1. Project Specifications
- **Unity Version**: Unity 6 (`6000.6.0f1`), Universal Render Pipeline (`URP 17.6.0`)
- **Cinemachine Version**: 3.x (`com.unity.cinemachine@6.6.0`)
- **Key Assemblies**: `Unity.Cinemachine`, `UnityEngine.Rendering.Universal`, `UnityEngine.Rendering`, `Arcade.Gameplay`, `Arcade.Editor`

---

## 2. Material System Workflow
Studio naming conventions:
- **Master Materials**: Prefix `MT_` (e.g. `MT_Master_PBR_URP.mat`).
  - Created via `create_asset` or C# `new Material(Shader.Find("Universal Render Pipeline/Lit"))`.
- **Material Instances (Variants)**: Prefix `MI_` (e.g. `MI_Block_Red.mat`).
  - In Unity 6 / URP, Material Variants inherit from master via `materialInstance.parent = masterMaterial;`.
  - Override properties (`_BaseColor`, `_Smoothness`, `_Metallic`) on instance while preserving parent linkage.
- **Assigning to MeshRenderer**: Set via `set_serialized_field` (`m_Materials.Array.data[0]`) or C# `eval` (`meshRenderer.sharedMaterial = ...`).

---

## 3. Animation & Animator Workflow
1. **Clip Creation**: Tool `create_animation_clip` (`path`, `frameRate: 60`, `loop: true`).
2. **Curve Generation**: Tool `set_animation_curve`.
   - Property paths: `m_LocalPosition.x/y/z` (Transform), `localEulerAnglesRaw.x/y/z` (Transform).
   - Keyframe format: `[{"time": t, "value": v, "inTangent": 0, "outTangent": 0}]`.
   - Zero tangents create smooth ease-in / ease-out endpoints without velocity snapping.
3. **Controller & State Setup**:
   - Tool `create_animator_controller` in `Assets/Animations/`.
   - Tool `add_animator_state` with motion clip and `isDefault: true`.
   - Bind to GameObject's `Animator` component field `m_Controller`.

---

## 4. Lighting & Environment Configuration
1. **Studio Three-Point Lighting**:
   - **Key Light**: Primary angled ~45° side, warm (`RGBA(1.0, 0.88, 0.45, 1.0)`), intensity `1.5 - 2.0`, soft shadows.
   - **Fill Light**: Angled opposite, cool (`RGBA(0.35, 0.65, 1.0, 1.0)`), intensity `0.6 - 0.8`, no shadows.
   - **Rim Light**: Angled from behind for silhouette separation.
2. **Atmosphere & Cosmic Dark Void**:
   - `RenderSettings.skybox = null;`
   - `RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;`
   - Camera `clearFlags = CameraClearFlags.SolidColor` with dark cosmic color.
3. **URP Post-Processing (Volume)**:
   - Profile at `Assets/Settings/SampleSceneProfile.asset`.
   - Access overrides (`Bloom`, `Tonemapping`, `Vignette`) via `profile.TryGet<T>(out var comp)`.
   - Always set `overrideState = true` on fields when modifying values.

---

## 5. Cinemachine 3.x Tracking
1. **Main Camera**: Requires `Unity.Cinemachine.CinemachineBrain`.
2. **Virtual Camera**: Component `Unity.Cinemachine.CinemachineCamera`.
   - Target binding: `vcam.LookAt = targetTransform;`, `vcam.Follow = targetTransform;`.
3. **Aim Behaviors**:
   - `CinemachineRotationComposer`: Smooth damped rotation (`Damping = Vector3(0.5, 0.5, 0.5)`).
   - `CinemachineHardLookAt`: Rigid look-at without damping.

---

## 6. MCP & Roslyn `eval` Best Practices
- **Standard Tools**: Use `get_scene_hierarchy`, `create_gameobject`, `set_transform`, `set_serialized_field` for common CRUD operations.
- **Roslyn `eval`**: Use for complex operations, multi-step transactions, or editor APIs lacking direct tool bindings (Material Variants, RenderSettings, Cinemachine bindings).
- **Rules**:
  - Always write valid C# statements terminated with semicolons.
  - Return a serializable result (`return "Success";`).
  - Call `Undo.RegisterCreatedObjectUndo(...)` or `Undo.RecordObject(...)` for undoability.
  - Call `EditorSceneManager.MarkSceneDirty(...)` and `EditorSceneManager.SaveOpenScenes()` or `AssetDatabase.SaveAssets()` to persist changes.

---

## 7. Asset Pipeline & Preset Manager Automation
1. **Naming Conventions**:
   - Audio: `AU_` prefix.
   - Static Meshes: `SM_` prefix.
   - Skeletal Meshes: `SK_` prefix.
   - Textures: `TX_` prefix with semantic suffixes (`_BaseColor`, `_MetallicSmoothness`, `_Normal`, `_AO`, `_Emissive`).
2. **Platform Overrides**:
   - Standalone: Textures BC7 (BC5 Normal), Max Size 2048. Audio Vorbis quality 0.7.
   - iOS: Textures ASTC 6x6, Max Size 2048. Audio AAC quality 0.7.
3. **Preset Manager Glob Patterns (`Assets/Presets/PR_*`)**:
   - `TextureImporter`: `*_Normal*` -> `PR_Normal.preset`, `*_MetallicSmoothness*` -> `PR_MetallicSmoothness.preset`, `*_Emissive*` -> `PR_Emissive.preset`, `*BaseColor*` / `*TX_*` -> `PR_BaseColor.preset`, `*Icons/*` -> `PR_Icon.preset`.
   - `ModelImporter`: `*SM_*` -> `PR_StaticMesh.preset`, `*SK_*` -> `PR_SkeletalMesh.preset`.
   - `AudioImporter`: `*AU_*` -> `PR_Audio.preset`.

---

## 8. Unity 6 UI Toolkit PanelRenderer Patterns
1. **Safe Runtime Initialization**:
   - In Unity 6, `RegisterUIReloadCallback` fires only on asset re-import, not Play Mode entry.
   - Use `EnsureInitialized()` guard querying `panelRenderer.rootVisualElement` before binding buttons or setting state.
2. **Script Execution Order**:
   - Apply `[DefaultExecutionOrder(-100)]` on `ArcadeGameManager` and `[-90]` on `ArcadeInputHandler` so managers initialize before entity `Awake()`/`Start()`.
3. **Pointer Event Pass-Through & USS Standards**:
   - Layout container overlays must specify `picking-mode="Ignore"` in UXML or `element.pickingMode = PickingMode.Ignore;` in C# so they do not block gameplay input.
   - **Never** use invalid web CSS in USS stylesheets (such as `pointer-events: none;`, `box-shadow`, `color: inherit;`, or multi-layer drop shadows), as these trigger compiler warnings in Unity 6. Use single `text-shadow`, native borders/backgrounds, and UXML picking modes instead.
4. **Touch Target Dimensions**: Minimum 54px–64px height for mobile/touch ergonomic targets.

---

## 9. Ball Trail & Dynamic VFX Architecture
1. **Dual-Layer Stacked Trails (`BallController.cs`)**:
   - Outer Trail: Translucent halo ($0.55$ start width, $0.22\text{s}$ duration, $40\%$ alpha).
   - Inner Trail: Luminous core ($0.25$ start width, $0.16\text{s}$ duration, $85\%$ alpha, $+65\%$ white core).
2. **Zero-Allocation MaterialPropertyBlock**:
   - Tint sphere mesh base color and emissive glow using `MaterialPropertyBlock` (`_EmissionColor`, `_BaseColor`), preserving SRP Batcher efficiency.
3. **Docking Lifecycle**:
   - Trails clear and disable emission while docked in `ReadyToLaunch` or paused; resume emission seamlessly on launch/unpause.

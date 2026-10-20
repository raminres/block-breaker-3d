using System.Collections.Generic;
using UnityEngine;

namespace Arcade.BlockBreaker
{
    /// <summary>
    /// Spawns particle bursts and 3D mesh particle debris when blocks shatter and powerups are collected.
    /// Standardized 100% on Unity's Particle System (Shuriken) for zero GC allocations, instant frame-0 PSO prewarming,
    /// and universal mobile/desktop/Web stability.
    /// Emits large and small mesh particles (SM_Block_Large_Particles, SM_Block_Small_Particles) matching block materials,
    /// and frost mesh particles (SM_Block_Frost_Particles) for glaciated blocks.
    /// </summary>
    public class BlockVFXManager : MonoBehaviour
    {
        public static BlockVFXManager Instance { get; private set; }

        [Header("Shuriken Shatter Prefab")]
        [SerializeField] private GameObject shatterPrefab;

        [Header("Materials")]
        [SerializeField] private Material particleMaterial;
        [SerializeField] private Material debrisMaterial;
        [SerializeField] private Material materialRed;
        [SerializeField] private Material materialGreen;
        [SerializeField] private Material materialBlue;
        [SerializeField] private Material materialGlass;

        [Header("Mesh Sources")]
        [SerializeField] private Mesh[] largeParticleMeshes;
        [SerializeField] private Mesh[] smallParticleMeshes;
        [SerializeField] private Mesh[] frostParticleMeshes;

        [Header("Pool Configuration")]
        [SerializeField] private int poolSize = 10;
        [SerializeField] private bool spawnPhysicalSubBoxes = true; // Kept for inspector compatibility
        [SerializeField] private int debrisPiecesPerBlock = 8;
        [SerializeField] private float debrisExplosionForce = 7f;

        public Material DebrisMaterial => debrisMaterial;
        public Material ParticleMaterial => particleMaterial;
        public GameObject ShatterPrefab => shatterPrefab;

        public void SetDebrisMaterial(Material mat) => debrisMaterial = mat;
        public void SetParticleMaterial(Material mat) => particleMaterial = mat;
        public void SetShatterPrefab(GameObject prefab) => shatterPrefab = prefab;

        public static void SetInstanceForTesting(BlockVFXManager instance)
        {
            Instance = instance;
        }

        private Material cachedFallbackMaterial;
        private Material cachedFallbackParticleMaterial;
        private Material cachedFallbackGlassMaterial;
        private static MaterialPropertyBlock debrisPropBlock;

        // Pooled Shuriken shatter instances
        public class ShatterInstance
        {
            public GameObject root;
            public ParticleSystem sparksPs;
            public ParticleSystemRenderer sparksPsr;
            public ParticleSystem largePs;
            public ParticleSystemRenderer largePsr;
            public ParticleSystem smallPs;
            public ParticleSystemRenderer smallPsr;
            public ParticleSystem frostPs;
            public ParticleSystemRenderer frostPsr;
            public float elapsed;
            public float duration;
            public int activeMeshCount;
        }

        private readonly Queue<ShatterInstance> shatterPool = new Queue<ShatterInstance>();
        private readonly List<ShatterInstance> activeShatterList = new List<ShatterInstance>(32);
        private int totalActiveDebrisCount = 0;

        private static readonly int BaseColorPropId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorPropId = Shader.PropertyToID("_Color");
        private static readonly int EmissionColorPropId = Shader.PropertyToID("_EmissionColor");

        public int AvailableVfxCount => shatterPool.Count;
        public int AvailableDebrisCount => shatterPool.Count * debrisPiecesPerBlock;
        public int ActiveDebrisCount => totalActiveDebrisCount;
        public int ActiveVfxCount => activeShatterList.Count;

        private Transform poolContainer;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                if (Application.isPlaying)
                    Destroy(gameObject);
                else
                    DestroyImmediate(gameObject);
                return;
            }

            Instance = this;
            EnsureReferences();
            InitializePool();
        }

        private void Start()
        {
            Prewarm();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
            ClearAllActive();
        }

        private Transform GetOrCreatePoolContainer()
        {
            if (poolContainer == null)
            {
                var existing = transform.Find("_Pool_VFX");
                if (existing != null)
                {
                    poolContainer = existing;
                }
                else
                {
                    var poolGo = new GameObject("_Pool_VFX");
                    poolGo.transform.SetParent(transform);
                    poolGo.transform.position = new Vector3(0f, -500f, 0f);
                    poolContainer = poolGo.transform;
                }
            }
            return poolContainer;
        }

        public void EnsureReferences()
        {
#if UNITY_EDITOR
            if (shatterPrefab == null)
            {
                shatterPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/VFX/PF_VFX_BlockShatter.prefab");
            }
            if (particleMaterial == null)
            {
                particleMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/BlockBreaker/MI_VFX_Burst.mat");
            }
            if (debrisMaterial == null)
            {
                debrisMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/BlockBreaker/MI_Block_Debris.mat");
            }
            if (materialRed == null)
            {
                materialRed = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/BlockBreaker/MI_Block_Red.mat");
            }
            if (materialGreen == null)
            {
                materialGreen = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/BlockBreaker/MI_Block_Green.mat");
            }
            if (materialBlue == null)
            {
                materialBlue = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/BlockBreaker/MI_Block_Blue.mat");
            }
            if (materialGlass == null)
            {
                materialGlass = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/BlockBreaker/MI_Block_Glass.mat");
            }
            if (largeParticleMeshes == null || largeParticleMeshes.Length == 0)
            {
                largeParticleMeshes = LoadMeshesFromFbx("Assets/Models/SM_Block_Large_Particles.fbx");
            }
            if (smallParticleMeshes == null || smallParticleMeshes.Length == 0)
            {
                smallParticleMeshes = LoadMeshesFromFbx("Assets/Models/SM_Block_Small_Particles.fbx");
            }
            if (frostParticleMeshes == null || frostParticleMeshes.Length == 0)
            {
                frostParticleMeshes = LoadMeshesFromFbx("Assets/Models/SM_Block_Frost_Particles.fbx");
            }
#endif
        }

        private static Mesh[] LoadMeshesFromFbx(string path)
        {
#if UNITY_EDITOR
            var all = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path);
            var list = new List<Mesh>();
            foreach (var a in all)
            {
                if (a is Mesh m && !m.name.StartsWith("__preview__"))
                {
                    list.Add(m);
                }
            }
            return list.ToArray();
#else
            return new Mesh[0];
#endif
        }

        public void InitializePool()
        {
            var container = GetOrCreatePoolContainer();
            EnsureReferences();

            for (int i = 0; i < poolSize; i++)
            {
                ShatterInstance inst = CreateShatterInstance();
                inst.root.SetActive(false);
                inst.root.transform.SetParent(container);
                inst.root.transform.position = new Vector3(0f, -500f, 0f);
                shatterPool.Enqueue(inst);
            }
        }

        private ShatterInstance CreateShatterInstance()
        {
            GameObject rootGo;
            if (shatterPrefab != null)
            {
                rootGo = Instantiate(shatterPrefab, GetOrCreatePoolContainer());
                rootGo.name = "VFX_Burst_Instance";
            }
            else
            {
                rootGo = BuildProceduralShatterInstance();
                rootGo.transform.SetParent(GetOrCreatePoolContainer());
            }

            rootGo.SetActive(false);
            rootGo.transform.position = new Vector3(0f, -500f, 0f);

            var inst = new ShatterInstance { root = rootGo };

            // Find child emitters
            var allPs = rootGo.GetComponentsInChildren<ParticleSystem>(true);
            foreach (var ps in allPs)
            {
                string n = ps.gameObject.name.ToLower();
                var psr = ps.GetComponent<ParticleSystemRenderer>();
                if (n.Contains("spark"))
                {
                    inst.sparksPs = ps;
                    inst.sparksPsr = psr;
                }
                else if (n.Contains("large"))
                {
                    inst.largePs = ps;
                    inst.largePsr = psr;
                }
                else if (n.Contains("small"))
                {
                    inst.smallPs = ps;
                    inst.smallPsr = psr;
                }
                else if (n.Contains("frost"))
                {
                    inst.frostPs = ps;
                    inst.frostPsr = psr;
                }
            }

            // Fallback bindings if named children not matched
            if (inst.sparksPs == null && allPs.Length > 0)
            {
                inst.sparksPs = allPs[0];
                inst.sparksPsr = allPs[0].GetComponent<ParticleSystemRenderer>();
            }

            return inst;
        }

        private GameObject BuildProceduralShatterInstance()
        {
            GameObject root = new GameObject("VFX_Burst_Instance");

            // Sparks
            GameObject sparksGo = new GameObject("Sparks");
            sparksGo.transform.SetParent(root.transform, false);
            var psSparks = sparksGo.AddComponent<ParticleSystem>();
            var psrSparks = sparksGo.GetComponent<ParticleSystemRenderer>();
            ConfigureSparksEmitter(psSparks, psrSparks, GetOrCreateParticleMaterial());

            // Large
            GameObject largeGo = new GameObject("Large_Shards");
            largeGo.transform.SetParent(root.transform, false);
            var psLarge = largeGo.AddComponent<ParticleSystem>();
            var psrLarge = largeGo.GetComponent<ParticleSystemRenderer>();
            ConfigureMeshEmitter(psLarge, psrLarge, largeParticleMeshes, GetOrCreateDebrisMaterial(), 3.5f, 7.0f, 0.75f, 1.1f, 0.75f, 1.0f, 2.5f);

            // Small
            GameObject smallGo = new GameObject("Small_Shards");
            smallGo.transform.SetParent(root.transform, false);
            var psSmall = smallGo.AddComponent<ParticleSystem>();
            var psrSmall = smallGo.GetComponent<ParticleSystemRenderer>();
            ConfigureMeshEmitter(psSmall, psrSmall, smallParticleMeshes, GetOrCreateDebrisMaterial(), 4.5f, 9.0f, 0.45f, 0.7f, 0.65f, 0.9f, 2.8f);

            // Frost
            GameObject frostGo = new GameObject("Frost_Shards");
            frostGo.transform.SetParent(root.transform, false);
            var psFrost = frostGo.AddComponent<ParticleSystem>();
            var psrFrost = frostGo.GetComponent<ParticleSystemRenderer>();
            ConfigureMeshEmitter(psFrost, psrFrost, frostParticleMeshes, GetOrCreateGlassMaterial(), 4.0f, 8.0f, 0.55f, 0.85f, 0.7f, 0.95f, 2.6f);

            return root;
        }

        private static void ConfigureSparksEmitter(ParticleSystem ps, ParticleSystemRenderer psr, Material mat)
        {
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 1.0f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.55f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(5f, 11f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.26f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = 0.5f;

            var emission = ps.emission;
            emission.enabled = false;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.25f;

            var col = ps.colorOverLifetime;
            col.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            col.color = grad;

            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0f)));

            if (psr != null && mat != null)
            {
                psr.renderMode = ParticleSystemRenderMode.Billboard;
                psr.sharedMaterial = mat;
            }
        }

        private static void ConfigureMeshEmitter(ParticleSystem ps, ParticleSystemRenderer psr, Mesh[] meshes, Material mat,
            float speedMin, float speedMax, float sizeMin, float sizeMax, float lifetimeMin, float lifetimeMax, float gravity)
        {
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 1.2f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifetimeMin, lifetimeMax);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speedMin, speedMax);
            main.startSize = new ParticleSystem.MinMaxCurve(sizeMin, sizeMax);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = gravity;

            var emission = ps.emission;
            emission.enabled = false;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.35f;

            var rot = ps.rotationOverLifetime;
            rot.enabled = true;
            rot.separateAxes = true;
            rot.x = new ParticleSystem.MinMaxCurve(-360f * Mathf.Deg2Rad, 360f * Mathf.Deg2Rad);
            rot.y = new ParticleSystem.MinMaxCurve(-360f * Mathf.Deg2Rad, 360f * Mathf.Deg2Rad);
            rot.z = new ParticleSystem.MinMaxCurve(-360f * Mathf.Deg2Rad, 360f * Mathf.Deg2Rad);

            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, 1f, 0f, 0f),
                new Keyframe(0.65f, 0.9f, -0.4f, -0.8f),
                new Keyframe(1.0f, 0f, -2.5f, 0f)
            ));

            if (psr != null)
            {
                psr.renderMode = ParticleSystemRenderMode.Mesh;
                if (meshes != null && meshes.Length > 0)
                {
                    psr.SetMeshes(meshes);
                }
                if (mat != null)
                {
                    psr.sharedMaterial = mat;
                }
            }
        }

        public void Prewarm()
        {
            if (!Application.isEditor)
            {
                Shader.WarmupAllShaders();
            }

            if (shatterPool.Count > 0)
            {
                var peek = shatterPool.Peek();
                if (peek != null)
                {
                    var baseMat = GetOrCreateParticleMaterial();
                    if (peek.sparksPsr != null && baseMat != null) peek.sparksPsr.sharedMaterial = baseMat;
                    var debMat = GetOrCreateDebrisMaterial();
                    if (peek.largePsr != null && debMat != null) peek.largePsr.sharedMaterial = debMat;
                    if (peek.smallPsr != null && debMat != null) peek.smallPsr.sharedMaterial = debMat;
                }
            }
        }

        public Material GetOrCreateParticleMaterial()
        {
            if (particleMaterial != null) return particleMaterial;

            if (cachedFallbackParticleMaterial == null)
            {
#if UNITY_EDITOR
                particleMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/BlockBreaker/MI_VFX_Burst.mat");
                if (particleMaterial != null) return particleMaterial;
#endif
                var shader = Shader.Find("Arcade/VFX_ParticleBurst") ?? Shader.Find("Universal Render Pipeline/Particles/Unlit");
                if (shader != null)
                {
                    cachedFallbackParticleMaterial = new Material(shader) { name = "M_VFX_Burst_Fallback" };
                }
            }
            return cachedFallbackParticleMaterial;
        }

        public Material GetOrCreateDebrisMaterial()
        {
            if (debrisMaterial != null) return debrisMaterial;

            if (cachedFallbackMaterial == null)
            {
#if UNITY_EDITOR
                debrisMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/BlockBreaker/MI_Block_Debris.mat");
                if (debrisMaterial != null) return debrisMaterial;
#endif
                var shader = Shader.Find("Arcade/VFX_BlockDebris") ?? Shader.Find("Universal Render Pipeline/Lit");
                if (shader != null)
                {
                    cachedFallbackMaterial = new Material(shader) { name = "M_BlockDebris_Fallback" };
                }
            }
            return cachedFallbackMaterial;
        }

        public Material GetOrCreateGlassMaterial()
        {
            if (materialGlass != null) return materialGlass;

            if (cachedFallbackGlassMaterial == null)
            {
#if UNITY_EDITOR
                materialGlass = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/BlockBreaker/MI_Block_Glass.mat");
                if (materialGlass != null) return materialGlass;
#endif
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Universal Render Pipeline/Unlit");
                if (shader != null)
                {
                    cachedFallbackGlassMaterial = new Material(shader) { name = "M_BlockGlass_Fallback" };
                }
            }
            return cachedFallbackGlassMaterial;
        }

        public Material ResolveMaterialForColor(Color blockColor)
        {
            if (debrisMaterial != null) return debrisMaterial;

            // Match color to dedicated material
            if (materialRed != null && blockColor.r > 0.8f && blockColor.g < 0.4f) return materialRed;
            if (materialGreen != null && blockColor.g > 0.7f && blockColor.b < 0.5f && blockColor.r < 0.5f) return materialGreen;
            if (materialBlue != null && blockColor.b > 0.7f && blockColor.r < 0.5f) return materialBlue;

#if UNITY_EDITOR
            if (blockColor.r > 0.8f && blockColor.g < 0.4f)
            {
                var mat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/BlockBreaker/MI_Block_Red.mat");
                if (mat != null) return mat;
            }
            if (blockColor.g > 0.7f && blockColor.b < 0.5f && blockColor.r < 0.5f)
            {
                var mat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/BlockBreaker/MI_Block_Green.mat");
                if (mat != null) return mat;
            }
            if (blockColor.b > 0.7f && blockColor.r < 0.5f)
            {
                var mat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/BlockBreaker/MI_Block_Blue.mat");
                if (mat != null) return mat;
            }
#endif

            return GetOrCreateDebrisMaterial();
        }

        /// <summary>
        /// Shatters a block into spark particles and 3D mesh particles.
        /// Spawns some large particles (SM_Block_Large_Particles) and a bit more small particles (SM_Block_Small_Particles)
        /// with gravity drop and tumbling rotation, styled with the original block's material.
        /// </summary>
        public void PlayBlockShatter(Vector3 position, Color blockColor, Vector3 hitNormal, Material blockMaterial = null, bool isFrost = false)
        {
            EnsureReferences();
            ShatterInstance inst = shatterPool.Count > 0 ? shatterPool.Dequeue() : CreateShatterInstance();
            inst.root.transform.position = position;
            inst.root.SetActive(true);

            debrisPropBlock ??= new MaterialPropertyBlock();
            debrisPropBlock.Clear();
            debrisPropBlock.SetColor(BaseColorPropId, blockColor);
            debrisPropBlock.SetColor(ColorPropId, blockColor);
            debrisPropBlock.SetColor(EmissionColorPropId, blockColor * 1.3f);

            ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams
            {
                applyShapeToPosition = true
            };

            int meshCount = 0;

            if (isFrost)
            {
                // Frosted block shatter: emit frost mesh shards (SM_Block_Frost_Particles)
                Material targetMat = blockMaterial ?? GetOrCreateGlassMaterial();
                if (inst.frostPs != null)
                {
                    if (inst.frostPsr != null)
                    {
                        if (targetMat != null) inst.frostPsr.sharedMaterial = targetMat;
                        inst.frostPsr.SetPropertyBlock(debrisPropBlock);
                    }
                    int frostCount = 8;
                    inst.frostPs.Emit(emitParams, frostCount);
                    meshCount += frostCount;
                }
            }
            else
            {
                // Regular or glass block: emit large and small shards matching block material
                Material targetMat = blockMaterial ?? ResolveMaterialForColor(blockColor);

                // 1. Large shards: some large particles (3 particles)
                if (inst.largePs != null)
                {
                    if (inst.largePsr != null)
                    {
                        if (targetMat != null) inst.largePsr.sharedMaterial = targetMat;
                        inst.largePsr.SetPropertyBlock(debrisPropBlock);
                    }
                    int largeCount = 3;
                    inst.largePs.Emit(emitParams, largeCount);
                    meshCount += largeCount;
                }

                // 2. Small shards: a bit more small particles (5 particles)
                if (inst.smallPs != null)
                {
                    if (inst.smallPsr != null)
                    {
                        if (targetMat != null) inst.smallPsr.sharedMaterial = targetMat;
                        inst.smallPsr.SetPropertyBlock(debrisPropBlock);
                    }
                    int smallCount = 5;
                    inst.smallPs.Emit(emitParams, smallCount);
                    meshCount += smallCount;
                }
            }

            // 3. Sparks burst
            if (inst.sparksPs != null)
            {
                if (inst.sparksPsr != null)
                {
                    Material pMat = GetOrCreateParticleMaterial();
                    if (pMat != null && inst.sparksPsr.sharedMaterial != pMat)
                    {
                        inst.sparksPsr.sharedMaterial = pMat;
                    }
                }
                ParticleSystem.EmitParams sparkParams = new ParticleSystem.EmitParams
                {
                    startColor = blockColor * 1.4f,
                    applyShapeToPosition = true
                };
                inst.sparksPs.Emit(sparkParams, 16);
            }

            inst.elapsed = 0f;
            inst.duration = 1.2f;
            inst.activeMeshCount = meshCount;
            totalActiveDebrisCount += meshCount;

            activeShatterList.Add(inst);
        }

        public void PlayBlockShatter(Vector3 position, Color blockColor, Vector3 hitNormal)
        {
            PlayBlockShatter(position, blockColor, hitNormal, null, false);
        }

        /// <summary>
        /// Shatters frost encasement on a block using SM_Block_Frost_Particles meshes with falling physics.
        /// </summary>
        public void PlayFrostShatter(Vector3 position, Vector3 hitNormal)
        {
            PlayBlockShatter(position, new Color(0.6f, 0.95f, 1.0f, 0.8f), hitNormal, GetOrCreateGlassMaterial(), true);
        }

        /// <summary>
        /// Shatters glass shell on a 2-hit block using falling glass mesh particles.
        /// </summary>
        public void PlayGlassShatter(Vector3 position, Vector3 hitNormal)
        {
            PlayBlockShatter(position, new Color(0.85f, 0.95f, 1.0f, 0.5f), hitNormal, GetOrCreateGlassMaterial(), false);
        }

        /// <summary>
        /// Fires a radiant particle burst when a powerup capsule is intercepted by the paddle.
        /// </summary>
        public void PlayPowerupCollect(Vector3 position, Color powerupColor)
        {
            EnsureReferences();
            ShatterInstance inst = shatterPool.Count > 0 ? shatterPool.Dequeue() : CreateShatterInstance();
            inst.root.transform.position = position;
            inst.root.SetActive(true);

            if (inst.sparksPs != null)
            {
                if (inst.sparksPsr != null)
                {
                    Material mat = GetOrCreateParticleMaterial();
                    if (mat != null && inst.sparksPsr.sharedMaterial != mat)
                    {
                        inst.sparksPsr.sharedMaterial = mat;
                    }
                }

                ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams
                {
                    startColor = powerupColor * 1.5f,
                    startLifetime = 0.55f,
                    applyShapeToPosition = true
                };
                inst.sparksPs.Emit(emitParams, 32);
            }

            inst.elapsed = 0f;
            inst.duration = 0.65f;
            inst.activeMeshCount = 0; // Powerup collects emit sparks only, zero debris pieces

            activeShatterList.Add(inst);
        }

        /// <summary>
        /// Emits a directional spark burst at the paddle collision contact point.
        /// </summary>
        public void PlayPaddleHitSpark(Vector3 position, bool isSmash = false)
        {
            EnsureReferences();
            ShatterInstance inst = shatterPool.Count > 0 ? shatterPool.Dequeue() : CreateShatterInstance();
            inst.root.transform.position = position;
            inst.root.SetActive(true);

            if (inst.sparksPs != null)
            {
                if (inst.sparksPsr != null)
                {
                    Material mat = GetOrCreateParticleMaterial();
                    if (mat != null && inst.sparksPsr.sharedMaterial != mat)
                    {
                        inst.sparksPsr.sharedMaterial = mat;
                    }
                }

                Color sparkColor = isSmash ? new Color(0.2f, 1f, 1f, 1f) : new Color(0f, 0.85f, 1f, 0.8f);
                int count = isSmash ? 24 : 10;
                float lifetime = isSmash ? 0.45f : 0.35f;

                ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams
                {
                    startColor = sparkColor * 1.5f,
                    startLifetime = lifetime,
                    applyShapeToPosition = true
                };
                inst.sparksPs.Emit(emitParams, count);
            }

            inst.elapsed = 0f;
            inst.duration = isSmash ? 0.5f : 0.4f;
            inst.activeMeshCount = 0;

            activeShatterList.Add(inst);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            for (int i = activeShatterList.Count - 1; i >= 0; i--)
            {
                ShatterInstance inst = activeShatterList[i];
                if (inst.root == null)
                {
                    activeShatterList.RemoveAt(i);
                    continue;
                }

                inst.elapsed += dt;
                if (inst.elapsed >= inst.duration)
                {
                    RecycleInstance(inst);
                    activeShatterList.RemoveAt(i);
                }
            }
        }

        private void RecycleInstance(ShatterInstance inst)
        {
            if (inst == null || inst.root == null) return;

            totalActiveDebrisCount = Mathf.Max(0, totalActiveDebrisCount - inst.activeMeshCount);
            inst.activeMeshCount = 0;

            if (inst.sparksPs != null)
            {
                inst.sparksPs.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                inst.sparksPs.Clear();
            }
            if (inst.largePs != null)
            {
                inst.largePs.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                inst.largePs.Clear();
            }
            if (inst.smallPs != null)
            {
                inst.smallPs.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                inst.smallPs.Clear();
            }
            if (inst.frostPs != null)
            {
                inst.frostPs.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                inst.frostPs.Clear();
            }

            inst.root.SetActive(false);
            inst.root.transform.position = new Vector3(0f, -500f, 0f);
            shatterPool.Enqueue(inst);
        }

        /// <summary>
        /// Instantly recycles all active VFX instances back into the pool.
        /// </summary>
        public void ClearAllActive()
        {
            for (int i = activeShatterList.Count - 1; i >= 0; i--)
            {
                RecycleInstance(activeShatterList[i]);
            }
            activeShatterList.Clear();
            totalActiveDebrisCount = 0;
        }
    }
}

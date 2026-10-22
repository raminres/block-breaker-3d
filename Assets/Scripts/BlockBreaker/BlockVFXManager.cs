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

        [Header("Shuriken Prefabs")]
        [SerializeField] private GameObject shatterPrefab;
        [SerializeField] private GameObject powerupCollectPrefab;
        [SerializeField] private GameObject paddleSparkPrefab;
        [SerializeField] private GameObject bombExplosionPrefab;

        public const string SHATTER_PREFAB_PATH = "Assets/Prefabs/VFX/PF_VFX_BlockShatter.prefab";
        public const string POWERUP_COLLECT_PREFAB_PATH = "Assets/Prefabs/VFX/PF_VFX_PowerupCollect.prefab";
        public const string PADDLE_SPARK_PREFAB_PATH = "Assets/Prefabs/VFX/PF_VFX_PaddleSpark.prefab";
        public const string BOMB_EXPLOSION_PREFAB_PATH = "Assets/Prefabs/VFX/PF_VFX_BombExplosion.prefab";

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
        public GameObject PowerupCollectPrefab => powerupCollectPrefab;
        public GameObject PaddleSparkPrefab => paddleSparkPrefab;
        public GameObject BombExplosionPrefab => bombExplosionPrefab;

        public void SetDebrisMaterial(Material mat) => debrisMaterial = mat;
        public void SetParticleMaterial(Material mat) => particleMaterial = mat;
        public void SetShatterPrefab(GameObject prefab) => shatterPrefab = prefab;
        public void SetPowerupCollectPrefab(GameObject prefab) => powerupCollectPrefab = prefab;
        public void SetPaddleSparkPrefab(GameObject prefab) => paddleSparkPrefab = prefab;
        public void SetBombExplosionPrefab(GameObject prefab) => bombExplosionPrefab = prefab;

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
            public ParticleSystem[] allPs;
            public ParticleSystemRenderer[] allPsr;
            public float elapsed;
            public float duration;
            public int activeMeshCount;
            public System.Action<ShatterInstance> recycleCallback;
        }

        private readonly Queue<ShatterInstance> shatterPool = new Queue<ShatterInstance>();
        private readonly Queue<ShatterInstance> powerupCollectPool = new Queue<ShatterInstance>();
        private readonly Queue<ShatterInstance> paddleSparkPool = new Queue<ShatterInstance>();
        private readonly Queue<ShatterInstance> bombExplosionPool = new Queue<ShatterInstance>();
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
                shatterPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(SHATTER_PREFAB_PATH);
            }
            if (powerupCollectPrefab == null)
            {
                powerupCollectPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(POWERUP_COLLECT_PREFAB_PATH);
            }
            if (paddleSparkPrefab == null)
            {
                paddleSparkPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(PADDLE_SPARK_PREFAB_PATH);
            }
            if (bombExplosionPrefab == null)
            {
                bombExplosionPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(BOMB_EXPLOSION_PREFAB_PATH);
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
            EnsureReferences();
            GameObject rootGo = null;
            if (shatterPrefab != null)
            {
                rootGo = Instantiate(shatterPrefab, GetOrCreatePoolContainer());
                rootGo.name = "VFX_Burst_Instance";
            }
            else
            {
#if UNITY_EDITOR
                shatterPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(SHATTER_PREFAB_PATH);
                if (shatterPrefab != null)
                {
                    rootGo = Instantiate(shatterPrefab, GetOrCreatePoolContainer());
                    rootGo.name = "VFX_Burst_Instance";
                }
#endif
            }

            if (rootGo == null)
            {
                rootGo = new GameObject("VFX_Burst_Instance");
                rootGo.transform.SetParent(GetOrCreatePoolContainer());
            }

            rootGo.SetActive(false);
            rootGo.transform.position = new Vector3(0f, -500f, 0f);

            var inst = new ShatterInstance
            {
                root = rootGo,
                allPs = rootGo.GetComponentsInChildren<ParticleSystem>(true),
                allPsr = rootGo.GetComponentsInChildren<ParticleSystemRenderer>(true),
                recycleCallback = i => { i.root.SetActive(false); shatterPool.Enqueue(i); }
            };

            // Find child emitters
            foreach (var ps in inst.allPs)
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
            if (inst.sparksPs == null && inst.allPs.Length > 0)
            {
                inst.sparksPs = inst.allPs[0];
                inst.sparksPsr = inst.allPsr.Length > 0 ? inst.allPsr[0] : null;
            }

            return inst;
        }

        private ShatterInstance CreatePowerupCollectInstance()
        {
            EnsureReferences();
            GameObject rootGo = null;
            if (powerupCollectPrefab != null)
            {
                rootGo = Instantiate(powerupCollectPrefab, GetOrCreatePoolContainer());
                rootGo.name = "VFX_PowerupCollect_Instance";
            }
            else
            {
#if UNITY_EDITOR
                powerupCollectPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(POWERUP_COLLECT_PREFAB_PATH);
                if (powerupCollectPrefab != null)
                {
                    rootGo = Instantiate(powerupCollectPrefab, GetOrCreatePoolContainer());
                    rootGo.name = "VFX_PowerupCollect_Instance";
                }
#endif
            }

            if (rootGo == null)
            {
                return CreateShatterInstance();
            }

            rootGo.SetActive(false);
            rootGo.transform.position = new Vector3(0f, -500f, 0f);

            var inst = new ShatterInstance
            {
                root = rootGo,
                allPs = rootGo.GetComponentsInChildren<ParticleSystem>(true),
                allPsr = rootGo.GetComponentsInChildren<ParticleSystemRenderer>(true),
                recycleCallback = i => { i.root.SetActive(false); powerupCollectPool.Enqueue(i); }
            };

            if (inst.allPs.Length > 0)
            {
                inst.sparksPs = inst.allPs[0];
                inst.sparksPsr = inst.allPsr.Length > 0 ? inst.allPsr[0] : null;
            }

            return inst;
        }

        private ShatterInstance CreatePaddleSparkInstance()
        {
            EnsureReferences();
            GameObject rootGo = null;
            if (paddleSparkPrefab != null)
            {
                rootGo = Instantiate(paddleSparkPrefab, GetOrCreatePoolContainer());
                rootGo.name = "VFX_PaddleSpark_Instance";
            }
            else
            {
#if UNITY_EDITOR
                paddleSparkPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(PADDLE_SPARK_PREFAB_PATH);
                if (paddleSparkPrefab != null)
                {
                    rootGo = Instantiate(paddleSparkPrefab, GetOrCreatePoolContainer());
                    rootGo.name = "VFX_PaddleSpark_Instance";
                }
#endif
            }

            if (rootGo == null)
            {
                return CreateShatterInstance();
            }

            rootGo.SetActive(false);
            rootGo.transform.position = new Vector3(0f, -500f, 0f);

            var inst = new ShatterInstance
            {
                root = rootGo,
                allPs = rootGo.GetComponentsInChildren<ParticleSystem>(true),
                allPsr = rootGo.GetComponentsInChildren<ParticleSystemRenderer>(true),
                recycleCallback = i => { i.root.SetActive(false); paddleSparkPool.Enqueue(i); }
            };

            if (inst.allPs.Length > 0)
            {
                inst.sparksPs = inst.allPs[0];
                inst.sparksPsr = inst.allPsr.Length > 0 ? inst.allPsr[0] : null;
            }

            return inst;
        }

        private ShatterInstance CreateBombExplosionInstance()
        {
            EnsureReferences();
            GameObject rootGo = null;
            if (bombExplosionPrefab != null)
            {
                rootGo = Instantiate(bombExplosionPrefab, GetOrCreatePoolContainer());
                rootGo.name = "VFX_BombExplosion_Instance";
            }
            else
            {
#if UNITY_EDITOR
                bombExplosionPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(BOMB_EXPLOSION_PREFAB_PATH);
                if (bombExplosionPrefab != null)
                {
                    rootGo = Instantiate(bombExplosionPrefab, GetOrCreatePoolContainer());
                    rootGo.name = "VFX_BombExplosion_Instance";
                }
#endif
            }

            if (rootGo == null)
            {
                return CreateShatterInstance();
            }

            rootGo.SetActive(false);
            rootGo.transform.position = new Vector3(0f, -500f, 0f);

            var inst = new ShatterInstance
            {
                root = rootGo,
                allPs = rootGo.GetComponentsInChildren<ParticleSystem>(true),
                allPsr = rootGo.GetComponentsInChildren<ParticleSystemRenderer>(true),
                recycleCallback = i => { i.root.SetActive(false); bombExplosionPool.Enqueue(i); }
            };

            if (inst.allPs.Length > 0)
            {
                inst.sparksPs = inst.allPs[0];
                inst.sparksPsr = inst.allPsr.Length > 0 ? inst.allPsr[0] : null;
            }

            return inst;
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
            ShatterInstance inst = powerupCollectPool.Count > 0 ? powerupCollectPool.Dequeue() : CreatePowerupCollectInstance();
            inst.root.transform.position = position;
            inst.root.SetActive(true);

            if (inst.allPs != null && inst.allPs.Length > 0)
            {
                for (int i = 0; i < inst.allPs.Length; i++)
                {
                    var ps = inst.allPs[i];
                    if (inst.allPsr != null && i < inst.allPsr.Length && inst.allPsr[i] != null)
                    {
                        Material mat = GetOrCreateParticleMaterial();
                        if (mat != null && inst.allPsr[i].sharedMaterial != mat)
                        {
                            inst.allPsr[i].sharedMaterial = mat;
                        }
                    }

                    ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams
                    {
                        startColor = powerupColor * 1.5f,
                        applyShapeToPosition = true
                    };
                    int count = ps.gameObject.name.ToLower().Contains("ring") ? 1 : 28;
                    ps.Emit(emitParams, count);
                }
            }
            else if (inst.sparksPs != null)
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
            ShatterInstance inst = paddleSparkPool.Count > 0 ? paddleSparkPool.Dequeue() : CreatePaddleSparkInstance();
            inst.root.transform.position = position;
            inst.root.SetActive(true);

            Color sparkColor = isSmash ? new Color(0.2f, 1f, 1f, 1f) : new Color(0f, 0.85f, 1f, 0.8f);
            int count = isSmash ? 24 : 12;
            float lifetime = isSmash ? 0.45f : 0.35f;

            if (inst.allPs != null && inst.allPs.Length > 0)
            {
                for (int i = 0; i < inst.allPs.Length; i++)
                {
                    var ps = inst.allPs[i];
                    if (inst.allPsr != null && i < inst.allPsr.Length && inst.allPsr[i] != null)
                    {
                        Material mat = GetOrCreateParticleMaterial();
                        if (mat != null && inst.allPsr[i].sharedMaterial != mat)
                        {
                            inst.allPsr[i].sharedMaterial = mat;
                        }
                    }

                    ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams
                    {
                        startColor = sparkColor * 1.5f,
                        startLifetime = lifetime,
                        applyShapeToPosition = true
                    };
                    ps.Emit(emitParams, count);
                }
            }
            else if (inst.sparksPs != null)
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

        /// <summary>
        /// Fires an explosive shockwave and spark blast when a Bomb block detonates.
        /// </summary>
        public void PlayBombExplosion(Vector3 position)
        {
            EnsureReferences();
            ShatterInstance inst = bombExplosionPool.Count > 0 ? bombExplosionPool.Dequeue() : CreateBombExplosionInstance();
            inst.root.transform.position = position;
            inst.root.SetActive(true);

            if (inst.allPs != null && inst.allPs.Length > 0)
            {
                for (int i = 0; i < inst.allPs.Length; i++)
                {
                    var ps = inst.allPs[i];
                    string n = ps.gameObject.name.ToLower();
                    ParticleSystem.EmitParams ep = new ParticleSystem.EmitParams { applyShapeToPosition = true };
                    ps.Emit(ep, n.Contains("shock") ? 1 : 36);
                }
            }

            inst.elapsed = 0f;
            inst.duration = 0.85f;
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

            if (inst.allPs != null)
            {
                foreach (var ps in inst.allPs)
                {
                    if (ps != null)
                    {
                        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                        ps.Clear();
                    }
                }
            }
            if (inst.sparksPs != null) { inst.sparksPs.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); inst.sparksPs.Clear(); }
            if (inst.largePs != null) { inst.largePs.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); inst.largePs.Clear(); }
            if (inst.smallPs != null) { inst.smallPs.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); inst.smallPs.Clear(); }
            if (inst.frostPs != null) { inst.frostPs.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); inst.frostPs.Clear(); }

            inst.root.SetActive(false);
            inst.root.transform.position = new Vector3(0f, -500f, 0f);

            if (inst.recycleCallback != null)
            {
                inst.recycleCallback(inst);
            }
            else
            {
                shatterPool.Enqueue(inst);
            }
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

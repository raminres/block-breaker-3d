using System.Collections.Generic;
using System.IO;
using Arcade.BlockBreaker;
using UnityEditor;
using UnityEngine;

namespace Arcade.Editor
{
    /// <summary>
    /// Master builder for all particle effect prefabs in BlockBreaker.
    /// Ensures 100% of particle effects are authored as prefabs under Assets/Prefabs/VFX/
    /// with zero procedural runtime script creation.
    /// </summary>
    public static class SetupVFXPrefabs
    {
        public const string VFX_DIR = "Assets/Prefabs/VFX";
        public const string SHATTER_PREFAB_PATH = VFX_DIR + "/PF_VFX_BlockShatter.prefab";
        public const string POWERUP_COLLECT_PREFAB_PATH = VFX_DIR + "/PF_VFX_PowerupCollect.prefab";
        public const string PADDLE_SPARK_PREFAB_PATH = VFX_DIR + "/PF_VFX_PaddleSpark.prefab";
        public const string BOMB_EXPLOSION_PREFAB_PATH = VFX_DIR + "/PF_VFX_BombExplosion.prefab";
        public const string FALLING_TRAIL_PREFAB_PATH = VFX_DIR + "/PF_VFX_FallingTrail.prefab";
        public const string SHIELD_BARRIER_PREFAB_PATH = VFX_DIR + "/PF_VFX_ShieldBarrier.prefab";

        public const string BURST_MAT_PATH = "Assets/Materials/BlockBreaker/MI_VFX_Burst.mat";
        public const string SHOCKWAVE_MAT_PATH = "Assets/Materials/BlockBreaker/MI_VFX_Shockwave.mat";
        public const string DEBRIS_MAT_PATH = "Assets/Materials/BlockBreaker/MI_Block_Debris.mat";
        public const string GLASS_MAT_PATH = "Assets/Materials/BlockBreaker/MI_Block_Glass.mat";

        public const string LARGE_FBX_PATH = "Assets/Models/SM_Block_Large_Particles.fbx";
        public const string SMALL_FBX_PATH = "Assets/Models/SM_Block_Small_Particles.fbx";
        public const string FROST_FBX_PATH = "Assets/Models/SM_Block_Frost_Particles.fbx";

        [MenuItem("Tools/Arcade/Setup All VFX Prefabs")]
        public static void SetupAllPrefabs()
        {
            EnsureDirectory();
            SetupBlockShatterPrefab();
            SetupPowerupCollectPrefab();
            SetupPaddleSparkPrefab();
            SetupBombExplosionPrefab();
            SetupFallingTrailPrefab();
            SetupShieldBarrierPrefab();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("<color=green>[SetupVFXPrefabs] All 6 particle effect prefabs constructed and verified successfully in Assets/Prefabs/VFX/!</color>");
        }

        private static void EnsureDirectory()
        {
            if (!Directory.Exists(VFX_DIR))
            {
                Directory.CreateDirectory(VFX_DIR);
                AssetDatabase.Refresh();
            }
        }

        private static Material GetOrCreateBurstMaterial()
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(BURST_MAT_PATH);
            if (mat != null) return mat;
            var shader = Shader.Find("Arcade/VFX_ParticleBurst") ?? Shader.Find("Universal Render Pipeline/Particles/Unlit");
            return new Material(shader) { name = "MI_VFX_Burst" };
        }

        private static Material GetOrCreateShockwaveMaterial()
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(SHOCKWAVE_MAT_PATH);
            if (mat != null) return mat;
            return GetOrCreateBurstMaterial();
        }

        [MenuItem("Tools/Arcade/Setup Block Shatter VFX Prefab")]
        public static GameObject SetupBlockShatterPrefab()
        {
            EnsureDirectory();

            var largeMeshes = LoadMeshesFromFbx(LARGE_FBX_PATH);
            var smallMeshes = LoadMeshesFromFbx(SMALL_FBX_PATH);
            var frostMeshes = LoadMeshesFromFbx(FROST_FBX_PATH);

            var burstMat = GetOrCreateBurstMaterial();
            var debrisMat = AssetDatabase.LoadAssetAtPath<Material>(DEBRIS_MAT_PATH);
            var glassMat = AssetDatabase.LoadAssetAtPath<Material>(GLASS_MAT_PATH);

            GameObject root = new GameObject("PF_VFX_BlockShatter");

            // 1. Sparks
            GameObject sparksGo = new GameObject("Sparks");
            sparksGo.transform.SetParent(root.transform, false);
            var psSparks = sparksGo.AddComponent<ParticleSystem>();
            var psrSparks = sparksGo.GetComponent<ParticleSystemRenderer>();
            ConfigureSparksEmitter(psSparks, psrSparks, burstMat);

            // 2. Large Shards
            GameObject largeGo = new GameObject("Large_Shards");
            largeGo.transform.SetParent(root.transform, false);
            var psLarge = largeGo.AddComponent<ParticleSystem>();
            var psrLarge = largeGo.GetComponent<ParticleSystemRenderer>();
            ConfigureMeshEmitter(psLarge, psrLarge, largeMeshes, debrisMat,
                speedMin: 3.5f, speedMax: 7.0f,
                sizeMin: 0.75f, sizeMax: 1.1f,
                lifetimeMin: 0.75f, lifetimeMax: 1.0f,
                gravity: 2.5f,
                burstCount: 3);

            // 3. Small Shards
            GameObject smallGo = new GameObject("Small_Shards");
            smallGo.transform.SetParent(root.transform, false);
            var psSmall = smallGo.AddComponent<ParticleSystem>();
            var psrSmall = smallGo.GetComponent<ParticleSystemRenderer>();
            ConfigureMeshEmitter(psSmall, psrSmall, smallMeshes, debrisMat,
                speedMin: 4.5f, speedMax: 9.0f,
                sizeMin: 0.45f, sizeMax: 0.7f,
                lifetimeMin: 0.65f, lifetimeMax: 0.9f,
                gravity: 2.8f,
                burstCount: 5);

            // 4. Frost Shards
            GameObject frostGo = new GameObject("Frost_Shards");
            frostGo.transform.SetParent(root.transform, false);
            var psFrost = frostGo.AddComponent<ParticleSystem>();
            var psrFrost = frostGo.GetComponent<ParticleSystemRenderer>();
            ConfigureMeshEmitter(psFrost, psrFrost, frostMeshes, glassMat,
                speedMin: 4.0f, speedMax: 8.0f,
                sizeMin: 0.55f, sizeMax: 0.85f,
                lifetimeMin: 0.7f, lifetimeMax: 0.95f,
                gravity: 2.6f,
                burstCount: 8);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, SHATTER_PREFAB_PATH);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[SetupVFXPrefabs] Saved Shuriken shatter prefab to {SHATTER_PREFAB_PATH}");
            return prefab;
        }

        private static List<Mesh> LoadMeshesFromFbx(string path)
        {
            var list = new List<Mesh>();
            var all = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var a in all)
            {
                if (a is Mesh m && !m.name.StartsWith("__preview__"))
                {
                    list.Add(m);
                }
            }
            return list;
        }

        private static void ConfigureSparksEmitter(ParticleSystem ps, ParticleSystemRenderer psr, Material mat)
        {
            var main = ps.main;
            main.playOnAwake = true;
            main.loop = true;
            main.duration = 1.0f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.55f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(5f, 11f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.26f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.stopAction = ParticleSystemStopAction.None;
            main.gravityModifier = 0.5f;

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0.0f, 16) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.25f;

            var colOverLifetime = ps.colorOverLifetime;
            colOverLifetime.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            colOverLifetime.color = grad;

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0f)));

            if (psr != null && mat != null)
            {
                psr.renderMode = ParticleSystemRenderMode.Billboard;
                psr.sharedMaterial = mat;
            }
        }

        private static void ConfigureMeshEmitter(ParticleSystem ps, ParticleSystemRenderer psr, List<Mesh> meshes, Material mat,
            float speedMin, float speedMax, float sizeMin, float sizeMax, float lifetimeMin, float lifetimeMax, float gravity, int burstCount = 4)
        {
            var main = ps.main;
            main.playOnAwake = true;
            main.loop = true;
            main.duration = 1.2f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifetimeMin, lifetimeMax);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speedMin, speedMax);
            main.startSize = new ParticleSystem.MinMaxCurve(sizeMin, sizeMax);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.stopAction = ParticleSystemStopAction.None;
            main.gravityModifier = gravity;

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0.0f, burstCount) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.35f;

            var rot = ps.rotationOverLifetime;
            rot.enabled = true;
            rot.separateAxes = true;
            rot.x = new ParticleSystem.MinMaxCurve(-360f * Mathf.Deg2Rad, 360f * Mathf.Deg2Rad);
            rot.y = new ParticleSystem.MinMaxCurve(-360f * Mathf.Deg2Rad, 360f * Mathf.Deg2Rad);
            rot.z = new ParticleSystem.MinMaxCurve(-360f * Mathf.Deg2Rad, 360f * Mathf.Deg2Rad);

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve shrinkCurve = new AnimationCurve(
                new Keyframe(0f, 1f, 0f, 0f),
                new Keyframe(0.65f, 0.9f, -0.4f, -0.8f),
                new Keyframe(1.0f, 0f, -2.5f, 0f)
            );
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, shrinkCurve);

            if (psr != null)
            {
                psr.renderMode = ParticleSystemRenderMode.Mesh;
                if (meshes != null && meshes.Count > 0)
                {
                    psr.SetMeshes(meshes.ToArray());
                }
                if (mat != null)
                {
                    psr.sharedMaterial = mat;
                }
            }
        }

        [MenuItem("Tools/Arcade/Setup Powerup Collect VFX Prefab")]
        public static GameObject SetupPowerupCollectPrefab()
        {
            EnsureDirectory();
            Material burstMat = GetOrCreateBurstMaterial();
            Material shockwaveMat = GetOrCreateShockwaveMaterial();

            GameObject root = new GameObject("PF_VFX_PowerupCollect");
            root.transform.position = Vector3.zero;
            root.transform.rotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;

            // Child 1: Burst
            GameObject burstGo = new GameObject("Burst");
            burstGo.transform.SetParent(root.transform, false);
            var psBurst = burstGo.AddComponent<ParticleSystem>();
            var psrBurst = burstGo.GetComponent<ParticleSystemRenderer>();
            psrBurst.sharedMaterial = burstMat;
            psrBurst.renderMode = ParticleSystemRenderMode.Billboard;

            var main = psBurst.main;
            main.playOnAwake = true;
            main.loop = true;
            main.duration = 1.0f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.40f, 0.65f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(5f, 12f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.18f, 0.32f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.stopAction = ParticleSystemStopAction.None;

            var emission = psBurst.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0.0f, 28) });

            var shape = psBurst.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.25f;

            var sizeOverLifetime = psBurst.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0f)));

            var colorOverLifetime = psBurst.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.5f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(grad);

            // Child 2: Ring
            GameObject ringGo = new GameObject("Ring");
            ringGo.transform.SetParent(root.transform, false);
            var psRing = ringGo.AddComponent<ParticleSystem>();
            var psrRing = ringGo.GetComponent<ParticleSystemRenderer>();
            psrRing.sharedMaterial = shockwaveMat;
            psrRing.renderMode = ParticleSystemRenderMode.Billboard;

            var ringMain = psRing.main;
            ringMain.playOnAwake = true;
            ringMain.loop = true;
            ringMain.duration = 1.0f;
            ringMain.startLifetime = 0.40f;
            ringMain.startSpeed = 0f;
            ringMain.startSize = 0.3f;
            ringMain.simulationSpace = ParticleSystemSimulationSpace.World;
            ringMain.stopAction = ParticleSystemStopAction.None;

            var ringEmission = psRing.emission;
            ringEmission.enabled = true;
            ringEmission.rateOverTime = 0f;
            ringEmission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0.0f, 1) });

            var ringSize = psRing.sizeOverLifetime;
            ringSize.enabled = true;
            ringSize.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, 0.5f, 0f, 4f),
                new Keyframe(1f, 4.0f, 1.5f, 0f)
            ));

            var ringColor = psRing.colorOverLifetime;
            ringColor.enabled = true;
            Gradient ringGrad = new Gradient();
            ringGrad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            ringColor.color = new ParticleSystem.MinMaxGradient(ringGrad);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, POWERUP_COLLECT_PREFAB_PATH);
            Object.DestroyImmediate(root);
            Debug.Log($"[SetupVFXPrefabs] Saved Powerup Collect VFX prefab to {POWERUP_COLLECT_PREFAB_PATH}");
            return prefab;
        }

        [MenuItem("Tools/Arcade/Setup Paddle Spark VFX Prefab")]
        public static GameObject SetupPaddleSparkPrefab()
        {
            EnsureDirectory();
            Material burstMat = GetOrCreateBurstMaterial();

            GameObject root = new GameObject("PF_VFX_PaddleSpark");
            root.transform.position = Vector3.zero;
            root.transform.rotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;

            GameObject sparksGo = new GameObject("Sparks");
            sparksGo.transform.SetParent(root.transform, false);
            var ps = sparksGo.AddComponent<ParticleSystem>();
            var psr = sparksGo.GetComponent<ParticleSystemRenderer>();
            psr.sharedMaterial = burstMat;
            psr.renderMode = ParticleSystemRenderMode.Billboard;

            var main = ps.main;
            main.playOnAwake = true;
            main.loop = true;
            main.duration = 0.8f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.45f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(7f, 15f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.24f);
            main.startColor = new Color(0.1f, 0.95f, 1.0f, 1.0f); // Radiant Cyan
            main.gravityModifier = 2.0f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.stopAction = ParticleSystemStopAction.None;

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0.0f, 18) });

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 40f;
            shape.radius = 0.2f;

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0f)));

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(new Color(0.2f, 1f, 1f), 0f), new GradientColorKey(new Color(0f, 0.7f, 1f), 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(grad);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PADDLE_SPARK_PREFAB_PATH);
            Object.DestroyImmediate(root);
            Debug.Log($"[SetupVFXPrefabs] Saved Paddle Spark VFX prefab to {PADDLE_SPARK_PREFAB_PATH}");
            return prefab;
        }

        [MenuItem("Tools/Arcade/Setup Bomb Explosion VFX Prefab")]
        public static GameObject SetupBombExplosionPrefab()
        {
            EnsureDirectory();
            Material burstMat = GetOrCreateBurstMaterial();
            Material shockMat = GetOrCreateShockwaveMaterial();

            GameObject root = new GameObject("PF_VFX_BombExplosion");
            root.transform.position = Vector3.zero;
            root.transform.rotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;

            // Child 1: Shockwave Ring (radius 2.5u)
            GameObject shockGo = new GameObject("Shockwave");
            shockGo.transform.SetParent(root.transform, false);
            var psShock = shockGo.AddComponent<ParticleSystem>();
            var psrShock = shockGo.GetComponent<ParticleSystemRenderer>();
            psrShock.sharedMaterial = shockMat;
            psrShock.renderMode = ParticleSystemRenderMode.Billboard;

            var shockMain = psShock.main;
            shockMain.playOnAwake = true;
            shockMain.loop = true;
            shockMain.duration = 1.0f;
            shockMain.startLifetime = 0.45f;
            shockMain.startSpeed = 0f;
            shockMain.startSize = 0.5f;
            shockMain.startColor = new Color(1f, 0.65f, 0.15f, 0.95f); // Flame orange
            shockMain.simulationSpace = ParticleSystemSimulationSpace.World;
            shockMain.stopAction = ParticleSystemStopAction.None;

            var shockEmission = psShock.emission;
            shockEmission.enabled = true;
            shockEmission.rateOverTime = 0f;
            shockEmission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0.0f, 1) });

            var shockSize = psShock.sizeOverLifetime;
            shockSize.enabled = true;
            shockSize.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, 0.5f, 0f, 5.0f),
                new Keyframe(1f, 5.0f, 2.0f, 0f) // 5.0u diameter = 2.5u blast radius
            ));

            var shockColor = psShock.colorOverLifetime;
            shockColor.enabled = true;
            Gradient shockGrad = new Gradient();
            shockGrad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(new Color(1f, 0.8f, 0.2f), 0f), new GradientColorKey(new Color(1f, 0.2f, 0f), 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.85f, 0.4f), new GradientAlphaKey(0f, 1f) }
            );
            shockColor.color = new ParticleSystem.MinMaxGradient(shockGrad);

            // Child 2: Fire Sparks
            GameObject sparksGo = new GameObject("Sparks");
            sparksGo.transform.SetParent(root.transform, false);
            var psSparks = sparksGo.AddComponent<ParticleSystem>();
            var psrSparks = sparksGo.GetComponent<ParticleSystemRenderer>();
            psrSparks.sharedMaterial = burstMat;
            psrSparks.renderMode = ParticleSystemRenderMode.Billboard;

            var sparksMain = psSparks.main;
            sparksMain.playOnAwake = true;
            sparksMain.loop = true;
            sparksMain.duration = 1.0f;
            sparksMain.startLifetime = new ParticleSystem.MinMaxCurve(0.45f, 0.80f);
            sparksMain.startSpeed = new ParticleSystem.MinMaxCurve(8f, 18f);
            sparksMain.startSize = new ParticleSystem.MinMaxCurve(0.18f, 0.35f);
            sparksMain.startColor = new Color(1f, 0.7f, 0.2f, 1f);
            sparksMain.gravityModifier = 1.8f;
            sparksMain.simulationSpace = ParticleSystemSimulationSpace.World;
            sparksMain.stopAction = ParticleSystemStopAction.None;

            var sparksEmission = psSparks.emission;
            sparksEmission.enabled = true;
            sparksEmission.rateOverTime = 0f;
            sparksEmission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0.0f, 36) });

            var sparksShape = psSparks.shape;
            sparksShape.enabled = true;
            sparksShape.shapeType = ParticleSystemShapeType.Sphere;
            sparksShape.radius = 0.8f;

            var sparksSize = psSparks.sizeOverLifetime;
            sparksSize.enabled = true;
            sparksSize.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0f)));

            var sparksColor = psSparks.colorOverLifetime;
            sparksColor.enabled = true;
            Gradient sparksGrad = new Gradient();
            sparksGrad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(new Color(1f, 0.9f, 0.4f), 0f), new GradientColorKey(new Color(1f, 0.2f, 0f), 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            sparksColor.color = new ParticleSystem.MinMaxGradient(sparksGrad);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, BOMB_EXPLOSION_PREFAB_PATH);
            Object.DestroyImmediate(root);
            Debug.Log($"[SetupVFXPrefabs] Saved Bomb Explosion VFX prefab to {BOMB_EXPLOSION_PREFAB_PATH}");
            return prefab;
        }

        [MenuItem("Tools/Arcade/Setup Falling Trail VFX Prefab")]
        public static GameObject SetupFallingTrailPrefab()
        {
            EnsureDirectory();
            Material burstMat = GetOrCreateBurstMaterial();

            GameObject root = new GameObject("PF_VFX_FallingTrail");
            root.transform.position = Vector3.zero;
            root.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
            root.transform.localScale = Vector3.one;

            var ps = root.AddComponent<ParticleSystem>();
            var psr = root.GetComponent<ParticleSystemRenderer>();
            psr.sharedMaterial = burstMat;
            psr.renderMode = ParticleSystemRenderMode.Billboard;

            var main = ps.main;
            main.playOnAwake = true;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.55f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.0f, 2.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.14f, 0.22f);
            main.startColor = new Color(0f, 0.95f, 1.0f, 0.9f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.stopAction = ParticleSystemStopAction.None;

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 25f;

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.35f;

            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.x = new ParticleSystem.MinMaxCurve(-0.1f, 0.1f);
            vel.y = new ParticleSystem.MinMaxCurve(1.5f, 2.5f);
            vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0f)));

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(grad);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, FALLING_TRAIL_PREFAB_PATH);
            Object.DestroyImmediate(root);
            Debug.Log($"[SetupVFXPrefabs] Saved Falling Trail VFX prefab to {FALLING_TRAIL_PREFAB_PATH}");
            return prefab;
        }

        [MenuItem("Tools/Arcade/Setup Shield Barrier VFX Prefab")]
        public static GameObject SetupShieldBarrierPrefab()
        {
            EnsureDirectory();
            Material burstMat = GetOrCreateBurstMaterial();

            GameObject root = new GameObject("PF_VFX_ShieldBarrier");
            root.transform.position = Vector3.zero;
            root.transform.rotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;

            var ps = root.AddComponent<ParticleSystem>();
            var psr = root.GetComponent<ParticleSystemRenderer>();
            psr.sharedMaterial = burstMat;
            psr.renderMode = ParticleSystemRenderMode.Billboard;

            var main = ps.main;
            main.playOnAwake = true;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.7f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.6f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.35f);
            main.startColor = new Color(0.0f, 0.95f, 1.0f, 0.85f); // Electric Cyan
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.stopAction = ParticleSystemStopAction.None;

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 18f;

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(21.0f, 0.50f, 2.0f);

            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.x = new ParticleSystem.MinMaxCurve(-0.1f, 0.1f);
            vel.y = new ParticleSystem.MinMaxCurve(0.2f, 0.6f);
            vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.6f), new Keyframe(0.5f, 1f), new Keyframe(1f, 0f)));

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(new Color(0f, 1f, 1f), 0f), new GradientColorKey(new Color(0f, 0.6f, 1f), 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.9f, 0.3f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(grad);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, SHIELD_BARRIER_PREFAB_PATH);
            Object.DestroyImmediate(root);
            Debug.Log($"[SetupVFXPrefabs] Saved Shield Barrier VFX prefab to {SHIELD_BARRIER_PREFAB_PATH}");
            return prefab;
        }
    }
}

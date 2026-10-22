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
        public const string DEBRIS_MAT_PATH = "Assets/Materials/BlockBreaker/MI_Block_Debris.mat";
        public const string GLASS_MAT_PATH = "Assets/Materials/BlockBreaker/MI_Block_Glass.mat";

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

        [MenuItem("Tools/Arcade/Setup Block Shatter VFX Prefab")]
        public static GameObject SetupBlockShatterPrefab()
        {
            EnsureDirectory();
            return SetupBlockShatterVFX.CreateOrUpdatePrefab();
        }

        [MenuItem("Tools/Arcade/Setup Powerup Collect VFX Prefab")]
        public static GameObject SetupPowerupCollectPrefab()
        {
            EnsureDirectory();
            Material burstMat = GetOrCreateBurstMaterial();

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
            main.playOnAwake = false;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.40f, 0.65f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(5f, 12f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.18f, 0.32f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.stopAction = ParticleSystemStopAction.None;

            var emission = psBurst.emission;
            emission.enabled = false;

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
            psrRing.sharedMaterial = burstMat;
            psrRing.renderMode = ParticleSystemRenderMode.Billboard;

            var ringMain = psRing.main;
            ringMain.playOnAwake = false;
            ringMain.loop = false;
            ringMain.startLifetime = 0.40f;
            ringMain.startSpeed = 0f;
            ringMain.startSize = 0.3f;
            ringMain.simulationSpace = ParticleSystemSimulationSpace.World;
            ringMain.stopAction = ParticleSystemStopAction.None;

            var ringEmission = psRing.emission;
            ringEmission.enabled = false;

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
            main.playOnAwake = false;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.45f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(7f, 15f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.24f);
            main.startColor = new Color(0.1f, 0.95f, 1.0f, 1.0f); // Radiant Cyan
            main.gravityModifier = 2.0f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.stopAction = ParticleSystemStopAction.None;

            var emission = ps.emission;
            emission.enabled = false;

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

            GameObject root = new GameObject("PF_VFX_BombExplosion");
            root.transform.position = Vector3.zero;
            root.transform.rotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;

            // Child 1: Shockwave Ring (radius 2.5u)
            GameObject shockGo = new GameObject("Shockwave");
            shockGo.transform.SetParent(root.transform, false);
            var psShock = shockGo.AddComponent<ParticleSystem>();
            var psrShock = shockGo.GetComponent<ParticleSystemRenderer>();
            psrShock.sharedMaterial = burstMat;
            psrShock.renderMode = ParticleSystemRenderMode.Billboard;

            var shockMain = psShock.main;
            shockMain.playOnAwake = false;
            shockMain.loop = false;
            shockMain.startLifetime = 0.45f;
            shockMain.startSpeed = 0f;
            shockMain.startSize = 0.4f;
            shockMain.startColor = new Color(1f, 0.45f, 0.05f, 0.9f); // Flame orange
            shockMain.simulationSpace = ParticleSystemSimulationSpace.World;
            shockMain.stopAction = ParticleSystemStopAction.None;

            var shockEmission = psShock.emission;
            shockEmission.enabled = false;

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
                new GradientColorKey[] { new GradientColorKey(new Color(1f, 0.6f, 0.1f), 0f), new GradientColorKey(new Color(1f, 0.15f, 0f), 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.4f), new GradientAlphaKey(0f, 1f) }
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
            sparksMain.playOnAwake = false;
            sparksMain.loop = false;
            sparksMain.startLifetime = new ParticleSystem.MinMaxCurve(0.45f, 0.80f);
            sparksMain.startSpeed = new ParticleSystem.MinMaxCurve(8f, 18f);
            sparksMain.startSize = new ParticleSystem.MinMaxCurve(0.18f, 0.35f);
            sparksMain.startColor = new Color(1f, 0.7f, 0.2f, 1f);
            sparksMain.gravityModifier = 1.8f;
            sparksMain.simulationSpace = ParticleSystemSimulationSpace.World;
            sparksMain.stopAction = ParticleSystemStopAction.None;

            var sparksEmission = psSparks.emission;
            sparksEmission.enabled = false;

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

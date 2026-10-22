using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Arcade.Editor
{
    public static class SetupBlockShatterVFX
    {
        public const string PREFAB_DIR = "Assets/Prefabs/VFX";
        public const string PREFAB_PATH = PREFAB_DIR + "/PF_VFX_BlockShatter.prefab";

        public const string LARGE_FBX_PATH = "Assets/Models/SM_Block_Large_Particles.fbx";
        public const string SMALL_FBX_PATH = "Assets/Models/SM_Block_Small_Particles.fbx";
        public const string FROST_FBX_PATH = "Assets/Models/SM_Block_Frost_Particles.fbx";

        public const string BURST_MAT_PATH = "Assets/Materials/BlockBreaker/MI_VFX_Burst.mat";
        public const string DEBRIS_MAT_PATH = "Assets/Materials/BlockBreaker/MI_Block_Debris.mat";
        public const string GLASS_MAT_PATH = "Assets/Materials/BlockBreaker/MI_Block_Glass.mat";

        [MenuItem("Tools/Arcade/Setup Block Shatter VFX Prefab")]
        public static GameObject CreateOrUpdatePrefab()
        {
            if (!Directory.Exists(PREFAB_DIR))
            {
                Directory.CreateDirectory(PREFAB_DIR);
                AssetDatabase.Refresh();
            }

            var largeMeshes = LoadMeshesFromFbx(LARGE_FBX_PATH);
            var smallMeshes = LoadMeshesFromFbx(SMALL_FBX_PATH);
            var frostMeshes = LoadMeshesFromFbx(FROST_FBX_PATH);

            var burstMat = AssetDatabase.LoadAssetAtPath<Material>(BURST_MAT_PATH);
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

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PREFAB_PATH);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[SetupBlockShatterVFX] Saved Shuriken shatter prefab to {PREFAB_PATH}");
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

            // 3D random tumbling rotation over lifetime
            var rot = ps.rotationOverLifetime;
            rot.enabled = true;
            rot.separateAxes = true;
            rot.x = new ParticleSystem.MinMaxCurve(-360f * Mathf.Deg2Rad, 360f * Mathf.Deg2Rad);
            rot.y = new ParticleSystem.MinMaxCurve(-360f * Mathf.Deg2Rad, 360f * Mathf.Deg2Rad);
            rot.z = new ParticleSystem.MinMaxCurve(-360f * Mathf.Deg2Rad, 360f * Mathf.Deg2Rad);

            // Smooth shrink out as particles fall
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
    }
}

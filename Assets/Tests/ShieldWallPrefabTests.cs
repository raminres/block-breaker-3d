using Arcade.BlockBreaker;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Arcade.Tests
{
    [TestFixture]
    public class ShieldWallPrefabTests
    {
        private const string PrefabPath = "Assets/Prefabs/Arena/PF_ShieldWall.prefab";

        [Test]
        public void ShieldWallPrefab_ExistsOnDisk()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Assert.IsNotNull(prefab, "PF_ShieldWall.prefab must exist at Assets/Prefabs/Arena/PF_ShieldWall.prefab");
        }

        [Test]
        public void ShieldWallPrefab_Root_HasPhysicsAndShieldWallComponent()
        {
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Assert.IsNotNull(root, "PF_ShieldWall.prefab must load.");

            var col = root.GetComponent<BoxCollider>();
            Assert.IsNotNull(col, "Root must have BoxCollider for physical deflection.");
            Assert.AreEqual(new Vector3(21f, 0.5f, 2f), col.size, "Root BoxCollider size must be (21, 0.5, 2).");
            Assert.AreEqual(Vector3.zero, col.center, "Root BoxCollider center must be (0,0,0).");
            Assert.IsNotNull(col.sharedMaterial, "Root BoxCollider must have bouncy PhysicMaterial assigned.");
            Assert.AreEqual("PM_ArcadeBounce", col.sharedMaterial.name);

            var wall = root.GetComponent<ShieldWall>();
            Assert.IsNotNull(wall, "Root must have ShieldWall component.");
            Assert.IsNull(root.GetComponent<MeshRenderer>(), "Root should not have MeshRenderer (visuals live in child 'model').");
        }

        [Test]
        public void ShieldWallPrefab_Children_HasModelAndVfxHierarchy()
        {
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Assert.IsNotNull(root, "PF_ShieldWall.prefab must load.");

            var wall = root.GetComponent<ShieldWall>();

            // 1. Child 'model' (Visual mesh, zero colliders)
            var model = root.transform.Find("model");
            Assert.IsNotNull(model, "PF_ShieldWall must have child 'model'.");
            Assert.IsNull(model.GetComponent<Collider>(), "Child 'model' must have zero colliders to prevent phantom catches.");
            var mr = model.GetComponent<MeshRenderer>();
            Assert.IsNotNull(mr, "Child 'model' must have MeshRenderer.");
            Assert.IsNotNull(mr.sharedMaterial, "Child 'model' must have material assigned.");
            Assert.AreEqual("MI_ShieldWall", mr.sharedMaterial.name);

            // 2. Child 'vfx' (Energy barrier particle effect)
            var vfx = root.transform.Find("vfx");
            Assert.IsNotNull(vfx, "PF_ShieldWall must have child 'vfx'.");
            var ps = vfx.GetComponent<ParticleSystem>();
            Assert.IsNotNull(ps, "Child 'vfx' must have ParticleSystem.");

            // 3. Serialized socket wiring
            Assert.AreEqual(model.gameObject, wall.ModelChild, "wall.ModelChild must reference child 'model'.");
            Assert.AreEqual(vfx.gameObject, wall.VfxChild, "wall.VfxChild must reference child 'vfx'.");
            Assert.AreEqual(ps, wall.ShieldVfx, "wall.ShieldVfx must reference child 'vfx' ParticleSystem.");
        }

        [Test]
        public void ShieldWallPrefab_ActivationAndDeactivation_ControlsVfxAndScale()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Assert.IsNotNull(prefab, "PF_ShieldWall.prefab must load.");

            var instance = Object.Instantiate(prefab);
            instance.name = "TestShieldWallInstance";
            var wall = instance.GetComponent<ShieldWall>();

            // Activate for 10 seconds
            wall.Activate(10f);

            Assert.IsTrue(instance.activeSelf, "Instance must be active upon Activate().");
            Assert.IsTrue(wall.VfxChild.activeSelf, "VfxChild must be active upon Activate().");
            Assert.AreEqual(-7.6f, instance.transform.position.y, 0.05f, "Shield wall must be positioned at wallY = -7.6.");

            // Deactivate immediately
            wall.Deactivate(immediate: true);

            Assert.IsFalse(instance.activeSelf, "Instance must be inactive after immediate Deactivate().");
            Assert.IsFalse(wall.VfxChild.activeSelf, "VfxChild must be inactive after immediate Deactivate().");
            Assert.AreEqual(Vector3.zero, instance.transform.localScale, "Scale must collapse to zero.");

            Object.DestroyImmediate(instance);
        }

        [Test]
        public void ShieldWallPrefab_PulseOnHit_EmitsParticlesWithoutError()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Assert.IsNotNull(prefab, "PF_ShieldWall.prefab must load.");

            var instance = Object.Instantiate(prefab);
            var wall = instance.GetComponent<ShieldWall>();
            wall.Activate(10f);

            Assert.DoesNotThrow(() => wall.PulseOnHit(), "PulseOnHit must execute cleanly without error.");

            Object.DestroyImmediate(instance);
        }

        [Test]
        public void ShieldWallPrefab_ParticleVelocityCurves_AllInSameMode()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Assert.IsNotNull(prefab, "PF_ShieldWall.prefab must load.");
            var wall = prefab.GetComponent<ShieldWall>();
            var ps = wall.ShieldVfx;
            Assert.IsNotNull(ps);

            var vel = ps.velocityOverLifetime;
            Assert.IsTrue(vel.enabled);
            Assert.AreEqual(vel.y.mode, vel.x.mode, "vel.x and vel.y must be in the same ParticleSystemCurveMode.");
            Assert.AreEqual(vel.y.mode, vel.z.mode, "vel.z and vel.y must be in the same ParticleSystemCurveMode.");
        }

        [Test]
        public void ShieldWallPrefab_EnsureShieldWall_InstantiatesFromPrefab()
        {
            var gmGo = new GameObject("TestGameManager");
            var gm = gmGo.AddComponent<Arcade.Core.ArcadeGameManager>();

            try
            {
                var wall = gm.EnsureShieldWall();
                Assert.IsNotNull(wall, "EnsureShieldWall must return a non-null ShieldWall.");
                Assert.IsNotNull(wall.ModelChild, "Instantiated ShieldWall must have ModelChild.");
                Assert.IsNotNull(wall.VfxChild, "Instantiated ShieldWall must have VfxChild.");
                Assert.AreEqual("ShieldWall", wall.gameObject.name);
                Assert.IsFalse(wall.gameObject.activeSelf, "Instantiated ShieldWall must start inactive until activated.");
            }
            finally
            {
                if (gm.ActiveShieldWall != null)
                {
                    Object.DestroyImmediate(gm.ActiveShieldWall.gameObject);
                }
                Object.DestroyImmediate(gmGo);
            }
        }
    }
}
